using UnityEngine;
using ExitGames.Client.Photon;
using System.Collections.Generic;
using Photon.Realtime;

namespace Photon.Pun.Demo.Asteroids
{
    /// <summary>
    /// This class manages hero data across scenes and ensures the selected hero data
    /// is available in the gameplay scene.
    /// </summary>
    public class HeroManager : MonoBehaviourPunCallbacks
    {
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

        // List of available heroes
        [SerializeField]
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
            
            // Load hero prefabs
            LoadHeroPrefabs();
        }

        private void LoadHeroPrefabs()
        {
            // Here we would normally load prefabs from Resources folder
            // For this example, we'll just set up placeholders
            
            // In a real implementation, you might do something like:
            // foreach (var hero in availableHeroes)
            // {
            //     GameObject prefab = Resources.Load<GameObject>("Heroes/" + hero.PrefabName);
            //     if (prefab != null)
            //     {
            //         heroPrefabs.Add(hero.Id, prefab);
            //     }
            // }
        }

        /// <summary>
        /// Gets the hero data for the specified hero ID
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
        /// Gets the currently selected hero ID for the specified player
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
        /// Gets the team assignment for the specified player
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
        /// Instantiates the player's selected hero in the game world
        /// </summary>
        public GameObject InstantiatePlayerHero(Player player, Vector3 position, Quaternion rotation)
        {
            int heroId = GetPlayerSelectedHeroId(player);
            
            if (heroId == -1)
            {
                Debug.LogError("Player has not selected a hero!");
                return null;
            }
            
            // Get the hero prefab
            GameObject heroPrefab = null;
            
            if (heroPrefabs.TryGetValue(heroId, out heroPrefab) && heroPrefab != null)
            {
                // Instantiate the hero
                if (player.IsLocal)
                {
                    // For the local player, use PhotonNetwork.Instantiate to ensure ownership
                    return PhotonNetwork.Instantiate(heroPrefab.name, position, rotation);
                }
                else
                {
                    // For remote players, just instantiate locally
                    // This assumes the actual player object is created by the owner via PhotonNetwork.Instantiate
                    return Instantiate(heroPrefab, position, rotation);
                }
            }
            
            Debug.LogError("Hero prefab not found for hero ID: " + heroId);
            return null;
        }

        /// <summary>
        /// Gets a list of all heroes selected by players in the specified team
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
        /// Gets all available heroes
        /// </summary>
        public List<HeroData> GetAvailableHeroes()
        {
            return availableHeroes;
        }
    }
}