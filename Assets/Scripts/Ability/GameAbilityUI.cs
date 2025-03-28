using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Text;

namespace Photon.Pun.Demo.Asteroids
{
    public class GameAbilityUI : MonoBehaviour
    {
        [System.Serializable]
        public class AbilitySlotUI
        {
            public GameObject slotObject;      // El GameObject contenedor del slot
            public Image abilityIcon;          // Icono de la habilidad
            public Image cooldownOverlay;      // Overlay para mostrar cooldown
            public TMP_Text hotkeyText;        // Texto de hotkey (Q, W, E, R)
            public TMP_Text cooldownText;      // Texto para mostrar tiempo restante de cooldown
            public TMP_Text manaCostText;      // Texto para mostrar costo de maná
            public TMP_Text abilityNameText;   // Texto para mostrar nombre de la habilidad
            public TMP_Text abilityInfoText;   // Texto para mostrar información adicional (duración, daño, rango)
            public Button abilityButton;       // Botón para activar la habilidad
            public int slotIndex;              // Índice del slot correspondiente
        }

        [Header("Prefab Settings")]
        public GameObject abilitySlotPrefab;   // Prefab para un slot de habilidad
        public Transform slotsContainer;       // Contenedor donde se crearán los slots

        [Header("Layout Settings")]
        public float slotSpacing = 10f;        // Espacio entre slots
        public bool useHorizontalLayout = true; // Si es falso, usará layout vertical
        public Vector2 slotSize = new Vector2(80, 80); // Tamaño de cada slot

        [Header("Debug Options")]
        public bool enableDebugOutput = true;  // Mostrar mensajes de depuración
        public bool createDebugLabels = true;  // Crear etiquetas de depuración visual
        public Color debugTextColor = Color.yellow; // Color para el texto de depuración

        [Header("References")]
        public HeroAbilityController abilityController;
        
        // Lista dinámica de slots de UI
        private List<AbilitySlotUI> abilitySlots = new List<AbilitySlotUI>();
        private HeroBase heroBase;
        private List<GameObject> debugObjects = new List<GameObject>();

        void Awake()
        {
            // Si no se asignó el abilityController, buscarlo en el padre
            if (abilityController == null)
            {
                abilityController = GetComponentInParent<HeroAbilityController>();
            }
            
            heroBase = GetComponentInParent<HeroBase>();
            
            // Si no encontramos en el padre, buscar en la raíz
            if (abilityController == null)
            {
                abilityController = transform.root.GetComponent<HeroAbilityController>();
            }
            
            if (heroBase == null)
            {
                heroBase = transform.root.GetComponent<HeroBase>();
            }
            
            // Crear un contenedor si no existe
            if (slotsContainer == null)
            {
                GameObject container = new GameObject("AbilitySlotsContainer");
                container.transform.SetParent(transform);
                RectTransform rt = container.AddComponent<RectTransform>();
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(500, 100);
                
                // Añadir HorizontalLayoutGroup si se usa layout horizontal
                if (useHorizontalLayout)
                {
                    HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
                    layout.spacing = slotSpacing;
                    layout.childAlignment = TextAnchor.MiddleCenter;
                }
                else
                {
                    VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
                    layout.spacing = slotSpacing;
                    layout.childAlignment = TextAnchor.MiddleCenter;
                }
                
                slotsContainer = container.transform;
            }
            
            if (abilityController == null)
            {
                DebugLog("No se encontró HeroAbilityController. La UI de habilidades no funcionará correctamente.", true);
            }
        }

        void Start()
        {
            // Solo activar para el jugador local
            if (heroBase != null && !heroBase.photonView.IsMine)
            {
                gameObject.SetActive(false);
                return;
            }
            
            // Inicializar slots de UI tras un pequeño delay para asegurar que las habilidades estén cargadas
            Invoke("InitializeAbilitySlots", 0.2f);
        }

        void Update()
        {
            // Solo actualizar para el jugador local
            if (heroBase == null || !heroBase.photonView.IsMine)
                return;
                
            UpdateAbilityUI();
        }
        
