using UnityEngine;
using Photon.Pun;
using System.Collections;

namespace Photon.Pun.Demo.Asteroids
{
    [RequireComponent(typeof(PhotonView))]
    public class Tower : MonoBehaviourPunCallbacks, IPunObservable, UnitBase
    {
        [Header("Información Básica")]
        public string towerName = "Torre";
        public int level = 1;
        public int teamId = 0; // 0 = Rojo, 1 = Azul
        
        [Header("Estadísticas")]
        public float maxHealth = 2000f;
        public float currentHealth;
        public float attackDamage = 150f;
        public float attackSpeed = 1f;
        public float attackRange = 10f;
        public float armor = 10f;
        public float magicResistance = 10f;
        
        [Header("Recompensas")]
        public float experienceReward = 150f;
        public float goldReward = 150f;
        public float teamGoldReward = 100f; // Oro para todo el equipo
        
        [Header("Efectos")]
        public GameObject deathEffectPrefab;
        public AudioClip deathSound;
        public float deathSoundVolume = 1f;
        
        // Estado de la torre
        private bool _isDead = false;
        private Transform currentTarget;
        private float attackCooldown = 0f;
        private AudioSource audioSource;
        
        // Implementación de propiedades de la interfaz UnitBase
        string UnitBase.name { get => towerName; }
        int UnitBase.level { get => level; }
        bool UnitBase.IsDead { get => _isDead; }
        
        void Awake()
        {
            // Inicializar componentes
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.spatialBlend = 1f;
                    audioSource.minDistance = 10f;
                    audioSource.maxDistance = 50f;
                }
            }
            
