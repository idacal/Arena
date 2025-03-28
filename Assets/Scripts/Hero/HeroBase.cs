using UnityEngine;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using System.Collections;
using System.Linq;

namespace Photon.Pun.Demo.Asteroids
{
    [RequireComponent(typeof(PhotonView))]
    public class HeroBase : MonoBehaviourPunCallbacks, IPunObservable, IFearable
    {
        [Header("Hero Identity")]
        public int heroId = -1;          // ID del héroe, debe coincidir con HeroData
        public string heroName = "";      // Nombre del héroe
        public HeroData heroData;         // Datos del héroe
        
        [Header("Team Settings")]
        public int teamId = 0;            // 0 = Rojo, 1 = Azul
        public Material teamRedMaterial;  // Material para equipo rojo
        public Material teamBlueMaterial; // Material para equipo azul
        public Renderer[] teamColorRenderers; // Renderers que cambiarán con el color del equipo
        
        [Header("Stats")]
        public float maxHealth;           // Se inicializará desde HeroDataSO
        public float currentHealth;
        public float maxMana;            // Se inicializará desde HeroDataSO
        public float currentMana;
        public float attackDamage;       // Se inicializará desde HeroDataSO
        public float attackSpeed;        // Se inicializará desde HeroDataSO
        public float moveSpeed;          // Se inicializará desde HeroDataSO
        public float armor;              // Se inicializará desde HeroDataSO
        public float magicResistance;    // Se inicializará desde HeroDataSO
        public float healthRegenRate;    // Se inicializará desde HeroDataSO
        public float manaRegenRate;      // Se inicializará desde HeroDataSO
        public float respawnTime;        // Se inicializará desde HeroDataSO
        
        [Header("Ability System")]
        public HeroAbilityController abilityController;
        
        [Header("References")]
        public HeroUIController uiController;
        public Animator animator;
        public GameObject uiCanvasPrefab;  // Prefab del canvas UI para asignar automáticamente
        
        [Header("Debug")]
        public bool debugMode = false;
        
        [Header("Combat Settings")]
        [SerializeField] protected float attackRange = 2f; // Para melee será corto, para rango será mayor
        [SerializeField] protected AttackType attackType = AttackType.Melee;
        [SerializeField] protected GameObject basicAttackProjectilePrefab; // Solo necesario para attackType = Ranged

        [Header("Combat Debug")]
        [SerializeField] protected bool showCombatDebug = false;
        
        [Header("Combat Effects")]
        public GameObject damageTextPrefab;
        public Transform floatingTextAnchor;
        public GameObject deathEffectPrefab;
        public AudioClip damageSound;
        public AudioClip deathSound;
        
        // Variables de control internas
        protected float attackCooldown = 0f;
        protected HeroBase currentTarget;
        protected bool isAttacking = false;
        protected float lastDamageTime = 0f;
        protected float damageImmunityTime = 0.1f;
        protected AudioSource audioSource;
        
        // Eventos
        public delegate void HeroEvent(HeroBase hero);
        public event HeroEvent OnHeroDeath;
        public event HeroEvent OnHeroRespawn;
        
        // Eventos de combate
        public delegate void HealthChangedDelegate(float currentHealth, float maxHealth);
        public event HealthChangedDelegate OnHealthChanged;
        
        public delegate void ManaChangedDelegate(float currentMana, float maxMana);
        public event ManaChangedDelegate OnManaChanged;
        
        public delegate void HeroDiedDelegate(HeroBase hero);
        public event HeroDiedDelegate OnHeroDied;
        
        // Constante para propiedad de equipo en Photon
        private const string PLAYER_TEAM = "PlayerTeam";
        
        [Header("Level System")]
        [SerializeField] private int _currentLevel = 1;
        public int CurrentLevel { get; private set; } = 1;
        private float _currentExperience = 0;
        public float CurrentExperience => _currentExperience;
        
        [Header("Experience Rewards")]
        [SerializeField] private float baseCreepXP = 25f;        // XP base por matar un creep
        [SerializeField] private float baseHeroKillXP = 100f;    // XP base por matar un héroe
        [SerializeField] private float baseAssistXP = 50f;       // XP base por asistencia
        [SerializeField] private float baseTowerXP = 150f;       // XP base por destruir torre
        [SerializeField] private float heroLevelXPMultiplier = 0.1f; // Multiplicador de XP por nivel del héroe asesinado
        [SerializeField] private float assistXPMultiplier = 0.5f;    // Multiplicador de XP para asistencias
        [SerializeField] private float xpRangeRadius = 1000f;        // Radio para compartir experiencia
        
        [Header("Skill System")]
        [SerializeField] private int _availableSkillPoints = 1; // Campo privado para los puntos de habilidad
        public int AvailableSkillPoints { get; private set; } = 1; // Comienza con 1 punto de habilidad
        
        public enum AttackType
        {
            Melee,
            Ranged
        }
        
        // Eventos de experiencia
        public delegate void ExperienceGainedDelegate(float amount, float total, float needed);
        public event ExperienceGainedDelegate OnExperienceGained;
        
        public delegate void LevelUpDelegate(int newLevel);
        public event LevelUpDelegate OnLevelUp;
        
        // Referencias privadas
        protected Rigidbody heroRigidbody;
        protected Collider heroCollider;
        protected bool _isDead = false;   // Cambiado a privado con un getter público
        
        // Propiedad pública para acceder al estado de muerte
        public bool IsDead => _isDead;