        /// <summary>
        /// Inicializa los slots de UI con los datos de las habilidades
        /// </summary>
        private void InitializeAbilitySlots()
        {
            if (abilityController == null)
            {
                DebugLog("No se encontró el controlador de habilidades.", true);
                return;
            }
            
            // Limpiar objetos de depuración anteriores
            ClearDebugObjects();
            
            // Antes de crear nuevos slots, limpiar los existentes
            ClearAbilitySlots();
            
            // Verificar si hay habilidades para mostrar
            if (abilityController.abilitySlots.Count == 0)
            {
                DebugLog("No hay habilidades configuradas para este héroe.", true);
                return;
            }
            
            DebugLog($"Creando {abilityController.abilitySlots.Count} slots de habilidades");
            
            // Crear un slot de UI para cada habilidad en el controlador
            for (int i = 0; i < abilityController.abilitySlots.Count; i++)
            {
                if (i < abilityController.abilitySlots.Count)
                {
                    HeroAbilityController.AbilitySlot abilityData = abilityController.abilitySlots[i];
                    
                    if (abilityData != null && abilityData.abilityData != null)
                    {
                        DebugLog($"Creando slot para habilidad: {abilityData.abilityData.Name} (Slot {i})");
                        
                        // Crear slot de UI
                        AbilitySlotUI uiSlot = CreateAbilitySlot(i, abilityData);
                        if (uiSlot != null)
                        {
                            abilitySlots.Add(uiSlot);
                        }
                    }
                    else
                    {
                        DebugLog($"Datos nulos para el slot {i}", true);
                    }
                }
            }
            
            // Organizar los slots
            ArrangeSlots();
        }
        
