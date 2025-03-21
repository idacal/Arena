using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Realtime;
using ExitGames.Client.Photon;
using TMPro;

namespace Photon.Pun.Demo.Asteroids
{
    public class PlayerListManager : MonoBehaviourPunCallbacks
    {
        [Header("UI References")]
        public GameObject PlayerListParent;     // Panel padre que contiene la lista
        public GameObject PlayerInfoEntryPrefab; // Prefab para cada entrada de jugador
        
        private Dictionary<int, GameObject> playerInfoEntries; // Diccionario para guardar las entradas
        private HeroSelectionManager heroSelectionManager; // Referencia al manager de selección de héroes

        void Awake()
        {
            // Inicializar el diccionario
            playerInfoEntries = new Dictionary<int, GameObject>();
            
            // Intentar encontrar el HeroSelectionManager
            heroSelectionManager = FindObjectOfType<HeroSelectionManager>();
            
            // Imprimir un error si no se encuentra
            if (heroSelectionManager == null)
            {
                Debug.LogError("HeroSelectionManager no encontrado en la escena!");
            }
            
            // Verificar que tenemos todas las referencias necesarias
            if (PlayerListParent == null)
            {
                Debug.LogError("PlayerListParent no asignado en el inspector!");
            }
            
            if (PlayerInfoEntryPrefab == null)
            {
                Debug.LogError("PlayerInfoEntryPrefab no asignado en el inspector!");
            }
        }

        void Start()
        {
            // Crear entradas para todos los jugadores actuales
            Debug.Log("PlayerListManager: Inicializando lista con " + PhotonNetwork.PlayerList.Length + " jugadores");
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                Debug.Log("PlayerListManager: Creando entrada para " + p.NickName);
                CreatePlayerInfoEntry(p);
            }
        }

        #region PHOTON CALLBACKS

        // Cuando un nuevo jugador entra en la sala
        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            Debug.Log("PlayerListManager: Jugador entró: " + newPlayer.NickName);
            CreatePlayerInfoEntry(newPlayer);
        }

        // Cuando un jugador sale de la sala
        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log("PlayerListManager: Jugador salió: " + otherPlayer.NickName);
            RemovePlayerInfoEntry(otherPlayer);
        }

        // Cuando se actualizan las propiedades de un jugador
        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            Debug.Log("PlayerListManager: Propiedades actualizadas para " + targetPlayer.NickName);
            // Actualizar la entrada del jugador si existe
            if (playerInfoEntries.ContainsKey(targetPlayer.ActorNumber))
            {
                PlayerInfoEntry entry = playerInfoEntries[targetPlayer.ActorNumber].GetComponent<PlayerInfoEntry>();
                if (entry != null)
                {
                    entry.UpdateDisplay();
                }
            }
        }

        #endregion

        #region PRIVATE METHODS

        // Crea una entrada en la lista para un jugador
        private void CreatePlayerInfoEntry(Player player)
        {
            // Verificar que tenemos las referencias necesarias
            if (PlayerInfoEntryPrefab == null || PlayerListParent == null)
            {
                Debug.LogError("Faltan referencias para crear la entrada del jugador!");
                return;
            }
            
            // Instanciar el prefab
            GameObject entry = Instantiate(PlayerInfoEntryPrefab);
            
            // Establecer el padre adecuado
            entry.transform.SetParent(PlayerListParent.transform);
            entry.transform.localScale = Vector3.one;
            
            // Inicializar la entrada
            PlayerInfoEntry playerInfoComp = entry.GetComponent<PlayerInfoEntry>();
            if (playerInfoComp != null)
            {
                playerInfoComp.Initialize(player.ActorNumber, player.NickName, heroSelectionManager);
                Debug.Log("PlayerListManager: Entrada inicializada para " + player.NickName);
            }
            else
            {
                Debug.LogError("El prefab no tiene el componente PlayerInfoEntry!");
            }
            
            // Añadir al diccionario
            playerInfoEntries.Add(player.ActorNumber, entry);
        }

        // Elimina la entrada de un jugador que ha salido
        private void RemovePlayerInfoEntry(Player player)
        {
            if (playerInfoEntries.ContainsKey(player.ActorNumber))
            {
                Destroy(playerInfoEntries[player.ActorNumber]);
                playerInfoEntries.Remove(player.ActorNumber);
                Debug.Log("PlayerListManager: Entrada eliminada para " + player.NickName);
            }
        }

        #endregion
    }
}