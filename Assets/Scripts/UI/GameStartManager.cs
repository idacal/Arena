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
        private float currentCountdownTime;
        private bool hasStartedGameSound = false;
        
        void Awake()
        {
            // Asegurarse de que tenemos un PhotonView
            if (GetComponent<PhotonView>() == null)
            {
                gameObject.AddComponent<PhotonView>();
            }
        }
        
        void Start()
        {
            // Configurar el audio source
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.volume = soundVolume;
            
            // Obtener referencia al HeroSelectionManager
            heroSelectionManager = FindObjectOfType<HeroSelectionManager>();
            
            // Configurar el botón de inicio
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartButtonClicked);
            }
            
            // Asegurarse de que el panel de countdown está oculto al inicio
            if (countdownPanel != null)
            {
                countdownPanel.SetActive(false);
            }
        }
        
        void Update()
        {
            // Si estamos contando y no somos el MasterClient, actualizar el tiempo local
            if (isCountingDown && !PhotonNetwork.IsMasterClient)
            {
                UpdateLocalCountdown();
            }
        }
        
        private void UpdateLocalCountdown()
        {
            if (currentCountdownTime > 0)
            {
                currentCountdownTime -= Time.deltaTime;
                UpdateCountdownDisplay(currentCountdownTime);
            }
        }
        
        private void UpdateCountdownDisplay(float timeRemaining)
        {
            if (countdownText != null)
            {
                int secondsRemaining = Mathf.CeilToInt(timeRemaining);
                countdownText.text = secondsRemaining.ToString();
                
                // Reproducir sonido de countdown
                if (secondsRemaining > 0 && countdownSound != null && audioSource != null)
                {
                    if (Mathf.Approximately(timeRemaining, Mathf.Floor(timeRemaining)))
                    {
                        audioSource.PlayOneShot(countdownSound);
                    }
                }
                
                // Reproducir sonido de inicio cuando llegue a 1
                if (secondsRemaining == 1 && !hasStartedGameSound && gameStartSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(gameStartSound);
                    hasStartedGameSound = true;
                }
            }
        }
        
        public void OnStartButtonClicked()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("[GameStartManager] Solo el MasterClient puede iniciar el contador");
                return;
            }
            
            if (isCountingDown || isSceneLoading)
            {
                Debug.LogWarning("[GameStartManager] El contador ya está en marcha o la escena se está cargando");
                return;
            }
            
            Debug.Log("[GameStartManager] Iniciando countdown");
            photonView.RPC("RPC_StartCountdown", RpcTarget.All);
        }
        
        [PunRPC]
        private void RPC_StartCountdown()
        {
            if (isCountingDown || isSceneLoading)
            {
                Debug.LogWarning("[GameStartManager] El contador ya está en marcha o la escena se está cargando");
                return;
            }
            
            Debug.Log("[GameStartManager] Iniciando countdown");
            isCountingDown = true;
            currentCountdownTime = countdownDuration;
            hasStartedGameSound = false;
            
            // Mostrar el panel de countdown
            if (countdownPanel != null)
            {
                countdownPanel.SetActive(true);
            }
            
            // Si somos el MasterClient, iniciamos la corrutina de countdown
            if (PhotonNetwork.IsMasterClient)
            {
                StartCoroutine(CountdownCoroutine());
            }
        }
        
        [PunRPC]
        private void RPC_UpdateCountdown(float remainingTime)
        {
            currentCountdownTime = remainingTime;
            UpdateCountdownDisplay(remainingTime);
        }
        
        [PunRPC]
        private void RPC_LoadGameScene()
        {
            Debug.Log("[GameStartManager] Cargando escena de juego");
            isSceneLoading = true;
            
            // Deshabilitar el botón de inicio
            if (heroSelectionManager != null)
            {
                heroSelectionManager.StartGameButton.interactable = false;
            }
            
            // Cambiar a la escena de gameplay
            PhotonNetwork.LoadLevel("GameplayScene");
        }
        
        private IEnumerator CountdownCoroutine()
        {
            float remainingTime = countdownDuration;
            
            while (remainingTime > 0)
            {
                // Sincronizar el tiempo con todos los clientes
                photonView.RPC("RPC_UpdateCountdown", RpcTarget.All, remainingTime);
                
                yield return new WaitForSeconds(countdownInterval);
                remainingTime -= countdownInterval;
            }
            
            // Mostrar "0" final
            photonView.RPC("RPC_UpdateCountdown", RpcTarget.All, 0f);
            
            // Esperar un momento antes de cargar la escena
            yield return new WaitForSeconds(4f);
            
            // Cargar la escena de juego
            photonView.RPC("RPC_LoadGameScene", RpcTarget.All);
        }
    }
} 