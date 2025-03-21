using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Photon.Pun.Demo.Asteroids
{
    public class HeroSelectionPanel : MonoBehaviour
    {
        [Header("UI References")]
        public TMP_Text RoomNameText;
        public TMP_Text PlayerCountText;
        public Button LeaveRoomButton;
        
        private HeroSelectionManager heroSelectionManager;

        void Start()
        {
            // Find the hero selection manager
            heroSelectionManager = FindObjectOfType<HeroSelectionManager>();
            
            // Set up the room information
            UpdateRoomInfo();
            
            // Set up the leave room button
            LeaveRoomButton.onClick.AddListener(OnLeaveRoomButtonClicked);
        }

        void Update()
        {
            // Keep the room information updated
            UpdateRoomInfo();
        }

        private void UpdateRoomInfo()
        {
            if (PhotonNetwork.CurrentRoom != null)
            {
                RoomNameText.text = "Room: " + PhotonNetwork.CurrentRoom.Name;
                PlayerCountText.text = "Players: " + PhotonNetwork.CurrentRoom.PlayerCount + " / " + PhotonNetwork.CurrentRoom.MaxPlayers;
            }
        }

        private void OnLeaveRoomButtonClicked()
        {
            PhotonNetwork.LeaveRoom();
            PhotonNetwork.LoadLevel("Lobby"); // Assuming "Lobby" is the name of your lobby scene
        }
    }
}