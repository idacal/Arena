using UnityEngine;
using Photon.Pun;
using System.Collections;

namespace Photon.Pun.Demo.Asteroids
{
    [RequireComponent(typeof(PhotonView))]
    public class NeutralCreep : MonoBehaviourPunCallbacks, IDamageable
    {
        [Header("Información Básica")]
        public string creepName = "Creep";
        public string creepType = "Neutral"; // Tipo de creep (ej: "Fungus", "Bat", etc.)
        public int level = 1;
        
        [Header("Estadísticas")]
        public float maxHealth = 100f;
        public float currentHealth;
        public float attackDamage = 10f;
        public float attackSpeed = 1f;
        public float moveSpeed = 3f;
        public float armor = 0f;
        public float magicResistance = 0f;
        
        [Header("Recompensas")]
        public float experienceReward = 25f;
        public float goldReward = 10f;
        public float healthRestore = 0f;    // Vida restaurada al matar
        public float manaRestore = 0f;      // Maná restaurado al matar
        
        [Header("Comportamiento")]
        public float aggroRange = 5f;       // Rango para detectar enemigos
        public float attackRange = 1.5f;    // Rango de ataque
        public float respawnTime = 60f;     // Tiempo para respawnear
        public float patrolRadius = 5f;    // Radio de patrulla
        public float minPatrolTime = 3f;   // Tiempo mínimo de patrulla
        public float maxPatrolTime = 8f;   // Tiempo máximo de patrulla
        
        [Header("Efectos")]
        public GameObject deathEffectPrefab;
        public AudioClip deathSound;
        public float deathSoundVolume = 1f;
        private AudioSource audioSource;
        
        [Header("Animaciones")]
        public Animator animator;
        
        [Header("UI")]
        public GameObject healthBarPrefab;
        private HealthBar healthBar;
        
        // Eventos
        public delegate void CreepDeathDelegate(NeutralCreep creep);
        public event CreepDeathDelegate OnDeath;
        
        // Variables privadas
        private bool isDead = false;
        private float attackCooldown = 0f;
        private Transform currentTarget;
        private Vector3 spawnPosition;
        private float timeSinceDeath = 0f;
        
        // Variables de patrulla
        private Vector3 patrolTarget;
        private float patrolTimer;
        private bool isPatrolling = true;
        
        // Nombres de los parámetros de animación
        private const string ANIM_IS_WALKING = "IsWalking";
        private const string ANIM_IS_ATTACKING = "IsAttacking";
        private const string ANIM_DIE = "Die";
        private const string ANIM_RESPAWN = "Respawn";
        
        void Awake()
        {
            // No necesitamos asignar photonView ya que es una propiedad de solo lectura
            // y se inicializa automáticamente por MonoBehaviourPunCallbacks
            
            // Configurar Rigidbody
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;
            
            // Configurar Collider
            CapsuleCollider capsuleCollider = GetComponent<CapsuleCollider>();
            if (capsuleCollider == null)
            {
                capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
            }
            capsuleCollider.isTrigger = false;
            capsuleCollider.height = 2f; // Ajusta según el tamaño de tu modelo
            capsuleCollider.radius = 0.5f; // Ajusta según el ancho de tu modelo
            capsuleCollider.center = new Vector3(0, 1f, 0); // Ajusta según el centro de tu modelo
            
            // Configurar Layer
            gameObject.layer = LayerMask.NameToLayer("NeutralCreep");
            
            // Obtener el Animator
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
            
            // Configurar AudioSource
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f; // Sonido 3D
                audioSource.minDistance = 5f;
                audioSource.maxDistance = 20f;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
            }
        }
        
        void Start()
        {
            currentHealth = maxHealth;
            spawnPosition = transform.position;
            SetNewPatrolTarget();
            
            // Crear la barra de vida
            if (healthBarPrefab != null)
            {
                GameObject healthBarObj = Instantiate(healthBarPrefab);
                healthBar = healthBarObj.GetComponent<HealthBar>();
                if (healthBar != null)
                {
                    healthBar.Initialize(transform, maxHealth, currentHealth);
                }
            }
        }
        
        void Update()
        {
            if (isDead)
            {
                HandleDeath();
                return;
            }
            
            // Actualizar cooldown de ataque
            if (attackCooldown > 0)
            {
                attackCooldown -= Time.deltaTime;
            }
            
            // Buscar y atacar objetivos
            if (currentTarget == null || !IsTargetValid(currentTarget))
            {
                FindTarget();
            }
            
            if (currentTarget != null)
            {
                // Comportamiento de combate
                HandleCombat();
            }
            else if (isPatrolling)
            {
                // Comportamiento de patrulla
                HandlePatrol();
            }
        }
        
