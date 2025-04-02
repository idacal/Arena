using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace Photon.Pun.Demo.Asteroids
{
    /// <summary>
    /// Sistema para mostrar mensajes informativos en el juego
    /// </summary>
    public class GameMessageSystem : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Panel que contiene el mensaje")]
        public GameObject messagePanel;
        
        [Tooltip("Componente de texto para mostrar el mensaje")]
        public TMP_Text messageText;
        
        [Tooltip("Animador para efectos de aparición/desaparición")]
        public Animator panelAnimator;
        
        [Header("Message Settings")]
        [Tooltip("Tiempo que permanece visible el mensaje por defecto")]
        public float defaultDisplayTime = 3f;
        
        [Tooltip("Nombre del trigger de animación para mostrar")]
        public string showTrigger = "Show";
        
        [Tooltip("Nombre del trigger de animación para ocultar")]
        public string hideTrigger = "Hide";
        
        // Cola de mensajes para mostrar en secuencia
        private Queue<MessageData> messageQueue = new Queue<MessageData>();
        
        // Control de estado
        private bool isShowingMessage = false;
        
        // Estructura para almacenar datos de mensajes
        private struct MessageData
        {
            public string text;
            public float displayTime;
            
            public MessageData(string text, float displayTime)
            {
                this.text = text;
                this.displayTime = displayTime;
            }
        }
        
        private void Awake()
        {
            // Ocultar el panel al inicio
            if (messagePanel != null)
            {
                messagePanel.SetActive(false);
            }
        }
        
        /// <summary>
        /// Muestra un mensaje en el panel con el tiempo predeterminado
        /// </summary>
        public void ShowMessage(string message)
        {
            ShowMessage(message, defaultDisplayTime);
        }
        
        /// <summary>
        /// Muestra un mensaje en el panel por un tiempo específico
        /// </summary>
        public void ShowMessage(string message, float displayTime)
        {
            // Agregar mensaje a la cola
            messageQueue.Enqueue(new MessageData(message, displayTime));
            
            // Si no estamos mostrando un mensaje, iniciar el proceso
            if (!isShowingMessage)
            {
                StartCoroutine(ProcessMessageQueue());
            }
        }
        
        /// <summary>
        /// Procesa la cola de mensajes secuencialmente
        /// </summary>
        private IEnumerator ProcessMessageQueue()
        {
            isShowingMessage = true;
            
            while (messageQueue.Count > 0)
            {
                // Obtener el siguiente mensaje de la cola
                MessageData currentMessage = messageQueue.Dequeue();
                
                // Configurar y mostrar el mensaje
                yield return StartCoroutine(DisplayMessage(currentMessage.text, currentMessage.displayTime));
            }
            
            isShowingMessage = false;
        }
        
        /// <summary>
        /// Muestra un mensaje individual por un tiempo específico
        /// </summary>
        private IEnumerator DisplayMessage(string message, float displayTime)
        {
            // Configurar el texto
            if (messageText != null)
            {
                messageText.text = message;
            }
            
            // Mostrar el panel
            if (messagePanel != null)
            {
                messagePanel.SetActive(true);
                
                // Activar animación si existe
                if (panelAnimator != null)
                {
                    panelAnimator.SetTrigger(showTrigger);
                    // Esperar a que la animación termine (opcional)
                    yield return new WaitForSeconds(0.5f);
                }
            }
            
            // Esperar el tiempo de visualización
            yield return new WaitForSeconds(displayTime);
            
            // Ocultar con animación si existe
            if (panelAnimator != null)
            {
                panelAnimator.SetTrigger(hideTrigger);
                // Esperar a que la animación termine
                yield return new WaitForSeconds(0.5f);
            }
            
            // Ocultar el panel
            if (messagePanel != null)
            {
                messagePanel.SetActive(false);
            }
        }
    }
} 