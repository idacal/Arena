using UnityEngine;
using UnityEngine.UI;
using ExitGames.Client.Photon;
using Photon.Realtime;

namespace Photon.Pun.Demo.Asteroids
{
    public class PlayerInfoEntry : MonoBehaviour
    {
        [Header("UI References")]
        public Text PlayerNameText;
        public Image TeamColorImage;
        public Text SelectedHeroText;
        public Image ReadyStatusImage;
        
        // Constants for custom properties
        private const string PLAYER_SELECTED_HERO = "SelectedHero";
        private const string PLAYER_TEAM = "PlayerTeam";
        private const string PLAYER_HERO_READY = "HeroReady";
        
        // Team constants
        private const int TEAM_RED = 0;
        private const int TEAM_BLUE = 1;
        
        private int playerId;
        private HeroSelectionManager heroSelectionManager;

        public void Initialize(int playerActorNumber, string playerName, HeroSelectionManager manager)
        {
            playerId = playerActorNumber;
            PlayerNameText.text = playerName;
            heroSelectionManager = manager;
            
            // Initially hide the ready status
            ReadyStatusImage.gameObject.SetActive(false);
            
            // Update the display
            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            Player targetPlayer = null;
            
            // Find the player with matching actor number
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                if (p.ActorNumber == playerId)
                {
                    targetPlayer = p;
                    break;
                }
            }
            
            if (targetPlayer == null) return;
            
            // Update team color
            object teamObj;
            if (targetPlayer.CustomProperties.TryGetValue(PLAYER_TEAM, out teamObj) && teamObj != null)
            {
                int team = (int)teamObj;
                TeamColorImage.color = (team == TEAM_RED) ? Color.red : Color.blue;
            }
            
            // Update selected hero
            object heroIdObj;
            if (targetPlayer.CustomProperties.TryGetValue(PLAYER_SELECTED_HERO, out heroIdObj) && heroIdObj != null)
            {
                int heroId = (int)heroIdObj;
                
                if (heroId != -1)
                {
                    // Find the hero name from the available heroes
                    string heroName = "Unknown";
                    foreach (var hero in heroSelectionManager.AvailableHeroes)
                    {
                        if (hero.Id == heroId)
                        {
                            heroName = hero.Name;
                            break;
                        }
                    }
                    
                    SelectedHeroText.text = "Hero: " + heroName;
                }
                else
                {
                    SelectedHeroText.text = "Selecting...";
                }
            }
            
            // Update ready status
            object isReadyObj;
            if (targetPlayer.CustomProperties.TryGetValue(PLAYER_HERO_READY, out isReadyObj) && isReadyObj != null)
            {
                bool isReady = (bool)isReadyObj;
                ReadyStatusImage.gameObject.SetActive(isReady);
            }
        }
    }
}