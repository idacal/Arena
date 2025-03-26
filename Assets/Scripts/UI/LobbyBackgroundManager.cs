using UnityEngine;
using UnityEngine.SceneManagement;

namespace Photon.Pun.Demo.Asteroids
{
    public class LobbyBackgroundManager : MonoBehaviour
    {
        private static LobbyBackgroundManager instance;
        public static LobbyBackgroundManager Instance => instance;

        [Header("Background Settings")]
        public GameObject starfieldPrefab;
        private GameObject currentStarfield;
        
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
            // Suscribirse al evento de cambio de escena
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            // Crear el fondo de estrellas inicial
            CreateStarfield();
        }
        
        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Verificar si estamos en una escena que necesita el fondo
            if (scene.name.Contains("Lobby") || scene.name.Contains("HeroSelection"))
            {
                if (currentStarfield == null)
                {
                    CreateStarfield();
                }
            }
            else
            {
                // Si estamos en otra escena, destruir el fondo
                if (currentStarfield != null)
                {
                    Destroy(currentStarfield);
                    currentStarfield = null;
                }
            }
        }
        
        private void CreateStarfield()
        {
            if (starfieldPrefab != null && currentStarfield == null)
            {
                currentStarfield = Instantiate(starfieldPrefab, transform);
                currentStarfield.transform.SetParent(transform);
                currentStarfield.transform.localPosition = Vector3.zero;
            }
        }
    }
} 