using UnityEngine;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using TMPro;

namespace Photon.Pun.Demo.Asteroids
{
    // Definición de interfaz UnitBase para ser utilizada por los sistemas de experiencia
    public interface UnitBase
    {
        // Propiedades básicas que cualquier unidad debe tener
        string name { get; }
        int level { get; }
        bool IsDead { get; }
    }
    
    [RequireComponent(typeof(PhotonView))]
    public class HeroBase : MonoBehaviourPunCallbacks, IPunObservable, IFearable, UnitBase
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
        public HeroBase currentTarget;  // Cambiado de protected a public
        protected bool isAttacking = false;
        protected float lastDamageTime = 0f;
        protected float damageImmunityTime = 0.1f;
        protected AudioSource audioSource;
        
        // Sistema de Kill Streak
        [Header("Kill Streak")]
        public int currentKillStreak = 0;
        private float lastKillTime = 0f;
        private float killStreakTimeout = 30f; // Tiempo en segundos para que expire la racha
        
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
        
        [Header("Currency")]
        [SerializeField] private float _currentGold = 0f;
        public float CurrentGold => _currentGold;
        public float StartingGold = 500f;
        
        [Header("Audio")]
        public AudioClip goldSound; // Sonido básico de oro
        public AudioClip bigGoldSound; // Sonido para cantidades grandes (>= 100)
        public AudioClip smallGoldSound; // Sonido para cantidades pequeñas (< 20)
        public AudioClip levelUpSound; // Sonido para subir de nivel
        
        [Header("Gold Rewards")]
        [SerializeField] private float baseHeroKillGold = 200f;    // Oro base por matar un héroe
        [SerializeField] private float heroLevelGoldMultiplier = 0.15f; // Multiplicador de oro por nivel del héroe asesinado
        [SerializeField] private float killStreakGoldMultiplier = 0.1f; // Multiplicador de oro por racha de asesinatos
        [SerializeField] private float assistGoldMultiplier = 0.4f;     // Multiplicador de oro para asistencias
        
        [Header("Level Up Effect")]
        public GameObject levelUpEffectPrefab; // Prefab del efecto visual de subir de nivel
        public Color levelUpEffectColor = new Color(1f, 0.8f, 0.2f); // Color dorado por defecto
        public float levelUpEffectDuration = 3f; // Duración del efecto en segundos
        
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

        // Implementación de las propiedades de la interfaz UnitBase
        string UnitBase.name { get => heroName; }
        int UnitBase.level { get => CurrentLevel; }
        
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
                _currentGold = StartingGold; // Inicializar oro
                
                // IMPORTANTE: Asegurar que el jugador siempre comience con la capa "Player" correcta
                if (gameObject.layer != LayerManager.PlayerLayerID)
                {
                    Debug.LogWarning($"[HeroBase] Capa incorrecta al iniciar: {LayerMask.LayerToName(gameObject.layer)}. Cambiando a {LayerManager.LAYER_PLAYER}");
                    LayerManager.SetLayerRecursively(transform, LayerManager.PlayerLayerID);
                }
                
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
            
            // Si estamos en el editor o modo de desarrollo, permitir pruebas con teclas
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Presionar L para subir de nivel (prueba del efecto visual y sonido)
            if (Input.GetKeyDown(KeyCode.L))
            {
                // Obtener la experiencia necesaria para el siguiente nivel
                float experienceNeeded = GetExperienceForNextLevel();
                
                // Añadir esa cantidad de experiencia para forzar la subida de nivel
                GainExperience(experienceNeeded);
                
                Debug.Log($"[HeroBase] Forzando subida de nivel con tecla L para {heroName}");
            }
            #endif
            
            // Verificación periódica de la capa del jugador (solo verificamos cada 1 segundo)
            if (Time.frameCount % 60 == 0)
            {
                // Si el jugador está vivo y no está en un arbusto, verificar que su capa sea Player
                EntityVisibilityTracker tracker = GetComponent<EntityVisibilityTracker>();
                if (tracker != null && !tracker.IsInBush && gameObject.layer != LayerManager.PlayerLayerID)
                {
                    Debug.LogWarning($"[HeroBase] 🔍 Detectada capa incorrecta durante el juego: {LayerMask.LayerToName(gameObject.layer)} pero no estamos en un arbusto. Forzando corrección.");
                    ForceSynchronizePlayerLayer();
                }
            }
            
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
            
