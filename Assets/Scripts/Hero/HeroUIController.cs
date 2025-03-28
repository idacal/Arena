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
        
        [Header("Player Information")]
        public TMP_Text playerNameText;
        public TMP_Text heroNameText;
        public TMP_Text levelText;
        
        [Header("Ability UI")]
        public GameObject abilityPanel;
        
        [Header("Damage/Heal Text")]
        public GameObject floatingTextPrefab;
        public Color damageColor = Color.red;
        public Color magicDamageColor = Color.magenta;
        public Color healColor = Color.green;
        
        [Header("Positioning")]
        public Vector3 floatingTextOffset = new Vector3(0, 2, 0);
        public Transform floatingTextParent;
        
        [Header("Canvas Settings")]
        public Canvas mainCanvas;
        public bool forceScreenSpaceOverlay = true;
        
        private Transform cameraTransform;
        private HeroBase heroOwner;
        
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
                Debug.LogError("[HeroUIController] No se pudo encontrar el HeroBase asociado a este UI");
            }
            
            // Auto-find UI references if not assigned
            if (heroNameText == null)
            {
                heroNameText = transform.FindDeepChild<TMP_Text>("Hero Name");
                if (heroNameText == null)
                    Debug.LogWarning("[HeroUIController] No se pudo encontrar el elemento Hero Name");
                else
                    Debug.Log("[HeroUIController] Se encontró automáticamente Hero Name");
            }
            
            if (playerNameText == null)
            {
                playerNameText = transform.FindDeepChild<TMP_Text>("PlayerName");
                if (playerNameText == null)
                    Debug.LogWarning("[HeroUIController] No se pudo encontrar el elemento PlayerName");
                else
                    Debug.Log("[HeroUIController] Se encontró automáticamente PlayerName");
            }
        }
        
        void Start()
        {
            Debug.Log("[HeroUIController] Inicializando UI para: " + (heroOwner != null ? heroOwner.heroName : "Desconocido"));
            
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
                Debug.LogError("[HeroUIController] No hay Canvas configurado");
                return;
            }
            
            // Si no es el jugador local, desactivar la interfaz
            if (heroOwner != null && !heroOwner.photonView.IsMine)
            {
                Debug.Log("[HeroUIController] Desactivando UI para jugador remoto");
                mainCanvas.enabled = false;
                return;
            }
            
            // Es el jugador local, configurar correctamente
            Debug.Log("[HeroUIController] Configurando UI para jugador local");
            
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
            
            if (healthBar == null) missingRefs += "healthBar, ";
            if (manaBar == null) missingRefs += "manaBar, ";
            if (healthText == null) missingRefs += "healthText, ";
            if (manaText == null) missingRefs += "manaText, ";
            if (playerNameText == null) missingRefs += "playerNameText, ";
            if (heroNameText == null) missingRefs += "heroNameText, ";
            
            if (missingRefs != "")
            {
                Debug.LogWarning("[HeroUIController] Faltan referencias: " + missingRefs);
            }
        }
        
        void LateUpdate()
        {
            // Si es un canvas en modo World Space, asegurar que sigue a la cámara
            if (mainCanvas != null && mainCanvas.renderMode == RenderMode.WorldSpace && cameraTransform != null)
            {
                transform.LookAt(transform.position + cameraTransform.rotation * Vector3.forward, 
                                 cameraTransform.rotation * Vector3.up);
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
        /// Muestra un texto flotante de daño
        /// </summary>
        public void ShowDamageText(float amount, bool isMagicDamage = false)
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
                textComponent.text = "-" + Mathf.FloorToInt(amount).ToString();
                textComponent.color = isMagicDamage ? magicDamageColor : damageColor;
                
                // Animar el texto
                StartCoroutine(AnimateFloatingText(textObj));
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