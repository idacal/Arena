using UnityEngine;
using UnityEngine.UI;
using ExitGames.Client.Photon;
using Photon.Realtime;

namespace Photon.Pun.Demo.Asteroids
{
    public class HeroEntry : MonoBehaviour
    {
        [Header("UI References")]
        public Text HeroNameText;
        public Text HeroDescriptionText;
        public Image HeroAvatarImage;
        public Button SelectButton;
        public Image SelectionFrame;
        public Image TeamIndicator;

        private HeroData heroData;
        private HeroSelectionManager selectionManager;
        
        // Constants for custom properties
        private const string PLAYER_SELECTED_HERO = "SelectedHero";
        private const string PLAYER_TEAM = "PlayerTeam";
        
        // Team constants
        private const int TEAM_RED = 0;
        private const int TEAM_BLUE = 1;

        public void Initialize(HeroData data, HeroSelectionManager manager)
        {
            heroData = data;
            selectionManager = manager;
            
            // Set up the UI elements
            HeroNameText.text = data.Name;
            HeroDescriptionText.text = data.Description;
            
            if (data.AvatarSprite != null)
            {
                HeroAvatarImage.sprite = data.AvatarSprite;
            }
            
            // Set up the select button
            SelectButton.onClick.AddListener(() => OnSelectButtonClicked());
            
            // Initially hide the selection frame
            SelectionFrame.gameObject.SetActive(false);
        }

        private void OnSelectButtonClicked()
        {
            selectionManager.OnHeroSelected(heroData.Id);
        }

        public void UpdateSelectionStatus()
        {
            // Check if this hero is selected by the local player
            bool isSelectedByLocalPlayer = (selectionManager.GetSelectedHeroId() == heroData.Id);
            
            // Update selection frame
            SelectionFrame.gameObject.SetActive(isSelectedByLocalPlayer);
            
            // If selected, show team color
            if (isSelectedByLocalPlayer)
            {
                int team = selectionManager.GetAssignedTeam();
                TeamIndicator.color = (team == TEAM_RED) ? Color.red : Color.blue;
                TeamIndicator.gameObject.SetActive(true);
            }
            else
            {
                // Check if this hero is selected by any other player
                bool isSelectedByOthers = false;
                foreach (Player p in PhotonNetwork.PlayerList)
                {
                    if (p == PhotonNetwork.LocalPlayer) continue;
                    
                    object heroIdObj;
                    if (p.CustomProperties.TryGetValue(PLAYER_SELECTED_HERO, out heroIdObj) && heroIdObj != null)
                    {
                        int heroId = (int)heroIdObj;
                        if (heroId == heroData.Id)
                        {
                            isSelectedByOthers = true;
                            
                            // Show which team selected this hero
                            object teamObj;
                            if (p.CustomProperties.TryGetValue(PLAYER_TEAM, out teamObj) && teamObj != null)
                            {
                                int team = (int)teamObj;
                                TeamIndicator.color = (team == TEAM_RED) ? Color.red : Color.blue;
                                TeamIndicator.gameObject.SetActive(true);
                            }
                            
                            break;
                        }
                    }
                }
                
                // If not selected by any player, hide team indicator
                if (!isSelectedByOthers)
                {
                    TeamIndicator.gameObject.SetActive(false);
                }
            }
            
            // Disable button if already selected by another player or if local player is ready
            SelectButton.interactable = !TeamIndicator.gameObject.activeSelf || isSelectedByLocalPlayer;
            
            // If player is ready, disable button regardless
            if (selectionManager.IsPlayerReady())
            {
                SelectButton.interactable = false;
            }
        }
    }
}