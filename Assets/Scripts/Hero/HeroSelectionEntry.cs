using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ExitGames.Client.Photon;
using TMPro;
using Photon.Realtime;

namespace Photon.Pun.Demo.Asteroids
{
    public class HeroSelectionEntry : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI References")]
        public Image HeroIconImage;            // Icono/Retrato del héroe para la selección
        public Image SelectionFrame;           // Marco que indica selección
        public Image TeamIndicator;            // Pequeño indicador de equipo (rojo/azul)
        public TMP_Text HeroNameText;          // Opcional: nombre del héroe
        
        // Variables privadas
        private HeroData heroData;
        private HeroSelectionManager selectionManager;
        private HeroDetailPanel detailPanel;
        
        // Constants for custom properties
        private const string PLAYER_SELECTED_HERO = "SelectedHero";
        private const string PLAYER_TEAM = "PlayerTeam";
        
        // Team constants
        private const int TEAM_RED = 0;
        private const int TEAM_BLUE = 1;

        /// <summary>
        /// Inicializa el icono de selección de héroe
        /// </summary>
        public void Initialize(HeroData data, HeroSelectionManager manager, HeroDetailPanel detailPanel)
        {
            heroData = data;
            selectionManager = manager;
            this.detailPanel = detailPanel;
            
            // Configurar el icono
            if (data.IconSprite != null)
            {
                HeroIconImage.sprite = data.IconSprite;
            }
            else if (data.AvatarSprite != null) // Usar avatar como fallback si no hay icono
            {
                HeroIconImage.sprite = data.AvatarSprite;
                Debug.LogWarning($"El héroe {data.Name} no tiene icono, usando avatar como fallback");
            }
            
            // Configurar el nombre si existe el campo de texto
            if (HeroNameText != null)
            {
                HeroNameText.text = data.Name;
            }
            
            // Inicialmente ocultar el marco de selección y el indicador de equipo
            SelectionFrame.gameObject.SetActive(false);
            TeamIndicator.gameObject.SetActive(false);
            
            // Añadir detector de clic al icono
            Button iconButton = HeroIconImage.GetComponent<Button>();
            if (iconButton == null)
            {
                iconButton = HeroIconImage.gameObject.AddComponent<Button>();
            }
            
            // Configurar el evento de clic para seleccionar el héroe
            iconButton.onClick.AddListener(OnHeroSelected);
        }

        /// <summary>
        /// Método llamado cuando se selecciona este héroe
        /// </summary>
        private void OnHeroSelected()
        {
            // Si el jugador ya está listo, no permitir cambios
            if (selectionManager.IsPlayerReady())
                return;
                
            // Seleccionar este héroe
            selectionManager.OnHeroSelected(heroData.Id);
            
            // Mostrar los detalles
            ShowHeroDetails();
        }
        
        /// <summary>
        /// Muestra los detalles del héroe en el panel inferior
        /// </summary>
        private void ShowHeroDetails()
        {
            if (detailPanel != null)
            {
                detailPanel.ShowHeroDetails(heroData);
            }
        }

        /// <summary>
        /// Actualiza el estado visual del icono según selección
        /// </summary>
        public void UpdateSelectionStatus()
        {
            // Verificar si este héroe está seleccionado por el jugador local
            bool isSelectedByLocalPlayer = (selectionManager.GetSelectedHeroId() == heroData.Id);
            
            // Actualizar marco de selección
            SelectionFrame.gameObject.SetActive(isSelectedByLocalPlayer);
            
            // Si está seleccionado, mostrar el color del equipo
            if (isSelectedByLocalPlayer)
            {
                int team = selectionManager.GetAssignedTeam();
                TeamIndicator.color = (team == TEAM_RED) ? Color.red : Color.blue;
                TeamIndicator.gameObject.SetActive(true);
                
                // Si el héroe está seleccionado por el jugador local, mostrar sus detalles
                ShowHeroDetails();
            }
            else
            {
                // Verificar si está seleccionado por otro jugador
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
                            
                            // Mostrar el color del equipo que lo seleccionó
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
                
                // Si no está seleccionado por ningún jugador, ocultar el indicador de equipo
                if (!isSelectedByOthers)
                {
                    TeamIndicator.gameObject.SetActive(false);
                }
            }
            
            // Deshabilitar interacción si ya está seleccionado por otro jugador o si el jugador está listo
            Button iconButton = HeroIconImage.GetComponent<Button>();
            if (iconButton != null)
            {
                bool isSelectedByOthers = TeamIndicator.gameObject.activeSelf && !isSelectedByLocalPlayer;
                iconButton.interactable = !isSelectedByOthers && !selectionManager.IsPlayerReady();
            }
        }
        
        /// <summary>
        /// Método llamado cuando el cursor entra en el ícono - muestra información en el panel inferior
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            // Solo mostrar detalles al hacer hover si no hay un héroe seleccionado o si este no es el seleccionado
            if (selectionManager.GetSelectedHeroId() == -1 || selectionManager.GetSelectedHeroId() != heroData.Id)
            {
                ShowHeroDetails();
            }
        }
        
        /// <summary>
        /// Método llamado cuando el cursor sale del ícono - oculta información si no está seleccionado
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            // Si este héroe no es el que está seleccionado actualmente, ocultar los detalles
            if (selectionManager.GetSelectedHeroId() != heroData.Id)
            {
                // Si hay un héroe seleccionado, mostrar sus detalles en su lugar
                if (selectionManager.GetSelectedHeroId() != -1)
                {
                    HeroData selectedHero = selectionManager.GetHeroById(selectionManager.GetSelectedHeroId());
                    if (selectedHero != null && detailPanel != null)
                    {
                        detailPanel.ShowHeroDetails(selectedHero);
                    }
                }
                else if (detailPanel != null)
                {
                    // Si no hay héroe seleccionado, ocultar el panel
                    detailPanel.HidePanel();
                }
            }
        }
    }
}