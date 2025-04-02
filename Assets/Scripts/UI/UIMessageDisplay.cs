using UnityEngine;
using TMPro;
using System.Collections;

namespace Photon.Pun.Demo.Asteroids
{
    /// <summary>
    /// Componente simple para mostrar mensajes temporales en la UI
    /// </summary>
    public class UIMessageDisplay : MonoBehaviour
    {
        [Tooltip("Referencia al componente de texto donde se mostrará el mensaje")]
        public TMP_Text messageText;
        
        [Tooltip("Contenedor que muestra/oculta todo el elemento de mensaje")]
        public GameObject messageContainer;
        
        [Tooltip("Duración predeterminada de los mensajes")]
        public float defaultDuration = 3f;
        
        [Tooltip("Curva de animación para la aparición/desaparición del texto")]
        public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Tooltip("Duración de la animación de fade")]
        public float fadeDuration = 0.5f;
        
        // Referencia a la coroutine actual para poder cancelarla
        private Coroutine activeMessageCoroutine;
        
        private void Awake()
        {
            // Asegurarse de que el mensaje esté oculto al inicio
            if (messageContainer != null)
            {
                messageContainer.SetActive(false);
            }
            
            // Si no hay un componente de texto asignado, intentar encontrarlo
            if (messageText == null)
            {
                messageText = GetComponentInChildren<TMP_Text>(true);
                
                if (messageText == null)
                {
                    Debug.LogWarning("[UIMessageDisplay] No se encontró componente TMP_Text. Los mensajes no se mostrarán correctamente.");
                }
            }
        }
        
        /// <summary>
        /// Muestra un mensaje en la UI por la duración predeterminada
        /// </summary>
        public void DisplayMessage(string message)
        {
            DisplayMessage(message, defaultDuration);
        }
        
        /// <summary>
        /// Muestra un mensaje en la UI por una duración específica
        /// </summary>
        public void DisplayMessage(string message, float duration)
        {
            // Cancelar el mensaje actual si existe
            if (activeMessageCoroutine != null)
            {
                StopCoroutine(activeMessageCoroutine);
            }
            
            // Mostrar el nuevo mensaje
            activeMessageCoroutine = StartCoroutine(ShowMessageCoroutine(message, duration));
        }
        
        /// <summary>
        /// Corrutina para mostrar y ocultar el mensaje con animación
        /// </summary>
        private IEnumerator ShowMessageCoroutine(string message, float duration)
        {
            // Configurar el texto
            if (messageText != null)
            {
                messageText.text = message;
                messageText.alpha = 0f;
            }
            
            // Mostrar el contenedor
            if (messageContainer != null)
            {
                messageContainer.SetActive(true);
            }
            
            // Fade in
            float startTime = Time.time;
            while (Time.time < startTime + fadeDuration)
            {
                float t = (Time.time - startTime) / fadeDuration;
                float alpha = fadeCurve.Evaluate(t);
                
                if (messageText != null)
                {
                    messageText.alpha = alpha;
                }
                
                yield return null;
            }
            
            // Asegurar que esté completamente visible
            if (messageText != null)
            {
                messageText.alpha = 1f;
            }
            
            // Mantener el mensaje visible
            yield return new WaitForSeconds(duration);
            
            // Fade out
            startTime = Time.time;
            while (Time.time < startTime + fadeDuration)
            {
                float t = (Time.time - startTime) / fadeDuration;
                float alpha = fadeCurve.Evaluate(1f - t);
                
                if (messageText != null)
                {
                    messageText.alpha = alpha;
                }
                
                yield return null;
            }
            
            // Ocultar completamente
            if (messageContainer != null)
            {
                messageContainer.SetActive(false);
            }
            
            if (messageText != null)
            {
                messageText.alpha = 0f;
            }
            
            activeMessageCoroutine = null;
        }
    }
} 