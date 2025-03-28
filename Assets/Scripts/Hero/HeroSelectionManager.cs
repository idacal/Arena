using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ExitGames.Client.Photon;
using Photon.Realtime;
using TMPro;
using System.Linq;

namespace Photon.Pun.Demo.Asteroids
{
    public class HeroSelectionManager : MonoBehaviourPunCallbacks
    {
        [Header("UI References")]
        public GameObject HeroGridContainer;     // Contenedor de la cuadrícula de héroes
        public GameObject HeroIconPrefab;        // Prefab de ícono de héroe
        public TMP_Text TeamAssignmentText;      // Texto que muestra el equipo asignado
        public Button ReadyButton;               // Botón de Ready
        public Button StartGameButton;           // Botón de Start Game (solo host)
        public TMP_Text GameStatusText;          // Estado del juego
        
        [Header("Hero Detail Panel")]
        public HeroDetailPanel DetailPanel;      // Panel de detalles de héroe

        [Header("Hero Data")]
        public List<HeroDataSO> HeroScriptableObjects = new List<HeroDataSO>();
        [HideInInspector]
        public List<HeroData> AvailableHeroes = new List<HeroData>();

        // Constants for custom properties
        private const string PLAYER_SELECTED_HERO = "SelectedHero";
        private const string PLAYER_TEAM = "PlayerTeam";
        private const string PLAYER_HERO_READY = "HeroReady";

        // Team constants
        private const int TEAM_RED = 0;
        private const int TEAM_BLUE = 1;

        // Variables privadas
        private Dictionary<int, GameObject> heroSelectionEntries;
        private int selectedHeroId = -1;
        private int assignedTeam = -1;
        private bool isReady = false;

        #region UNITY

        void Awake()
        {
            heroSelectionEntries = new Dictionary<int, GameObject>();
            
            // Convertir los ScriptableObjects a HeroData
            ConvertScriptableObjectsToHeroData();

            // Si no hay héroes disponibles, crear algunos por defecto
            if (AvailableHeroes.Count == 0)
            {
                Debug.LogWarning("No se encontraron héroes configurados. Creando héroes por defecto.");
                CreateDefaultHeroes();
            }
        }

        void Start()
        {
            // Configurar propiedades iniciales para el jugador local
            Hashtable initialProps = new Hashtable 
            { 
                { PLAYER_SELECTED_HERO, -1 },
                { PLAYER_HERO_READY, false },
                { ArenaGame.PLAYER_LOADED_LEVEL, true }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(initialProps);

            // Asignar equipo - con un algoritmo determinista
            AssignTeamDeterministic();

            // Crear la cuadrícula de héroes
            PopulateHeroGrid();

            // Configurar el botón de Ready
            ReadyButton.onClick.AddListener(OnReadyButtonClicked);

            // Solo el host puede iniciar el juego
            StartGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
            StartGameButton.onClick.AddListener(OnStartGameButtonClicked);
            StartGameButton.interactable = false; // Desactivado hasta que todos estén listos
            
            // Actualizar la UI
            UpdateUI();
        }

        // Convierte los ScriptableObjects a HeroData
        private void ConvertScriptableObjectsToHeroData()
        {
            AvailableHeroes.Clear();
            
            foreach (var heroSO in HeroScriptableObjects)
            {
                if (heroSO != null)
                {
                    AvailableHeroes.Add(heroSO.ToHeroData());
                }
            }
            
            Debug.Log($"Se han cargado {AvailableHeroes.Count} héroes desde ScriptableObjects.");
        }

        public override void OnMasterClientSwitched(Player newMasterClient)
        {
            StartGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
        }

        #endregion

        #region PHOTON CALLBACKS

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            // Actualizar la UI cuando cambien las propiedades de cualquier jugador
            UpdateUI();

            // Verificar si todos están listos
            if (PhotonNetwork.IsMasterClient)
            {
                StartGameButton.interactable = AreAllPlayersReady();
            }
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            Debug.Log($"Jugador {newPlayer.NickName} entró a la sala. Rebalanceando equipos...");
            if (PhotonNetwork.IsMasterClient)
            {
                // Si el Master Client está en la sala, forzar un rebalanceo para todos
                Debug.Log("Master Client enviando comando de rebalanceo de equipos...");
                photonView.RPC("RebalanceTeamsRPC", RpcTarget.All);
            }
            else
            {
                // Si no es el Master, simplemente actualizar la UI
                UpdateUI();
            }
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log($"Jugador {otherPlayer.NickName} salió de la sala. Rebalanceando equipos...");
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log("Master Client enviando comando de rebalanceo de equipos...");
                photonView.RPC("RebalanceTeamsRPC", RpcTarget.All);
            }
            
