using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Photon.Pun.Demo.Asteroids
{
    public class HealthBar : MonoBehaviour
    {
        [Header("Referencias")]
        public RectTransform fillRect;    // Referencia al RectTransform de la barra de relleno
        public Image fillImage;           // Referencia a la imagen de relleno
        public TextMeshProUGUI healthText; // Usando TextMeshPro en lugar de Text legacy
        public Image backgroundImage;     // Referencia al fondo de la barra de vida
        
        [Header("Configuración")]
        public Vector3 offset = new Vector3(0, 2f, 0);
        public bool alwaysFaceCamera = true;
        public Color fullHealthColor = Color.green;
        public Color lowHealthColor = Color.red;
        public float lowHealthThreshold = 0.3f; // 30% de vida se considera baja
        public bool hideAtFullHealth = true;    // Nueva opción para ocultar la barra cuando está llena
        
        private Transform target;
        private Camera mainCamera;
        private float maxHealth;
        private float currentHealth;
        private float originalFillWidth;
        private CanvasGroup canvasGroup;
        
        private void Awake()
        {
            // Obtener o agregar el CanvasGroup
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        private void Start()
        {
            mainCamera = Camera.main;
            if (fillRect != null)
            {
                originalFillWidth = fillRect.sizeDelta.x;
            }
        }
        
        private void LateUpdate()
        {
            if (target == null)
            {
                Destroy(gameObject);
                return;
            }
            
            // Actualizar posición
            transform.position = target.position + offset;
            
            // Hacer que la barra siempre mire a la cámara
            if (alwaysFaceCamera && mainCamera != null)
            {
                transform.LookAt(transform.position + mainCamera.transform.forward);
            }
        }
        
        public void Initialize(Transform targetTransform, float max, float current)
        {
            target = targetTransform;
            maxHealth = max;
            currentHealth = current;
            UpdateHealthBar();
        }
        
        public void UpdateHealth(float current)
        {
            currentHealth = current;
            UpdateHealthBar();
        }
        
        private void UpdateHealthBar()
        {
            float healthPercentage = currentHealth / maxHealth;
            
            // Ocultar o mostrar la barra según la vida
            if (hideAtFullHealth)
            {
                bool isFullHealth = Mathf.Approximately(healthPercentage, 1f);
                canvasGroup.alpha = isFullHealth ? 0f : 1f;
                canvasGroup.blocksRaycasts = !isFullHealth;
            }
            
            // Actualizar el ancho del relleno
            if (fillRect != null)
            {
                Vector2 sizeDelta = fillRect.sizeDelta;
                sizeDelta.x = originalFillWidth * healthPercentage;
                fillRect.sizeDelta = sizeDelta;
            }
            
            // Actualizar el color basado en la vida restante
            if (fillImage != null)
            {
                fillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, healthPercentage);
            }
            
            // Actualizar el texto
            if (healthText != null)
            {
                healthText.text = $"{Mathf.Round(currentHealth)}/{Mathf.Round(maxHealth)}";
            }
        }
    }
} 