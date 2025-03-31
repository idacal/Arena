using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace Photon.Pun.Demo.Asteroids
{
    public class HeroUIController : MonoBehaviour
    {
        [Header("Health and Mana UI")]
        public Slider healthBar;
        public Slider manaBar;
        public TMP_Text healthText;
        public TMP_Text manaText;
        public GameObject healthRegenPanel;  // Panel containing health regeneration text
        public GameObject manaRegenPanel;    // Panel containing mana regeneration text
        
        [Header("Experience UI")]
        public Slider experienceBar;
        public TMP_Text experienceText;  // Ahora mostrará "XP: actual/necesaria"
        
        [Header("Player Information")]
        public TMP_Text playerNameText;
        public TMP_Text heroNameText;
        public TMP_Text levelText;
        public TMP_Text primaryAttributeText; // New: to show primary attribute
        public TMP_Text goldText; // Para mostrar el oro del jugador
        
        [Header("Base Stats")]
        public TMP_Text strengthText;
        public TMP_Text intelligenceText;
        public TMP_Text agilityText;
        public TMP_Text strengthScalingText;
        public TMP_Text intelligenceScalingText;
        public TMP_Text agilityScalingText;
        
        [Header("Hero Stats")]
        public TMP_Text attackDamageText;
        public TMP_Text attackSpeedText;
        public TMP_Text moveSpeedText;
        public TMP_Text attackRangeText;
        public TMP_Text armorText;
        public TMP_Text magicResistanceText;
        public TMP_Text healthRegenText;
        public TMP_Text manaRegenText;
        
        [Header("Ability UI")]
        public GameObject abilityPanel;
        
        [Header("Damage/Heal Text")]
        public GameObject floatingTextPrefab;
        public Color damageColor = Color.red;
        public Color magicDamageColor = Color.magenta;
        public Color healColor = Color.green;
        public Color goldColor = new Color(1f, 0.84f, 0f); // Color dorado para recompensas de oro
        
        [Header("Positioning")]
        public Vector3 floatingTextOffset = new Vector3(0, 2, 0);
        public Transform floatingTextParent;
        
        [Header("Canvas Settings")]
        public Canvas mainCanvas;
        public bool forceScreenSpaceOverlay = true;
        
        private Transform cameraTransform;
        private HeroBase heroOwner;
        
        /// <summary>
        /// Inicializa el controlador de UI con el héroe propietario
        /// </summary>
        /// <param name="hero">Referencia al héroe propietario de esta UI</param>
        public void Initialize(HeroBase hero)
        {
            heroOwner = hero;
            Debug.Log($"[HeroUIController] Initialized for hero: {hero.heroName}");
            
            // Initialize health and mana bars with initial values
            if (hero != null)
            {
                UpdateHealthBar(hero.CurrentHealth, hero.MaxHealth);
                UpdateManaBar(hero.currentMana, hero.maxMana);
                UpdateHeroStats(hero);
                
                // Inicializar la barra de experiencia
                if (hero.heroData != null)
                {
                    Debug.Log($"[HeroUIController] Calculando experiencia necesaria:");
                    Debug.Log($"[HeroUIController] BaseExperience: {hero.heroData.BaseExperience}");
                    Debug.Log($"[HeroUIController] ExperienceScaling: {hero.heroData.ExperienceScaling}");
                    Debug.Log($"[HeroUIController] CurrentLevel: {hero.CurrentLevel}");
                    
                    // Asegurarnos de que el nivel sea al menos 1
                    int level = Mathf.Max(1, hero.CurrentLevel);
                    float experienceNeeded = hero.heroData.BaseExperience * Mathf.Pow(hero.heroData.ExperienceScaling, level - 1);
                    
                    Debug.Log($"[HeroUIController] Nivel usado para el cálculo: {level}");
                    Debug.Log($"[HeroUIController] Experiencia necesaria calculada: {experienceNeeded}");
                    
                    // Asegurarnos de que la experiencia necesaria sea al menos 100
                    experienceNeeded = Mathf.Max(100f, experienceNeeded);
                    
                    UpdateExperienceBar(0, hero.CurrentExperience, experienceNeeded);
                }
                
                // Suscribirse a los eventos
                hero.OnExperienceGained += UpdateExperienceBar;
                hero.OnLevelUp += UpdateLevelText;
                
                // Actualizar el texto del nivel inicial
                UpdateLevelText(hero.CurrentLevel);
                
                // Actualizar el texto del oro inicial
                if (goldText != null)
                {
                    UpdateGoldText(hero.CurrentGold);
                }
                
                if (playerNameText != null && hero.photonView != null && hero.photonView.Owner != null)
                {
                    playerNameText.text = hero.photonView.Owner.NickName;
                }
                
                if (heroNameText != null)
                {
                    SetHeroName(hero.heroName);
                }
            }
        }
        
        void Awake()
        {
            // Buscar referencias si no están asignadas
            if (mainCanvas == null)
            {
                mainCanvas = GetComponent<Canvas>();
                if (mainCanvas == null)
                {
                    mainCanvas = GetComponentInChildren<Canvas>();
                }
            }
            
            // Obtener referencia al héroe propietario
            heroOwner = GetComponentInParent<HeroBase>();
            if (heroOwner == null)
            {
                heroOwner = transform.root.GetComponent<HeroBase>();
            }
            
            if (heroOwner == null)
            {
                Debug.LogError("[HeroUIController] Could not find HeroBase associated with this UI");
            }
            
            // Auto-find UI references if not assigned
            if (heroNameText == null)
            {
                heroNameText = transform.FindDeepChild<TMP_Text>("Hero Name");
                if (heroNameText == null)
                    Debug.LogWarning("[HeroUIController] Could not find Hero Name element");
                else
                    Debug.Log("[HeroUIController] Hero Name found automatically");
            }
            
            if (playerNameText == null)
            {
                playerNameText = transform.FindDeepChild<TMP_Text>("PlayerName");
                if (playerNameText == null)
                    Debug.LogWarning("[HeroUIController] Could not find PlayerName element");
                else
                    Debug.Log("[HeroUIController] PlayerName found automatically");
            }
        }
        
        void Start()
        {
            Debug.Log("[HeroUIController] Initializing UI for: " + (heroOwner != null ? heroOwner.heroName : "Unknown"));
            
            // Obtener la cámara principal
            cameraTransform = Camera.main.transform;
            
            // Configurar el padre de los textos flotantes
            if (floatingTextParent == null)
            {
                floatingTextParent = transform;
            }
            
            // Configurar el canvas
            ConfigureCanvas();
            
            // Verificar referencias importantes
            CheckReferences();
            
            // Ocultar paneles de regeneración al inicio
            if (healthRegenPanel != null)
            {
                healthRegenPanel.SetActive(false);
            }
            if (manaRegenPanel != null)
            {
                manaRegenPanel.SetActive(false);
            }
            
            // Set hero name and player name
            if (heroOwner != null)
            {
                // Set hero name
                if (heroNameText != null)
                {
                    heroNameText.text = heroOwner.heroName;
                    Debug.Log("[HeroUIController] Hero name set to: " + heroOwner.heroName);
                }
                
                // Set player name if it exists
                if (playerNameText != null && heroOwner.photonView != null && heroOwner.photonView.Owner != null)
                {
                    playerNameText.text = heroOwner.photonView.Owner.NickName;
                    Debug.Log("[HeroUIController] Player name set to: " + heroOwner.photonView.Owner.NickName);
                }
            }
            else
            {
                Debug.LogError("[HeroUIController] No heroOwner assigned, can't set hero or player name");
            }
        }
        
        /// <summary>
        /// Configura el canvas según si pertenece al jugador local o a un jugador remoto
        /// </summary>
        private void ConfigureCanvas()
        {
            if (mainCanvas == null)
            {
                Debug.LogError("[HeroUIController] No Canvas configured");
                return;
            }
            
            // Si no es el jugador local, desactivar la interfaz
            if (heroOwner != null && !heroOwner.photonView.IsMine)
            {
                Debug.Log("[HeroUIController] Disabling UI for remote player");
                mainCanvas.enabled = false;
                return;
            }
            
            // Es el jugador local, configurar correctamente
            Debug.Log("[HeroUIController] Configuring UI for local player");
            
            // Forzar a pantalla completa si está configurado así
            if (forceScreenSpaceOverlay)
            {
                mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                mainCanvas.sortingOrder = 100; // Asegurar que está en primer plano
                
                // Reposicionar el canvas para que esté en la raíz de la jerarquía
                // Esto evita problemas de escala y posición
                if (transform.parent != null)
                {
                    Transform originalParent = transform.parent;
                    transform.SetParent(null);
                    // Si necesitamos mantener alguna referencia al padre original, hacerlo aquí
                }
            }
            
            mainCanvas.enabled = true;
        }
        
        /// <summary>
        /// Verifica que todas las referencias importantes estén configuradas
        /// </summary>
        private void CheckReferences()
        {
            string missingRefs = "";
            
            // Verificar referencias de UI
            if (healthBar == null) missingRefs += "healthBar, ";
            if (manaBar == null) missingRefs += "manaBar, ";
            if (healthText == null) missingRefs += "healthText, ";
            if (manaText == null) missingRefs += "manaText, ";
            if (experienceBar == null) missingRefs += "experienceBar, ";
            if (experienceText == null) missingRefs += "experienceText, ";
            if (levelText == null) missingRefs += "levelText, ";
            if (playerNameText == null) missingRefs += "playerNameText, ";
            if (heroNameText == null) missingRefs += "heroNameText, ";
            if (primaryAttributeText == null) missingRefs += "primaryAttributeText, ";
            if (goldText == null) missingRefs += "goldText, ";
            
            // Verificar referencias de stats
            if (strengthText == null) missingRefs += "strengthText, ";
            if (intelligenceText == null) missingRefs += "intelligenceText, ";
            if (agilityText == null) missingRefs += "agilityText, ";
            if (attackDamageText == null) missingRefs += "attackDamageText, ";
            if (attackSpeedText == null) missingRefs += "attackSpeedText, ";
            if (attackRangeText == null) missingRefs += "attackRangeText, ";
            if (moveSpeedText == null) missingRefs += "moveSpeedText, ";
            if (armorText == null) missingRefs += "armorText, ";
            if (magicResistanceText == null) missingRefs += "magicResistanceText, ";
            if (healthRegenText == null) missingRefs += "healthRegenText, ";
            if (manaRegenText == null) missingRefs += "manaRegenText, ";
            
            if (missingRefs != "")
            {
                Debug.LogWarning("[HeroUIController] Missing references: " + missingRefs);
            }
        }
        
        void Update()
        {
            // Si es un canvas en modo World Space, asegurar que sigue a la cámara
            if (mainCanvas != null && mainCanvas.renderMode == RenderMode.WorldSpace && cameraTransform != null)
            {
                transform.LookAt(transform.position + cameraTransform.rotation * Vector3.forward, 
                                 cameraTransform.rotation * Vector3.up);
            }

            // Actualizar estadísticas si tenemos un héroe propietario
            if (heroOwner != null)
            {
                UpdateHeroStats(heroOwner);
            }
        }
        
        /// <summary>
        /// Actualiza la barra de vida
        /// </summary>
        public void UpdateHealthBar(float currentHealth, float maxHealth)
        {
            if (healthBar != null)
            {
                healthBar.value = currentHealth / maxHealth;
            }
            
            if (healthText != null)
            {
                healthText.text = $"{Mathf.FloorToInt(currentHealth)}/{Mathf.FloorToInt(maxHealth)}";
            }
        }
        
        /// <summary>
        /// Actualiza la barra de maná
        /// </summary>
        public void UpdateManaBar(float currentMana, float maxMana)
        {
            if (manaBar != null)
            {
                manaBar.value = currentMana / maxMana;
            }
            
            if (manaText != null)
            {
                manaText.text = $"{Mathf.FloorToInt(currentMana)}/{Mathf.FloorToInt(maxMana)}";
            }
        }
        
        /// <summary>
        /// Establece el nombre del jugador en la UI
        /// </summary>
        public void SetPlayerName(string name)
        {
            if (playerNameText != null)
            {
                playerNameText.text = name;
                Debug.Log("[HeroUIController] Player name set to: " + name);
            }
            else
            {
                Debug.LogWarning("[HeroUIController] playerNameText is null, can't set player name");
            }
        }
        
        /// <summary>
        /// Establece el nombre del héroe en la UI
        /// </summary>
        public void SetHeroName(string name)
        {
            if (heroNameText != null)
            {
                heroNameText.text = name;
                Debug.Log("[HeroUIController] Hero name set to: " + name);
            }
            else
            {
                Debug.LogWarning("[HeroUIController] heroNameText is null, can't set hero name");
            }
        }
        
        /// <summary>
        /// Actualiza el nivel mostrado
        /// </summary>
        public void SetLevel(int level)
        {
            if (levelText != null)
            {
                levelText.text = level.ToString();
            }
        }
        
        /// <summary>
        /// Actualiza todas las estadísticas del héroe en la UI
        /// </summary>
        public void UpdateHeroStats(HeroBase hero)
        {
            if (hero == null || hero.heroData == null)
            {
                Debug.LogWarning("[HeroUIController] No hero data to update");
                return;
            }

            var heroData = hero.heroData;

            // Actualizar atributo principal con color
            if (primaryAttributeText != null)
            {
                string attributeColor = heroData.PrimaryAttribute switch
                {
                    "Strength" => "#FF5555",    // Rojo para fuerza
                    "Intelligence" => "#5555FF", // Azul para inteligencia
                    "Agility" => "#55FF55",     // Verde para agilidad
                    _ => "#FFFFFF"              // Blanco por defecto
                };
                primaryAttributeText.text = $"<color={attributeColor}>{heroData.PrimaryAttribute}</color>";
            }

            // Update base stats con colores
            if (strengthText != null)
                strengthText.text = $"<color=#FF5555>{Mathf.FloorToInt(heroData.CurrentStrength)}</color>";
            if (intelligenceText != null)
                intelligenceText.text = $"<color=#5555FF>{Mathf.FloorToInt(heroData.CurrentIntelligence)}</color>";
            if (agilityText != null)
                agilityText.text = $"<color=#55FF55>{Mathf.FloorToInt(heroData.CurrentAgility)}</color>";
            if (strengthScalingText != null)
                strengthScalingText.text = $"<color=#FF5555>+{Mathf.FloorToInt(heroData.StrengthScaling)}</color>";
            if (intelligenceScalingText != null)
                intelligenceScalingText.text = $"<color=#5555FF>+{Mathf.FloorToInt(heroData.IntelligenceScaling)}</color>";
            if (agilityScalingText != null)
                agilityScalingText.text = $"<color=#55FF55>+{Mathf.FloorToInt(heroData.AgilityScaling)}</color>";

            // Actualizar estadísticas derivadas con solo valores
            if (attackDamageText != null)
                attackDamageText.text = $"{heroData.CurrentAttackDamage:F0}";
            if (attackSpeedText != null)
                attackSpeedText.text = $"{heroData.CurrentAttackSpeed:F2}";
            if (moveSpeedText != null)
                moveSpeedText.text = $"{heroData.MovementSpeed:F0}";
            if (attackRangeText != null)
                attackRangeText.text = $"{hero.AttackRange:F1}";
            if (armorText != null)
                armorText.text = $"{heroData.CurrentArmor:F1}";
            if (magicResistanceText != null)
                magicResistanceText.text = $"{heroData.CurrentMagicResistance:F1}";
            if (healthRegenText != null)
                healthRegenText.text = $"+{heroData.CurrentHealthRegen:F1}/s";
            if (manaRegenText != null)
                manaRegenText.text = $"+{heroData.CurrentManaRegen:F1}/s";

            // Actualizar nivel
            if (levelText != null)
            {
                levelText.text = $"Lvl {hero.CurrentLevel}";
            }
        }
        
        /// <summary>
        /// Muestra un texto flotante de daño
        /// </summary>
        public void ShowDamageText(float amount, bool isMagicDamage = false)
        {
            // Verificar si el objeto está activo antes de continuar
            if (!gameObject.activeInHierarchy)
            {
                Debug.LogWarning($"[HeroUIController] No se puede mostrar texto de daño: GameObject '{gameObject.name}' inactivo");
                return;
            }

            if (floatingTextPrefab == null)
                return;
                
            // Crear texto flotante en la posición del personaje + offset
            Vector3 position = transform.position + floatingTextOffset;
            GameObject textObj = Instantiate(floatingTextPrefab, position, Quaternion.identity, floatingTextParent);
            
            // Configurar el texto
            TMP_Text textComponent = textObj.GetComponent<TMP_Text>();
            if (textComponent != null)
            {
                textComponent.text = "-" + Mathf.FloorToInt(amount).ToString();
                textComponent.color = isMagicDamage ? magicDamageColor : damageColor;
                
                // Animar el texto de forma segura con StartCoroutine
                try
                {
                    StartCoroutine(AnimateFloatingText(textObj));
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[HeroUIController] Error al iniciar coroutine: {ex.Message}. Objeto activo: {gameObject.activeInHierarchy}");
                    // Destruir el objeto de texto para evitar fugas de memoria
                    Destroy(textObj);
                }
            }
        }
        
        /// <summary>
        /// Muestra un texto flotante de curación
        /// </summary>
        public void ShowHealText(float amount)
        {
            if (floatingTextPrefab == null)
                return;
                
            // Crear texto flotante en la posición del personaje + offset
            Vector3 position = transform.position + floatingTextOffset;
            GameObject textObj = Instantiate(floatingTextPrefab, position, Quaternion.identity, floatingTextParent);
            
            // Configurar el texto
            TMP_Text textComponent = textObj.GetComponent<TMP_Text>();
            if (textComponent != null)
            {
                textComponent.text = "+" + Mathf.FloorToInt(amount).ToString();
                textComponent.color = healColor;
                
                // Animar el texto
                StartCoroutine(AnimateFloatingText(textObj));
            }
        }
        
        /// <summary>
        /// Anima un texto flotante
        /// </summary>
        private IEnumerator AnimateFloatingText(GameObject textObj)
        {
            float duration = 1.0f;
            float elapsed = 0f;
            
            // Posición inicial
            Vector3 startPos = textObj.transform.localPosition;
            Vector3 endPos = startPos + Vector3.up * 1.0f;
            
            // Escala inicial
            Vector3 startScale = textObj.transform.localScale;
            Vector3 maxScale = startScale * 1.2f;
            Vector3 endScale = startScale * 0.8f;
            
            // Color inicial
            TMP_Text textComponent = textObj.GetComponent<TMP_Text>();
            Color startColor = textComponent.color;
            Color endColor = startColor;
            endColor.a = 0f;
            
            // Fase 1: Crecer
            while (elapsed < duration * 0.3f)
            {
                float t = elapsed / (duration * 0.3f);
                textObj.transform.localPosition = Vector3.Lerp(startPos, startPos + Vector3.up * 0.3f, t);
                textObj.transform.localScale = Vector3.Lerp(startScale, maxScale, t);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Fase 2: Flotar hacia arriba y desvanecer
            while (elapsed < duration)
            {
                float t = (elapsed - duration * 0.3f) / (duration * 0.7f);
                textObj.transform.localPosition = Vector3.Lerp(startPos + Vector3.up * 0.3f, endPos, t);
                textObj.transform.localScale = Vector3.Lerp(maxScale, endScale, t);
                textComponent.color = Color.Lerp(startColor, endColor, t);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Destruir el objeto al terminar
            Destroy(textObj);
        }

        /// <summary>
        /// Muestra el panel de regeneración de vida
        /// </summary>
        public void ShowHealthRegenPanel()
        {
            if (healthRegenPanel != null)
            {
                healthRegenPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Oculta el panel de regeneración de vida
        /// </summary>
        public void HideHealthRegenPanel()
        {
            if (healthRegenPanel != null)
            {
                healthRegenPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Muestra el panel de regeneración de maná
        /// </summary>
        public void ShowManaRegenPanel()
        {
            if (manaRegenPanel != null)
            {
                manaRegenPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Oculta el panel de regeneración de maná
        /// </summary>
        public void HideManaRegenPanel()
        {
            if (manaRegenPanel != null)
            {
                manaRegenPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Actualiza la barra de experiencia y su texto
        /// </summary>
        public void UpdateExperienceBar(float gainedXP, float currentXP, float neededXP)
        {
            Debug.Log($"[HeroUIController] Actualizando barra de experiencia:");
            Debug.Log($"[HeroUIController] XP ganada: {gainedXP}");
            Debug.Log($"[HeroUIController] XP actual: {currentXP}");
            Debug.Log($"[HeroUIController] XP necesaria: {neededXP}");
            
            // Asegurarnos de que los valores sean válidos
            currentXP = Mathf.Max(0, currentXP);
            neededXP = Mathf.Max(1, neededXP); // Evitar división por cero
            
            if (experienceBar != null)
            {
                float progress = currentXP / neededXP;
                experienceBar.value = Mathf.Clamp01(progress);
                Debug.Log($"[HeroUIController] Valor de la barra: {experienceBar.value}");
            }
            else
            {
                Debug.LogWarning("[HeroUIController] experienceBar es null");
            }
            
            if (experienceText != null)
            {
                experienceText.text = $"{Mathf.FloorToInt(currentXP)}/{Mathf.FloorToInt(neededXP)}";
                Debug.Log($"[HeroUIController] Texto de experiencia actualizado: {experienceText.text}");
            }
            else
            {
                Debug.LogWarning("[HeroUIController] experienceText es null");
            }
        }

        private void UpdateLevelText(int newLevel)
        {
            if (levelText != null)
            {
                levelText.text = $"Lvl {newLevel}";
                Debug.Log($"[HeroUIController] Actualizando texto del nivel a: {newLevel}");
            }
        }

        /// <summary>
        /// Actualiza el texto del oro
        /// </summary>
        public void UpdateGoldText(float amount)
        {
            if (goldText != null)
            {
                goldText.text = $"{amount:F0}";
                // Añade una pequeña animación para destacar el cambio
                StartCoroutine(AnimateGoldText());
            }
        }
        
        /// <summary>
        /// Anima el texto del oro para destacar cambios
        /// </summary>
        private IEnumerator AnimateGoldText()
        {
            if (goldText == null) yield break;
            
            // Guardar el color original
            Color originalColor = goldText.color;
            
            // Cambiar a color de destaque (dorado)
            goldText.color = new Color(1f, 0.8f, 0f);
            
            // Escalar ligeramente
            Vector3 originalScale = goldText.transform.localScale;
            goldText.transform.localScale = originalScale * 1.2f;
            
            // Esperar un momento
            yield return new WaitForSeconds(0.3f);
            
            // Volver al color y escala original gradualmente
            float duration = 0.5f;
            float elapsed = 0;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                goldText.color = Color.Lerp(new Color(1f, 0.8f, 0f), originalColor, t);
                goldText.transform.localScale = Vector3.Lerp(originalScale * 1.2f, originalScale, t);
                
                yield return null;
            }
            
            // Asegurar que vuelve exactamente a los valores originales
            goldText.color = originalColor;
            goldText.transform.localScale = originalScale;
        }

        /// <summary>
        /// Muestra un texto flotante para el oro ganado
        /// </summary>
        public void ShowGoldRewardText(float amount, string sourceText = "", Vector3? enemyPosition = null)
        {
            // Verificar si el objeto está activo antes de continuar
            if (!gameObject.activeInHierarchy)
            {
                Debug.LogWarning($"[HeroUIController] No se puede mostrar recompensa de oro: GameObject '{gameObject.name}' inactivo");
                try {
                    // Intentar activar el objeto
                    gameObject.SetActive(true);
                    Debug.Log($"[HeroUIController] Se activó el GameObject '{gameObject.name}' para mostrar recompensa de oro");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[HeroUIController] Error al activar GameObject: {ex.Message}");
                    return;
                }
            }
            
            // Evitar llamadas duplicadas usando una ID única para cada posición
            string positionId = enemyPosition.HasValue ? enemyPosition.Value.ToString("F2") : "player";
            string callId = $"{positionId}_{Time.frameCount}";
            
            // Verificar si ya se mostró un efecto en esta posición recientemente
            if (IsRecentGoldEffect(positionId))
            {
                Debug.LogWarning($"Evitando crear efecto de oro duplicado en {positionId} (frame: {Time.frameCount})");
                return;
            }
            
            // Registrar esta llamada
            RegisterGoldEffect(positionId);
            
            // Usar la posición del enemigo si se proporciona, sino la del jugador
            Vector3 position = (enemyPosition.HasValue) 
                ? enemyPosition.Value + Vector3.up * 2.0f // Posición más alta para que caiga
                : transform.position + floatingTextOffset + new Vector3(0, 1.0f, 0);
                
            // Preparar el texto a mostrar
            string displayText = $"+{Mathf.FloorToInt(amount)}";
            
            // Añadir el texto de la fuente para dar más contexto
            if (!string.IsNullOrEmpty(sourceText))
            {
                // Si es una cantidad grande o viene de matar héroe, hacer el texto más visible
                if (sourceText.Contains("eliminado") || amount >= 100)
                {
                    displayText = $"{displayText}";
                }
                else
                {
                    displayText = $"{displayText}";
                }
            }
            
            // Verificar específicamente si es oro por eliminar héroe
            bool isHeroKill = !string.IsNullOrEmpty(sourceText) && sourceText.Contains("eliminado");
            
            if (isHeroKill)
            {
                Debug.Log($"[HeroUIController] Mostrando recompensa por matar héroe: {amount} oro");
            }
            
            float particleMultiplier = isHeroKill ? 2.0f : 1.0f;
            
            try
            {
                // Crear las partículas de monedas (más si es una recompensa grande)
                CreateGoldParticles(position, amount * particleMultiplier);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[HeroUIController] Error al crear partículas de oro: {ex.Message}");
            }
            
            // Intentar usar el prefab si existe
            if (floatingTextPrefab != null)
            {
                try
                {
                    // Crear el texto flotante sin padre para evitar errores con objetos persistentes
                    GameObject textObj = Instantiate(floatingTextPrefab, position, Quaternion.identity);
                    textObj.name = $"GoldText_{callId}";
                    
                    // Configurar el texto
                    TMP_Text textComponent = textObj.GetComponent<TMP_Text>();
                    if (textComponent != null)
                    {
                        // Establecer el texto
                        textComponent.text = displayText.Trim();
                        textComponent.color = goldColor;
                        
                        // Si es recompensa por matar héroe, hacer texto más grande y duradero
                        if (isHeroKill || amount >= 100)
                        {
                            textComponent.fontSize *= 1.5f;
                            textComponent.fontStyle = TMPro.FontStyles.Bold;
                            
                            // Animar con coroutina especial para recompensas de héroe
                            try
                            {
                                StartCoroutine(AnimateHeroKillGoldText(textObj));
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogError($"[HeroUIController] Error al iniciar coroutine de animación de oro por héroe: {ex.Message}");
                                Destroy(textObj);
                            }
                        }
                        else
                        {
                            textComponent.fontSize *= 1.2f; // Texto ligeramente más grande
                            // Animar el texto con corrutina estándar
                            try
                            {
                                StartCoroutine(AnimateGoldFloatingText(textObj));
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogError($"[HeroUIController] Error al iniciar coroutine de animación de oro: {ex.Message}");
                                Destroy(textObj);
                            }
                        }
                        
                        Debug.Log($"Mostrando texto de oro: '{textComponent.text}' en posición {position}");
                        
                        // Asegurar que el objeto mire a la cámara
                        Billboard billboard = textObj.GetComponent<Billboard>();
                        if (billboard == null)
                        {
                            billboard = textObj.AddComponent<Billboard>();
                        }
                        
                        // Asegurar destrucción en caso de fallo
                        Destroy(textObj, 5f);
                    }
                    else
                    {
                        Debug.LogError("El prefab de texto flotante no tiene componente TextMeshPro");
                        Destroy(textObj);
                        
                        // Crear texto dinámicamente como fallback
                        CreateDynamicFloatingText(position, displayText);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[HeroUIController] Error al crear texto flotante: {ex.Message}");
                }
            }
            else
            {
                // Si no hay prefab, crear el texto dinámicamente
                CreateDynamicFloatingText(position, displayText);
            }
        }
        
        /// <summary>
        /// Anima el texto flotante para recompensas de oro por matar héroes (efecto especial)
        /// </summary>
        private IEnumerator AnimateHeroKillGoldText(GameObject textObj)
        {
            if (textObj == null) yield break;
            
            TMP_Text textComponent = textObj.GetComponent<TMP_Text>();
            if (textComponent == null) yield break;
            
            // Variables de animación
            float duration = 2.5f; // Duración más larga para recompensas importantes
            float startTime = Time.time;
            Vector3 startPosition = textObj.transform.position;
            Vector3 targetPosition = startPosition + Vector3.up * 2.5f;
            Color startColor = textComponent.color;
            
            // Efecto de pulso para recompensas de héroe
            float pulseSpeed = 5.0f;
            float maxScale = 1.5f;
            
            // Bucle de animación
            while (Time.time - startTime < duration)
            {
                float elapsed = Time.time - startTime;
                float t = elapsed / duration;
                
                // Movimiento hacia arriba con rebote
                float yOffset = Mathf.Sin(t * Mathf.PI) * 0.5f;
                textObj.transform.position = Vector3.Lerp(startPosition, targetPosition, t) + new Vector3(Mathf.Sin(t * 8f) * 0.2f, yOffset, 0);
                
                // Efecto de pulso en el tamaño
                float pulse = 1.0f + Mathf.Sin(elapsed * pulseSpeed) * 0.2f;
                textObj.transform.localScale = Vector3.one * Mathf.Lerp(1.0f, pulse, Mathf.Min(1, elapsed * 2));
                
                // Color con brillo extra
                Color glowColor = Color.Lerp(startColor, Color.white, Mathf.Sin(elapsed * 10f) * 0.3f);
                textComponent.color = Color.Lerp(glowColor, new Color(glowColor.r, glowColor.g, glowColor.b, 0), Mathf.Pow(t, 0.5f));
                
                yield return null;
            }
            
            // Asegurar que el objeto se destruye
            Destroy(textObj);
        }

        /// <summary>
        /// Registro de efectos de oro recientes para evitar duplicación
        /// </summary>
        private System.Collections.Generic.Dictionary<string, float> recentGoldEffects = new System.Collections.Generic.Dictionary<string, float>();
        private const float GOLD_EFFECT_COOLDOWN = 0.5f; // Medio segundo de cooldown
        
        /// <summary>
        /// Verifica si se mostró un efecto de oro recientemente en esta posición
        /// </summary>
        private bool IsRecentGoldEffect(string positionId)
        {
            if (recentGoldEffects.TryGetValue(positionId, out float time))
            {
                if (Time.time - time < GOLD_EFFECT_COOLDOWN)
                {
                    return true; // Es muy reciente, no crear otro
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Registra que se mostró un efecto de oro en esta posición
        /// </summary>
        private void RegisterGoldEffect(string positionId)
        {
            recentGoldEffects[positionId] = Time.time;
            
            // Limpiar posiciones antiguas cada cierto tiempo
            if (Time.frameCount % 100 == 0)
            {
                CleanupOldEffects();
            }
        }
        
        /// <summary>
        /// Limpia registros de efectos antiguos
        /// </summary>
        private void CleanupOldEffects()
        {
            float currentTime = Time.time;
            System.Collections.Generic.List<string> keysToRemove = new System.Collections.Generic.List<string>();
            
            foreach (var entry in recentGoldEffects)
            {
                if (currentTime - entry.Value > GOLD_EFFECT_COOLDOWN * 2)
                {
                    keysToRemove.Add(entry.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                recentGoldEffects.Remove(key);
            }
        }
        
        /// <summary>
        /// Crea un texto flotante dinámicamente sin necesidad de prefab
        /// </summary>
        private GameObject CreateDynamicFloatingText(Vector3 position, string text)
        {
            Debug.Log($"Creando texto flotante dinámico: '{text}' en posición {position}");
            
            // Intentar usar el FloatingTextCreator si existe
            FloatingTextCreator creator = FloatingTextCreator.Instance;
            if (creator != null)
            {
                GameObject textObj = creator.CreateFloatingText(position, text, goldColor, 3f, 2f);
                
                // Animar el texto con nuestra corrutina
                StartCoroutine(AnimateGoldFloatingText(textObj));
                return textObj;
            }
            else
            {
                // Crear manualmente como último recurso
                GameObject textObj = new GameObject("FloatingText");
                textObj.transform.position = position;
                
                // Añadir TextMeshPro
                TextMeshPro textComponent = textObj.AddComponent<TextMeshPro>();
                textComponent.text = text;
                textComponent.fontSize = 3;
                textComponent.fontStyle = FontStyles.Bold;
                textComponent.alignment = TextAlignmentOptions.Center;
                textComponent.color = goldColor;
                textComponent.enableCulling = false;
                
                // Añadir Billboard
                textObj.AddComponent<Billboard>();
                
                // Animar
                StartCoroutine(AnimateGoldFloatingText(textObj));
                
                // Destruir después de tiempo
                Destroy(textObj, 5f);
                
                return textObj;
            }
        }
        
        /// <summary>
        /// Anima el texto flotante del oro con efecto especial (caída y desvanecimiento)
        /// </summary>
        private IEnumerator AnimateGoldFloatingText(GameObject textObj)
        {
            // Comprobación inicial de seguridad
            if (textObj == null) 
            {
                Debug.LogWarning("AnimateGoldFloatingText: objeto de texto nulo");
                yield break;
            }
            
            // Obtener componente de texto
            TMP_Text textComponent = textObj.GetComponent<TMP_Text>();
            if (textComponent == null) 
            {
                Debug.LogWarning("AnimateGoldFloatingText: componente de texto no encontrado");
                Destroy(textObj);
                yield break;
            }
            
            // Guardar texto original para debug
            string originalText = textComponent.text;
            Debug.Log($"Animando texto de oro: '{originalText}'");
            
            float duration = 1.5f; // Duración de la animación
            float elapsed = 0f;
            
            // Posición inicial
            Vector3 startPos = textObj.transform.position;
            // Posición final (caída)
            Vector3 endPos = startPos + new Vector3(0, -1.5f, 0);
            
            // Escala inicial
            Vector3 startScale = textObj.transform.localScale;
            Vector3 maxScale = startScale * 1.5f;
            Vector3 endScale = startScale * 0.8f;
            
            // Color inicial
            Color startColor = textComponent.color;
            Color peakColor = new Color(1f, 0.9f, 0.1f, 1f); // Amarillo brillante
            Color endColor = startColor;
            endColor.a = 0f; // Transparente al final
            
            // Fase 1: Aparecer y crecer
            float appearDuration = duration * 0.2f;
            while (elapsed < appearDuration && textObj != null)
            {
                float t = elapsed / appearDuration;
                textObj.transform.localScale = Vector3.Lerp(startScale, maxScale, t);
                textComponent.color = Color.Lerp(startColor, peakColor, t);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Verificar si el objeto aún existe
            if (textObj == null) yield break;
            
            // Fase 2: Mantener brevemente
            float holdTime = 0.2f;
            elapsed = 0f;
            while (elapsed < holdTime && textObj != null)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Verificar si el objeto aún existe
            if (textObj == null) yield break;
            
            // Fase 3: Caer y desvanecer
            elapsed = 0f;
            float fallDuration = duration * 0.6f;
            
            // Añadir un ligero movimiento horizontal aleatorio
            float randomX = Random.Range(-0.5f, 0.5f);
            Vector3 horizontalOffset = new Vector3(randomX, 0, 0);
            
            while (elapsed < fallDuration && textObj != null)
            {
                float t = elapsed / fallDuration;
                
                // Movimiento con rebote suave al caer
                float verticalOffset = Mathf.Sin(t * Mathf.PI) * 0.2f;
                Vector3 currentPos = Vector3.Lerp(startPos, endPos, t) + horizontalOffset;
                currentPos.y += verticalOffset;
                
                textObj.transform.position = currentPos;
                textObj.transform.localScale = Vector3.Lerp(maxScale, endScale, t);
                textComponent.color = Color.Lerp(peakColor, endColor, t);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            Debug.Log($"Animación de oro completada para: '{originalText}'");
            
            // Asegurar que siempre se destruya el objeto al final
            if (textObj != null)
            {
                Destroy(textObj);
                Debug.Log("Texto de oro destruido correctamente");
            }
        }

        /// <summary>
        /// Crea partículas de monedas en la posición especificada
        /// </summary>
        private void CreateGoldParticles(Vector3 position, float goldAmount)
        {
            // Asegurarnos de que existe el CoinSpriteCreator en la escena
            CoinSpriteCreator coinCreator = FindObjectOfType<CoinSpriteCreator>();
            if (coinCreator == null)
            {
                GameObject creatorObj = new GameObject("CoinSpriteCreator");
                coinCreator = creatorObj.AddComponent<CoinSpriteCreator>();
                Debug.Log("Creado CoinSpriteCreator porque no existía en la escena");
            }
            
            // Ajustar la cantidad de partículas según la cantidad de oro
            // Más monedas para cantidades grandes como recompensas por matar héroes
            int baseParticles = Mathf.Clamp(Mathf.FloorToInt(3 + goldAmount / 10), 5, 20);
            
            // Detectar si es una recompensa grande (ej: matar héroe)
            bool isLargeReward = goldAmount >= 100;
            int particleCount = isLargeReward ? baseParticles * 2 : baseParticles;
            
            // Crear objeto de partículas con nombre único
            string particleId = $"GoldCoins_{Random.Range(1000, 9999)}";
            GameObject particleObj = new GameObject(particleId);
            particleObj.transform.position = position;
            
            // Añadir sistema de partículas con configuración para oro por héroe
            ParticleSystem particleSystem = particleObj.AddComponent<ParticleSystem>();
            
            // Configurar sistema de partículas
            var main = particleSystem.main;
            main.startLifetime = 1.5f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.0f, 3.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
            main.startColor = new Color(1f, 1f, 1f); // Color blanco para no afectar al sprite
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = particleCount * 2;
            main.gravityModifier = 2.0f; // Aumentar gravedad para efecto más realista
            main.loop = false; // Asegurar que el sistema NO se repita
            main.playOnAwake = false; // No reproducir automáticamente al crearse
            
            // Emisión
            var emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = 0; // Sin emisión continua
            emission.rateOverDistance = 0; // Sin emisión por distancia
            
            // Configurar un solo burst al inicio
            ParticleSystem.Burst singleBurst = new ParticleSystem.Burst(0f, (short)particleCount);
            singleBurst.cycleCount = 1; // Solo un ciclo
            singleBurst.repeatInterval = 999f; // Intervalo muy largo para asegurar que no se repita
            emission.SetBursts(new ParticleSystem.Burst[] { singleBurst });
            
            // Forma
            var shape = particleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 30f;
            shape.radius = 0.1f;
            shape.arc = 360f;
            
            // Rotación inicial aleatoria
            main.startRotation = new ParticleSystem.MinMaxCurve(-Mathf.PI, Mathf.PI);
            
            // Rotación durante el ciclo de vida
            var rotation = particleSystem.rotationOverLifetime;
            rotation.enabled = true;
            rotation.separateAxes = false;
            rotation.z = new ParticleSystem.MinMaxCurve(-5f, 5f);
            
            // Velocidad sobre tiempo de vida (ligero efecto de resistencia del aire)
            var velocityOverLifetime = particleSystem.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            // En lugar de dampen, usar curva de velocidad X, Y, Z
            velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(1f, 0.2f)
            ));
            velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(1f, 0.2f)
            ));
            velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(1f, 0.2f)
            ));
            
            // Tamaño sobre tiempo de vida
            var sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 1f);
            sizeCurve.AddKey(0.8f, 1f);
            sizeCurve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);
            
            // Colisiones
            var collision = particleSystem.collision;
            collision.enabled = true;
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.dampen = 0.6f;
            collision.bounce = 0.4f;
            collision.lifetimeLoss = 0.2f;
            
            // Color sobre tiempo de vida (para desvanecer al final)
            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 0.8f),
                    new GradientColorKey(Color.white, 1f)
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.7f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;
            
            // Renderizador
            var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                // Obtener sprite de moneda
                Sprite coinSprite = CoinSpriteCreator.GetCoinSprite();
                if (coinSprite != null)
                {
                    // Crear y configurar material con shader disponible
                    Material material = null;
                    
                    // Intentar usar Particles/Standard Unlit
                    Shader particleShader = Shader.Find("Particles/Standard Unlit");
                    if (particleShader != null)
                    {
                        material = new Material(particleShader);
                    }
                    else
                    {
                        // Alternativa: usar shader estándar
                        Debug.LogWarning("Shader 'Particles/Standard Unlit' no encontrado, usando shader alternativo");
                        
                        // Intentar con Unlit/Texture primero
                        Shader unlitShader = Shader.Find("Unlit/Texture");
                        if (unlitShader != null)
                        {
                            material = new Material(unlitShader);
                        }
                        else
                        {
                            // Último recurso: usar el shader estándar
                            material = new Material(Shader.Find("Standard"));
                        }
                    }
                    
                    // Asignar textura
                    material.mainTexture = coinSprite.texture;
                    renderer.material = material;
                    
                    // Configurar renderizador
                    renderer.renderMode = ParticleSystemRenderMode.Billboard;
                    renderer.sortMode = ParticleSystemSortMode.Distance;
                    renderer.alignment = ParticleSystemRenderSpace.View;
                    renderer.normalDirection = 1f;
                    renderer.allowRoll = true;
                    
                    // Cada partícula es una imagen cuadrada
                    renderer.pivot = new Vector3(0.5f, 0.5f, 0.5f);
                    
                    // Intentar añadir efectos de brillo según el shader
                    if (particleShader != null)
                    {
                        // Para Standard Unlit
                        try
                        {
                            material.SetFloat("_Glossiness", 0.7f);
                            material.SetColor("_EmissionColor", new Color(0.3f, 0.3f, 0.1f));
                            material.EnableKeyword("_EMISSION");
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogWarning($"No se pudieron aplicar propiedades de material: {ex.Message}");
                        }
                    }
                }
                else
                {
                    Debug.LogError("No se pudo crear o cargar un sprite de moneda");
                }
            }
            
            // Reproducir el sistema de partículas una sola vez
            particleSystem.Play(true); // true para reiniciar el sistema, asegurando que comienza limpio
            
            // Establecer un nombre único para evitar duplicados
            string uniqueName = $"GoldParticles_{Random.Range(1000, 9999)}_{Time.frameCount}";
            particleObj.name = uniqueName;
            
            Debug.Log($"Creado sistema de partículas único: {uniqueName} con {particleCount} partículas");
            
            // Añadir un controlador para verificar que el sistema solo se reproduzca una vez
            GoldParticleController controller = particleObj.AddComponent<GoldParticleController>();
            controller.Initialize(particleSystem, main.duration + main.startLifetime.constantMax + 0.5f);
            
            // Añadir un sonido de monedas
            AudioSource audioSource = particleObj.AddComponent<AudioSource>();
            audioSource.spatialBlend = 0.0f; // Sonido 2D para asegurar que se escuche
            audioSource.volume = 0.7f;
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            
            // Intentar cargar sonido de monedas desde varias ubicaciones
            AudioClip coinSound = null;
            
            // Intento 1: Recursos directos
            coinSound = Resources.Load<AudioClip>("Sounds/CoinDrop");
            
            // Intento 2: Sonidos
            if (coinSound == null)
                coinSound = Resources.Load<AudioClip>("Sounds/Coin");
                
            // Intento 3: Efectos
            if (coinSound == null)
                coinSound = Resources.Load<AudioClip>("SFX/Coin");
                
            // Intento 4: Audio
            if (coinSound == null)
                coinSound = Resources.Load<AudioClip>("Audio/CoinDrop");
            
            // Verificar si se encontró algún sonido
            if (coinSound != null)
            {
                audioSource.PlayOneShot(coinSound);
                Debug.Log($"Reproduciendo sonido de monedas: {coinSound.name}");
            }
            else
            {
                Debug.LogWarning("No se encontró ningún sonido de monedas en los recursos");
                
                // Buscar sonido alternativo en el héroe dueño
                if (heroOwner != null && heroOwner.goldSound != null)
                {
                    audioSource.PlayOneShot(heroOwner.goldSound);
                    Debug.Log("Usando sonido de oro del héroe como alternativa");
                }
            }

            // Si es recompensa grande, hacer partículas más vistosas
            if (isLargeReward)
            {
                // Aumentar tamaño de partículas
                main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
                
                // Más velocidad inicial para dispersión mayor
                main.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 4.0f);
                
                // Aumentar tiempo de vida para que se vean más tiempo
                main.startLifetime = 2.0f;
                
                Debug.Log($"Creando efecto especial de oro por héroe con {particleCount} partículas");
            }
        }

        private void OnDestroy()
        {
            if (heroOwner != null)
            {
                heroOwner.OnExperienceGained -= UpdateExperienceBar;
                heroOwner.OnLevelUp -= UpdateLevelText;
            }
        }
    }
    
    // Extension method to find child transforms recursively
    public static class TransformExtensions
    {
        public static T FindDeepChild<T>(this Transform parent, string name) where T : Component
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                {
                    T component = child.GetComponent<T>();
                    if (component != null)
                        return component;
                }
                
                T result = child.FindDeepChild<T>(name);
                if (result != null)
                    return result;
            }
            
            return null;
        }
    }
}