using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Photon.Pun;
using TMPro;
using UnityEngine.SceneManagement;

namespace Photon.Pun.Demo.Asteroids
{
    public class GameStartManager : MonoBehaviourPunCallbacks
    {
        [Header("UI Settings")]
        public TextMeshProUGUI countdownText;
        public Button startButton;
        public GameObject countdownPanel;
        
        [Header("Sound Settings")]
        public AudioClip countdownSound;
        public AudioClip gameStartSound;
        public float soundVolume = 0.5f;
        
        [Header("Countdown Settings")]
        public float countdownDuration = 5f;
        public float countdownInterval = 1f;
        
        private AudioSource audioSource;
        private bool isCountingDown = false;
        private bool isSceneLoading = false;
        private HeroSelectionManager heroSelectionManager;
        
        void Start()
        {
            // Obtener referencia al HeroSelectionManager
            heroSelectionManager = FindObjectOfType<HeroSelectionManager>();
            if (heroSelectionManager == null)
            {
                Debug.LogError("No se encontró el HeroSelectionManager!");
                return;
            }
            
            // Configurar el AudioSource
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = soundVolume;
            audioSource.spatialBlend = 0f; // Sonido 2D
            audioSource.priority = 128;
            audioSource.loop = false;
            audioSource.pitch = 1f;
            audioSource.panStereo = 0f;
            audioSource.spread = 0f;
            audioSource.dopplerLevel = 0f;
            audioSource.minDistance = 1f;
            audioSource.maxDistance = 500f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.bypassEffects = true; // Desactivar efectos de audio
            audioSource.bypassListenerEffects = true; // Desactivar efectos del listener
            audioSource.outputAudioMixerGroup = null; // No usar mixer group
            
            // Asegurarse de que el panel esté oculto al inicio
            if (countdownPanel != null)
            {
                countdownPanel.SetActive(false);
            }
            
            // Configurar el botón
            if (startButton != null)
            {
                startButton.onClick.RemoveAllListeners(); // Limpiar listeners existentes
                startButton.onClick.AddListener(StartCountdown);
            }
            else
            {
                Debug.LogError("Start Button no está asignado en el GameStartManager!");
            }
            
            // Verificar que el texto está asignado
            if (countdownText == null)
            {
                Debug.LogError("Countdown Text no está asignado en el GameStartManager!");
            }
            
            // Suscribirse al evento de carga de escena
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        
        void OnDestroy()
        {
            // Desuscribirse del evento de carga de escena
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"Escena cargada: {scene.name}");
        }
        
        public void StartCountdown()
        {
            if (!isCountingDown && !isSceneLoading)
            {
                Debug.Log("Iniciando countdown...");
                isCountingDown = true;
                
                // Deshabilitar el botón
                if (startButton != null)
                {
                    startButton.interactable = false;
                }
                
                // Mostrar el panel de countdown
                if (countdownPanel != null)
                {
                    countdownPanel.SetActive(true);
                    Debug.Log("Panel de countdown activado");
                }
                else
                {
                    Debug.LogError("Countdown Panel no está asignado en el GameStartManager!");
                }
                
                // Iniciar la corrutina del countdown
                StartCoroutine(CountdownRoutine());
            }
        }
        
        private IEnumerator CountdownRoutine()
        {
            float remainingTime = countdownDuration;
            bool hasStartedGameSound = false;
            
            while (remainingTime > 0)
            {
                // Actualizar el texto
                if (countdownText != null)
                {
                    countdownText.text = Mathf.Ceil(remainingTime).ToString();
                    Debug.Log($"Countdown: {Mathf.Ceil(remainingTime)}");
                }
                
                // Reproducir sonido de countdown
                if (countdownSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(countdownSound);
                }
                
                // Si llegamos a 1, comenzar a reproducir el sonido de inicio
                if (Mathf.Ceil(remainingTime) == 1 && !hasStartedGameSound && gameStartSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(gameStartSound);
                    hasStartedGameSound = true;
                }
                
                // Esperar el intervalo
                yield return new WaitForSeconds(countdownInterval);
                remainingTime -= countdownInterval;
            }
            
            // Mostrar "0"
            if (countdownText != null)
            {
                countdownText.text = "0";
                Debug.Log("0");
            }
            
            // Esperar 4 segundos antes de cambiar de escena
            yield return new WaitForSeconds(4f);
            
            Debug.Log("Iniciando carga de escena...");
            isSceneLoading = true;
            
            // Deshabilitar el botón de inicio en el HeroSelectionManager
            if (heroSelectionManager != null)
            {
                heroSelectionManager.StartGameButton.interactable = false;
            }
            
            // Cambiar a la escena de gameplay
            PhotonNetwork.LoadLevel("GameplayScene");
        }
    }
} 