        /// <summary>
        /// Crea un slot de habilidad en la UI
        /// </summary>
        private AbilitySlotUI CreateAbilitySlot(int index, HeroAbilityController.AbilitySlot abilityData)
        {
            // Verificar si tenemos un prefab
            if (abilitySlotPrefab == null)
            {
                DebugLog("No hay prefab configurado para los slots de habilidad. Crea un prefab y asígnalo en el inspector.", true);
                return null;
            }
            
            // Verificar los datos de habilidad
            if (abilityData == null || abilityData.abilityData == null)
            {
                DebugLog($"Datos de habilidad nulos para el slot {index}", true);
                return null;
            }
            
            // Instanciar el prefab
            GameObject slotObject = Instantiate(abilitySlotPrefab, slotsContainer);
            slotObject.name = $"AbilitySlot_{index}_{abilityData.abilityData.Name}";
            
            // Crear la estructura del slot de UI
            AbilitySlotUI uiSlot = new AbilitySlotUI();
            uiSlot.slotObject = slotObject;
            uiSlot.slotIndex = index;
            
            // Obtener componentes principales de UI
            uiSlot.abilityButton = slotObject.GetComponent<Button>();
            
            // Encontrar todos los componentes Text y Image
            TMP_Text[] allTexts = slotObject.GetComponentsInChildren<TMP_Text>(true);
            Image[] allImages = slotObject.GetComponentsInChildren<Image>(true);
            
            // Imprimir todos los textos encontrados para depuración
            DebugLog($"Encontrados {allTexts.Length} componentes TMP_Text en el slot {index}:");
            foreach (TMP_Text text in allTexts)
            {
                DebugLog($"  - {text.name} (GameObject: {text.gameObject.name})");
            }
            
            // Imprimir todas las imágenes encontradas para depuración
            DebugLog($"Encontrados {allImages.Length} componentes Image en el slot {index}:");
            foreach (Image image in allImages)
            {
                DebugLog($"  - {image.name} (GameObject: {image.gameObject.name})");
            }
            
            // Asignar componentes basados en nombres
            foreach (TMP_Text text in allTexts)
            {
                // Usar el nombre del objeto o el nombre del componente para identificarlo
                string componentName = text.gameObject.name;
                
                if (componentName.Contains("HotkeyText"))
                {
                    uiSlot.hotkeyText = text;
                    DebugLog($"Asignado HotkeyText: {componentName}");
                }
                else if (componentName.Contains("CooldownText"))
                {
                    uiSlot.cooldownText = text;
                    DebugLog($"Asignado CooldownText: {componentName}");
                }
                else if (componentName.Contains("ManaCostText"))
                {
                    uiSlot.manaCostText = text;
                    DebugLog($"Asignado ManaCostText: {componentName}");
                }
                else if (componentName.Contains("AbilityNameText") || componentName.Contains("NameText"))
                {
                    uiSlot.abilityNameText = text;
                    DebugLog($"Asignado AbilityNameText: {componentName}");
                }
                else if (componentName.Contains("InfoText") || componentName.Contains("DescriptionText"))
                {
                    uiSlot.abilityInfoText = text;
                    DebugLog($"Asignado AbilityInfoText: {componentName}");
                }
            }
            
            // Si no encontramos específicamente el AbilityNameText, buscar por contenido
            if (uiSlot.abilityNameText == null)
            {
                // Crear un texto temporal para el nombre de la habilidad si no existe
                DebugLog($"No se encontró AbilityNameText para {abilityData.abilityData.Name}, buscando alternativas o creando uno nuevo");
                
                // Buscar un texto que no tenga asignación aún
                foreach (TMP_Text text in allTexts)
                {
                    if (text != uiSlot.hotkeyText && 
                        text != uiSlot.cooldownText && 
                        text != uiSlot.manaCostText &&
                        text != uiSlot.abilityInfoText)
                    {
                        uiSlot.abilityNameText = text;
                        DebugLog($"Usando texto sin asignar como AbilityNameText: {text.gameObject.name}");
                        break;
                    }
                }
                
                // Si todavía no tenemos un texto para el nombre, crear uno nuevo
                if (uiSlot.abilityNameText == null && createDebugLabels)
                {
                    GameObject nameTextObj = new GameObject("AbilityNameText_Created");
                    nameTextObj.transform.SetParent(slotObject.transform);
                    RectTransform rt = nameTextObj.AddComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(0, -40); // Posicionar debajo del icono
                    rt.sizeDelta = new Vector2(100, 20);
                    
                    TMP_Text nameText = nameTextObj.AddComponent<TextMeshProUGUI>();
                    nameText.fontSize = 12;
                    nameText.alignment = TextAlignmentOptions.Center;
                    nameText.color = debugTextColor;
                    
                    uiSlot.abilityNameText = nameText;
                    DebugLog("Creado nuevo AbilityNameText");
                    
                    // Añadir a la lista de objetos de depuración
                    debugObjects.Add(nameTextObj);
                }
            }
            
            // Lo mismo para el texto de información adicional
            if (uiSlot.abilityInfoText == null && createDebugLabels)
            {
                GameObject infoTextObj = new GameObject("AbilityInfoText_Created");
                infoTextObj.transform.SetParent(slotObject.transform);
                RectTransform rt = infoTextObj.AddComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(0, -60); // Posicionar debajo del nombre
                rt.sizeDelta = new Vector2(120, 40);
                
                TMP_Text infoText = infoTextObj.AddComponent<TextMeshProUGUI>();
                infoText.fontSize = 10;
                infoText.alignment = TextAlignmentOptions.Center;
                infoText.color = debugTextColor;
                
                uiSlot.abilityInfoText = infoText;
                DebugLog("Creado nuevo AbilityInfoText");
                
                // Añadir a la lista de objetos de depuración
                debugObjects.Add(infoTextObj);
            }
            
            // Asignar imágenes
            foreach (Image image in allImages)
            {
                string componentName = image.gameObject.name;
                
                if (componentName.Contains("AbilityIcon") || componentName.Contains("Icon"))
                {
                    uiSlot.abilityIcon = image;
                    DebugLog($"Asignado AbilityIcon: {componentName}");
                }
                else if (componentName.Contains("CooldownOverlay") || componentName.Contains("Overlay"))
                {
                    uiSlot.cooldownOverlay = image;
                    DebugLog($"Asignado CooldownOverlay: {componentName}");
                }
            }
            
            // Si no encontramos un icono específico, usar el primer Image que no sea el overlay
            if (uiSlot.abilityIcon == null)
            {
                foreach (Image image in allImages)
                {
                    if (image != uiSlot.cooldownOverlay)
                    {
                        uiSlot.abilityIcon = image;
                        DebugLog($"Usando imagen alternativa como AbilityIcon: {image.gameObject.name}");
                        break;
                    }
                }
            }
            
            // Configurar el slot con los datos de la habilidad
            ConfigureSlot(uiSlot, abilityData);
            
            return uiSlot;
        }
        