            // Inicializar vida
            currentHealth = maxHealth;
        }
        
        void Start()
        {
            // Configurar capa según equipo
            string layerName = (teamId == 0) ? "RedTeam" : "BlueTeam";
            gameObject.layer = LayerMask.NameToLayer(layerName);
            
            // Configurar tag
            gameObject.tag = (teamId == 0) ? "RedTeam" : "BlueTeam";
            
            // Sincronizar equipo si somos el dueño
            if (photonView.IsMine)
            {
                photonView.RPC("RPC_SyncTeam", RpcTarget.OthersBuffered, teamId);
            }
        }
        
        void Update()
        {
            if (_isDead || !photonView.IsMine) return;
            
            // Reducir cooldown de ataque
            if (attackCooldown > 0)
            {
                attackCooldown -= Time.deltaTime;
            }
            
            // Buscar y atacar objetivos
            if (currentTarget == null || !IsTargetValid(currentTarget))
            {
                FindTarget();
            }
            else if (attackCooldown <= 0)
            {
                Attack();
            }
        }
        
        private void FindTarget()
        {
            // Buscar heroes enemigos en rango
            Collider[] colliders = Physics.OverlapSphere(transform.position, attackRange);
            float closestDistance = attackRange;
            
            foreach (Collider col in colliders)
            {
                HeroBase hero = col.GetComponent<HeroBase>();
                if (hero != null && !hero.IsDead && hero.teamId != this.teamId)
                {
                    float distance = Vector3.Distance(transform.position, hero.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        currentTarget = hero.transform;
                    }
                }
            }
            
            // Si encontramos objetivo, sincronizar
            if (currentTarget != null && photonView.IsMine)
            {
                PhotonView targetView = currentTarget.GetComponent<PhotonView>();
                if (targetView != null)
                {
                    photonView.RPC("RPC_SetTarget", RpcTarget.Others, targetView.ViewID);
                }
            }
        }
        
        private bool IsTargetValid(Transform target)
        {
            if (target == null) return false;
            
            HeroBase hero = target.GetComponent<HeroBase>();
            if (hero == null || hero.IsDead || hero.teamId == this.teamId) return false;
            
            return Vector3.Distance(transform.position, target.position) <= attackRange;
        }
        
        private void Attack()
        {
            if (currentTarget == null) return;
            
            // Obtener el héroe objetivo
            HeroBase targetHero = currentTarget.GetComponent<HeroBase>();
            if (targetHero != null && !targetHero.IsDead)
            {
                // Aplicar daño
                targetHero.TakeDamage(attackDamage, photonView.ViewID);
                
                // Resetear cooldown
                attackCooldown = 1f / attackSpeed;
            }
        }
        
        public void TakeDamage(float damage, HeroBase attacker)
        {
            if (_isDead || !photonView.IsMine) return;
            
            // Calcular daño real con armadura
            float actualDamage = damage * (100f / (100f + armor));
            currentHealth -= actualDamage;
            
            // Sincronizar salud
            photonView.RPC("RPC_SyncHealth", RpcTarget.All, currentHealth);
            
            // Si la torre muere
            if (currentHealth <= 0 && !_isDead)
            {
                Die(attacker);
            }
        }
        
        private void Die(HeroBase killer)
        {
            if (_isDead) return;
            
            _isDead = true;
            currentHealth = 0;
            
            // Desactivar collider
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            
            // Efectos visuales
            if (deathEffectPrefab != null)
            {
                Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            }
            
            // Sonido
            if (deathSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(deathSound, deathSoundVolume);
            }
            
            // Sincronizar muerte
            int killerViewID = (killer != null && killer.photonView != null) ? killer.photonView.ViewID : -1;
            photonView.RPC("RPC_SyncDeath", RpcTarget.All, killerViewID);
            
            // Otorgar recompensas
            if (killer != null)
            {
                // Experiencia y oro al asesino
                killer.AwardTowerKillExperience();
                killer.AddGold(goldReward, $"¡{towerName} destruida!", transform.position);
                
                // Oro al equipo del asesino (pendiente de implementar)
                // TODO: Otorgar teamGoldReward a todos los miembros del equipo del asesino
            }
        }
        
        [PunRPC]
        private void RPC_SyncTeam(int newTeamId)
        {
            teamId = newTeamId;
            
            // Actualizar capa y tag
            string layerName = (teamId == 0) ? "RedTeam" : "BlueTeam";
            gameObject.layer = LayerMask.NameToLayer(layerName);
            gameObject.tag = (teamId == 0) ? "RedTeam" : "BlueTeam";
        }
        
        [PunRPC]
        private void RPC_SyncHealth(float newHealth)
        {
            currentHealth = newHealth;
            
            // Verificar si debe morir por sincronización
            if (currentHealth <= 0 && !_isDead && !photonView.IsMine)
            {
                _isDead = true;
            }
        }
        
        [PunRPC]
        private void RPC_SetTarget(int targetViewID)
        {
            if (targetViewID < 0)
            {
                currentTarget = null;
                return;
            }
            
            PhotonView targetView = PhotonView.Find(targetViewID);
            if (targetView != null)
            {
                currentTarget = targetView.transform;
            }
        }
        
        [PunRPC]
        private void RPC_SyncDeath(int killerViewID)
        {
            _isDead = true;
            currentHealth = 0;
            
            // Desactivar collider
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            
            // Efectos visuales si no los hemos mostrado ya
            if (deathEffectPrefab != null && !photonView.IsMine)
            {
                Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            }
            
            // Sonido si no lo hemos reproducido ya
            if (deathSound != null && audioSource != null && !photonView.IsMine)
            {
                audioSource.PlayOneShot(deathSound, deathSoundVolume);
            }
        }
        
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // Datos que enviamos
                stream.SendNext(_isDead);
                stream.SendNext(currentHealth);
                stream.SendNext(teamId);
                
                // ID del objetivo si existe
                int targetID = -1;
                if (currentTarget != null)
                {
                    PhotonView targetView = currentTarget.GetComponent<PhotonView>();
                    if (targetView != null)
                    {
                        targetID = targetView.ViewID;
                    }
                }
                stream.SendNext(targetID);
            }
            else
            {
                // Datos que recibimos
                _isDead = (bool)stream.ReceiveNext();
                currentHealth = (float)stream.ReceiveNext();
                teamId = (int)stream.ReceiveNext();
                
                // Actualizar objetivo
                int targetID = (int)stream.ReceiveNext();
                if (targetID >= 0)
                {
                    PhotonView targetView = PhotonView.Find(targetID);
                    if (targetView != null)
                    {
                        currentTarget = targetView.transform;
                    }
                }
                else
                {
                    currentTarget = null;
                }
            }
        }
    }
} 