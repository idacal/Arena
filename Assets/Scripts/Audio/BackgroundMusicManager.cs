using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Photon.Pun.Demo.Asteroids
{
    public class BackgroundMusicManager : MonoBehaviour
    {
        private static BackgroundMusicManager instance;
        public static BackgroundMusicManager Instance => instance;

        [Header("Music Settings")]
        public AudioClip lobbyMusic;
        public AudioClip gameplayMusic;
        [Range(0f, 1f)]
        public float defaultVolume = 0.5f;
        
        [Header("UI Elements")]
        public Slider volumeSlider;
        public GameObject volumePanel;
        
        private AudioSource audioSource;
        private string currentScene;
        private const string VolumePrefsKey = "BackgroundMusicVolume";
        
        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeVolume();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// Inicializa el volumen desde PlayerPrefs o usa el valor por defecto
        /// </summary>
        private void InitializeVolume()
        {
            // Carga el volumen guardado o usa el valor por defecto
            float savedVolume = PlayerPrefs.GetFloat(VolumePrefsKey, defaultVolume);
            
            // Asegura que el valor esté en el rango válido
            savedVolume = Mathf.Clamp01(savedVolume);
            
            // Crea el AudioSource
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.loop = true;
                audioSource.playOnAwake = true;
            }
            
            // Aplica el volumen
            audioSource.volume = savedVolume;
            
            Debug.Log($"[BackgroundMusicManager] Volumen inicializado: {savedVolume}");
        }
        
        void Start()
        {
            // Configurar slider si está asignado
            SetupVolumeSlider();
            
            // Suscribirse al evento de cambio de escena
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            // Iniciar con la música del lobby
            PlayLobbyMusic();
        }
        
        /// <summary>
        /// Configura el slider de volumen si está disponible
        /// </summary>
        private void SetupVolumeSlider()
        {
            if (volumeSlider != null)
            {
                // Asignar valor actual
                volumeSlider.value = audioSource.volume;
                
                // Remover listeners existentes para evitar duplicados
                volumeSlider.onValueChanged.RemoveAllListeners();
                
                // Añadir listener para cambios de volumen
                volumeSlider.onValueChanged.AddListener(SetVolume);
                
                Debug.Log("[BackgroundMusicManager] Slider de volumen configurado");
            }
        }
        
        /// <summary>
        /// Busca y configura un slider de volumen en la escena actual
        /// </summary>
        public void FindAndSetupVolumeSlider(Slider slider)
        {
            if (slider != null)
            {
                volumeSlider = slider;
                SetupVolumeSlider();
            }
        }
        
        /// <summary>
        /// Muestra u oculta el panel de control de volumen
        /// </summary>
        public void ToggleVolumePanel(bool show)
        {
            if (volumePanel != null)
            {
                volumePanel.SetActive(show);
            }
        }
        
        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            currentScene = scene.name;
            
            // Cambiar la música según la escena
            if (currentScene.Contains("Lobby") || currentScene.Contains("HeroSelection"))
            {
                PlayLobbyMusic();
            }
            else if (currentScene.Contains("Gameplay"))
            {
                PlayGameplayMusic();
            }
        }
        
        public void PlayLobbyMusic()
        {
            if (audioSource.clip != lobbyMusic)
            {
                audioSource.clip = lobbyMusic;
                audioSource.Play();
            }
        }
        
        public void PlayGameplayMusic()
        {
            if (audioSource.clip != gameplayMusic)
            {
                audioSource.clip = gameplayMusic;
                audioSource.Play();
            }
        }
        
        /// <summary>
        /// Establece el volumen y lo guarda en PlayerPrefs
        /// </summary>
        public void SetVolume(float volume)
        {
            // Asegurar que el volumen esté en el rango correcto
            volume = Mathf.Clamp01(volume);
            
            // Aplicar al audio source
            if (audioSource != null)
            {
                audioSource.volume = volume;
            }
            
            // Actualizar el slider si existe
            if (volumeSlider != null && Mathf.Abs(volumeSlider.value - volume) > 0.01f)
            {
                volumeSlider.value = volume;
            }
            
            // Guardar en PlayerPrefs para persistencia
            PlayerPrefs.SetFloat(VolumePrefsKey, volume);
            PlayerPrefs.Save();
            
            Debug.Log($"[BackgroundMusicManager] Volumen establecido: {volume}");
        }
        
        /// <summary>
        /// Obtiene el volumen actual
        /// </summary>
        public float GetVolume()
        {
            return audioSource != null ? audioSource.volume : defaultVolume;
        }
        
        /// <summary>
        /// Método para silenciar/reactivar la música
        /// </summary>
        public void ToggleMute()
        {
            if (audioSource != null)
            {
                if (audioSource.volume > 0)
                {
                    // Guardamos el volumen actual antes de silenciar
                    PlayerPrefs.SetFloat("PreviousMusicVolume", audioSource.volume);
                    SetVolume(0);
                }
                else
                {
                    // Recuperamos el volumen anterior o usamos el valor por defecto
                    float previousVolume = PlayerPrefs.GetFloat("PreviousMusicVolume", defaultVolume);
                    SetVolume(previousVolume);
                }
                
                Debug.Log($"[BackgroundMusicManager] Mute toggled: {audioSource.volume <= 0}");
            }
        }
    }
} 