        /// <summary>
        /// Configura un slot con los datos de la habilidad
        /// </summary>
        private void ConfigureSlot(AbilitySlotUI uiSlot, HeroAbilityController.AbilitySlot abilityData)
        {
            if (abilityData.abilityData == null)
            {
                DebugLog($"No hay datos de habilidad para el slot {uiSlot.slotIndex}", true);
                return;
            }
            
            // Guardar datos para depuración
            string abilityName = abilityData.abilityData.Name;
            string abilityHotkey = abilityData.abilityData.Hotkey;
            int manaCost = abilityData.abilityData.ManaCost;
            float damage = abilityData.abilityData.DamageAmount;
            float duration = abilityData.abilityData.Duration;
            float range = abilityData.abilityData.Range;
            
            // Configurar icono
            if (uiSlot.abilityIcon != null && abilityData.abilityData.IconSprite != null)
            {
                uiSlot.abilityIcon.sprite = abilityData.abilityData.IconSprite;
                uiSlot.abilityIcon.enabled = true;
                DebugLog($"Icono configurado para {abilityName}");
            }
            else
            {
                DebugLog($"No se pudo configurar el icono para {abilityName}. abilityIcon nulo: {uiSlot.abilityIcon == null}, IconSprite nulo: {abilityData.abilityData.IconSprite == null}", true);
            }
            
            // Configurar texto de hotkey
            if (uiSlot.hotkeyText != null)
            {
                uiSlot.hotkeyText.text = abilityHotkey;
                DebugLog($"Hotkey configurado para {abilityName}: {abilityHotkey}");
            }
            
            // Configurar texto de costo de maná
            if (uiSlot.manaCostText != null)
            {
                uiSlot.manaCostText.text = manaCost.ToString();
                DebugLog($"Mana cost configurado para {abilityName}: {manaCost}");
            }
            
            // Configurar texto de nombre de habilidad
            if (uiSlot.abilityNameText != null)
            {
                uiSlot.abilityNameText.text = abilityName;
                DebugLog($"Ability name configurado para {abilityName}");
                
                // Hacer un log del estado del texto después de configurarlo
                DebugLog($"AbilityNameText después de configurar: text='{uiSlot.abilityNameText.text}', " +
                      $"enabled={uiSlot.abilityNameText.enabled}, " +
                      $"gameObject.active={uiSlot.abilityNameText.gameObject.activeSelf}, " +
                      $"color={uiSlot.abilityNameText.color}, " +
                      $"alpha={uiSlot.abilityNameText.color.a}, " +
                      $"fontSize={uiSlot.abilityNameText.fontSize}");
            }
            else
            {
                DebugLog($"AbilityNameText es nulo para {abilityName}, no se puede configurar el nombre", true);
            }
            
            // Configurar texto de información adicional (daño, duración, rango)
            if (uiSlot.abilityInfoText != null)
            {
                StringBuilder infoText = new StringBuilder();
                
                if (damage > 0)
                    infoText.AppendLine($"DMG: {damage}");
                
                if (duration > 0)
                    infoText.AppendLine($"DUR: {duration}s");
                
                if (range > 0)
                    infoText.AppendLine($"RNG: {range}");
                
                uiSlot.abilityInfoText.text = infoText.ToString();
                DebugLog($"Ability info configurado para {abilityName}: {infoText}");
            }
            
            // Inicializar overlay de cooldown (inicialmente invisible)
            if (uiSlot.cooldownOverlay != null)
            {
                uiSlot.cooldownOverlay.fillAmount = 0f;
                uiSlot.cooldownOverlay.enabled = false;
            }
            
            // Ocultar texto de cooldown inicialmente
            if (uiSlot.cooldownText != null)
            {
                uiSlot.cooldownText.gameObject.SetActive(false);
            }
            
            // Configurar botón si existe
            if (uiSlot.abilityButton != null)
            {
                // Remover listeners anteriores
                uiSlot.abilityButton.onClick.RemoveAllListeners();
                
                // Añadir el listener para usar la habilidad
                int index = uiSlot.slotIndex; // Capturar el índice actual
                uiSlot.abilityButton.onClick.AddListener(() => OnAbilityButtonClicked(index));
            }
        }
        
        /// <summary>
        /// Limpia todos los slots de habilidad existentes
        /// </summary>
        private void ClearAbilitySlots()
        {
            foreach (AbilitySlotUI slot in abilitySlots)
            {
                if (slot.slotObject != null)
                {
                    Destroy(slot.slotObject);
                }
            }
            
            abilitySlots.Clear();
        }
        
        /// <summary>
        /// Limpia los objetos de depuración creados
        /// </summary>
        private void ClearDebugObjects()
        {
            foreach (GameObject obj in debugObjects)
            {
                Destroy(obj);
            }
            
            debugObjects.Clear();
        }
        
