using UnityEngine;
using UnityEngine.UI;
using ExitGames.Client.Photon;
using TMPro;
using Photon.Realtime;

namespace Photon.Pun.Demo.Asteroids
{
    public class PlayerInfoEntry : MonoBehaviour
    {
        [Header("UI References")]
        public TMP_Text PlayerNameText;
        public Image TeamColorImage;
        public TMP_Text SelectedHeroText;
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

        /// <summary>
        /// Inicializa la entrada con los datos del jugador
        /// </summary>
        public void Initialize(int playerActorNumber, string playerName, HeroSelectionManager manager)
        {
            Debug.Log($"PlayerInfoEntry: Inicializando entrada para {playerName} (ID: {playerActorNumber})");
            
            playerId = playerActorNumber;
            
            // Configurar el nombre del jugador
            if (PlayerNameText != null)
            {
                PlayerNameText.text = playerName;
            }
            else
            {
                Debug.LogError("PlayerNameText no asignado en el inspector!");
            }
            
            // Guardar referencia al manager
            heroSelectionManager = manager;
            
            // Inicialmente ocultar el estado de listo
            if (ReadyStatusImage != null)
            {
                ReadyStatusImage.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogError("ReadyStatusImage no asignado en el inspector!");
            }
            
            // Actualizar la visualización
            UpdateDisplay();
        }

        /// <summary>
        /// Actualiza la visualización basada en las propiedades actuales del jugador
        /// </summary>
        public void UpdateDisplay()
        {
            // Buscar el jugador correspondiente a este ID
            Player targetPlayer = null;
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                if (p.ActorNumber == playerId)
                {
                    targetPlayer = p;
                    break;
                }
            }
            
            // Si no se encuentra el jugador, salir
            if (targetPlayer == null)
            {
                Debug.LogWarning($"No se encontró al jugador con ID {playerId}");
                return;
            }
            
            // Actualizar el color del equipo
            if (TeamColorImage != null)
            {
                object teamObj;
                if (targetPlayer.CustomProperties.TryGetValue(PLAYER_TEAM, out teamObj) && teamObj != null)
                {
                    int team = (int)teamObj;
                    TeamColorImage.color = (team == TEAM_RED) ? Color.red : Color.blue;
                    TeamColorImage.gameObject.SetActive(true);
                }
                else
                {
                    // Si no tiene equipo asignado, ocultar o usar un color neutral
                    TeamColorImage.color = Color.gray;
                    TeamColorImage.gameObject.SetActive(true);
                }
            }
            else
            {
                Debug.LogError("TeamColorImage no asignado en el inspector!");
            }
            
            // Actualizar el héroe seleccionado
            if (SelectedHeroText != null)
            {
                object heroIdObj;
                if (targetPlayer.CustomProperties.TryGetValue(PLAYER_SELECTED_HERO, out heroIdObj) && heroIdObj != null)
                {
                    int heroId = (int)heroIdObj;
                    
                    if (heroId != -1 && heroSelectionManager != null)
                    {
                        // Buscar el nombre del héroe
                        HeroData heroData = heroSelectionManager.GetHeroById(heroId);
                        if (heroData != null)
                        {
                            SelectedHeroText.text = heroData.Name;
                        }
                        else
                        {
                            SelectedHeroText.text = "Héroe: Desconocido";
                        }
                    }
                    else
                    {
                        SelectedHeroText.text = "Picking";
                    }
                }
                else
                {
                    SelectedHeroText.text = "Sin selección";
                }
            }
            else
            {
                Debug.LogError("SelectedHeroText no asignado en el inspector!");
            }
            
            // Actualizar el estado de listo
            if (ReadyStatusImage != null)
            {
                object isReadyObj;
                if (targetPlayer.CustomProperties.TryGetValue(PLAYER_HERO_READY, out isReadyObj) && isReadyObj != null)
                {
                    bool isReady = (bool)isReadyObj;
                    ReadyStatusImage.gameObject.SetActive(isReady);
                }
                else
                {
                    ReadyStatusImage.gameObject.SetActive(false);
                }
            }
        }
    }
}