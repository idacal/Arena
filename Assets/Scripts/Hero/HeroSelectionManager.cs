using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ExitGames.Client.Photon;
using Photon.Realtime;
using UnityEngine.SceneManagement;

namespace Photon.Pun.Demo.Asteroids
{
    public class HeroSelectionManager : MonoBehaviourPunCallbacks
    {
        [Header("UI References")]
        public GameObject HeroSelectionPanel;
        public GameObject HeroListContent;
        public GameObject HeroEntryPrefab;
        public Text TeamAssignmentText;
        public Button ReadyButton;
        public Button StartGameButton;
        public Text GameStatusText;

        [Header("Hero Data")]
        public List<HeroData> AvailableHeroes = new List<HeroData>();

        // Constants for custom properties
        private const string PLAYER_SELECTED_HERO = "SelectedHero";
        private const string PLAYER_TEAM = "PlayerTeam";
        private const string PLAYER_HERO_READY = "HeroReady";

        // Team constants
        private const int TEAM_RED = 0;
        private const int TEAM_BLUE = 1;

        private Dictionary<int, GameObject> heroSelectionEntries;
        private int selectedHeroId = -1;
        private int assignedTeam = -1;
        private bool isReady = false;

        #region UNITY

        void Awake()
        {
            heroSelectionEntries = new Dictionary<int, GameObject>();

            // If we don't have predefined heroes in the inspector, create some defaults
            if (AvailableHeroes.Count == 0)
            {
                CreateDefaultHeroes();
            }
        }

        void Start()
        {
            // Set up initial properties for the local player
            Hashtable initialProps = new Hashtable 
            { 
                { PLAYER_SELECTED_HERO, -1 },
                { PLAYER_TEAM, -1 },
                { PLAYER_HERO_READY, false },
                { ArenaGame.PLAYER_LOADED_LEVEL, true }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(initialProps);

            // Setup the team assignment - we'll try to balance teams
            AssignTeam();

            // Populate the hero selection UI
            PopulateHeroSelection();

            // Set up the ready button
            ReadyButton.onClick.AddListener(OnReadyButtonClicked);

            // Only the master client can start the game
            StartGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
            StartGameButton.onClick.AddListener(OnStartGameButtonClicked);
            StartGameButton.interactable = false; // Disabled until all players are ready
            
            // Update the UI
            UpdateUI();
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            StartGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
        }

        #endregion

        #region PHOTON CALLBACKS

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            // Update the UI when any player's properties change
            UpdateUI();

            // Check if everyone is ready
            if (PhotonNetwork.IsMasterClient)
            {
                StartGameButton.interactable = AreAllPlayersReady();
            }
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            // Update the team balance if a player leaves
            if (PhotonNetwork.IsMasterClient)
            {
                RebalanceTeams();
            }
            
            // Update the UI
            UpdateUI();
            
            // Check if all remaining players are ready
            if (PhotonNetwork.IsMasterClient)
            {
                StartGameButton.interactable = AreAllPlayersReady();
            }
        }

        #endregion

        #region UI CALLBACKS