        /// <summary>
        /// Organiza los slots según la configuración de layout
        /// </summary>
        private void ArrangeSlots()
        {
            // Si estamos usando un LayoutGroup, no necesitamos posicionar manualmente
            if (slotsContainer.GetComponent<HorizontalLayoutGroup>() != null || 
                slotsContainer.GetComponent<VerticalLayoutGroup>() != null)
            {
                return;
            }
            
            // Posicionamiento manual si no hay LayoutGroup
            float totalWidth = (slotSize.x * abilitySlots.Count) + (slotSpacing * (abilitySlots.Count - 1));
            float startX = -totalWidth / 2 + slotSize.x / 2;
            
            for (int i = 0; i < abilitySlots.Count; i++)
            {
                if (abilitySlots[i].slotObject != null)
                {
                    RectTransform rt = abilitySlots[i].slotObject.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        if (useHorizontalLayout)
                        {
                            rt.anchoredPosition = new Vector2(startX + i * (slotSize.x + slotSpacing), 0);
                        }
                        else
                        {
                            rt.anchoredPosition = new Vector2(0, startX + i * (slotSize.y + slotSpacing));
                        }
                        
                        rt.sizeDelta = slotSize;
                    }
                }
            }
        }
        
        /// <summary>
        /// Actualiza la UI de habilidades (cooldowns, etc.)
        /// </summary>
        private void UpdateAbilityUI()
        {
            if (abilityController == null)
                return;
                
            for (int i = 0; i < abilitySlots.Count; i++)
            {
                if (i >= abilityController.abilitySlots.Count)
                    break;
                    
                HeroAbilityController.AbilitySlot abilityData = abilityController.abilitySlots[i];
                AbilitySlotUI uiSlot = abilitySlots[i];
                
                // Actualizar overlay de cooldown
                if (uiSlot.cooldownOverlay != null)
                {
                    if (abilityData.cooldownRemaining > 0)
                    {
                        float cooldownRatio = abilityData.cooldownRemaining / abilityData.abilityData.Cooldown;
                        uiSlot.cooldownOverlay.fillAmount = cooldownRatio;
                        uiSlot.cooldownOverlay.enabled = true;
                        
                        // Mostrar texto de tiempo restante
                        if (uiSlot.cooldownText != null)
                        {
                            uiSlot.cooldownText.gameObject.SetActive(true);
                            uiSlot.cooldownText.text = Mathf.Ceil(abilityData.cooldownRemaining).ToString("0");
                        }
                    }
                    else
                    {
                        uiSlot.cooldownOverlay.fillAmount = 0f;
                        uiSlot.cooldownOverlay.enabled = false;
                        
                        // Ocultar texto de cooldown
                        if (uiSlot.cooldownText != null)
                        {
                            uiSlot.cooldownText.gameObject.SetActive(false);
                        }
                    }
                }
                
                // Actualizar efecto visual si no hay suficiente maná
                bool enoughMana = heroBase != null && heroBase.currentMana >= abilityData.abilityData.ManaCost;
                
                // Cambiar color del icono según si hay suficiente maná
                if (uiSlot.abilityIcon != null)
                {
                    uiSlot.abilityIcon.color = enoughMana ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
                }
                
                // Cambiar color del texto de maná
                if (uiSlot.manaCostText != null)
                {
                    uiSlot.manaCostText.color = enoughMana ? Color.white : Color.red;
                }
                
                // Actualizar interactividad del botón
                if (uiSlot.abilityButton != null)
                {
                    uiSlot.abilityButton.interactable = enoughMana && abilityData.cooldownRemaining <= 0 && !heroBase.IsDead;
                }
            }
        }
        
        /// <summary>
        /// Método público para actualizar todos los slots de la UI
        /// </summary>
        public void RefreshAbilityUI()
        {
            InitializeAbilitySlots();
        }
        
        /// <summary>
        /// Para llamar desde botones de UI
        /// </summary>
        public void OnAbilityButtonClicked(int index)
        {
            if (abilityController != null)
            {
                abilityController.OnAbilityButtonClicked(index);
            }
        }
        
        /// <summary>
        /// Muestra un mensaje de depuración si está habilitado
        /// </summary>
        private void DebugLog(string message, bool isError = false)
        {
            if (enableDebugOutput)
            {
                if (isError)
                    Debug.LogError($"[GameAbilityUI] {message}");
                else
                    Debug.Log($"[GameAbilityUI] {message}");
            }
        }
        
        /// <summary>
        /// Busca un hijo recursivamente por nombre
        /// </summary>
        private Transform FindChildRecursively(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name.Contains(name))
                {
                    return child;
                }
                
                Transform result = FindChildRecursively(child, name);
                if (result != null)
                {
                    return result;
                }
            }
            
            return null;
        }
    }
}