        #region UNITY CALLBACKS
        
        protected virtual void Awake()
        {
            // Obtener componentes
            heroRigidbody = GetComponent<Rigidbody>();
            heroCollider = GetComponent<Collider>();
            
            // Configurar audio
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1.0f;
                audioSource.minDistance = 5.0f;
                audioSource.maxDistance = 20.0f;
            }
            
            // Configurar punto de anclaje para texto flotante
            if (floatingTextAnchor == null)
            {
                GameObject anchor = new GameObject("FloatingTextAnchor");
                anchor.transform.SetParent(transform);
                anchor.transform.localPosition = new Vector3(0, 2f, 0);
                floatingTextAnchor = anchor.transform;
            }
            
            // Inicializar controlador de habilidades si existe
            if (abilityController == null)
            {
                abilityController = GetComponent<HeroAbilityController>();
            }
            
            // Inicializar controlador de UI si existe
            if (uiController == null)
            {
                uiController = GetComponent<HeroUIController>();
            }
            
            // Inicializar stats
            currentHealth = maxHealth;
            currentMana = maxMana;

            // Inicializar valores de experiencia
            if (heroData != null)
            {
                heroData.BaseExperience = 100f;
                heroData.ExperienceScaling = 1.5f;
                heroData.CurrentLevel = 1;
                heroData.CurrentExperience = 0;
                heroData.AvailableSkillPoints = 0;
                Debug.Log($"[HeroBase] Inicializando valores de experiencia: BaseExperience={heroData.BaseExperience}, ExperienceScaling={heroData.ExperienceScaling}");
            }
        }
        