        private void HandleCombat()
        {
            isPatrolling = false;
            Vector3 direction = (currentTarget.position - transform.position).normalized;
            
            // Mover hacia el objetivo
            transform.position += direction * moveSpeed * Time.deltaTime;
            
            // Rotar hacia el objetivo
            transform.rotation = Quaternion.LookRotation(direction);
            
            // Atacar si está en rango
            if (Vector3.Distance(transform.position, currentTarget.position) <= attackRange)
            {
                Attack();
            }
            else
            {
                // Caminar hacia el objetivo
                SetAnimationState(true, false);
            }
        }
        
        private void HandlePatrol()
        {
            // Actualizar timer de patrulla
            patrolTimer -= Time.deltaTime;
            
            // Si llegamos al objetivo o se acabó el tiempo, elegir nuevo objetivo
            if (Vector3.Distance(transform.position, patrolTarget) < 0.1f || patrolTimer <= 0)
            {
                SetNewPatrolTarget();
            }
            
            // Mover hacia el objetivo de patrulla
            Vector3 direction = (patrolTarget - transform.position).normalized;
            transform.position += direction * (moveSpeed * 0.5f) * Time.deltaTime;
            
            // Rotar hacia la dirección del movimiento
            transform.rotation = Quaternion.LookRotation(direction);
            
            // Activar animación de caminata
            SetAnimationState(true, false);
        }
        
        private void SetNewPatrolTarget()
        {
            // Generar punto aleatorio dentro del radio de patrulla
            Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
            patrolTarget = spawnPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
            
            // Establecer tiempo aleatorio de patrulla
            patrolTimer = Random.Range(minPatrolTime, maxPatrolTime);
        }
        
        private void FindTarget()
        {
            // Buscar jugadores en el rango de aggro
            Collider[] colliders = Physics.OverlapSphere(transform.position, aggroRange);
            float closestDistance = aggroRange;
            
            foreach (Collider col in colliders)
            {
                HeroBase hero = col.GetComponent<HeroBase>();
                if (hero != null && !hero.IsDead)
                {
                    float distance = Vector3.Distance(transform.position, hero.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        currentTarget = hero.transform;
                    }
                }
            }
        }
        
        private bool IsTargetValid(Transform target)
        {
            if (target == null) return false;
            
            HeroBase hero = target.GetComponent<HeroBase>();
            if (hero == null || hero.IsDead) return false;
            
            return Vector3.Distance(transform.position, target.position) <= aggroRange;
        }
        
        private void Attack()
        {
            if (attackCooldown <= 0)
            {
                HeroBase targetHero = currentTarget.GetComponent<HeroBase>();
                if (targetHero != null)
                {
                    targetHero.TakeDamage(attackDamage, null); // Pasamos null como atacante
                    attackCooldown = 1f / attackSpeed;
                    
                    // Activar animación de ataque
                    SetAnimationState(false, true);
                }
            }
        }
        
        private void SetAnimationState(bool isWalking, bool isAttacking)
        {
            if (animator != null)
            {
                animator.SetBool(ANIM_IS_WALKING, isWalking);
                animator.SetBool(ANIM_IS_ATTACKING, isAttacking);
            }
        }
        
        public void TakeDamage(float damage, object attacker)
        {
            if (isDead) return;
            
            Debug.Log($"[NeutralCreep] {creepName} recibe {damage} de daño. Atacante: {(attacker != null ? attacker.ToString() : "null")}");
            
            // Calcular daño real considerando la armadura
            float actualDamage = damage * (100 / (100 + armor));
            currentHealth -= actualDamage;
            
            // Actualizar la barra de vida
            if (healthBar != null)
            {
                healthBar.UpdateHealth(currentHealth);
            }
            
            // Si el daño viene de un héroe, establecerlo como objetivo
            if (attacker is HeroBase hero)
            {
                Debug.Log($"[NeutralCreep] El daño viene del héroe {hero.heroName}");
                currentTarget = hero.transform;
                isPatrolling = false; // Desactivar patrulla cuando es atacado
            }
            else
            {
                Debug.LogWarning($"[NeutralCreep] El atacante no es un héroe: {attacker?.GetType().Name ?? "null"}");
            }
            
            // Verificar muerte
            if (currentHealth <= 0)
            {
                Die(attacker as HeroBase);
            }
        }
        
