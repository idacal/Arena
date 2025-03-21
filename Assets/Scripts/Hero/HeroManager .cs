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
        
        [Header("Depuración")]
        public bool debugMode = true;
        
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
                        DontDestroyOnLoad(obj);
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
            
            // Verificar que AvailableHeroesSO no sea nulo
            if (AvailableHeroesSO == null || AvailableHeroesSO.Count == 0)
            {
                LogDebug("¡ADVERTENCIA! No hay ScriptableObjects de héroes configurados. Verifica la lista AvailableHeroesSO en el inspector.");
            }
            else
            {
                LogDebug($"HeroManager inicializado con {AvailableHeroesSO.Count} ScriptableObjects de héroes.");
            }
            
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
            
            if (AvailableHeroesSO == null)
                return;
                
            foreach (var heroSO in AvailableHeroesSO)
            {
                if (heroSO != null)
                {
                    LogDebug($"Convirtiendo ScriptableObject a HeroData: {heroSO.Name}");
                    availableHeroes.Add(heroSO.ToHeroData());
                }
                else
                {
                    LogDebug("¡ADVERTENCIA! Se encontró un ScriptableObject nulo en la lista.");
                }
            }
            
            LogDebug($"Conversión completada. Total de HeroData: {availableHeroes.Count}");
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
                    // Intentar cargar el prefab usando la estructura de carpetas "Heroes/[PrefabName]"
                    GameObject prefab = Resources.Load<GameObject>("Heroes/" + hero.PrefabName);
                    
                    if (prefab != null)
                    {
                        LogDebug($"Prefab cargado con éxito: Heroes/{hero.PrefabName} para héroe ID: {hero.Id}");
                        heroPrefabs.Add(hero.Id, prefab);
                    }
                    else
                    {
                        // Intentar cargar directamente de la carpeta Resources
                        prefab = Resources.Load<GameObject>(hero.PrefabName);
                        
                        if (prefab != null)
                        {
                            LogDebug($"Prefab cargado con éxito (directamente): {hero.PrefabName} para héroe ID: {hero.Id}");
                            heroPrefabs.Add(hero.Id, prefab);
                        }
                        else
                        {
                            Debug.LogError($"¡No se pudo cargar el prefab '{hero.PrefabName}' para el héroe {hero.Name} (ID: {hero.Id})!");
                            Debug.LogError("Asegúrate de que el prefab está en la carpeta Resources/Heroes/ o directamente en Resources/");
                        }
                    }
                }
            }
            
            LogDebug($"Carga de prefabs completada. Total de prefabs cargados: {heroPrefabs.Count}");
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
            
            LogDebug($"¡No se encontró HeroData para ID: {heroId}!");
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
            
            LogDebug($"El jugador {player.NickName} no tiene un héroe seleccionado.");
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
            
            LogDebug($"El jugador {player.NickName} no tiene un equipo asignado.");
            return -1;
        }

        /// <summary>
        /// Instancia el héroe seleccionado por un jugador en el mundo
        /// </summary>
        public GameObject InstantiatePlayerHero(Player player, Vector3 position, Quaternion rotation)
        {
            // IMPORTANTE: Solo instanciar si es el jugador local
            if (!player.IsLocal)
            {
                LogDebug($"No se instancia héroe para {player.NickName} porque no es el jugador local");
                return null;
            }
            
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
                LogDebug($"Instanciando héroe '{heroPrefab.name}' para jugador {player.NickName} usando la ruta 'Heroes/{heroPrefab.name}'");
                
                // IMPORTANTE: Usar path relativo a Resources incluyendo la subcarpeta
                return PhotonNetwork.Instantiate("Heroes/" + heroPrefab.name, position, rotation);
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
        
        /// <summary>
        /// Registra mensajes de depuración si el modo debug está activado
        /// </summary>
        private void LogDebug(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[HeroManager] {message}");
            }
        }
    }
}