        protected virtual void Start()
        {
            if (photonView.IsMine)
            {
                animator = GetComponentInChildren<Animator>();
                uiController = GetComponent<HeroUIController>();
                
                // Configuración inicial
                currentHealth = maxHealth;
                currentMana = maxMana;
                
                // Inicializar controlador de UI si no existe
                if (uiController == null && uiCanvasPrefab != null)
                {
                    GameObject uiCanvas = Instantiate(uiCanvasPrefab, transform);
                    uiController = uiCanvas.GetComponent<HeroUIController>();
                    if (uiController != null)
                    {
                        uiController.Initialize(this);
                    }
                }
                
                // Verificar que el NavMeshAgent está activado (muy importante para la colisión)
                UnityEngine.AI.NavMeshAgent navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (navAgent != null)
                {
                    if (!navAgent.enabled)
                    {
                        navAgent.enabled = true;
                        Debug.LogWarning($"[HeroBase] NavMeshAgent estaba desactivado en Start, activándolo para {heroName}");
                    }
                    
                    Debug.Log($"[HeroBase] Estado de NavMeshAgent en Start: {(navAgent.enabled ? "ACTIVO" : "INACTIVO")} - {heroName}");
                }
                else
                {
                    Debug.LogError($"[HeroBase] ¡Error grave! No se encontró NavMeshAgent en {heroName}");
                }
                
                // Verificar que el teamId sea correcto según los datos del jugador
                Player player = photonView.Owner;
                if (player != null)
                {
                    // Obtener la propiedad de equipo desde el objeto Player (usando la constante correcta)
                    if (player.CustomProperties.TryGetValue(PLAYER_TEAM, out object teamObj))
                    {
                        int networkTeamId = (int)teamObj;
                        if (teamId != networkTeamId)
                        {
                            Debug.LogWarning($"[HeroBase] Corrigiendo teamId incorrecto: {teamId} -> {networkTeamId} para {heroName}");
                            teamId = networkTeamId;
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[HeroBase] El jugador no tiene configurada la propiedad '{PLAYER_TEAM}'. Usando teamId por defecto: {teamId}");
                    }
                }
                
                // Aplicar color de equipo según teamId
                ApplyTeamColor();
                
                // Configurar layer según equipo usando LayerManager
                LayerManager.SetTeamLayerAndTag(gameObject, teamId);
                
                // Validar que los layers existen en el proyecto
                LayerManager.ValidateLayersAndTags();
                
                // Si tenemos photonView, sincronizamos la inicialización
                if (PhotonNetwork.IsConnected)
                {
                    Debug.Log($"[HeroBase] Sincronizando equipo a todos los clientes: teamId={teamId} para {heroName}");
                    photonView.RPC("RPC_SyncTeamConfig", RpcTarget.AllBuffered, teamId);
                    photonView.RPC("RPC_ForceModelUpdate", RpcTarget.OthersBuffered);
                }
                
                // Programar una verificación final del NavMeshAgent
                Invoke("CheckNavMeshAgentDelayed", 0.5f);
            }
            else
            {
                // Para clientes remotos, asegurar que al menos el tag sea correcto desde el principio
                Player owner = photonView.Owner;
                if (owner != null && owner.CustomProperties.TryGetValue(PLAYER_TEAM, out object teamObj))
                {
                    int networkTeamId = (int)teamObj;
                    if (teamId != networkTeamId)
                    {
                        teamId = networkTeamId;
                        Debug.Log($"[HeroBase] Cliente remoto: Actualizado teamId={teamId} para {heroName}");
                    }
                }
            }
            
            LoadHeroData();
        }
        
        /// <summary>
        /// Verifica el estado del NavMeshAgent después de un breve retraso 
        /// para asegurarnos de que no se desactive por otra parte del código
        /// </summary>
        private void CheckNavMeshAgentDelayed()
        {
            if (!photonView.IsMine) return;
            
            UnityEngine.AI.NavMeshAgent navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (navAgent != null)
            {
                if (!navAgent.enabled)
                {
                    navAgent.enabled = true;
                    Debug.LogWarning($"[HeroBase] NavMeshAgent se desactivó después de Start, reactivándolo para {heroName}");
                }
                
                Debug.Log($"[HeroBase] Estado final de NavMeshAgent: {(navAgent.enabled ? "ACTIVO" : "INACTIVO")} - {heroName}");
            }
        }
        
        protected virtual void Update()
        {
            if (!photonView.IsMine)
                return;
            
            // No regenerar salud si está muerto
            if (_isDead)
                return;
            
            // La lógica de control se implementará en clases derivadas
            HandleInput();
            
            // Regeneración de maná
            RegenerateMana();
            
            // Regeneración de salud
            if (currentHealth < maxHealth)
            {
                currentHealth = Mathf.Min(currentHealth + healthRegenRate * Time.deltaTime, maxHealth);
                if (uiController != null)
                {
                    uiController.UpdateHealthBar(currentHealth, maxHealth);
                }
            }
            
            // Actualizar cooldown de ataque
            if (attackCooldown > 0)
            {
                attackCooldown -= Time.deltaTime;
            }
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
            if (photonView.IsMine)
            {
                // Obtener el ID del héroe seleccionado
                Player player = photonView.Owner;
                int selectedHeroId = HeroManager.Instance.GetPlayerSelectedHeroId(player);
                if (selectedHeroId != -1)
                {
                    heroId = selectedHeroId;
                    
                    // Obtener el equipo del jugador
                    teamId = HeroManager.Instance.GetPlayerTeam(player);
                    
                    // Cargar datos del héroe
                    heroData = HeroManager.Instance.GetHeroData(heroId);
                    if (heroData != null)
                    {
                        heroName = heroData.Name;
                        maxHealth = heroData.MaxHealth;
                        currentHealth = maxHealth;
                        maxMana = heroData.MaxMana;
                        currentMana = maxMana;
                        attackDamage = heroData.CurrentAttackDamage;
                        attackSpeed = heroData.CurrentAttackSpeed;
                        moveSpeed = heroData.MovementSpeed * 0.01f; // Convertir a unidades de Unity
                        armor = heroData.CurrentArmor;
                        magicResistance = heroData.CurrentMagicResistance;
                        healthRegenRate = heroData.CurrentHealthRegen;
                        manaRegenRate = heroData.CurrentManaRegen;
                        respawnTime = heroData.RespawnTime;
                        
                        // Configurar habilidades
                        if (abilityController != null)
                        {
                            abilityController.SetupAbilities(heroData.Abilities);
                        }
                    }
                    else
                    {
                        Debug.LogError($"[HeroBase] No se encontraron datos para el héroe con ID {heroId}");
                    }
                }
                else
                {
                    Debug.LogError("[HeroBase] No se ha seleccionado ningún héroe");
                }
            }
        }
        
        /// <summary>
        /// Regenera maná con el tiempo
        /// </summary>
        protected virtual void RegenerateMana()
        {
            if (currentMana < maxMana)
            {
                float manaToRegen = maxMana * manaRegenRate * Time.deltaTime;
                currentMana = Mathf.Min(currentMana + manaToRegen, maxMana);
                
                // Notificar cambio de maná
                OnManaChanged?.Invoke(currentMana, maxMana);
                
                // Actualizar UI si existe
                if (uiController != null)
                {
                    uiController.UpdateManaBar(currentMana, maxMana);
                }
            }
        }
        
        /// <summary>
        /// Aplica daño al héroe
        /// </summary>
        public virtual void TakeDamage(float amount, HeroBase attacker, bool isMagicDamage = false)
        {
            if (_isDead)
                return;
                
            // Evitar daño demasiado frecuente
            if (Time.time - lastDamageTime < damageImmunityTime) return;
            lastDamageTime = Time.time;
            
            // Solo enviar al Master Client para procesar el daño
            if (attacker != null)
            {
                photonView.RPC("RPC_TakeDamage", RpcTarget.MasterClient, amount, attacker.photonView.ViewID, isMagicDamage);
                
                // Debug log para verificar que se está llamando
                Debug.Log($"[HeroBase] {heroName} recibió {amount} de daño de {attacker.heroName}");
            }
        }
        
        /// <summary>
        /// Sobrecarga para aplicar daño usando el ViewID del atacante
        /// </summary>
        public virtual void TakeDamage(float amount, int attackerViewID, bool isMagicDamage = false)
        {
            if (_isDead)
                return;
                
            // Solo enviar al Master Client para procesar el daño
            photonView.RPC("RPC_TakeDamage", RpcTarget.MasterClient, amount, attackerViewID, isMagicDamage);
            
            // Debug log para verificar que se está llamando
            Debug.Log($"[HeroBase] {heroName} recibió {amount} de daño del ViewID {attackerViewID}");
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
            // Desactivar regeneración y controles
            _isDead = true;
            DisableControls();
            
            // Reproducir animación de muerte
            if (animator != null)
            {
                // Usar parámetros genéricos que cada héroe configurará en su Animator
                animator.SetBool("IsDead", true);
                animator.SetTrigger("Die");
            }
            
            // Reproducir efecto de muerte si existe
            if (deathEffectPrefab != null)
            {
                Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            }
            
            // Reproducir sonido de muerte
            if (deathSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(deathSound);
            }
            
            // Notificar a todos los clientes
            photonView.RPC("RPC_OnDeath", RpcTarget.All);
            
            // Solo el dueño del objeto inicia el respawn
            if (photonView.IsMine)
            {
                StartCoroutine(RespawnAfterDelay());
            }
            
            // Notificar a los listeners
            OnHeroDeath?.Invoke(this);
            OnHeroDied?.Invoke(this);
        }
        
        private IEnumerator RespawnAfterDelay()
        {
            // Esperar el tiempo de respawn
            yield return new WaitForSeconds(respawnTime);
            
            // Buscar el GameplayManager
            GameplayManager gameplayManager = FindObjectOfType<GameplayManager>();
            if (gameplayManager != null)
            {
                // Obtener el punto de spawn según el equipo
                Transform spawnPoint = (teamId == 0) ? gameplayManager.redTeamSpawn : gameplayManager.blueTeamSpawn;
                
                if (spawnPoint != null)
                {
                    // Añadir variación para evitar superposiciones
                    Vector3 spawnPosition = spawnPoint.position + new Vector3(
                        Random.Range(-2f, 2f),
                        0f,
                        Random.Range(-2f, 2f)
                    );
                    
                    // Preparar el modelo para el respawn
            if (animator != null)
                    {
                        animator.SetBool("IsDead", false);
                        animator.ResetTrigger("Die");
                    }
                    
                    // Teletransportar al punto de spawn
                    transform.position = spawnPosition;
                    transform.rotation = spawnPoint.rotation;
                    
                    // Restaurar salud y mana
                    currentHealth = maxHealth;
                    currentMana = maxMana;
                    _isDead = false;
                    
                    // Notificar a todos los clientes
                    photonView.RPC("RPC_OnRespawn", RpcTarget.All, spawnPosition, spawnPoint.rotation);
                    
                    Debug.Log($"[HeroBase] Héroe respawneado en posición {spawnPosition} para el equipo {(teamId == 0 ? "Rojo" : "Azul")}");
                }
                else
                {
                    Debug.LogError($"[HeroBase] No se encontró el punto de spawn para el equipo {teamId}");
                }
            }
            else
            {
                Debug.LogError("[HeroBase] No se encontró el GameplayManager en la escena");
            }
        }
        
        private void DisableControls()
        {
            // Desactivar componentes de control
            var movementController = GetComponent<HeroMovementController>();
            if (movementController != null)
            {
                movementController.enabled = false;
            }
            
            var attackController = GetComponent<BasicAttackController>();
            if (attackController != null)
            {
                attackController.enabled = false;
            }
            
            // Desactivar colliders si es necesario
            var colliders = GetComponents<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }
        }
        