        [PunRPC]
        private void RPC_PlayDeathSound()
        {
            Debug.Log($"[NeutralCreep] RPC_PlayDeathSound llamado para {creepName}");
            if (deathSound != null)
            {
                // Usar el AudioSource existente en vez de crear uno nuevo
                if (audioSource != null)
                {
                    audioSource.clip = deathSound;
                    audioSource.spatialBlend = 1f; // Sonido 3D
                    audioSource.volume = deathSoundVolume;
                    audioSource.priority = 0; // Alta prioridad
                    audioSource.pitch = 1f;
                    audioSource.dopplerLevel = 1f;
                    audioSource.spread = 0f;
                    audioSource.rolloffMode = AudioRolloffMode.Linear;
                    audioSource.minDistance = 1f;
                    audioSource.maxDistance = 50f;
                    audioSource.PlayOneShot(deathSound, deathSoundVolume);
                    
                    Debug.Log($"[NeutralCreep] Reproduciendo sonido de muerte usando AudioSource existente: {deathSound.name}, volumen: {deathSoundVolume}");
                }
                else
                {
                    // Fallback: crear un AudioSource temporal si por alguna razón no existe el principal
                    GameObject audioObj = new GameObject("DeathSound");
                    audioObj.transform.position = transform.position;
                    AudioSource tempAudio = audioObj.AddComponent<AudioSource>();
                    tempAudio.clip = deathSound;
                    tempAudio.spatialBlend = 1f;
                    tempAudio.volume = deathSoundVolume;
                    tempAudio.priority = 0;
                    tempAudio.pitch = 1f;
                    tempAudio.dopplerLevel = 1f;
                    tempAudio.spread = 0f;
                    tempAudio.rolloffMode = AudioRolloffMode.Linear;
                    tempAudio.minDistance = 1f;
                    tempAudio.maxDistance = 50f;
                    tempAudio.PlayOneShot(deathSound, deathSoundVolume);
                    
                    Destroy(audioObj, deathSound.length + 0.1f);
                    Debug.Log($"[NeutralCreep] Reproduciendo sonido de muerte usando AudioSource temporal: {deathSound.name}, volumen: {deathSoundVolume}");
                }
            }
            else
            {
                Debug.LogWarning("[NeutralCreep] No hay sonido de muerte asignado");
            }
        }
        
        private void Die(HeroBase killer)
        {
            if (isDead) return;
            
            isDead = true;
            currentHealth = 0;
            
            Debug.Log($"[NeutralCreep] {creepName} está muriendo. Killer: {(killer != null ? killer.heroName : "null")}");
            
            // Reproducir efectos visuales
            if (deathEffectPrefab != null)
            {
                Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            }
            
            // Reproducir sonido de muerte a través de RPC
            photonView.RPC("RPC_PlayDeathSound", RpcTarget.All);
            
            // Activar animación de muerte
            if (animator != null)
            {
                animator.SetTrigger(ANIM_DIE);
            }
            
            // Otorgar recompensas al asesino
            if (killer != null)
            {
                Debug.Log($"[NeutralCreep] Otorgando {experienceReward} XP al héroe {killer.heroName}");
                
                // Asegurarnos de que el killer es el dueño del objeto
                if (killer.photonView.IsMine)
                {
                    Debug.Log($"[NeutralCreep] El héroe {killer.heroName} es el dueño, otorgando XP");
                    killer.AwardCreepKillExperience(this);
                }
                else
                {
                    Debug.Log($"[NeutralCreep] El héroe {killer.heroName} no es el dueño, no se otorga XP");
                }
                
                // Restaurar vida/maná si corresponde
                if (healthRestore > 0)
                {
                    killer.Heal(healthRestore);
                }
                if (manaRestore > 0)
                {
                    killer.UseMana(-Mathf.RoundToInt(manaRestore));
                }
            }
            else
            {
                Debug.LogError("[NeutralCreep] No hay killer asignado, no se otorga XP");
            }
            
            // Desactivar el collider y el renderer
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = false;
            
            // Destruir la barra de vida
            if (healthBar != null)
            {
                Destroy(healthBar.gameObject);
            }
            
            // Notificar el evento de muerte
            OnDeath?.Invoke(this);
            
            // Esperar a que termine la animación antes de destruir
            StartCoroutine(DestroyAfterAnimation());
        }
        
        private System.Collections.IEnumerator DestroyAfterAnimation()
        {
            // Esperar el tiempo de la animación
            yield return new WaitForSeconds(1.5f);
            
            // Destruir el objeto
            if (photonView.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void HandleDeath()
        {
            // Ya no necesitamos este método porque destruimos el objeto inmediatamente
            // El respawn se maneja en CreepSpawnPoint
        }
        
        private void Respawn()
        {
            isDead = false;
            currentHealth = maxHealth;
            timeSinceDeath = 0f;
            
            // Reactivar el collider y el renderer
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = true;
            
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = true;
            
            // Volver a la posición de spawn
            transform.position = spawnPosition;
            currentTarget = null;
            
            // Activar animación de respawn
            if (animator != null)
            {
                animator.SetTrigger(ANIM_RESPAWN);
            }
            
            // Reiniciar patrulla
            SetNewPatrolTarget();
        }
    }
} 