            // Actualizar racha de kills
            if (currentKillStreak > 0 && Time.time - lastKillTime > killStreakTimeout)
            {
                ResetKillStreak();
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
                        Debug.Log($"[HeroBase] Cargando datos para héroe {heroId}: {heroData.Name}");
                        Debug.Log($"[HeroBase] Atributos base - Fuerza: {heroData.BaseStrength}, Inteligencia: {heroData.BaseIntelligence}, Agilidad: {heroData.BaseAgility}");
                        Debug.Log($"[HeroBase] Escalados - Fuerza: {heroData.StrengthScaling}, Inteligencia: {heroData.IntelligenceScaling}, Agilidad: {heroData.AgilityScaling}");
                        
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
                        
                        Debug.Log($"[HeroBase] Estadísticas finales - Daño: {attackDamage}, Velocidad de ataque: {attackSpeed}, Armadura: {armor}");
                        
                        // Configurar habilidades
                        if (abilityController != null)
                        {
                            abilityController.SetupAbilities(heroData.Abilities);
                        }
                    }
                    else
                    {
                        Debug.LogError($"[HeroBase] No se encontraron datos para el héroe ID: {heroId}");
                    }
                }
                else
                {
                    Debug.LogError($"[HeroBase] No se encontró ID de héroe seleccionado para el jugador {player.NickName}");
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
            
            // Establecer el atacante como objetivo actual
            if (attacker != null)
            {
                currentTarget = attacker;
            }
            
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
            Debug.Log($"[HeroBase] Die() llamado para {heroName} (IsMine: {photonView.IsMine})");
            
            // Desactivar regeneración y controles
            _isDead = true;
            DisableControls();
            
            // Reproducir animación de muerte
            if (animator != null)
            {
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

            // Reiniciar la racha de asesinatos al morir
            ResetKillStreak();

            // El CombatManager se encargará de esto a través del evento OnHeroDeath
            // Pero como fallback, también notificamos directamente al KillFeedManager
            if (currentTarget != null)
            {
                KillFeedManager killFeedManager = FindObjectOfType<KillFeedManager>();
                if (killFeedManager != null)
                {
                    Debug.Log($"[HeroBase] Notificando muerte al KillFeedManager: {currentTarget.heroName} mató a {heroName}");
                    killFeedManager.HandlePlayerKill(
                        currentTarget.photonView.Owner.ActorNumber,
                        photonView.Owner.ActorNumber
                    );
                }
            }

            // Otorgar experiencia al héroe que causó la muerte
            if (currentTarget != null)
            {
                Debug.Log($"[HeroBase] Otorgando experiencia y oro a {currentTarget.heroName} por matar a {heroName}");
                currentTarget.AwardHeroKillExperience(this);
                currentTarget.AwardHeroKillGold(this);
            }
            else
            {
                Debug.LogWarning($"[HeroBase] No se encontró currentTarget para otorgar experiencia por la muerte de {heroName}");
            }
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

            // Calcular valor aleatorio de daño dentro del rango (95% a 100%)
            float randomFactor = Random.Range(0.95f, 1.0f);
            float randomizedDamage = amount * randomFactor;
            
            // Calcular mitigación de daño
            float damageReduction = isMagicDamage ? 
                100 / (100 + magicResistance) : 
                100 / (100 + armor);
                
            float actualDamage = randomizedDamage * damageReduction;
            
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
            
            // IMPORTANTE: SIEMPRE asignar capa Player al respawnear, sin importar dónde murió el jugador
            // Esto garantiza que el jugador siempre esté visible después del respawn
            int currentLayer = gameObject.layer;
            if (currentLayer != LayerManager.PlayerLayerID)
            {
                Debug.LogWarning($"[HeroBase] ⚠️ Capa incorrecta al respawnear: {LayerMask.LayerToName(currentLayer)} ({currentLayer}). Corrigiendo a {LayerManager.LAYER_PLAYER} ({LayerManager.PlayerLayerID})");
                
                // Aplicar la capa correcta de forma recursiva a este objeto y todos sus hijos
                LayerManager.SetLayerRecursively(transform, LayerManager.PlayerLayerID);
                
                // Forzar la capa Player directamente en el objeto principal y sus hijos principales
                // para mayor seguridad de que el cambio se aplique
                gameObject.layer = LayerManager.PlayerLayerID;
                foreach (Transform child in transform)
                {
                    if (child != null)
                    {
                        child.gameObject.layer = LayerManager.PlayerLayerID;
                        foreach (Transform subChild in child)
                        {
                            if (subChild != null)
                                subChild.gameObject.layer = LayerManager.PlayerLayerID;
                        }
                    }
                }
                
                // Verificar que el cambio fue efectivo
                if (gameObject.layer != LayerManager.PlayerLayerID)
                {
                    Debug.LogError($"[HeroBase] ❌ ERROR CRÍTICO: No se pudo cambiar la capa al respawnear. Capa actual: {LayerMask.LayerToName(gameObject.layer)}");
                }
                else
                {
                    Debug.Log($"[HeroBase] ✅ Capa corregida exitosamente a {LayerManager.LAYER_PLAYER}");
                }
            }
            
            // Si tenemos un EntityVisibilityTracker, asegurarnos de que no esté marcado como "en arbusto"
            EntityVisibilityTracker tracker = GetComponent<EntityVisibilityTracker>();
            if (tracker != null)
            {
                // Forzar al tracker a reconocer que ya no está en un arbusto
                Debug.Log("[HeroBase] Notificando a EntityVisibilityTracker que ya no estamos en un arbusto");
                if (tracker.IsInBush && tracker.CurrentBush != null)
                {
                    tracker.ExitBush(tracker.CurrentBush);
                }
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
            
            
        }
        
        [PunRPC]
        private void RPC_OnDeath()
        {
            Debug.Log($"[HeroBase] RPC_OnDeath recibido para {heroName} (IsMine: {photonView.IsMine})");
            
            // Asegurarnos de que el modelo esté en estado de muerte
            if (animator != null)
            {
                animator.SetBool("IsDead", true);
                animator.SetTrigger("Die");
            }
            
            // Desactivar colliders y controles
            DisableControls();
            
            // Reproducir efecto de muerte si existe (localmente en cada cliente)
            if (deathEffectPrefab != null)
            {
                Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            }
            
            // Reproducir sonido de muerte localmente
            if (deathSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(deathSound);
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
            
            // Calcular daño aleatorio entre el mínimo y máximo
            float damageAmount = Random.Range(heroData.MinAttackDamage, heroData.MaxAttackDamage);
            
            // Mandar RPC para sincronizar el daño en todos los clientes
            photonView.RPC("RPC_ApplyDamage", RpcTarget.All, target.photonView.ViewID, damageAmount);
        }
        
        // Ataque a distancia
        protected virtual void ExecuteRangedAttack(HeroBase target)
        {
            if (showCombatDebug) Debug.Log($"{heroName} dispara proyectil a {target.heroName}");
            
            if (basicAttackProjectilePrefab != null)
            {
                // Calcular daño aleatorio entre el mínimo y máximo
                float damageAmount = Random.Range(heroData.MinAttackDamage, heroData.MaxAttackDamage);
                
                // Posición de origen del proyectil
                Vector3 spawnPosition = transform.position + transform.forward * 0.5f + Vector3.up * 1.0f;
                
                // Dirección hacia el objetivo
                Vector3 direction = (target.transform.position - spawnPosition).normalized;
                
                // Instanciar proyectil vía Photon para que sea visible en la red
                object[] instantiationData = new object[] { 
                    damageAmount, 
                    photonView.ViewID, // ID del atacante
                    target.photonView.ViewID, // ID del objetivo
                    photonView.ViewID // Añadimos el ViewID del atacante para el currentTarget
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
            // Calcular valor aleatorio de daño dentro del rango (95% a 100%)
            float randomFactor = Random.Range(0.95f, 1.0f);
            float actualDamage = damageAmount * randomFactor;
            
            // Reducir salud
            currentHealth -= actualDamage;
            
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
            
            if (showCombatDebug) Debug.Log($"{heroName} recibe {actualDamage:F1} de daño (variación {randomFactor:P0}). Salud restante: {currentHealth:F1}");
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
        public void AwardCreepKillExperience(NeutralCreep killedCreep)
        {
            if (!photonView.IsMine || killedCreep == null)
            {
                Debug.Log($"[HeroBase] No se otorga XP: IsMine={photonView.IsMine}, Creep={killedCreep != null}");
                return;
            }
            
            // Usar la experiencia del creep asesinado
            float creepXP = killedCreep.experienceReward;
            Debug.Log($"[HeroBase] {heroName} recibirá {creepXP} XP por matar a {killedCreep.creepName}");
            
            // Encontrar héroes aliados cercanos para compartir XP
            var nearbyAllies = Physics.OverlapSphere(transform.position, xpRangeRadius)
                                    .Select(c => c.GetComponent<HeroBase>())
                                    .Where(h => h != null && h.teamId == this.teamId)
                                    .ToList();
            
            Debug.Log($"[HeroBase] Héroes aliados cercanos: {nearbyAllies.Count}");
            
            // Dividir la XP entre los héroes cercanos
            float sharedXP = creepXP / nearbyAllies.Count;
            Debug.Log($"[HeroBase] XP compartida: {sharedXP} por héroe");
            
            foreach (var ally in nearbyAllies)
            {
                if (ally != null)
                {
                    ally.GainExperience(sharedXP);
                    Debug.Log($"[HeroBase] {ally.heroName} recibió {sharedXP} XP");
                }
            }
        }
        
        /// <summary>
        /// Otorga experiencia por matar un héroe
        /// </summary>
        public void AwardHeroKillExperience(HeroBase killedHero)
        {
            if (!photonView.IsMine || killedHero == null)
            {
                Debug.Log($"[HeroBase] No se otorga XP por matar héroe: IsMine={photonView.IsMine}, KilledHero={killedHero?.heroName ?? "null"}");
                return;
            }
            
            // Calcular XP base más bonus por nivel
            float xpReward = baseHeroKillXP + (baseHeroKillXP * killedHero.CurrentLevel * heroLevelXPMultiplier);
            Debug.Log($"[HeroBase] {heroName} recibirá {xpReward} XP por matar a {killedHero.heroName} (nivel {killedHero.CurrentLevel})");
            
            // Encontrar héroes aliados cercanos para asistencias
            var nearbyAllies = Physics.OverlapSphere(transform.position, xpRangeRadius)
                                    .Select(c => c.GetComponent<HeroBase>())
                                    .Where(h => h != null && h.teamId == this.teamId && h != this)
                                    .ToList();
            
            Debug.Log($"[HeroBase] Héroes aliados cercanos para asistencia: {nearbyAllies.Count}");
            
            // Otorgar XP al asesino
            GainExperience(xpReward);
            
            // Otorgar XP de asistencia
            float assistXP = xpReward * assistXPMultiplier;
            foreach (var ally in nearbyAllies)
            {
                Debug.Log($"[HeroBase] Otorgando {assistXP} XP de asistencia a {ally.heroName}");
                ally.GainExperience(assistXP);
            }
        }
        
        /// <summary>
        /// Otorga oro por matar un héroe
        /// </summary>
        public void AwardHeroKillGold(HeroBase killedHero)
        {
            if (!photonView.IsMine || killedHero == null) return;
            
            // Actualizar racha de asesinatos
            UpdateKillStreak();
            
            // Calcular oro base más bonus por nivel del héroe asesinado
            float goldReward = baseHeroKillGold + (baseHeroKillGold * killedHero.CurrentLevel * heroLevelGoldMultiplier);
            
            // Añadir bonus por racha de víctima (si tiene racha alta, vale más oro)
            if (killedHero.currentKillStreak > 1)
            {
                float streakBonus = baseHeroKillGold * (killedHero.currentKillStreak * killStreakGoldMultiplier);
                goldReward += streakBonus;
                Debug.Log($"[HeroBase] Bonus por racha de víctima: +{streakBonus} oro (racha: {killedHero.currentKillStreak})");
            }
            
            // Encontrar héroes aliados cercanos para asistencias
            var nearbyAllies = Physics.OverlapSphere(transform.position, xpRangeRadius)
                                    .Select(c => c.GetComponent<HeroBase>())
                                    .Where(h => h != null && h.teamId == this.teamId && h != this)
                                    .ToList();
            
            // Crear mensaje descriptivo para el oro ganado
            string killMessage = $"¡{killedHero.heroName} eliminado!";
            
            // IMPORTANTE: Obtener posición exacta del héroe asesinado para el efecto visual
            Vector3 killedHeroPosition = killedHero.transform.position + Vector3.up * 1.5f;
            
            // Reproducir el sonido de oro directamente si existe (como en NeutralCreep)
            if (goldSound != null)
            {
                // Forzar la reproducción del sonido de oro directamente como sonido GLOBAL
                Debug.Log("[HeroBase] Forzando reproducción del sonido de oro GLOBAL por matar a un héroe");
                PlaySound(goldSound, 1.5f, true); // Force global = true
            }
            
            // Otorgar oro al asesino
            AddGold(goldReward, killMessage, killedHeroPosition);
            Debug.Log($"[HeroBase] {heroName} recibió {goldReward} oro por matar a {killedHero.heroName}");
            
            // Otorgar oro de asistencia
            float assistGold = goldReward * assistGoldMultiplier;
            foreach (var ally in nearbyAllies)
            {
                // Usar la misma posición para los efectos de oro de asistencia
                ally.AddGold(assistGold, $"Asistencia: {killedHero.heroName}", killedHeroPosition);
                Debug.Log($"[HeroBase] {ally.heroName} recibió {assistGold} oro por asistencia");
            }
        }
        
        /// <summary>
        /// Actualiza la racha de asesinatos del héroe
        /// </summary>
        private void UpdateKillStreak()
        {
            // Verificar si la última muerte fue reciente para continuar la racha
            if (Time.time - lastKillTime > killStreakTimeout)
            {
                currentKillStreak = 0; // Reiniciar racha si pasó mucho tiempo
            }
            
            // Incrementar la racha y actualizar tiempo
            currentKillStreak++;
            lastKillTime = Time.time;
            
            // Anunciar rachas importantes
            if (currentKillStreak >= 3)
            {
                string streakMessage = GetKillStreakMessage(currentKillStreak);
                Debug.Log($"[HeroBase] {heroName}: {streakMessage}");
                
                // Aquí se podría implementar un anuncio en el chat o UI
            }
        }
        
        /// <summary>
        /// Obtiene un mensaje apropiado según la racha de asesinatos
        /// </summary>
        private string GetKillStreakMessage(int streak)
        {
            switch (streak)
            {
                case 3: return "¡Triple Kill!";
                case 4: return "¡Quadra Kill!";
                case 5: return "¡Penta Kill!";
                case 6: return "¡Unstoppable!";
                case 7: return "¡Godlike!";
                case 8: return "¡Legendary!";
                default:
                    if (streak > 8) return "¡DOMINACIÓN TOTAL!";
                    return $"¡{streak} kills!";
            }
        }
        
        /// <summary>
        /// Reinicia la racha de asesinatos al morir
        /// </summary>
        private void ResetKillStreak()
        {
            if (currentKillStreak >= 3)
            {
                Debug.Log($"[HeroBase] {heroName}: ¡Racha de {currentKillStreak} kills terminada!");
            }
            currentKillStreak = 0;
            lastKillTime = 0f;
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
            Debug.Log($"[HeroBase] Intentando ganar {amount} XP. IsMine: {photonView?.IsMine}, Hero: {heroName}");
            
            if (!photonView.IsMine || amount <= 0)
            {
                Debug.Log($"[HeroBase] No se otorga XP: IsMine={photonView?.IsMine}, amount={amount}");
                return;
            }
            
            float experienceNeeded = GetExperienceForNextLevel();
            _currentExperience += amount;
            
            Debug.Log($"[HeroBase] XP actual: {_currentExperience}, XP necesaria: {experienceNeeded}");
            
            // Notificar ganancia de experiencia
            OnExperienceGained?.Invoke(amount, _currentExperience, experienceNeeded);
            
            // Verificar si subimos de nivel
            while (_currentExperience >= experienceNeeded && CurrentLevel < heroData.MaxLevel)
            {
                // Calcular experiencia sobrante para el siguiente nivel
                float excessExperience = _currentExperience - experienceNeeded;
                
                Debug.Log($"[HeroBase] Subiendo de nivel de {CurrentLevel} a {CurrentLevel + 1} con {excessExperience} XP sobrante");
                
                CurrentLevel++;
                _currentLevel = CurrentLevel;
                
                // Otorgar puntos de habilidad
                AddSkillPoint(heroData.SkillPointsPerLevel);
                
                // Actualizar stats basados en el nuevo nivel
                UpdateStatsForLevel();
                
                // Reproducir sonido de subida de nivel
                PlayLevelUpSound();
                
                // Mostrar efecto visual de subida de nivel
                ShowLevelUpEffect();
                
                // Notificar la subida de nivel
                OnLevelUp?.Invoke(CurrentLevel);
                
                // Calcular la experiencia necesaria para el siguiente nivel
                experienceNeeded = GetExperienceForNextLevel();
                
                // Actualizar la experiencia actual con el excedente
                _currentExperience = excessExperience;
                
                Debug.Log($"[HeroBase] Nuevo nivel: {CurrentLevel}, XP necesaria para siguiente nivel: {experienceNeeded}, XP actual: {_currentExperience}");
                
                // Notificar la actualización de la UI con la nueva experiencia (incluyendo el excedente)
                OnExperienceGained?.Invoke(0, _currentExperience, experienceNeeded);
            }
            
            // Si estamos al máximo nivel, mantener la experiencia al máximo
            if (CurrentLevel >= heroData.MaxLevel)
            {
                _currentExperience = experienceNeeded;
                Debug.Log($"[HeroBase] Alcanzado nivel máximo ({CurrentLevel})");
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
        /// Actualiza las estadísticas basadas en el nivel actual
        /// </summary>
        private void UpdateStatsForLevel()
        {
            if (heroData == null) return;
            
            // Incrementar atributos base en 2 puntos cada uno al subir de nivel
            heroData.BaseStrength += 2;
            heroData.BaseAgility += 2;
            heroData.BaseIntelligence += 2;
            
            Debug.Log($"[HeroBase] {heroName} subió de nivel. Nuevos atributos base: " +
                      $"Fuerza={heroData.BaseStrength}, " +
                      $"Agilidad={heroData.BaseAgility}, " +
                      $"Inteligencia={heroData.BaseIntelligence}");
            
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

        // Método para añadir oro al jugador
        public void AddGold(float amount, string sourceText = "", Vector3? sourcePosition = null)
        {
            if (!photonView.IsMine) return;
            
            _currentGold += amount;
            Debug.Log($"[HeroBase] {heroName} ganó {amount} de oro. Total: {_currentGold}");
            
            // Notificar a la UI si existe
            if (uiController != null)
            {
                // Verificar que el objeto de la UI esté activo
                if (!uiController.gameObject.activeInHierarchy)
                {
                    Debug.LogWarning($"[HeroBase] El UI Canvas del héroe {heroName} está inactivo, intentando activarlo");
                    uiController.gameObject.SetActive(true);
                }
                
                // Actualizar contador de oro
                uiController.UpdateGoldText(_currentGold);
                
                // Mostrar texto flotante con el oro ganado en la posición del origen
                uiController.ShowGoldRewardText(amount, sourceText, sourcePosition);
            }
            else
            {
                Debug.LogError($"[HeroBase] No se pudo mostrar recompensa de oro: uiController es null para {heroName}");
            }
            
            // PRUEBA: Reproducir sonido como 2D global
            if (amount > 0)
            {
                AudioClip clipToPlay = null;
                
                // Seleccionar el sonido apropiado según la cantidad
                if (amount >= 100 && bigGoldSound != null)
                {
                    clipToPlay = bigGoldSound;
                    Debug.Log("[HeroBase] Usando sonido de oro GRANDE");
                }
                else if (amount < 20 && smallGoldSound != null)
                {
                    clipToPlay = smallGoldSound;
                    Debug.Log("[HeroBase] Usando sonido de oro PEQUEÑO");
                }
                else if (goldSound != null)
                {
                    clipToPlay = goldSound;
                    Debug.Log("[HeroBase] Usando sonido de oro NORMAL");
                }
                
                // Reproducir el sonido como GLOBAL con volumen alto
                if (clipToPlay != null)
                {
                    Debug.Log($"[HeroBase] Intentando reproducir sonido de oro: {clipToPlay.name}");
                    // Usar forceGlobal=true para reproducir como sonido 2D global
                    PlaySound(clipToPlay, 1.0f, true);
                }
                else
                {
                    Debug.LogError($"[HeroBase] Error: No hay sonido de oro asignado");
                }
            }
        }
        
        // Método para usar oro (por ejemplo, en compras)
        public bool SpendGold(float amount)
        {
            if (!photonView.IsMine) return false;
            
            // Verificar si hay suficiente oro
            if (_currentGold < amount)
            {
                Debug.Log($"[HeroBase] {heroName} no tiene suficiente oro. Necesita: {amount}, Disponible: {_currentGold}");
                return false;
            }
            
            _currentGold -= amount;
            Debug.Log($"[HeroBase] {heroName} gastó {amount} de oro. Restante: {_currentGold}");
            
            // Notificar a la UI si existe
            if (uiController != null)
            {
                uiController.UpdateGoldText(_currentGold);
            }
            
            return true;
        }

        // Método para reproducir explícitamente un efecto de sonido
        public void PlaySound(AudioClip sound, float volume = 1.0f, bool forceGlobal = false)
        {
            if (sound == null)
            {
                Debug.LogError($"[HeroBase] Intento de reproducir un sonido nulo en {heroName}");
                return;
            }
            
            // PRUEBA: Crear un AudioSource temporal para cada sonido
            if (forceGlobal)
            {
                // Crear un objeto separado para reproducir el sonido
                GameObject audioObj = new GameObject("TempAudio_" + sound.name);
                audioObj.transform.position = Camera.main ? Camera.main.transform.position : transform.position;
                AudioSource tempSource = audioObj.AddComponent<AudioSource>();
                
                // Configurar como sonido global (2D)
                tempSource.spatialBlend = 0f; // 0 = 2D, 1 = 3D
                tempSource.volume = 1.0f;
                tempSource.priority = 0; // Alta prioridad
                tempSource.clip = sound;
                tempSource.Play();
                
                // Destruir después de reproducir
                Destroy(audioObj, sound.length + 0.5f);
                
                Debug.Log($"[HeroBase] Reproduciendo sonido en fuente TEMPORAL: {sound.name} (vol={volume})");
                return;
            }
            
            // Método original usando el AudioSource del objeto
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 0f; // Cambiado a 0 para hacer que sea 2D (global)
                audioSource.volume = 1.0f;
                audioSource.priority = 128; // Prioridad media (0 es la más alta)
                Debug.Log($"[HeroBase] Creado nuevo AudioSource para {heroName}");
            }
            
            // Configurar audio source para asegurar que se escuche
            audioSource.mute = false;
            audioSource.spatialBlend = 0f; // Forzar a 2D para pruebas
            audioSource.volume = 1.0f; // Aumentar volumen base
            
            // Reproducir el sonido con volumen más alto
            audioSource.PlayOneShot(sound, volume * 2.0f); // Duplicar volumen para pruebas
            
            Debug.Log($"[HeroBase] Reproduciendo sonido: {sound.name} con volumen {volume * 2.0f}");
        }
        
        // Método de depuración para el sonido de oro
        public void DebugGoldSound()
        {
            Debug.Log($"[HeroBase] Depuración de sonido de oro:");
            Debug.Log($"[HeroBase] goldSound asignado: {(goldSound != null ? "SÍ" : "NO")}");
            Debug.Log($"[HeroBase] smallGoldSound asignado: {(smallGoldSound != null ? "SÍ" : "NO")}");
            Debug.Log($"[HeroBase] bigGoldSound asignado: {(bigGoldSound != null ? "SÍ" : "NO")}");
            Debug.Log($"[HeroBase] audioSource presente: {(audioSource != null ? "SÍ" : "NO")}");
            
            if (audioSource != null)
            {
                Debug.Log($"[HeroBase] audioSource.mute: {audioSource.mute}");
                Debug.Log($"[HeroBase] audioSource.volume: {audioSource.volume}");
                Debug.Log($"[HeroBase] audioSource.enabled: {audioSource.enabled}");
            }
            
            // Reproducir cada sonido disponible para probar
            if (goldSound != null)
            {
                Debug.Log("[HeroBase] Probando sonido de oro normal...");
                PlaySound(goldSound, 1.0f);
            }
            
            // Esperar un momento y reproducir el siguiente sonido
            StartCoroutine(PlayDelayedSmallGoldSound());
        }
        
        private IEnumerator PlayDelayedSmallGoldSound()
        {
            yield return new WaitForSeconds(1.0f);
            
            if (smallGoldSound != null)
            {
                Debug.Log("[HeroBase] Probando sonido de oro pequeño...");
                PlaySound(smallGoldSound, 1.0f);
            }
            
            yield return new WaitForSeconds(1.0f);
            
            if (bigGoldSound != null)
            {
                Debug.Log("[HeroBase] Probando sonido de oro grande...");
                PlaySound(bigGoldSound, 1.0f);
            }
        }

        // Método para probar los sonidos de oro inmediatamente
        public void TestGoldSounds()
        {
            // Intentar cada método posible para reproducir el sonido
            Debug.Log("========== PRUEBA DE SONIDOS DE ORO ==========");
            
            // 1. Prueba con AudioSource normal
            if (goldSound != null)
            {
                Debug.Log("1. Probando con AudioSource normal");
                
                // Asegurar que tenemos AudioSource
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
                
                // Configurarlo para reproducción óptima
                audioSource.spatialBlend = 0f; // 2D
                audioSource.volume = 1.0f;
                audioSource.pitch = 1.0f;
                audioSource.mute = false;
                
                // Reproducir directamente
                audioSource.PlayOneShot(goldSound, 1.0f);
                Debug.Log("   Sonido reproducido con PlayOneShot");
            }
            
            // 2. Prueba con AudioSource temporal
            if (goldSound != null)
            {
                Debug.Log("2. Probando con AudioSource temporal");
                GameObject tempObj = new GameObject("TempAudioTest");
                tempObj.transform.position = Camera.main ? Camera.main.transform.position : transform.position;
                
                AudioSource tempSource = tempObj.AddComponent<AudioSource>();
                tempSource.spatialBlend = 0f; // 2D
                tempSource.volume = 1.0f;
                tempSource.priority = 0;
                tempSource.clip = goldSound;
                tempSource.Play();
                
                Destroy(tempObj, goldSound.length + 0.5f);
                Debug.Log("   Sonido reproducido con AudioSource temporal");
            }
            
            // 3. Prueba con AudioSource.PlayClipAtPoint
            if (goldSound != null)
            {
                Debug.Log("3. Probando con PlayClipAtPoint");
                Vector3 cameraPos = Camera.main ? Camera.main.transform.position : transform.position;
                AudioSource.PlayClipAtPoint(goldSound, cameraPos, 1.0f);
                Debug.Log("   Sonido reproducido con PlayClipAtPoint");
            }
            
            // 4. Prueba con nuestro método mejorado
            if (goldSound != null)
            {
                Debug.Log("4. Probando con nuestro método PlaySound");
                PlaySound(goldSound, 1.0f, true);
                Debug.Log("   Sonido reproducido con PlaySound");
            }
            
            // Información de depuración
            if (goldSound != null)
            {
                Debug.Log($"Información del clip de oro: Nombre={goldSound.name}, Duración={goldSound.length}s, Canales={goldSound.channels}, Frecuencia={goldSound.frequency}Hz");
            }
            else
            {
                Debug.LogError("ERROR: No hay clip de sonido de oro asignado");
            }
            
            Debug.Log("===========================================");
        }

        /// <summary>
        /// Fuerza la sincronización de la capa "Player" en todos los clientes
        /// </summary>
        public void ForceSynchronizePlayerLayer()
        {
            if (!photonView.IsMine) return;
            
            // Verificar si la capa actual es incorrecta
            if (gameObject.layer != LayerManager.PlayerLayerID)
            {
                Debug.LogWarning($"[HeroBase] Capa incorrecta detectada: {LayerMask.LayerToName(gameObject.layer)}. Forzando sincronización a {LayerManager.LAYER_PLAYER}");
                
                // Aplicar localmente primero
                LayerManager.SetLayerRecursively(transform, LayerManager.PlayerLayerID);
                
                // Luego forzar en todos los clientes
                photonView.RPC("RPC_ForcePlayerLayer", RpcTarget.AllBuffered);
            }
        }
        
        [PunRPC]
        private void RPC_ForcePlayerLayer()
        {
            // No importa quién lo llame, siempre aplicar la capa Player
            Debug.Log($"[HeroBase] RPC_ForcePlayerLayer recibido. Capa actual: {LayerMask.LayerToName(gameObject.layer)}");
            
            // Aplicar la capa Player recursivamente
            LayerManager.SetLayerRecursively(transform, LayerManager.PlayerLayerID);
            
            // Verificación directa en GameObject y sus hijos principales
            gameObject.layer = LayerManager.PlayerLayerID;
            foreach (Transform child in transform)
            {
                if (child != null)
                {
                    child.gameObject.layer = LayerManager.PlayerLayerID;
                    // También aplicar a nietos importantes
                    foreach (Transform subChild in child)
                    {
                        if (subChild != null)
                            subChild.gameObject.layer = LayerManager.PlayerLayerID;
                    }
                }
            }
            
            // Si teníamos un tracker de visibilidad, asegurarnos de que no esté marcado como "en arbusto"
            EntityVisibilityTracker tracker = GetComponent<EntityVisibilityTracker>();
            if (tracker != null && tracker.IsInBush)
            {
                Debug.Log("[HeroBase] RPC_ForcePlayerLayer: Detectado IsInBush=true pero estamos forzando capa Player");
                if (tracker.CurrentBush != null)
                {
                    tracker.ExitBush(tracker.CurrentBush);
                }
            }
            
            Debug.Log($"[HeroBase] Capa corregida a {LayerManager.LAYER_PLAYER} en RPC_ForcePlayerLayer");
        }
        
        /// <summary>
        /// Reproduce el sonido de subida de nivel
        /// </summary>
        private void PlayLevelUpSound()
        {
            if (audioSource != null && levelUpSound != null)
            {
                // Asegurar que el volumen esté alto y que se escuche sin importar la distancia
                float originalSpatialBlend = audioSource.spatialBlend;
                float originalVolume = audioSource.volume;
                
                // Configurar para reproducción clara
                audioSource.spatialBlend = 0f; // 0 = 2D (no espacial)
                audioSource.volume = 1.0f;
                audioSource.pitch = 1.0f;
                
                // Reproducir sonido
                audioSource.PlayOneShot(levelUpSound);
                
                // Programar la restauración de los valores originales
                StartCoroutine(RestoreAudioSourceSettings(originalSpatialBlend, originalVolume));
                
                Debug.Log($"[HeroBase] Reproduciendo sonido de subida de nivel");
                
                // También notificar a otros clientes para que reproduzcan el sonido
                photonView.RPC("RPC_PlayLevelUpSound", RpcTarget.Others);
            }
            else
            {
                Debug.LogWarning($"[HeroBase] No se pudo reproducir el sonido de nivel: audioSource={audioSource != null}, levelUpSound={levelUpSound != null}");
            }
        }
        
        /// <summary>
        /// Restaura la configuración original del AudioSource después de reproducir un sonido especial
        /// </summary>
        private IEnumerator RestoreAudioSourceSettings(float originalSpatialBlend, float originalVolume)
        {
            yield return new WaitForSeconds(1.0f);
            
            if (audioSource != null)
            {
                audioSource.spatialBlend = originalSpatialBlend;
                audioSource.volume = originalVolume;
            }
        }
        
        /// <summary>
        /// RPC para reproducir el sonido de subida de nivel en otros clientes
        /// </summary>
        [PunRPC]
        private void RPC_PlayLevelUpSound()
        {
            if (audioSource != null && levelUpSound != null && !photonView.IsMine)
            {
                audioSource.spatialBlend = 0f;
                audioSource.volume = 1.0f;
                audioSource.pitch = 1.0f;
                audioSource.PlayOneShot(levelUpSound);
            }
        }
        
        /// <summary>
        /// Muestra un efecto visual cuando el héroe sube de nivel
        /// </summary>
        private void ShowLevelUpEffect()
        {
            // Si tenemos un prefab de efecto de nivel, instanciarlo
            if (levelUpEffectPrefab != null)
            {
                // Instanciar el prefab en la posición del héroe
                GameObject effect = Instantiate(levelUpEffectPrefab, transform.position + Vector3.up, Quaternion.identity);
                
                // Configurar el efecto para que siga al héroe si es necesario
                effect.transform.SetParent(transform);
                
                // Ajustar color de las partículas si es posible
                ParticleSystem[] particleSystems = effect.GetComponentsInChildren<ParticleSystem>();
                foreach (ParticleSystem ps in particleSystems)
                {
                    var main = ps.main;
                    main.startColor = levelUpEffectColor;
                }
                
                // Destruir después de la duración configurada
                Destroy(effect, levelUpEffectDuration);
                
                Debug.Log($"[HeroBase] Mostrando efecto visual de subida de nivel para {heroName}");
                
                // Sincronizar con todos los clientes
                photonView.RPC("RPC_ShowLevelUpEffect", RpcTarget.Others);
            }
            else
            {
                // Si no tenemos prefab, crear un efecto básico de partículas en tiempo de ejecución
                CreateDynamicLevelUpEffect();
                
                // Sincronizar con todos los clientes - asegurar que todos ven el efecto incluso sin prefab
                photonView.RPC("RPC_ShowLevelUpEffect", RpcTarget.Others);
            }
        }
        
        /// <summary>
        /// Crea un efecto dinámico de partículas si no hay prefab asignado
        /// </summary>
        private void CreateDynamicLevelUpEffect()
        {
            // Crear objeto para el sistema de partículas principal
            GameObject effectObj = new GameObject("LevelUpEffect");
            effectObj.transform.position = transform.position;
            effectObj.transform.SetParent(transform);
            
            // Crear anillos alrededor del héroe
            StartCoroutine(CreateLevelUpRingsSequentially(effectObj));
            
            // Destruir después de completar
            Destroy(effectObj, levelUpEffectDuration);
        }
        
        /// <summary>
        /// Crea los anillos secuencialmente para evitar errores de partículas
        /// </summary>
        private IEnumerator CreateLevelUpRingsSequentially(GameObject parent)
        {
            // Crear los anillos con un pequeño retraso entre ellos
            yield return StartCoroutine(CreateLevelUpRing(parent, 0, 0.7f));
            yield return new WaitForSeconds(0.1f);
            yield return StartCoroutine(CreateLevelUpRing(parent, 1, 1.0f));
            yield return new WaitForSeconds(0.1f);
            yield return StartCoroutine(CreateLevelUpRing(parent, 2, 1.3f));
            yield return new WaitForSeconds(0.1f);
            
            // Añadir partículas centrales al final
            CreateSimpleCentralEffect(parent);
        }
        
        /// <summary>
        /// Crea un anillo de escáner para el efecto de subida de nivel
        /// </summary>
        private IEnumerator CreateLevelUpRing(GameObject parent, int ringIndex, float radius)
        {
            // Crear objeto para el anillo
            GameObject ringObj = new GameObject($"LevelUpRing_{ringIndex}");
            ringObj.transform.position = transform.position + new Vector3(0, 1.0f, 0); // Centrado a la altura de los hombros
            ringObj.transform.SetParent(parent.transform);
            
            // Añadir sistema de partículas
            ParticleSystem ps = ringObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            
            // Primero inicializamos el sistema con valores por defecto
            main.startLifetime = 1f;
            main.startSize = 0.1f;
            main.startSpeed = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 100;
            main.loop = true;
            
            var emission = ps.emission;
            emission.rateOverTime = 20;
            
            // Forma de emisión en círculo
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;
            shape.radiusThickness = 0f; // Emitir solo desde el borde
            shape.arc = 360f;
            
            // Color a lo largo del tiempo para efecto de brillo
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(levelUpEffectColor, 0f),
                    new GradientColorKey(Color.white, 0.5f),
                    new GradientColorKey(levelUpEffectColor, 1f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.1f),
                    new GradientAlphaKey(1f, 0.9f),
                    new GradientAlphaKey(0f, 1f) 
                }
            );
            colorOverLifetime.color = grad;
            
            // Tamaño de partículas
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve(
                new Keyframe(0f, 0.3f),
                new Keyframe(0.5f, 1f),
                new Keyframe(1f, 0.3f)
            );
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
            
            // Renderizador de partículas
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            
            // Iniciamos el sistema
            ps.Play();
            
            // Ahora configuramos la duración mientras está detenido
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            
            yield return null; // Esperamos un frame para asegurar que se detiene
            
            main.duration = levelUpEffectDuration;
            
            // Y reiniciamos el sistema
            ps.Play();
            
            // Animar el movimiento vertical del anillo
            float startDelay = ringIndex * 0.3f;
            StartCoroutine(AnimateRingPosition(ringObj, startDelay));
            
            yield return null;
        }
        
        /// <summary>
        /// Crea un efecto simple en el centro para la subida de nivel
        /// </summary>
        private void CreateSimpleCentralEffect(GameObject parent)
        {
            // Crear objeto para el efecto central
            GameObject centerObj = new GameObject("LevelUpCenter");
            centerObj.transform.position = transform.position + new Vector3(0, 0.5f, 0);
            centerObj.transform.SetParent(parent.transform);
            
            // Añadir sistema de partículas
            ParticleSystem ps = centerObj.AddComponent<ParticleSystem>();
            
            // Configuración básica
            var main = ps.main;
            main.startColor = new Color(levelUpEffectColor.r, levelUpEffectColor.g, levelUpEffectColor.b, 0.5f); // Más transparente
            main.startSize = 0.08f; // Partículas más pequeñas
            main.startSpeed = 1.5f; // Velocidad más lenta
            main.startLifetime = 1.5f;
            main.maxParticles = 30; // Menos partículas
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            // Emisión
            var emission = ps.emission;
            emission.rateOverTime = 10; // Menos partículas por segundo
            
            // Forma (disparo hacia arriba más sutil)
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 10f; // Ángulo más cerrado
            shape.radius = 0.05f; // Radio más pequeño
            shape.radiusThickness = 1f;
            
            // Tamaño de partículas
            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve(
                new Keyframe(0f, 0.3f),
                new Keyframe(0.3f, 0.7f),
                new Keyframe(1f, 0f)
            );
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
            
            // Color
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(levelUpEffectColor, 0f),
                    new GradientColorKey(Color.white, 0.5f),
                    new GradientColorKey(levelUpEffectColor, 1f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0.4f, 0f), // Más transparente
                    new GradientAlphaKey(0.3f, 0.5f), // Más transparente
                    new GradientAlphaKey(0f, 1f) 
                }
            );
            colorOverLifetime.color = grad;
            
            // Renderizador - aseguramos que las partículas sean redondas
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            
            // Establecer textura circular para las partículas
            renderer.material.SetTexture("_MainTex", CreateCircleTexture());
            
            // Iniciar el sistema
            ps.Play();
        }
        
        /// <summary>
        /// Crea una textura circular para las partículas
        /// </summary>
        private Texture2D CreateCircleTexture()
        {
            // Crear una textura de 32x32 píxeles
            Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            
            // Radio del círculo
            float radius = 15f;
            float radiusSquared = radius * radius;
            
            // Centro de la textura
            Vector2 center = new Vector2(15.5f, 15.5f);
            
            // Dibujar un círculo suave
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float distSquared = (x - center.x) * (x - center.x) + (y - center.y) * (y - center.y);
                    
                    // Calcular la transparencia basada en la distancia al centro
                    float alpha = 0f;
                    if (distSquared <= radiusSquared)
                    {
                        // Suavizado de bordes
                        float dist = Mathf.Sqrt(distSquared);
                        if (dist < radius - 2f)
                            alpha = 1f;
                        else
                            alpha = 1f - ((dist - (radius - 2f)) / 2f);
                    }
                    
                    // Establecer el color con transparencia
                    Color color = new Color(1f, 1f, 1f, alpha);
                    texture.SetPixel(x, y, color);
                }
            }
            
            // Aplicar los cambios a la textura
            texture.Apply();
            return texture;
        }
        
        /// <summary>
        /// Anima la posición del anillo para crear efecto de escaneo
        /// </summary>
        private IEnumerator AnimateRingPosition(GameObject ring, float startDelay)
        {
            if (startDelay > 0)
                yield return new WaitForSeconds(startDelay);
            
            float duration = levelUpEffectDuration - startDelay;
            float elapsed = 0f;
            float height = 1.8f; // Altura total del movimiento
            Vector3 basePosition = ring.transform.localPosition;
            float baseY = basePosition.y;
            
            while (elapsed < duration && ring != null)
            {
                // Calcular posición Y con movimiento oscilante
                float t = elapsed / 2.0f; // Ciclo más rápido que la duración total
                float yPos = baseY + Mathf.PingPong(t, height) - height/2;
                
                if (ring != null)
                {
                    ring.transform.localPosition = new Vector3(0, yPos, 0);
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        /// <summary>
        /// RPC para mostrar el efecto de subida de nivel en otros clientes
        /// </summary>
        [PunRPC]
        private void RPC_ShowLevelUpEffect()
        {
            if (!photonView.IsMine)
            {
                Debug.Log($"[HeroBase] RPC_ShowLevelUpEffect recibido para {heroName}");
                
                // Comprobamos si existe un prefab de efecto de nivel para este héroe
                if (levelUpEffectPrefab != null)
                {
                    // Instanciar el prefab en la posición del héroe
                    GameObject effect = Instantiate(levelUpEffectPrefab, transform.position + Vector3.up, Quaternion.identity);
                    
                    // Configurar el efecto para que siga al héroe
                    effect.transform.SetParent(transform);
                    
                    // Ajustar color de las partículas
                    ParticleSystem[] particleSystems = effect.GetComponentsInChildren<ParticleSystem>();
                    foreach (ParticleSystem ps in particleSystems)
                    {
                        var main = ps.main;
                        main.startColor = levelUpEffectColor;
                    }
                    
                    // Destruir después de la duración configurada
                    Destroy(effect, levelUpEffectDuration);
                }
                else
                {
                    // Crear un efecto dinámico en clientes remotos
                    CreateDynamicLevelUpEffect();
                }
            }
        }

        /// <summary>
        /// Añade experiencia al héroe y maneja la subida de nivel
        /// </summary>
        public void AddExperience(float amount)
        {
            Debug.Log($"[HeroBase] Intentando ganar {amount} XP. IsMine: {photonView?.IsMine}, Hero: {heroName}");
            
            if (!photonView.IsMine || amount <= 0)
            {
                Debug.Log($"[HeroBase] No se otorga XP: IsMine={photonView?.IsMine}, amount={amount}");
                return;
            }
            
            float experienceNeeded = GetExperienceForNextLevel();
            _currentExperience += amount;
            
            Debug.Log($"[HeroBase] XP actual: {_currentExperience}, XP necesaria: {experienceNeeded}");
            
            // Notificar ganancia de experiencia
            OnExperienceGained?.Invoke(amount, _currentExperience, experienceNeeded);
            
            // Verificar si subimos de nivel
            while (_currentExperience >= experienceNeeded && CurrentLevel < heroData.MaxLevel)
            {
                // Calcular experiencia sobrante para el siguiente nivel
                float excessExperience = _currentExperience - experienceNeeded;
                
                Debug.Log($"[HeroBase] Subiendo de nivel de {CurrentLevel} a {CurrentLevel + 1} con {excessExperience} XP sobrante");
                
                CurrentLevel++;
                _currentLevel = CurrentLevel;
                
                // Otorgar puntos de habilidad
                AddSkillPoint(heroData.SkillPointsPerLevel);
                
                // Actualizar stats basados en el nuevo nivel
                UpdateStatsForLevel();
                
                // Reproducir sonido de subida de nivel
                PlayLevelUpSound();
                
                // Mostrar efecto visual de subida de nivel
                ShowLevelUpEffect();
                
                // Notificar la subida de nivel
                OnLevelUp?.Invoke(CurrentLevel);
                
                // Calcular la experiencia necesaria para el siguiente nivel
                experienceNeeded = GetExperienceForNextLevel();
                
                // Actualizar la experiencia actual con el excedente
                _currentExperience = excessExperience;
                
                Debug.Log($"[HeroBase] Nuevo nivel: {CurrentLevel}, XP necesaria para siguiente nivel: {experienceNeeded}, XP actual: {_currentExperience}");
                
                // Notificar la actualización de la UI con la nueva experiencia (incluyendo el excedente)
                OnExperienceGained?.Invoke(0, _currentExperience, experienceNeeded);
            }
            
            // Si estamos al máximo nivel, mantener la experiencia al máximo
            if (CurrentLevel >= heroData.MaxLevel)
            {
                _currentExperience = experienceNeeded;
                Debug.Log($"[HeroBase] Alcanzado nivel máximo ({CurrentLevel})");
            }
        }

        /// <summary>
        /// Otorga experiencia al héroe cuando un objetivo muere.
        /// </summary>
        public void GainExperience(HeroBase killer, UnitBase dyingObject, float assistMultiplier = 1.0f)
        {
            Debug.Log($"[HeroBase] Intentando otorgar experiencia a {heroName}. IsMine: {photonView?.IsMine}");

            if (!photonView.IsMine) return;

            float experienceGain = 0;
            float experienceNeeded = GetExperienceForNextLevel();

            experienceGain = CalculateExperienceGain(killer, dyingObject, assistMultiplier);

            if (experienceGain <= 0)
            {
                Debug.Log($"[HeroBase] No se otorga XP: experienceGain={experienceGain}");
                return;
            }

            _currentExperience += experienceGain;

            Debug.Log($"[HeroBase] XP ganada: {experienceGain}, XP actual: {_currentExperience}, XP necesaria: {experienceNeeded}");

            // Notificar ganancia de experiencia
            OnExperienceGained?.Invoke(experienceGain, _currentExperience, experienceNeeded);

            // Verificar si subimos de nivel
            while (_currentExperience >= experienceNeeded && CurrentLevel < heroData.MaxLevel)
            {
                // Calcular experiencia sobrante para el siguiente nivel
                float excessExperience = _currentExperience - experienceNeeded;
                
                Debug.Log($"[HeroBase] Subiendo de nivel de {CurrentLevel} a {CurrentLevel + 1} con {excessExperience} XP sobrante");
                
                CurrentLevel++;
                _currentLevel = CurrentLevel;

                // Otorgar puntos de habilidad
                AddSkillPoint(heroData.SkillPointsPerLevel);

                // Actualizar stats basados en el nuevo nivel
                UpdateStatsForLevel();
                
                // Reproducir sonido de subida de nivel
                PlayLevelUpSound();
                
                // Mostrar efecto visual de subida de nivel
                ShowLevelUpEffect();

                // Notificar la subida de nivel
                OnLevelUp?.Invoke(CurrentLevel);

                // Calcular la experiencia necesaria para el siguiente nivel
                experienceNeeded = GetExperienceForNextLevel();
                
                // Actualizar la experiencia actual con el excedente
                _currentExperience = excessExperience;
                
                Debug.Log($"[HeroBase] Nuevo nivel: {CurrentLevel}, XP necesaria para siguiente nivel: {experienceNeeded}, XP actual: {_currentExperience}");
                
                // Notificar la actualización de la UI con la nueva experiencia (incluyendo el excedente)
                OnExperienceGained?.Invoke(0, _currentExperience, experienceNeeded);
            }

            // Si estamos al máximo nivel, mantener la experiencia al máximo
            if (CurrentLevel >= heroData.MaxLevel)
            {
                _currentExperience = experienceNeeded;
                Debug.Log($"[HeroBase] Alcanzado nivel máximo ({CurrentLevel})");
            }
        }

        /// <summary>
        /// Calcula la cantidad de experiencia que se obtiene por matar a una unidad
        /// </summary>
        /// <param name="killer">El héroe que realizó el asesinato</param>
        /// <param name="dyingObject">La unidad que murió</param>
        /// <param name="assistMultiplier">Multiplicador para asistencias (1.0 para asesinato, menor para asistencia)</param>
        /// <returns>La cantidad de experiencia a otorgar</returns>
        private float CalculateExperienceGain(HeroBase killer, UnitBase dyingObject, float assistMultiplier = 1.0f)
        {
            if (dyingObject == null)
            {
                Debug.LogWarning($"[HeroBase] CalculateExperienceGain: dyingObject es null");
                return 0;
            }
            
            Debug.Log($"[HeroBase] Calculando XP por matar a {dyingObject.GetType().Name}");
            
            float baseXP = 0;
            
            // Determinar la XP base según el tipo de unidad
            if (dyingObject is HeroBase dyingHero)
            {
                // XP base para héroe + bonus por nivel
                baseXP = baseHeroKillXP + (baseHeroKillXP * dyingHero.CurrentLevel * heroLevelXPMultiplier);
                Debug.Log($"[HeroBase] XP base por matar un héroe: {baseXP} (nivel {dyingHero.CurrentLevel})");
            }
            else if (dyingObject is NeutralCreep dyingCreep)
            {
                // XP del creep
                baseXP = dyingCreep.experienceReward;
                Debug.Log($"[HeroBase] XP base por matar un creep: {baseXP}");
            }
            else
            {
                // Si es otro tipo de unidad (como Torre), usar la experiencia base para torres
                baseXP = baseTowerXP;
                Debug.Log($"[HeroBase] XP base por destruir otra unidad: {baseXP}");
            }
            
            // Aplicar modificador de asistencia si corresponde
            float modifiedXP = baseXP * assistMultiplier;
            
            Debug.Log($"[HeroBase] XP final (después de multiplicadores): {modifiedXP}");
            
            return modifiedXP;
        }
    }
}