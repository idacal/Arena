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
        
        [Header("Damage/Heal Text")]
        public GameObject floatingTextPrefab;
        public Color damageColor = Color.red;
        public Color magicDamageColor = Color.magenta;
        public Color healColor = Color.green;
        
        [Header("Positioning")]
        public Vector3 floatingTextOffset = new Vector3(0, 2, 0);
        public Transform floatingTextParent;
        
        private Transform cameraTransform;
        
        void Start()
        {
            // Obtener la cámara
            cameraTransform = Camera.main.transform;
            
            // Configurar el padre de los textos flotantes
            if (floatingTextParent == null)
            {
                floatingTextParent = transform;
            }
        }
        
        void LateUpdate()
        {
            // Asegurar que la UI siempre mira a la cámara si hay uno
            if (cameraTransform != null)
            {
                // Solo rotar la UI, no los elementos del personaje
                if (GetComponent<Canvas>() != null)
                {
                    transform.LookAt(transform.position + cameraTransform.rotation * Vector3.forward, cameraTransform.rotation * Vector3.up);
                }
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
}