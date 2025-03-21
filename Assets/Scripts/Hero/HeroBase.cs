using UnityEngine;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;

namespace Photon.Pun.Demo.Asteroids
{
    [RequireComponent(typeof(PhotonView))]
    public class HeroBase : MonoBehaviourPunCallbacks, IPunObservable
    {
        [Header("Hero Identity")]
        public int heroId = -1;          // ID del héroe, debe coincidir con HeroData
        public string heroName = "";      // Nombre del héroe
        
        [Header("Team Settings")]
        public int teamId = 0;            // 0 = Rojo, 1 = Azul
        public Material teamRedMaterial;  // Material para equipo rojo
        public Material teamBlueMaterial; // Material para equipo azul
        public Renderer[] teamColorRenderers; // Renderers que cambiarán con el color del equipo
        
        [Header("Stats")]
        public float maxHealth = 1000f;
        public float currentHealth;
        public float maxMana = 500f;
        public float currentMana;
        public float attackDamage = 60f;
        public float attackSpeed = 1.0f;
        public float moveSpeed = 5.0f;
        public float armor = 20f;
        public float magicResistance = 20f;
        
        [Header("Ability System")]
        public HeroAbilityController abilityController;
        
        [Header("References")]
        public HeroUIController uiController;
        public Animator animator;
        
        // Referencias privadas
        protected Rigidbody heroRigidbody;
        protected Collider heroCollider;
        protected bool _isDead = false;   // Cambiado a privado con un getter público
        
        // Eventos
        public delegate void HeroEvent(HeroBase hero);
        public event HeroEvent OnHeroDeath;
        public event HeroEvent OnHeroRespawn;
        
        // Propiedad pública para acceder al estado de muerte
        public bool IsDead => _isDead;

        #region UNITY CALLBACKS
        
        protected virtual void Awake()
        {
            // Obtener componentes
            heroRigidbody = GetComponent<Rigidbody>();
            heroCollider = GetComponent<Collider>();
            
            // Inicializar controlador de habilidades si existe
            if (abilityController == null)
            {
                abilityController = GetComponent<HeroAbilityController>();
            }
            
            // Inicializar controlador de UI si existe
            if (uiController == null)
            {
                uiController = GetComponentInChildren<HeroUIController>();
            }
            
            // Inicializar stats
            currentHealth = maxHealth;
            currentMana = maxMana;
        }
        
        // Añade esto en la sección de [Header("References")] de HeroBase.cs
public GameObject uiCanvasPrefab;  // Prefab del canvas UI para asignar automáticamente

// Reemplaza/modifica el método Start() en HeroBase.cs
protected virtual void Start()
{
    // Configurar héroe según los datos del jugador
    if (photonView.IsMine)
    {
        // Cargar datos del héroe seleccionado
        LoadHeroData();
        
        // Instanciar el canvas UI si tenemos el prefab y somos el jugador local
        if (uiCanvasPrefab != null)
        {
            GameObject canvasInstance = Instantiate(uiCanvasPrefab, transform);
            
            // Buscar el componente HeroUIController en el canvas instanciado
            HeroUIController canvasUIController = canvasInstance.GetComponent<HeroUIController>();
            if (canvasUIController != null)
            {
                // Asignar referencias
                uiController = canvasUIController;
            }
        }
    }
    
    // Aplicar color del equipo
    ApplyTeamColor();
    
    // Actualizar UI si existe el controlador
    if (uiController != null)
    {
        uiController.UpdateHealthBar(currentHealth, maxHealth);
        uiController.UpdateManaBar(currentMana, maxMana);
        uiController.SetPlayerName(photonView.Owner.NickName);
    }
}
        
        protected virtual void Update()
        {
            if (!photonView.IsMine || _isDead)
                return;
            
            // La lógica de control se implementará en clases derivadas
            HandleInput();
            
            // Regeneración de maná
            RegenerateMana();
        }
        
        #endregion
        
        #region GAMEPLAY METHODS
        
        /// <summary>
        /// Maneja la entrada del jugador, debe implementarse en clases derivadas
        /// </summary>
        protected virtual void HandleInput()
        {
            // Esta lógica debe ser implementada en las clases derivadas
        }
        
        /// <summary>
        /// Aplica el color del equipo a los renderers configurados
        /// </summary>
        protected virtual void ApplyTeamColor()
        {
            if (teamColorRenderers == null || teamColorRenderers.Length == 0)
                return;
                
            Material teamMaterial = (teamId == 0) ? teamRedMaterial : teamBlueMaterial;
            
            if (teamMaterial != null)
            {
                foreach (Renderer renderer in teamColorRenderers)
                {
                    if (renderer != null)
                    {
                        renderer.material = teamMaterial;
                    }
                }
            }
        }
        
        /// <summary>
        /// Carga los datos del héroe desde el HeroManager
        /// </summary>
        protected virtual void LoadHeroData()
        {
            // Obtener el ID del héroe seleccionado por el jugador
            Player player = photonView.Owner;
            int selectedHeroId = HeroManager.Instance.GetPlayerSelectedHeroId(player);
            
            if (selectedHeroId != -1)
            {
                heroId = selectedHeroId;
                
                // Obtener el equipo del jugador
                teamId = HeroManager.Instance.GetPlayerTeam(player);
                
                // Cargar datos del héroe
                HeroData heroData = HeroManager.Instance.GetHeroData(heroId);
                if (heroData != null)
                {
                    heroName = heroData.Name;
                    maxHealth = heroData.Health;
                    currentHealth = maxHealth;
                    maxMana = heroData.Mana;
                    currentMana = maxMana;
                    attackDamage = heroData.AttackDamage;
                    attackSpeed = heroData.AttackSpeed;
                    moveSpeed = heroData.MovementSpeed * 0.01f; // Convertir a unidades de Unity
                    armor = heroData.Armor;
                    magicResistance = heroData.MagicResistance;
                    
                    // Configurar habilidades
                    if (abilityController != null)
                    {
                        abilityController.SetupAbilities(heroData.Abilities);
                    }
                }
            }
        }
        
