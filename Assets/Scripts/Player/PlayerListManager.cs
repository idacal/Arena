using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using ExitGames.Client.Photon;

namespace Photon.Pun.Demo.Asteroids
{
    public class PlayerListManager : MonoBehaviourPunCallbacks
    {
        [Header("UI References")]
        public GameObject PlayerListContent;
        public GameObject PlayerInfoEntryPrefab;
        
        private Dictionary<int, GameObject> playerInfoEntries;
        private HeroSelectionManager heroSelectionManager;

        void Awake()
        {
            playerInfoEntries = new Dictionary<int, GameObject>();
            heroSelectionManager = FindObjectOfType<HeroSelectionManager>();
        }

        void Start()
        {
            // Create entries for all current players
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                CreatePlayerInfoEntry(p);
            }
        }

        #region PHOTON CALLBACKS

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            CreatePlayerInfoEntry(newPlayer);
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            RemovePlayerInfoEntry(otherPlayer);
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            // Update the player info display when properties change
            if (playerInfoEntries.ContainsKey(targetPlayer.ActorNumber))
            {
                playerInfoEntries[targetPlayer.ActorNumber].GetComponent<PlayerInfoEntry>().UpdateDisplay();
            }
        }

        #endregion

        #region PRIVATE METHODS

        private void CreatePlayerInfoEntry(Player player)
        {
            GameObject entry = Instantiate(PlayerInfoEntryPrefab);
            entry.transform.SetParent(PlayerListContent.transform);
            entry.transform.localScale = Vector3.one;
            
            // Initialize the entry
            entry.GetComponent<PlayerInfoEntry>().Initialize(player.ActorNumber, player.NickName, heroSelectionManager);
            
            // Add to dictionary
            playerInfoEntries.Add(player.ActorNumber, entry);
        }

        private void RemovePlayerInfoEntry(Player player)
        {
            if (playerInfoEntries.ContainsKey(player.ActorNumber))
            {
                Destroy(playerInfoEntries[player.ActorNumber]);
                playerInfoEntries.Remove(player.ActorNumber);
            }
        }

        #endregion
    }
}