        private void EnableControls()
        {
            // Reactivar componentes de control
            var movementController = GetComponent<HeroMovementController>();
            if (movementController != null)
            {
                movementController.enabled = true;
                // Resetear el destino del movimiento al punto actual
                if (photonView.IsMine)
                {
                    movementController.ResetMovement();
                }
            }
            
            var attackController = GetComponent<BasicAttackController>();
            if (attackController != null)
            {
                attackController.enabled = true;
            }
            
            // Reactivar colliders
            var colliders = GetComponents<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = true;
            }
        }
        
        /// <summary>
        /// Hace el modelo transparente o restaura su opacidad
        /// </summary>
        protected virtual void MakeTransparent(bool transparent)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.materials;
                foreach (Material material in materials)
                {
                    if (transparent)
                    {
                        Color color = material.color;
                        color.a = 0.5f;
                        material.color = color;
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.EnableKeyword("_ALPHABLEND_ON");
                        material.renderQueue = 3000;
                    }
                    else
                    {
                        Color color = material.color;
                        color.a = 1f;
                        material.color = color;
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        material.DisableKeyword("_ALPHABLEND_ON");
                        material.renderQueue = -1;
                    }
                }
            }
        }
        
        /// <summary>
        /// Verifica y corrige la visibilidad del modelo con retraso
        /// </summary>
        private void DelayedVisibilityCheck()
        {
            // Si los renderers siguen desactivados, activarlos
            bool foundInvisibleRenderer = false;
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            
            foreach (Renderer renderer in renderers)
            {
                if (!renderer.enabled)
                {
                    renderer.enabled = true;
                    foundInvisibleRenderer = true;
                    
                    if (debugMode) {
                        Debug.Log($"Activado renderer: {renderer.name} en modelo del héroe");
                    }
                }
            }
            
            if (foundInvisibleRenderer && photonView.IsMine)
            {
                photonView.RPC("RPC_ForceModelUpdate", RpcTarget.Others);
            }
        }
        
        #endregion
        
        #region PHOTON RPC
        
        [PunRPC]
        protected virtual void RPC_TakeDamage(float amount, int attackerViewID, bool isMagicDamage, PhotonMessageInfo info)
        {
            // Solo el master procesa el daño real para evitar trampas
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }

            // Calcular mitigación de daño
            float damageReduction = isMagicDamage ? 
                100 / (100 + magicResistance) : 
                100 / (100 + armor);
                
            float actualDamage = amount * damageReduction;
            
            // Aplicar daño
            currentHealth -= actualDamage;
            
            // Limitar a 0 como mínimo
            currentHealth = Mathf.Max(0f, currentHealth);
            
            // Mostrar efecto de daño
            if (damageTextPrefab != null && floatingTextAnchor != null)
            {
                GameObject damageText = Instantiate(damageTextPrefab, floatingTextAnchor.position, Quaternion.identity, floatingTextAnchor);
                damageText.GetComponent<TextMesh>().text = Mathf.RoundToInt(actualDamage).ToString();
                Destroy(damageText, 1.5f);
            }
            
            // Reproducir sonido de daño
            if (damageSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(damageSound);
            }
            
            // Actualizar UI
            if (uiController != null)
            {
                uiController.UpdateHealthBar(currentHealth, maxHealth);
                uiController.ShowDamageText(actualDamage, isMagicDamage);
            }
            
            // Verificar muerte y sincronizar con todos los clientes
            bool shouldDie = currentHealth <= 0 && !_isDead;
            photonView.RPC("RPC_SyncHealth", RpcTarget.All, currentHealth, shouldDie);
        }
        
        [PunRPC]
        protected virtual void RPC_SyncHealth(float newHealth, bool isDead)
        {
            // Actualizar salud
            currentHealth = newHealth;
            
            // Actualizar UI
            if (uiController != null)
            {
                uiController.UpdateHealthBar(currentHealth, maxHealth);
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
            }
            
            // Si debe morir y no está muerto, iniciar el proceso de muerte
            if (isDead && !_isDead)
            {
                _isDead = true;
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
        
        [PunRPC]
        private void RPC_ForceModelUpdate()
        {
            // Asegurarse de que todos los renderers están habilitados
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (!renderer.enabled)
                {
                    renderer.enabled = true;
                    
                    if (debugMode) {
                        Debug.Log($"RPC_ForceModelUpdate: Activado renderer {renderer.name}");
                    }
                }
            }
            
            // Si hay animator, asegurarse de que está habilitado y trigger una actualización
            if (animator != null)
            {
                animator.enabled = true;
                animator.Rebind(); // Forza la actualización de la animación
                animator.Update(0f); // Actualiza el estado del animator inmediatamente
                
                if (debugMode) {
                    Debug.Log("RPC_ForceModelUpdate: Animator actualizado");
                }
            }
            
            // Verificar NavMeshAgent para jugador local
            if (photonView.IsMine)
            {
                // Obtener el controlador de movimiento
                HeroMovementController movementController = GetComponent<HeroMovementController>();
                if (movementController != null)
                {
                    // Obtener NavMeshAgent
                    UnityEngine.AI.NavMeshAgent navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
                    
                    if (navAgent != null)
                    {
                        // Asegurarse de que está activado para el jugador local
                        if (!navAgent.enabled)
                        {
                            navAgent.enabled = true;
                            Debug.Log($"RPC_ForceModelUpdate: NavMeshAgent reactivado para {gameObject.name}");
                        }
                        
                        // Verificar si está en un NavMesh válido
                        if (!navAgent.isOnNavMesh)
                        {
                            Debug.LogWarning($"RPC_ForceModelUpdate: NavMeshAgent no está en NavMesh válido - {gameObject.name}");
                            
                            // Intentar recuperar la posición
                            UnityEngine.AI.NavMeshHit hit;
                            if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out hit, 5f, UnityEngine.AI.NavMesh.AllAreas))
                            {
                                navAgent.Warp(hit.position);
                                Debug.Log($"RPC_ForceModelUpdate: Corregida posición en NavMesh para {gameObject.name}");
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"RPC_ForceModelUpdate: No se encontró NavMeshAgent en {gameObject.name}");
                    }
                }
            }
        }
        
        [PunRPC]
        private void RPC_SyncTeamConfig(int teamId)
        {
            // Asignar layer y tag según el teamId, incluso en clientes remotos
            this.teamId = teamId;
            
            Debug.Log($"[HeroBase] RPC_SyncTeamConfig para {gameObject.name}: Recibido teamId={teamId}, photonView.IsMine={photonView.IsMine}");
            
            // Aplicar el color del equipo
            ApplyTeamColor();
            
            // Configurar layer y tag correctamente según el teamId
            string expectedTag = teamId == 0 ? LayerManager.TAG_RED_TEAM : LayerManager.TAG_BLUE_TEAM;
            LayerManager.SetTeamLayerAndTag(gameObject, teamId);
            
            // Verificación para asegurar que el tag fue asignado correctamente
            if (gameObject.tag != expectedTag)
            {
                Debug.LogError($"[HeroBase] ERROR DE TAG: El tag debería ser {expectedTag} pero es {gameObject.tag}. Corrigiendo...");
                gameObject.tag = expectedTag;
            }
            
            // Verificación adicional para todas las propiedades importantes
            Debug.Log($"[RPC_SyncTeamConfig] VERIFICACIÓN FINAL para {gameObject.name}: " + 
                     $"teamId={teamId}, tag={gameObject.tag}, layer={gameObject.layer}, " +
                     $"photonView.ViewID={photonView.ViewID}, Owner={photonView.Owner?.NickName}");
            
            // Verificar si hay discrepancia entre TeamId y el tag asignado
            if ((teamId == 0 && gameObject.tag != LayerManager.TAG_RED_TEAM) || 
                (teamId == 1 && gameObject.tag != LayerManager.TAG_BLUE_TEAM))
            {
                Debug.LogError($"[HeroBase] DISCREPANCIA CRÍTICA: teamId={teamId} no coincide con tag={gameObject.tag}");
                // Forzar el tag correcto
                gameObject.tag = teamId == 0 ? LayerManager.TAG_RED_TEAM : LayerManager.TAG_BLUE_TEAM;
            }
            
            Debug.Log($"[RPC] Sincronizada configuración de equipo: {gameObject.name}, teamId={teamId}, tag={gameObject.tag}");
        }
        
        [PunRPC]
        private void RPC_OnRespawn(Vector3 position, Quaternion rotation)
        {
            // Actualizar posición y rotación
            transform.position = position;
            transform.rotation = rotation;
            
            // Restaurar estado
            _isDead = false;
            
            // Resetear animador a estado normal
            if (animator != null)
            {
                animator.SetBool("IsDead", false);
                animator.SetTrigger("Respawn");
            }
            
            // Detener cualquier movimiento previo y preparar para nuevos movimientos
            var navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (navAgent != null && photonView.IsMine)
            {
                navAgent.ResetPath();
                navAgent.velocity = Vector3.zero;
                navAgent.enabled = true;
                navAgent.isStopped = false;
                navAgent.updatePosition = true;
                navAgent.updateRotation = true;
                navAgent.updateUpAxis = true;
                // Asegurarnos de que el agente esté en la posición correcta
                navAgent.Warp(position);
            }
            
            // Habilitar controles (esto también reseteará el movimiento)
            EnableControls();
            
            // Actualizar UI
            if (uiController != null)
            {
                uiController.UpdateHealthBar(currentHealth, maxHealth);
                uiController.UpdateManaBar(currentMana, maxMana);
            }
            
            // Notificar a los listeners
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            OnManaChanged?.Invoke(currentMana, maxMana);
            OnHeroRespawn?.Invoke(this);
            
            Debug.Log($"[HeroBase] Héroe respawneado en posición {position} y listo para moverse");
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
                stream.SendNext(isAttacking);
            }
            else
            {
                // Datos que recibimos
                currentHealth = (float)stream.ReceiveNext();
                currentMana = (float)stream.ReceiveNext();
                _isDead = (bool)stream.ReceiveNext();
                isAttacking = (bool)stream.ReceiveNext();
                
                // Actualizar UI
                if (uiController != null)
                {
                    uiController.UpdateHealthBar(currentHealth, maxHealth);
                    uiController.UpdateManaBar(currentMana, maxMana);
                }
                
                // Notificar cambio de salud
                OnHealthChanged?.Invoke(currentHealth, maxHealth);
            }
        }
        
        #endregion
        
        // Propiedades de combate
        public float AttackDamage => attackDamage;
        public float AttackSpeed => attackSpeed;
        public float AttackRange => attackRange;
        public AttackType HeroAttackType => attackType;
        public float AttackCooldown => attackCooldown;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsAttacking => isAttacking;
        
        #region Combat Methods
        
        // Método para realizar un ataque básico
        public virtual bool TryBasicAttack(HeroBase target)
        {
            if (!photonView.IsMine) return false;
            
            // Verificar si está en cooldown
            if (attackCooldown > 0)
            {
                if (showCombatDebug) Debug.Log($"{heroName} no puede atacar aún, cooldown: {attackCooldown:F1}s");
                return false;
            }
            
            // Verificar distancia
            float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
            if (distanceToTarget > attackRange)
            {
                if (showCombatDebug) Debug.Log($"{heroName} fuera de rango para atacar a {target.heroName}, distancia: {distanceToTarget:F1}m");
                return false;
            }
            
            // Efectuar el ataque según el tipo
            if (attackType == AttackType.Melee)
            {
                // Ejecutar ataque melee directamente
                ExecuteMeleeAttack(target);
            }
            else if (attackType == AttackType.Ranged)
            {
                // Crear proyectil para ataque a distancia
                ExecuteRangedAttack(target);
            }
            
            // Establecer cooldown basado en velocidad de ataque
            attackCooldown = 1f / attackSpeed;
            isAttacking = true;
            
            // Trigger de animación (se implementará después)
            TriggerAttackAnimation();
            
            return true;
        }
        
        // Ataque cuerpo a cuerpo
        protected virtual void ExecuteMeleeAttack(HeroBase target)
        {
            if (showCombatDebug) Debug.Log($"{heroName} realiza ataque cuerpo a cuerpo a {target.heroName}");
            
            // Mandar RPC para sincronizar el daño en todos los clientes
            photonView.RPC("RPC_ApplyDamage", RpcTarget.All, target.photonView.ViewID, attackDamage);
        }
        
        // Ataque a distancia
        protected virtual void ExecuteRangedAttack(HeroBase target)
        {
            if (showCombatDebug) Debug.Log($"{heroName} dispara proyectil a {target.heroName}");
            
            if (basicAttackProjectilePrefab != null)
            {
                // Posición de origen del proyectil (podría ajustarse a un punto específico del personaje)
                Vector3 spawnPosition = transform.position + transform.forward * 0.5f + Vector3.up * 1.0f;
                
                // Dirección hacia el objetivo
                Vector3 direction = (target.transform.position - spawnPosition).normalized;
                
                // Instanciar proyectil vía Photon para que sea visible en la red
                object[] instantiationData = new object[] { 
                    attackDamage, 
                    photonView.ViewID, // ID del atacante
                    target.photonView.ViewID // ID del objetivo
                };
                
                // Instanciar el proyectil
                PhotonNetwork.Instantiate(
                    basicAttackProjectilePrefab.name, 
                    spawnPosition, 
                    Quaternion.LookRotation(direction), 
                    0, 
                    instantiationData
                );
            }
            else
            {
                Debug.LogError($"No hay prefab de proyectil asignado para el ataque básico de {heroName}");
            }
        }
        
        // Método para recibir daño
        public virtual bool TakeDamage(float damageAmount, HeroBase attacker)
        {
            if (!photonView.IsMine) return false;
            
            // Aplicar daño y sincronizar
            photonView.RPC("RPC_ApplyDamage", RpcTarget.All, photonView.ViewID, damageAmount);
            
            return true;
        }
        
        // Efecto visual de recibir daño
        protected virtual void ShowDamageEffect()
        {
            // Implementar efectos visuales (partículas, animación, etc.)
        }
        
        // Trigger de animación de ataque
        protected virtual void TriggerAttackAnimation()
        {
            // Implementar animación de ataque
        }
        
        // Método llamado cuando el héroe muere en combate
        protected virtual void DieInCombat()
        {
            if (showCombatDebug) Debug.Log($"{heroName} ha muerto");
            
            // Invocar evento de muerte
            OnHeroDied?.Invoke(this);
            
            // Desactivar controles
            if (GetComponent<HeroMovementController>() != null)
            {
                GetComponent<HeroMovementController>().enabled = false;
            }
            
            if (GetComponent<HeroAbilityController>() != null)
            {
                GetComponent<HeroAbilityController>().enabled = false;
            }
            
            // Si somos propietarios, informar al GameManager
            if (photonView.IsMine)
            {
                // Implementar lógica de respawn, puntuación, etc.
            }
        }
        
        // Método para curar al héroe
        public virtual void Heal(float amount)
        {
            if (!photonView.IsMine) return;
            
            photonView.RPC("RPC_Heal", RpcTarget.All, photonView.ViewID, amount);
        }
        
        #endregion
        
        #region RPCs for Combat
        
        [PunRPC]
        protected virtual void RPC_ApplyDamage(int targetViewID, float damageAmount)
        {
            // Buscar el objetivo por su ViewID
            PhotonView targetView = PhotonView.Find(targetViewID);
            if (targetView == null) return;
            
            // Obtener componente HeroBase
            HeroBase targetHero = targetView.GetComponent<HeroBase>();
            if (targetHero == null) return;
            
            // Aplicar daño localmente en cada cliente
            targetHero.ApplyDamageLocally(damageAmount);
        }
        
        // Método local para aplicar el daño (llamado desde RPC)
        public virtual void ApplyDamageLocally(float damageAmount)
        {
            // Reducir salud
            currentHealth -= damageAmount;
            
            // Limitar a 0 como mínimo
            currentHealth = Mathf.Max(0f, currentHealth);
            
            // Invocar evento de cambio de salud
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            
            // Mostrar efecto visual de daño
            ShowDamageEffect();
            
            // Verificar si el héroe ha muerto
            if (currentHealth <= 0)
            {
                DieInCombat();
            }
            
            if (showCombatDebug) Debug.Log($"{heroName} recibe {damageAmount:F1} de daño. Salud restante: {currentHealth:F1}");
        }
        
        [PunRPC]
        protected virtual void RPC_Heal(int targetViewID, float healAmount)
        {
            // Buscar el objetivo por su ViewID
            PhotonView targetView = PhotonView.Find(targetViewID);
            if (targetView == null) return;
            
            // Obtener componente HeroBase
            HeroBase targetHero = targetView.GetComponent<HeroBase>();
            if (targetHero == null) return;
            
            // Aplicar curación localmente en cada cliente
            targetHero.HealLocally(healAmount);
        }
        
        // Método local para aplicar curación (llamado desde RPC)
        protected virtual void HealLocally(float healAmount)
        {
            // Aumentar salud
            currentHealth += healAmount;
            
            // Limitar al máximo
            currentHealth = Mathf.Min(maxHealth, currentHealth);
            
            // Invocar evento de cambio de salud
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
            
            if (showCombatDebug) Debug.Log($"{heroName} recibe {healAmount:F1} de curación. Salud: {currentHealth:F1}");
        }
        
        #endregion

        public void ApplyFear(float duration)
        {
            if (!photonView.IsMine) return;

            HeroMovementController moveController = GetComponent<HeroMovementController>();
            if (moveController != null)
            {
                // Aplicar efecto de miedo
                moveController.ApplyStun(duration * 0.5f); // Stun por la mitad de la duración del miedo
                
                // Hacer que el héroe huya en dirección aleatoria
                Vector3 randomDirection = Random.insideUnitSphere;
                randomDirection.y = 0;
                randomDirection.Normalize();
                
                Vector3 fleePosition = transform.position + randomDirection * 10f;
                moveController.SetDestination(fleePosition);
            }
        }

        public void AddSkillPoint(int points = 1)
        {
            _availableSkillPoints += points;
            AvailableSkillPoints = _availableSkillPoints;
            // Notificar a la UI para que se actualice
            if (abilityController != null)
            {
                abilityController.RefreshAbilityUI();
            }
        }

        public bool UseSkillPoint()
        {
            if (_availableSkillPoints > 0)
            {
                _availableSkillPoints--;
                AvailableSkillPoints = _availableSkillPoints;
                return true;
            }
            return false;
        }

        public void SetLevel(int level)
        {
            _currentLevel = Mathf.Max(1, level);
            CurrentLevel = _currentLevel;
            // Aquí podrías agregar lógica adicional cuando el héroe sube de nivel
            // Por ejemplo, otorgar puntos de habilidad
        }

        /// <summary>
        /// Usa maná del héroe
        /// </summary>
        public void UseMana(int amount)
        {
            if (amount <= 0) return;
            
            currentMana = Mathf.Max(0, currentMana - amount);
            
            // Notificar a la UI si es necesario
            if (uiController != null)
            {
                uiController.UpdateManaBar(currentMana, maxMana);
            }
        }

        /// <summary>
        /// Otorga experiencia por matar un creep
        /// </summary>
        public void AwardCreepKillExperience()
        {
            if (!photonView.IsMine) return;
            
            // Encontrar héroes aliados cercanos para compartir XP
            var nearbyAllies = Physics.OverlapSphere(transform.position, xpRangeRadius)
                                    .Select(c => c.GetComponent<HeroBase>())
                                    .Where(h => h != null && h.teamId == this.teamId)
                                    .ToList();
            
            // Dividir la XP entre los héroes cercanos
            float sharedXP = baseCreepXP / nearbyAllies.Count;
            
            foreach (var ally in nearbyAllies)
            {
                ally.GainExperience(sharedXP);
            }
        }
        
        /// <summary>
        /// Otorga experiencia por matar un héroe
        /// </summary>
        public void AwardHeroKillExperience(HeroBase killedHero)
        {
            if (!photonView.IsMine || killedHero == null) return;
            
            // Calcular XP base más bonus por nivel
            float xpReward = baseHeroKillXP + (baseHeroKillXP * killedHero.CurrentLevel * heroLevelXPMultiplier);
            
            // Encontrar héroes aliados cercanos para asistencias
            var nearbyAllies = Physics.OverlapSphere(transform.position, xpRangeRadius)
                                    .Select(c => c.GetComponent<HeroBase>())
                                    .Where(h => h != null && h.teamId == this.teamId && h != this)
                                    .ToList();
            
            // Otorgar XP al asesino
            GainExperience(xpReward);
            
            // Otorgar XP de asistencia
            float assistXP = xpReward * assistXPMultiplier;
            foreach (var ally in nearbyAllies)
            {
                ally.GainExperience(assistXP);
            }
        }
        
        /// <summary>
        /// Otorga experiencia por destruir una torre
        /// </summary>
        public void AwardTowerKillExperience()
        {
            if (!photonView.IsMine) return;
            
            // Encontrar héroes aliados cercanos para compartir XP
            var nearbyAllies = Physics.OverlapSphere(transform.position, xpRangeRadius)
                                    .Select(c => c.GetComponent<HeroBase>())
                                    .Where(h => h != null && h.teamId == this.teamId)
                                    .ToList();
            
            // Dividir la XP entre los héroes cercanos
            float sharedXP = baseTowerXP / nearbyAllies.Count;
            
            foreach (var ally in nearbyAllies)
            {
                ally.GainExperience(sharedXP);
            }
        }
        
        /// <summary>
        /// Método principal para ganar experiencia
        /// </summary>
        public void GainExperience(float amount)
        {
            if (!photonView.IsMine || amount <= 0) return;
            
            float experienceNeeded = GetExperienceForNextLevel();
            _currentExperience += amount;
            
            // Notificar ganancia de experiencia
            OnExperienceGained?.Invoke(amount, _currentExperience, experienceNeeded);
            
            // Verificar si subimos de nivel
            while (_currentExperience >= experienceNeeded && CurrentLevel < heroData.MaxLevel)
            {
                _currentExperience -= experienceNeeded;
                LevelUp();
                experienceNeeded = GetExperienceForNextLevel();
            }
            
            // Si estamos al máximo nivel, mantener la experiencia al máximo
            if (CurrentLevel >= heroData.MaxLevel)
            {
                _currentExperience = experienceNeeded;
            }
        }
        
        /// <summary>
        /// Calcula la experiencia necesaria para el siguiente nivel
        /// </summary>
        public float GetExperienceForNextLevel()
        {
            return heroData.BaseExperience * Mathf.Pow(heroData.ExperienceScaling, CurrentLevel - 1);
        }
        
        /// <summary>
        /// Maneja la subida de nivel
        /// </summary>
        private void LevelUp()
        {
            CurrentLevel++;
            _currentLevel = CurrentLevel;
            
            // Otorgar puntos de habilidad
            AddSkillPoint(heroData.SkillPointsPerLevel);
            
            // Actualizar stats basados en el nuevo nivel
            UpdateStatsForLevel();
            
            // Notificar la subida de nivel
            OnLevelUp?.Invoke(CurrentLevel);
        }
        
        /// <summary>
        /// Actualiza las estadísticas basadas en el nivel actual
        /// </summary>
        private void UpdateStatsForLevel()
        {
            if (heroData == null) return;
            
            // Actualizar estadísticas derivadas del nivel
            maxHealth = heroData.MaxHealth;
            maxMana = heroData.MaxMana;
            attackDamage = heroData.CurrentAttackDamage;
            attackSpeed = heroData.CurrentAttackSpeed;
            armor = heroData.CurrentArmor;
            magicResistance = heroData.CurrentMagicResistance;
            healthRegenRate = heroData.CurrentHealthRegen;
            manaRegenRate = heroData.CurrentManaRegen;
            
            // Restaurar vida y maná al subir de nivel (opcional, como en DOTA 2)
            currentHealth = maxHealth;
            currentMana = maxMana;
        }
    }
}