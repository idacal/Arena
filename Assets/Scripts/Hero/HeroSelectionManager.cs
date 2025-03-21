using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ExitGames.Client.Photon;
using Photon.Realtime;
using TMPro;

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
                { PLAYER_TEAM, -1 },
                { PLAYER_HERO_READY, false },
                { ArenaGame.PLAYER_LOADED_LEVEL, true }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(initialProps);

            // Asignar equipo - intentaremos equilibrar equipos
            AssignTeam();

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

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            // Actualizar el balance de equipos si un jugador se va
            if (PhotonNetwork.IsMasterClient)
            {
                RebalanceTeams();
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
            if (!AreAllPlayersReady()) return;

            // Cargar la escena de juego
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;
            
            // Reemplazar "GameplayScene" con el nombre real de tu escena de juego
            PhotonNetwork.LoadLevel("GameplayScene");
        }

        #endregion

        #region PRIVATE METHODS

        private void CreateDefaultHeroes()
        {
            // Crear algunos héroes por defecto con habilidades
            
            // Guerrero
            HeroData warrior = new HeroData 
            { 
                Id = 0, 
                Name = "Guerrero", 
                Description = "Tanque con alta vida y resistencia",
                AvatarSprite = null,
                Health = 1000,
                Mana = 400,
                HeroType = "Tanque",
                AttackDamage = 80,
                AttackSpeed = 0.8f,
                MovementSpeed = 340,
                Armor = 30,
                MagicResistance = 25
            };
            
            warrior.Abilities.Add(new HeroAbility 
            { 
                Name = "Golpe Devastador", 
                Description = "Golpea con fuerza al enemigo causando daño físico y ralentización",
                Hotkey = "Q",
                Cooldown = 8,
                ManaCost = 60,
                AbilityType = "Activa",
                DamageAmount = 120,
                Duration = 2.5f,
                Range = 2
            });
            
            warrior.Abilities.Add(new HeroAbility 
            { 
                Name = "Grito de Guerra", 
                Description = "Aumenta la armadura y resistencia mágica durante un periodo de tiempo",
                Hotkey = "W",
                Cooldown = 15,
                ManaCost = 80,
                AbilityType = "Activa",
                Duration = 5,
            });
            
            warrior.Abilities.Add(new HeroAbility 
            { 
                Name = "Carga Heroica", 
                Description = "Carga hacia un objetivo enemigo y lo aturde",
                Hotkey = "E",
                Cooldown = 12,
                ManaCost = 70,
                AbilityType = "Activa",
                DamageAmount = 80,
                Duration = 1.5f,
                Range = 600
            });
            
            warrior.Abilities.Add(new HeroAbility 
            { 
                Name = "Furia Indomable", 
                Description = "Aumenta temporalmente la vida máxima y el daño de ataque",
                Hotkey = "R",
                Cooldown = 90,
                ManaCost = 100,
                AbilityType = "Ultimate",
                Duration = 10,
            });
            
            // Mago
            HeroData mage = new HeroData 
            { 
                Id = 1, 
                Name = "Mago", 
                Description = "Especialista en daño mágico a distancia",
                AvatarSprite = null,
                Health = 650,
                Mana = 800,
                HeroType = "Daño Mágico",
                AttackDamage = 60,
                AttackSpeed = 0.6f,
                MovementSpeed = 330,
                Armor = 15,
                MagicResistance = 30
            };
            
            mage.Abilities.Add(new HeroAbility 
            { 
                Name = "Orbe Arcano", 
                Description = "Lanza un orbe de energía que explota al impactar",
                Hotkey = "Q",
                Cooldown = 5,
                ManaCost = 70,
                AbilityType = "Activa",
                DamageAmount = 150,
                Range = 800
            });
            
            mage.Abilities.Add(new HeroAbility 
            { 
                Name = "Barrera Mágica", 
                Description = "Crea un escudo que absorbe daño",
                Hotkey = "W",
                Cooldown = 18,
                ManaCost = 90,
                AbilityType = "Activa",
                Duration = 4,
            });
            
            mage.Abilities.Add(new HeroAbility 
            { 
                Name = "Teletransporte", 
                Description = "Se teletransporta una corta distancia",
                Hotkey = "E",
                Cooldown = 15,
                ManaCost = 80,
                AbilityType = "Activa",
                Range = 400
            });
            
            mage.Abilities.Add(new HeroAbility 
            { 
                Name = "Explosión Arcana",

                Description = "Causa una explosión en un área que daña a todos los enemigos",
                Hotkey = "R",
                Cooldown = 120,
                ManaCost = 150,
                AbilityType = "Ultimate",
                DamageAmount = 300,
                Range = 600
            });
            
            // Arquero
            HeroData archer = new HeroData 
            { 
                Id = 2, 
                Name = "Arquero", 
                Description = "Daño físico a distancia con alta velocidad de ataque",
                AvatarSprite = null,
                Health = 700,
                Mana = 500,
                HeroType = "Daño Físico",
                AttackDamage = 75,
                AttackSpeed = 1.2f,
                MovementSpeed = 345,
                Armor = 20,
                MagicResistance = 20
            };
            
            archer.Abilities.Add(new HeroAbility 
            { 
                Name = "Flecha Penetrante", 
                Description = "Dispara una flecha que atraviesa a los enemigos",
                Hotkey = "Q",
                Cooldown = 7,
                ManaCost = 60,
                AbilityType = "Activa",
                DamageAmount = 130,
                Range = 1000
            });
            
            archer.Abilities.Add(new HeroAbility 
            { 
                Name = "Visión del Halcón", 
                Description = "Aumenta el rango de visión y el daño crítico",
                Hotkey = "W",
                Cooldown = 20,
                ManaCost = 70,
                AbilityType = "Activa",
                Duration = 6,
            });
            
            archer.Abilities.Add(new HeroAbility 
            { 
                Name = "Salto Acrobático", 
                Description = "Salta hacia atrás y aumenta la velocidad de movimiento",
                Hotkey = "E",
                Cooldown = 14,
                ManaCost = 65,
                AbilityType = "Activa",
                Duration = 3,
                Range = 350
            });
            
            archer.Abilities.Add(new HeroAbility 
            { 
                Name = "Lluvia de Flechas", 
                Description = "Dispara múltiples flechas al cielo que caen en un área",
                Hotkey = "R",
                Cooldown = 100,
                ManaCost = 120,
                AbilityType = "Ultimate",
                DamageAmount = 250,
                Duration = 4,
                Range = 800
            });
            
            // Añadir los héroes a la lista
            AvailableHeroes.Add(warrior);
            AvailableHeroes.Add(mage);
            AvailableHeroes.Add(archer);
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

        private void AssignTeam()
        {
            // Contar jugadores en cada equipo
            int redCount = 0;
            int blueCount = 0;
            
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                if (p == PhotonNetwork.LocalPlayer) continue; // Saltar jugador local
                
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
            
            // Asignar al equipo con menos jugadores
            assignedTeam = (redCount <= blueCount) ? TEAM_RED : TEAM_BLUE;
            
            // Actualizar propiedades personalizadas
            Hashtable props = new Hashtable { { PLAYER_TEAM, assignedTeam } };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        private void RebalanceTeams()
        {
            // Esta función podría ser llamada cuando un jugador sale para reequilibrar si es necesario
            // Por ahora lo mantendremos simple y no forzaremos a los jugadores a cambiar de equipo
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
            TeamAssignmentText.text = "Equipo: " + (assignedTeam == TEAM_RED ? "Rojo" : "Azul");
            TeamAssignmentText.color = (assignedTeam == TEAM_RED ? Color.red : Color.blue);
            
            // Actualizar estado visual de los iconos de héroe
            foreach (var entry in heroSelectionEntries.Values)
            {
                entry.GetComponent<HeroSelectionEntry>().UpdateSelectionStatus();
            }
            
            // Actualizar texto de estado del juego
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