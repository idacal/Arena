using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

namespace Photon.Pun.Demo.Asteroids
{
    public class GameplayManager : MonoBehaviourPunCallbacks
    {
        [Header("Spawn Points")]
        public Transform redTeamSpawn;
        public Transform blueTeamSpawn;
        
        [Header("Debug Options")]
        public bool showDebugMessages = true;
        
        // Eventos personalizados
        private const byte HERO_INSTANTIATION_EVENT = 1;
        
        // Dictionary para rastrear los héroes instanciados
        private Dictionary<int, GameObject> spawnedHeroes = new Dictionary<int, GameObject>();
        
        // Control para evitar duplicación
        private bool hasInstantiatedLocalHero = false;
        
        void Start()
        {
            LogInfo("GameplayManager iniciando...");
            
            // Verificar si tenemos los puntos de spawn
            if (redTeamSpawn == null || blueTeamSpawn == null)
            {
                Debug.LogError("¡Faltan puntos de spawn! Asigna los puntos de spawn para ambos equipos.");
                return;
            }
            
            // Asegurarse de que estamos conectados a Photon
            if (!PhotonNetwork.IsConnected)
            {
                Debug.LogError("No estás conectado a Photon. Volviendo a la escena del Lobby.");
                PhotonNetwork.LoadLevel("Lobby");
                return;
            }
            
            // Registrar el evento de propiedades del jugador
            PhotonNetwork.NetworkingClient.EventReceived += OnEventReceived;
            
            LogInfo($"Escena cargada. Jugador local: {PhotonNetwork.LocalPlayer.NickName} (ActorNumber: {PhotonNetwork.LocalPlayer.ActorNumber})");
            LogInfo($"Total de jugadores en la sala: {PhotonNetwork.CurrentRoom.PlayerCount}");

            // Establecer la propiedad para indicar que hemos cargado el nivel
            Hashtable props = new Hashtable
            {
                {ArenaGame.PLAYER_LOADED_LEVEL, true}
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
            
            LogInfo("Propiedad PLAYER_LOADED_LEVEL establecida a true para el jugador local.");
            
            // Verificar si todos los jugadores ya están cargados
            if (CheckAllPlayersLoaded())
            {
                LogInfo("Todos los jugadores ya están cargados. Iniciando juego inmediatamente.");
                StartGame();
            }
            else
            {
                LogInfo("Esperando a que todos los jugadores carguen el nivel...");
            }
        }
        
        void OnDestroy()
        {
            // Desregistrar eventos al destruir el objeto
            if (PhotonNetwork.NetworkingClient != null)
            {
                PhotonNetwork.NetworkingClient.EventReceived -= OnEventReceived;
            }
        }
        
        public override void OnDisable()
        {
            base.OnDisable();
            
            // Desregistrar el evento de propiedades del jugador
            if (PhotonNetwork.NetworkingClient != null)
            {
                PhotonNetwork.NetworkingClient.EventReceived -= OnEventReceived;
            }
        }
        
        /// <summary>
        /// Llamado cuando todos los jugadores están listos
        /// </summary>
        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            // Verificar si el jugador ha cargado el nivel
            if (changedProps.ContainsKey(ArenaGame.PLAYER_LOADED_LEVEL))
            {
                bool isLoaded = (bool)changedProps[ArenaGame.PLAYER_LOADED_LEVEL];
                LogInfo($"Jugador {targetPlayer.NickName} ha actualizado su propiedad PLAYER_LOADED_LEVEL a: {isLoaded}");
                
                if (isLoaded && CheckAllPlayersLoaded())
                {
                    LogInfo("¡Todos los jugadores han cargado el nivel! Iniciando partida...");
                    StartGame();
                }
            }
        }
        