            // Actualizar la UI
            UpdateUI();
            
            // Verificar si todos los jugadores restantes están listos
            if (PhotonNetwork.IsMasterClient)
            {
                StartGameButton.interactable = AreAllPlayersReady();
            }
        }

        #endregion

        #region UI CALLBACKS

        public void OnHeroSelected(int heroId)
        {
            if (isReady) return; // No se puede cambiar el héroe una vez listo

            selectedHeroId = heroId;
            
            // Actualizar propiedades personalizadas
            Hashtable props = new Hashtable { { PLAYER_SELECTED_HERO, heroId } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            
            // Actualizar la UI
            UpdateUI();
            
            // Mostrar detalles del héroe seleccionado
            if (DetailPanel != null)
            {
                HeroData selectedHero = GetHeroById(heroId);
                if (selectedHero != null)
                {
                    DetailPanel.ShowHeroDetails(selectedHero);
                }
            }
        }

        public void OnReadyButtonClicked()
        {
            if (selectedHeroId == -1)
            {
                // No se puede estar listo sin seleccionar un héroe
                GameStatusText.text = "Por favor selecciona un héroe primero";
                return;
            }

            isReady = !isReady;
            
            // Actualizar propiedades personalizadas
            Hashtable props = new Hashtable { { PLAYER_HERO_READY, isReady } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            
            // Actualizar el texto del botón
            TMP_Text buttonText = ReadyButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = isReady ? "No Listo" : "Listo";
            }
            
            // Actualizar la UI
            UpdateUI();
        }

        public void OnStartGameButtonClicked()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogWarning("Solo el MasterClient puede iniciar el juego");
                return;
            }

            if (!AreAllPlayersReady())
            {
                Debug.LogWarning("No todos los jugadores están listos");
                return;
            }