        /// <summary>
        /// Regenera maná con el tiempo
        /// </summary>
        protected virtual void RegenerateMana()
        {
            // Regeneración básica de maná
            float manaRegenRate = maxMana * 0.01f; // 1% por segundo
            currentMana = Mathf.Min(currentMana + manaRegenRate * Time.deltaTime, maxMana);
            
            // Actualizar UI si es necesario
            if (uiController != null)
            {
                uiController.UpdateManaBar(currentMana, maxMana);
            }
        }
        
        /// <summary>
        /// Aplica daño al héroe
        /// </summary>
        public virtual void TakeDamage(float amount, HeroBase attacker, bool isMagicDamage = false)
        {
            if (_isDead)
                return;
                
            // Sincronizar con todos los clientes
            photonView.RPC("RPC_TakeDamage", RpcTarget.All, amount, attacker.photonView.ViewID, isMagicDamage);
        }
        
        /// <summary>
        /// Consume maná para usar una habilidad
        /// </summary>
        public virtual bool ConsumeMana(float amount)
        {
            if (currentMana >= amount)
            {
                currentMana -= amount;
                
                // Actualizar UI
                if (uiController != null)
                {
                    uiController.UpdateManaBar(currentMana, maxMana);
                }
                
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Maneja la muerte del héroe
        /// </summary>
        protected virtual void Die()
        {
            if (_isDead)
                return;
                
            _isDead = true;
            
            // Desactivar controles
            if (heroCollider != null)
                heroCollider.enabled = false;
                
            if (heroRigidbody != null)
                heroRigidbody.isKinematic = true;
                
            // Reproducir animación de muerte si hay animador
            if (animator != null)
                animator.SetTrigger("Die");
                
            // Invocar evento de muerte
            OnHeroDeath?.Invoke(this);
            
            // Iniciar respawn si es el jugador local
            if (photonView.IsMine)
            {
                Invoke("Respawn", 5.0f); // 5 segundos para respawn
            }
        }
        
        /// <summary>
        /// Maneja el respawn del héroe
        /// </summary>
        protected virtual void Respawn()
        {
            // Implementación básica, se puede mejorar para posicionar en spawn points, etc.
            _isDead = false;
            currentHealth = maxHealth;
            currentMana = maxMana;
            
            // Reactivar componentes
            if (heroCollider != null)
                heroCollider.enabled = true;
                
            if (heroRigidbody != null)
                heroRigidbody.isKinematic = false;
                
            // Reproducir animación si hay animador
            if (animator != null)
                animator.SetTrigger("Respawn");
                
            // Invocar evento de respawn
            OnHeroRespawn?.Invoke(this);
            
            // Actualizar UI
            if (uiController != null)
            {
                uiController.UpdateHealthBar(currentHealth, maxHealth);
                uiController.UpdateManaBar(currentMana, maxMana);
            }
        }
        
        #endregion
        
        #region PHOTON RPC
        
        [PunRPC]
        protected virtual void RPC_TakeDamage(float amount, int attackerViewID, bool isMagicDamage, PhotonMessageInfo info)
        {
            // Calcular mitigación de daño
            float damageReduction = isMagicDamage ? 
                100 / (100 + magicResistance) : 
                100 / (100 + armor);
                
            float actualDamage = amount * damageReduction;
            
            // Aplicar daño
            currentHealth -= actualDamage;
            
            // Actualizar UI
            if (uiController != null)
            {
                uiController.UpdateHealthBar(currentHealth, maxHealth);
                uiController.ShowDamageText(actualDamage, isMagicDamage);
            }
            
            // Verificar muerte
            if (currentHealth <= 0 && !_isDead)
            {
                Die();
            }
        }
        
        [PunRPC]
        protected virtual void RPC_HealHealth(float amount, PhotonMessageInfo info)
        {
            // Aplicar curación
            currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
            
            // Actualizar UI
            if (uiController != null)
            {
                uiController.UpdateHealthBar(currentHealth, maxHealth);
                uiController.ShowHealText(amount);
            }
        }
        
        #endregion
        
        #region IPunObservable Implementation
        
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            // Enviar datos relevantes por la red
            if (stream.IsWriting)
            {
                // Datos que enviamos
                stream.SendNext(currentHealth);
                stream.SendNext(currentMana);
                stream.SendNext(_isDead);
            }
            else
            {
                // Datos que recibimos
                currentHealth = (float)stream.ReceiveNext();
                currentMana = (float)stream.ReceiveNext();
                _isDead = (bool)stream.ReceiveNext();
                
                // Actualizar UI
                if (uiController != null)
                {
                    uiController.UpdateHealthBar(currentHealth, maxHealth);
                    uiController.UpdateManaBar(currentMana, maxMana);
                }
            }
        }
        
        #endregion
    }
}