        /// <summary>
        /// Verifica si todos los jugadores han cargado el nivel
        /// </summary>
        private bool CheckAllPlayersLoaded()
        {
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                object isPlayerLoaded;
                if (p.CustomProperties.TryGetValue(ArenaGame.PLAYER_LOADED_LEVEL, out isPlayerLoaded))
                {
                    if (!(bool)isPlayerLoaded)
                    {
                        LogInfo($"Jugador {p.NickName} aún no ha cargado el nivel.");
                        return false;
                    }
                }
                else
                {
                    LogInfo($"Jugador {p.NickName} no tiene la propiedad PLAYER_LOADED_LEVEL.");
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Inicia la partida cuando todos están listos
        /// </summary>
        private void StartGame()
        {
            LogInfo("Iniciando juego...");
            
            // Importante: SÓLO EL JUGADOR LOCAL instancia su propio héroe
            // NO intentamos instanciar héroes para otros jugadores
            InstantiateLocalPlayerHero();
        }
        
        /// <summary>
        /// Recibe eventos de Photon
        /// </summary>
        private void OnEventReceived(EventData photonEvent)
        {
            // No usamos eventos para la instanciación, pero mantenemos este método
            // por si necesitamos implementar alguna funcionalidad basada en eventos más adelante
            if (photonEvent.Code == HERO_INSTANTIATION_EVENT)
            {
                LogInfo("Recibido evento de sincronización de héroes.");
            }
        }
        
        /// <summary>
        /// Instancia el héroe del jugador local
        /// </summary>
        private void InstantiateLocalPlayerHero()
        {
            // IMPORTANTE: Verificar si ya hemos instanciado un héroe para el jugador local
            if (hasInstantiatedLocalHero)
            {
                LogInfo("Ya se ha instanciado un héroe para el jugador local. Ignorando.");
                return;
            }
            
            LogInfo("Instanciando héroe para el jugador local...");
            
            // Obtener datos del jugador local
            Player localPlayer = PhotonNetwork.LocalPlayer;
            int team = HeroManager.Instance.GetPlayerTeam(localPlayer);
            
            // Determinar la posición de spawn según el equipo
            Transform spawnPoint = (team == 0) ? redTeamSpawn : blueTeamSpawn;
            
            // Añadir variación para evitar superposiciones
            Vector3 spawnPosition = spawnPoint.position + new Vector3(
                Random.Range(-2f, 2f),
                0f,
                Random.Range(-2f, 2f)
            );
            
            LogInfo($"Jugador {localPlayer.NickName} (Team: {(team == 0 ? "Rojo" : "Azul")}) instanciando héroe en posición {spawnPosition}");
            
            // Instanciar el héroe a través de HeroManager
            GameObject heroInstance = HeroManager.Instance.InstantiatePlayerHero(
                localPlayer, 
                spawnPosition, 
                spawnPoint.rotation
            );
            
            // Verificar si se creó correctamente
            if (heroInstance != null)
            {
                LogInfo($"Héroe instanciado correctamente para {localPlayer.NickName} en equipo {(team == 0 ? "Rojo" : "Azul")}");
                
                // Añadir al diccionario
                spawnedHeroes[localPlayer.ActorNumber] = heroInstance;
                
                // Marcar que ya hemos instanciado
                hasInstantiatedLocalHero = true;
                
                // Configurar la cámara para seguir al héroe local
                SetupCameraForLocalHero(heroInstance);
            }
            else
            {
                Debug.LogError($"Error al instanciar héroe para {localPlayer.NickName}");
            }
        }
        
        /// <summary>
        /// Configura la cámara para seguir al héroe local
        /// </summary>
        private void SetupCameraForLocalHero(GameObject heroInstance)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                // Posicionar la cámara inicialmente
                mainCamera.transform.position = heroInstance.transform.position + new Vector3(0, 10, -5);
                mainCamera.transform.LookAt(heroInstance.transform);
                
                // Si ya existe un componente CameraFollow, eliminarlo
                CameraFollow existingFollow = mainCamera.gameObject.GetComponent<CameraFollow>();
                if (existingFollow != null)
                {
                    Destroy(existingFollow);
                }
                
                // Añadir componente de seguimiento
                CameraFollow cameraFollow = mainCamera.gameObject.AddComponent<CameraFollow>();
                cameraFollow.target = heroInstance.transform;
                cameraFollow.offset = new Vector3(0, 10, -5);
                cameraFollow.smoothSpeed = 0.125f;
                
                LogInfo("Cámara configurada para seguir al héroe local.");
            }
            else
            {
                Debug.LogWarning("No se encontró la cámara principal. No se pudo configurar el seguimiento de cámara.");
            }
        }
        
        /// <summary>
        /// Muestra mensajes de depuración
        /// </summary>
        private void LogInfo(string message)
        {
            if (showDebugMessages)
            {
                Debug.Log($"[GameplayManager] {message}");
            }
        }
    }
    
    /// <summary>
    /// Script simple para seguimiento de cámara
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0, 10, -5);
        public float smoothSpeed = 0.125f;
        
        void LateUpdate()
        {
            if (target == null)
                return;
                
            Vector3 desiredPosition = target.position + offset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothedPosition;
            
            transform.LookAt(target);
        }
    }
}