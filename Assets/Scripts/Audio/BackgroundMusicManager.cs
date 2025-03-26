using UnityEngine;
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
        
        private AudioSource audioSource;
        private string currentScene;
        
        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void Start()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.playOnAwake = true;
            
            // Suscribirse al evento de cambio de escena
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            // Iniciar con la música del lobby
            PlayLobbyMusic();
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
        
        public void SetVolume(float volume)
        {
            audioSource.volume = Mathf.Clamp01(volume);
        }
    }
} 