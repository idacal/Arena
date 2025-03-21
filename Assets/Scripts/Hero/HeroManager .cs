using UnityEngine;
using ExitGames.Client.Photon;
using System.Collections.Generic;
using Photon.Realtime;

namespace Photon.Pun.Demo.Asteroids
{
    /// <summary>
    /// Este manager gestiona los héroes en todo el juego (persiste entre escenas)
    /// </summary>
    public class HeroManager : MonoBehaviourPunCallbacks
    {
        [Header("Configuración de Héroes")]
        public List<HeroDataSO> AvailableHeroesSO;
        
        // Constants for custom properties
        private const string PLAYER_SELECTED_HERO = "SelectedHero";
        private const string PLAYER_TEAM = "PlayerTeam";
        
        // Team constants
        private const int TEAM_RED = 0;
        private const int TEAM_BLUE = 1;
        
        // Singleton instance
        private static HeroManager _instance;
        public static HeroManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<HeroManager>();
                    
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject("HeroManager");
                        _instance = obj.AddComponent<HeroManager>();
                    }
                }
                
                return _instance;
            }
        }

        // Lista de héroes disponibles en formato de datos
        private List<HeroData> availableHeroes = new List<HeroData>();
        
        // Dictionary of hero prefabs indexed by hero ID
        private Dictionary<int, GameObject> heroPrefabs = new Dictionary<int, GameObject>();

        void Awake()
        {
            // Singleton pattern - ensure only one instance exists
            if (_instance != null && _instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
            
            // Convertir ScriptableObjects a HeroData
            ConvertScriptableObjectsToHeroData();
            
            // Cargar prefabs de héroes
            LoadHeroPrefabs();
        }
        
        /// <summary>
        /// Convierte los ScriptableObjects a HeroData
        /// </summary>
        private void ConvertScriptableObjectsToHeroData()
        {
            availableHeroes.Clear();
            
            foreach (var heroSO in AvailableHeroesSO)
            {
                if (heroSO != null)
                {
                    availableHeroes.Add(heroSO.ToHeroData());
                }
            }
            
            Debug.Log($"HeroManager: Cargados {availableHeroes.Count} héroes");
        }

        /// <summary>
        /// Carga los prefabs de los héroes desde Resources
        /// </summary>
        private void LoadHeroPrefabs()
        {
            heroPrefabs.Clear();
            
            foreach (var hero in availableHeroes)
            {
                if (!string.IsNullOrEmpty(hero.PrefabName))
                {
                    GameObject prefab = Resources.Load<GameObject>("Heroes/" + hero.PrefabName);
                    if (prefab != null)
                    {
                        heroPrefabs.Add(hero.Id, prefab);
                        Debug.Log($"HeroManager: Cargado prefab para {hero.Name}");
                    }
                    else
                    {
                        Debug.LogWarning($"HeroManager: No se encontró el prefab '{hero.PrefabName}' para el héroe {hero.Name}");
                    }
                }
            }
        }

        /// <summary>
        /// Obtiene todos los datos de los héroes disponibles
        /// </summary>
        public List<HeroData> GetAllHeroes()
        {
            return availableHeroes;
        }

        /// <summary>
        /// Obtiene los datos de un héroe por su ID
        /// </summary>
        public HeroData GetHeroData(int heroId)
        {
            foreach (var hero in availableHeroes)
            {
                if (hero.Id == heroId)
                {
                    return hero;
                }
            }
            
            return null;
        }

        /// <summary>
        /// Obtiene el ID del héroe seleccionado por un jugador
        /// </summary>
        public int GetPlayerSelectedHeroId(Player player)
        {
            object heroIdObj;
            if (player.CustomProperties.TryGetValue(PLAYER_SELECTED_HERO, out heroIdObj) && heroIdObj != null)
            {
                return (int)heroIdObj;
            }
            
            return -1;
        }

        /// <summary>
        /// Obtiene el equipo asignado a un jugador
        /// </summary>
        public int GetPlayerTeam(Player player)
        {
            object teamObj;
            if (player.CustomProperties.TryGetValue(PLAYER_TEAM, out teamObj) && teamObj != null)
            {
                return (int)teamObj;
            }
            
            return -1;
        }

        /// <summary>
        /// Instancia el héroe seleccionado por un jugador en el mundo
        /// </summary>
        public GameObject InstantiatePlayerHero(Player player, Vector3 position, Quaternion rotation)
        {
            int heroId = GetPlayerSelectedHeroId(player);
            
            if (heroId == -1)
            {
                Debug.LogError("Player has not selected a hero!");
                return null;
            }
            
            // Obtener el prefab del héroe
            GameObject heroPrefab = null;
            
            if (heroPrefabs.TryGetValue(heroId, out heroPrefab) && heroPrefab != null)
            {
                // Instanciar el héroe
                if (player.IsLocal)
                {
                    // Para el jugador local, usar PhotonNetwork.Instantiate para asegurar propiedad
                    return PhotonNetwork.Instantiate(heroPrefab.name, position, rotation);
                }
                else
                {
                    // Para jugadores remotos, simplemente instanciar localmente
                    // Esto asume que el objeto del jugador actual es creado por el propietario via PhotonNetwork.Instantiate
                    return Instantiate(heroPrefab, position, rotation);
                }
            }
            
            Debug.LogError("Hero prefab not found for hero ID: " + heroId);
            return null;
        }

        /// <summary>
        /// Obtiene una lista de todos los IDs de héroes seleccionados por jugadores en un equipo específico
        /// </summary>
        public List<int> GetTeamHeroIds(int team)
        {
            List<int> heroIds = new List<int>();
            
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                int playerTeam = GetPlayerTeam(p);
                
                if (playerTeam == team)
                {
                    int heroId = GetPlayerSelectedHeroId(p);
                    
                    if (heroId != -1)
                    {
                        heroIds.Add(heroId);
                    }
                }
            }
            
            return heroIds;
        }
    }
}