            // Buscar el GameStartManager y llamar a su método OnStartButtonClicked
            GameStartManager gameStartManager = FindObjectOfType<GameStartManager>();
            if (gameStartManager != null)
            {
                Debug.Log("Iniciando el contador del juego...");
                gameStartManager.OnStartButtonClicked();
            }
            else
            {
                Debug.LogError("No se encontró el GameStartManager! Asegúrate de que existe en la escena.");
            }
        }

        #endregion

        #region TEAM ASSIGNMENT METHODS

        /// <summary>
        /// Usa el ID del jugador para determinar el equipo de manera consistente
        /// </summary>
        private void AssignTeamDeterministic()
        {
            Debug.Log("Iniciando asignación determinista de equipos...");
            
            // Ordenar los jugadores por su ID para asignación determinista
            List<Player> sortedPlayers = new List<Player>(PhotonNetwork.PlayerList);
            sortedPlayers.Sort((a, b) => a.ActorNumber.CompareTo(b.ActorNumber));
            
            Debug.Log($"Jugadores ordenados: {string.Join(", ", sortedPlayers.Select(p => $"{p.NickName}({p.ActorNumber})").ToArray())}");
            
            // Asignar jugadores de manera alternativa a los equipos
            for (int i = 0; i < sortedPlayers.Count; i++)
            {
                Player p = sortedPlayers[i];
                
                // Asignar al equipo rojo o azul según su posición en la lista ordenada
                int team = (i % 2 == 0) ? TEAM_RED : TEAM_BLUE;
                
                // Si es el jugador local, guardar el equipo asignado
                if (p.ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
                {
                    assignedTeam = team;
                    Debug.Log($"Jugador local {p.NickName} (ID: {p.ActorNumber}) asignado al equipo: {(team == TEAM_RED ? "Rojo" : "Azul")}");
                }
                
                // Actualizar propiedades del jugador si es el jugador local
                if (p.IsLocal)
                {
                    Debug.Log($"Estableciendo CustomProperties PLAYER_TEAM={team} para jugador {p.NickName}");
                    Hashtable props = new Hashtable { { PLAYER_TEAM, team } };
                    p.SetCustomProperties(props);
                }
            }
            
            // Loguear todos los jugadores y sus equipos para debug
            Debug.Log("Estado de equipos después de asignación:");
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                object teamObj;
                string teamName = "No asignado";
                if (p.CustomProperties.TryGetValue(PLAYER_TEAM, out teamObj) && teamObj != null)
                {
                    teamName = ((int)teamObj == TEAM_RED) ? "Rojo" : "Azul";
                    Debug.Log($"  - Jugador {p.NickName} (ID: {p.ActorNumber}): Equipo {teamName}, valor={teamObj}");
                }
                else
                {
                    Debug.LogWarning($"  - Jugador {p.NickName} (ID: {p.ActorNumber}): ¡No tiene equipo asignado!");
                }
            }
        }

        /// <summary>
        /// RPC para rebalancear equipos cuando cambia la composición de la sala
        /// </summary>
        [PunRPC]
        public void RebalanceTeamsRPC()
        {
            Debug.Log("Ejecutando rebalanceo de equipos...");
            AssignTeamDeterministic();
            UpdateUI();
        }

        #endregion

        #region PRIVATE METHODS

        private void CreateDefaultHeroes()
        {
            // Crear un héroe de fuerza
            HeroData strengthHero = new HeroData
            {
                Id = 0,
                Name = "Guerrero",
                Description = "Un guerrero fuerte y resistente",
                PrefabName = "Warrior",
                
                // Atributos base
                BaseStrength = 25f,
                BaseIntelligence = 15f,
                BaseAgility = 20f,
                PrimaryAttribute = "Strength",
                
                // Escalados
                StrengthScaling = 2.5f,
                IntelligenceScaling = 1.5f,
                AgilityScaling = 1.5f,
                
                // Estadísticas derivadas
                HealthPerStrength = 20f,
                ManaPerIntelligence = 10f,
                ArmorPerAgility = 0.5f,
                AttackDamagePerStrength = 1.5f,
                AttackDamagePerIntelligence = 0.5f,
                AttackDamagePerAgility = 0.5f,
                AttackSpeedPerAgility = 0.01f,
                MagicResistancePerIntelligence = 0.5f,
                HealthRegenPerStrength = 0.1f,
                ManaRegenPerIntelligence = 0.05f,
                MovementSpeed = 350f,
                RespawnTime = 5.0f,
                
                // Sistema de niveles
                MaxLevel = 18,
                BaseExperience = 100f,
                ExperienceScaling = 1.5f,
                SkillPointsPerLevel = 1,
                CurrentLevel = 1,
                CurrentExperience = 0,
                AvailableSkillPoints = 0
            };
            AvailableHeroes.Add(strengthHero);

            // Crear un héroe de inteligencia
            HeroData intelligenceHero = new HeroData
            {
                Id = 1,
                Name = "Mago",
                Description = "Un poderoso mago con gran control mágico",
                PrefabName = "Mage",
                
                // Atributos base
                BaseStrength = 15f,
                BaseIntelligence = 25f,
                BaseAgility = 20f,
                PrimaryAttribute = "Intelligence",
                
                // Escalados
                StrengthScaling = 1.5f,
                IntelligenceScaling = 2.5f,
                AgilityScaling = 1.5f,
                
                // Estadísticas derivadas
                HealthPerStrength = 15f,
                ManaPerIntelligence = 15f,
                ArmorPerAgility = 0.5f,
                AttackDamagePerStrength = 0.5f,
                AttackDamagePerIntelligence = 1.5f,
                AttackDamagePerAgility = 0.5f,
                AttackSpeedPerAgility = 0.01f,
                MagicResistancePerIntelligence = 0.8f,
                HealthRegenPerStrength = 0.05f,
                ManaRegenPerIntelligence = 0.1f,
                MovementSpeed = 350f,
                RespawnTime = 5.0f,
                
                // Sistema de niveles
                MaxLevel = 18,
                BaseExperience = 100f,
                ExperienceScaling = 1.5f,
                SkillPointsPerLevel = 1,
                CurrentLevel = 1,
                CurrentExperience = 0,
                AvailableSkillPoints = 0
            };
            AvailableHeroes.Add(intelligenceHero);

            // Crear un héroe de agilidad
            HeroData agilityHero = new HeroData
            {
                Id = 2,
                Name = "Asesino",
                Description = "Un asesino rápido y ágil",
                PrefabName = "Assassin",
                
                // Atributos base
                BaseStrength = 20f,
                BaseIntelligence = 15f,
                BaseAgility = 25f,
                PrimaryAttribute = "Agility",
                
                // Escalados
                StrengthScaling = 1.5f,
                IntelligenceScaling = 1.5f,
                AgilityScaling = 2.5f,
                
                // Estadísticas derivadas
                HealthPerStrength = 15f,
                ManaPerIntelligence = 10f,
                ArmorPerAgility = 0.8f,
                AttackDamagePerStrength = 0.5f,
                AttackDamagePerIntelligence = 0.5f,
                AttackDamagePerAgility = 1.5f,
                AttackSpeedPerAgility = 0.02f,
                MagicResistancePerIntelligence = 0.5f,
                HealthRegenPerStrength = 0.05f,
                ManaRegenPerIntelligence = 0.05f,
                MovementSpeed = 350f,
                RespawnTime = 5.0f,
                
                // Sistema de niveles
                MaxLevel = 18,
                BaseExperience = 100f,
                ExperienceScaling = 1.5f,
                SkillPointsPerLevel = 1,
                CurrentLevel = 1,
                CurrentExperience = 0,
                AvailableSkillPoints = 0
            };
            AvailableHeroes.Add(agilityHero);

            Debug.Log($"Se han creado {AvailableHeroes.Count} héroes por defecto.");
        }

        private void PopulateHeroGrid()
        {
            // Crear elementos de UI para cada héroe disponible en forma de cuadrícula
            foreach (var hero in AvailableHeroes)
            {
                GameObject entry = Instantiate(HeroIconPrefab);
                entry.transform.SetParent(HeroGridContainer.transform);
                entry.transform.localScale = Vector3.one;
                
                // Configurar el icono del héroe
                HeroSelectionEntry heroEntry = entry.GetComponent<HeroSelectionEntry>();
                heroEntry.Initialize(hero, this, DetailPanel);
                
                // Almacenar la referencia
                heroSelectionEntries.Add(hero.Id, entry);
            }
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
            // Actualizar texto del equipo
            if (TeamAssignmentText != null)
            {
                TeamAssignmentText.text = "Equipo: " + (assignedTeam == TEAM_RED ? "Rojo" : "Azul");
                TeamAssignmentText.color = (assignedTeam == TEAM_RED ? Color.red : Color.blue);
            }
            
            // Actualizar estado visual de los iconos de héroe
            foreach (var entry in heroSelectionEntries.Values)
            {
                HeroSelectionEntry entryComponent = entry.GetComponent<HeroSelectionEntry>();
                if (entryComponent != null)
                {
                    entryComponent.UpdateSelectionStatus();
                }
            }
            
            // Actualizar texto de estado del juego
            if (GameStatusText != null)
            {
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
                
                GameStatusText.text = $"Jugadores Listos: {readyCount}/{totalPlayers}";
            }
        }

        #endregion

        #region PUBLIC METHODS

        /// <summary>
        /// Obtiene el ID del héroe seleccionado actualmente
        /// </summary>
        public int GetSelectedHeroId()
        {
            return selectedHeroId;
        }

        /// <summary>
        /// Obtiene el equipo asignado
        /// </summary>
        public int GetAssignedTeam()
        {
            return assignedTeam;
        }

        /// <summary>
        /// Verifica si el jugador está listo
        /// </summary>
        public bool IsPlayerReady()
        {
            return isReady;
        }
        
        /// <summary>
        /// Obtiene los datos de un héroe por su ID
        /// </summary>
        public HeroData GetHeroById(int heroId)
        {
            foreach (var hero in AvailableHeroes)
            {
                if (hero.Id == heroId)
                {
                    return hero;
                }
            }
            
            return null;
        }

        #endregion
    }
}