        public void OnHeroSelected(int heroId)
        {
            if (isReady) return; // Can't change hero once ready

            selectedHeroId = heroId;
            
            // Update the custom properties
            Hashtable props = new Hashtable { { PLAYER_SELECTED_HERO, heroId } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            
            // Update the UI
            UpdateUI();
        }

        public void OnReadyButtonClicked()
        {
            if (selectedHeroId == -1)
            {
                // Can't be ready without selecting a hero
                GameStatusText.text = "Please select a hero first!";
                return;
            }

            isReady = !isReady;
            
            // Update the custom properties
            Hashtable props = new Hashtable { { PLAYER_HERO_READY, isReady } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            
            // Update the button text
            ReadyButton.GetComponentInChildren<Text>().text = isReady ? "Not Ready" : "Ready";
            
            // Update the UI
            UpdateUI();
        }

        public void OnStartGameButtonClicked()
        {
            if (!AreAllPlayersReady()) return;

            // Load the gameplay scene
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;
            
            // You would replace "GameplayScene" with your actual gameplay scene name
            PhotonNetwork.LoadLevel("GameplayScene");
        }

        #endregion

        #region PRIVATE METHODS

        private void CreateDefaultHeroes()
        {
            // Create some default heroes if none are defined in the inspector
            AvailableHeroes.Add(new HeroData { Id = 0, Name = "Warrior", Description = "Tank class with high HP", AvatarSprite = null });
            AvailableHeroes.Add(new HeroData { Id = 1, Name = "Mage", Description = "High magic damage", AvatarSprite = null });
            AvailableHeroes.Add(new HeroData { Id = 2, Name = "Archer", Description = "Ranged physical damage", AvatarSprite = null });
            AvailableHeroes.Add(new HeroData { Id = 3, Name = "Healer", Description = "Support class with healing abilities", AvatarSprite = null });
            AvailableHeroes.Add(new HeroData { Id = 4, Name = "Assassin", Description = "High burst damage", AvatarSprite = null });
        }

        private void PopulateHeroSelection()
        {
            // Create hero entry UI elements for each available hero
            foreach (var hero in AvailableHeroes)
            {
                GameObject entry = Instantiate(HeroEntryPrefab);
                entry.transform.SetParent(HeroListContent.transform);
                entry.transform.localScale = Vector3.one;
                
                // Setup the hero entry
                HeroEntry heroEntry = entry.GetComponent<HeroEntry>();
                heroEntry.Initialize(hero, this);
                
                heroSelectionEntries.Add(hero.Id, entry);
            }
        }

        private void AssignTeam()
        {
            // Count players in each team
            int redCount = 0;
            int blueCount = 0;
            
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                if (p == PhotonNetwork.LocalPlayer) continue; // Skip local player
                
                object teamObj;
                if (p.CustomProperties.TryGetValue(PLAYER_TEAM, out teamObj) && teamObj != null)
                {
                    int team = (int)teamObj;
                    if (team == TEAM_RED)
                        redCount++;
                    else if (team == TEAM_BLUE)
                        blueCount++;
                }
            }
            
            // Assign to the team with fewer players
            assignedTeam = (redCount <= blueCount) ? TEAM_RED : TEAM_BLUE;
            
            // Update the custom properties
            Hashtable props = new Hashtable { { PLAYER_TEAM, assignedTeam } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        private void RebalanceTeams()
        {
            // This could be called when a player leaves to rebalance if needed
            // For now we'll keep it simple and not force players to switch teams mid-selection
        }

        private bool AreAllPlayersReady()
        {
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                object isReady;
                if (!p.CustomProperties.TryGetValue(PLAYER_HERO_READY, out isReady) || isReady == null || !(bool)isReady)
                {
                    return false;
                }
                
                object selectedHero;
                if (!p.CustomProperties.TryGetValue(PLAYER_SELECTED_HERO, out selectedHero) || selectedHero == null || (int)selectedHero == -1)
                {
                    return false;
                }
            }
            
            return true;
        }

        private void UpdateUI()
        {
            // Update team display
            TeamAssignmentText.text = "Team: " + (assignedTeam == TEAM_RED ? "Red" : "Blue");
            TeamAssignmentText.color = (assignedTeam == TEAM_RED ? Color.red : Color.blue);
            
            // Update hero selection UI
            foreach (var entry in heroSelectionEntries.Values)
            {
                entry.GetComponent<HeroEntry>().UpdateSelectionStatus();
            }
            
            // Update game status text
            int readyCount = 0;
            int totalPlayers = PhotonNetwork.PlayerList.Length;
            
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                object isReady;
                if (p.CustomProperties.TryGetValue(PLAYER_HERO_READY, out isReady) && isReady != null && (bool)isReady)
                {
                    readyCount++;
                }
            }
            
            GameStatusText.text = $"Players Ready: {readyCount}/{totalPlayers}";
        }

        #endregion

        #region PUBLIC METHODS

        public int GetSelectedHeroId()
        {
            return selectedHeroId;
        }

        public int GetAssignedTeam()
        {
            return assignedTeam;
        }

        public bool IsPlayerReady()
        {
            return isReady;
        }

        #endregion
    }
}