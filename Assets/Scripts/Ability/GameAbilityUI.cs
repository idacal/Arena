using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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

        [Header("References")]
        public HeroAbilityController abilityController;
        
        // Lista dinámica de slots de UI
        private List<AbilitySlotUI> abilitySlots = new List<AbilitySlotUI>();
        private HeroBase heroBase;

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
                Debug.LogError("No se encontró HeroAbilityController. La UI de habilidades no funcionará correctamente.");
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
                Debug.LogWarning("No se encontró el controlador de habilidades.");
                return;
            }
            
            // Antes de crear nuevos slots, limpiar los existentes
            ClearAbilitySlots();
            
            // Verificar si hay habilidades para mostrar
            if (abilityController.abilitySlots.Count == 0)
            {
                Debug.LogWarning("No hay habilidades configuradas para este héroe.");
                return;
            }
            
            Debug.Log($"Creando {abilityController.abilitySlots.Count} slots de habilidades");
            
            // Crear un slot de UI para cada habilidad en el controlador
            for (int i = 0; i < abilityController.abilitySlots.Count; i++)
            {
                HeroAbilityController.AbilitySlot abilityData = abilityController.abilitySlots[i];
                
                // Crear slot de UI
                AbilitySlotUI uiSlot = CreateAbilitySlot(i, abilityData);
                abilitySlots.Add(uiSlot);
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
                Debug.LogError("No hay prefab configurado para los slots de habilidad. Crea un prefab y asígnalo en el inspector.");
                return null;
            }
            
            // Instanciar el prefab
            GameObject slotObject = Instantiate(abilitySlotPrefab, slotsContainer);
            slotObject.name = $"AbilitySlot_{index}";
            
            // Crear la estructura del slot de UI
            AbilitySlotUI uiSlot = new AbilitySlotUI();
            uiSlot.slotObject = slotObject;
            uiSlot.slotIndex = index;
            
            // Intentar encontrar los componentes en el prefab
            uiSlot.abilityIcon = slotObject.GetComponentInChildren<Image>();
            uiSlot.abilityButton = slotObject.GetComponent<Button>();
            
            // Si hay un transform llamado "CooldownOverlay", buscar la imagen allí
            Transform cooldownTransform = slotObject.transform.Find("CooldownOverlay");
            if (cooldownTransform != null)
            {
                uiSlot.cooldownOverlay = cooldownTransform.GetComponent<Image>();
            }
            
            // Buscar los textos por nombre
            Transform hotkeyTransform = slotObject.transform.Find("HotkeyText");
            if (hotkeyTransform != null)
            {
                uiSlot.hotkeyText = hotkeyTransform.GetComponent<TMP_Text>();
            }
            
            Transform cooldownTextTransform = slotObject.transform.Find("CooldownText");
            if (cooldownTextTransform != null)
            {
                uiSlot.cooldownText = cooldownTextTransform.GetComponent<TMP_Text>();
            }
            
            Transform manaCostTransform = slotObject.transform.Find("ManaCostText");
            if (manaCostTransform != null)
            {
                uiSlot.manaCostText = manaCostTransform.GetComponent<TMP_Text>();
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
                Debug.LogWarning($"No hay datos de habilidad para el slot {uiSlot.slotIndex}");
                return;
            }
            
            // Configurar icono
            if (uiSlot.abilityIcon != null && abilityData.abilityData.IconSprite != null)
            {
                uiSlot.abilityIcon.sprite = abilityData.abilityData.IconSprite;
                uiSlot.abilityIcon.enabled = true;
            }
            
            // Configurar texto de hotkey
            if (uiSlot.hotkeyText != null)
            {
                uiSlot.hotkeyText.text = abilityData.abilityData.Hotkey;
            }
            
            // Configurar texto de costo de maná
            if (uiSlot.manaCostText != null)
            {
                uiSlot.manaCostText.text = abilityData.abilityData.ManaCost.ToString();
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
    }
}