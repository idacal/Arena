using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System;

public class CombatManager : MonoBehaviourPunCallbacks
{
    // Singleton
    public static CombatManager Instance { get; private set; }
    
    // Eventos
    public event Action<int, int> OnPlayerKill; // (killerActorNumber, victimActorNumber)
    public event Action<int> OnPlayerDeath; // (victimActorNumber)
    
    // Estadísticas
    private Dictionary<int, int> playerKills = new Dictionary<int, int>();
    private Dictionary<int, int> playerDeaths = new Dictionary<int, int>();
    
    // Prefabs
    public GameObject floatingDamageTextPrefab;
    public GameObject deathEffectPrefab;
    
    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    private void Start()
    {
        // Suscribirse a eventos cuando los héroes se inicialicen
        if (PhotonNetwork.IsConnected)
        {
            photonView.RPC("RPC_RegisterPlayers", RpcTarget.AllBuffered);
        }
    }
    
    [PunRPC]
    private void RPC_RegisterPlayers()
    {
        // Encontrar todos los héroes y registrar sus eventos
        StartCoroutine(RegisterHeroesWhenAvailable());
    }
    
    private IEnumerator RegisterHeroesWhenAvailable()
    {
        // Esperar a que los héroes estén disponibles
        yield return new WaitForSeconds(1f);
        
        HeroHealth[] heroHealthComponents = FindObjectsOfType<HeroHealth>();
        
        foreach (HeroHealth health in heroHealthComponents)
        {
            // Suscribirse a eventos de daño y muerte
            health.OnHeroDeath += HandleHeroDeath;
            health.OnDamageTaken += HandleDamageTaken;
            
            // Inicializar estadísticas si este héroe es nuevo
            int actorNumber = health.photonView.Owner.ActorNumber;
            if (!playerKills.ContainsKey(actorNumber))
            {
                playerKills[actorNumber] = 0;
            }
            if (!playerDeaths.ContainsKey(actorNumber))
            {
                playerDeaths[actorNumber] = 0;
            }
        }
    }
    
    private void HandleHeroDeath(int killerActorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
        // Obtener información del héroe muerto
        HeroHealth deadHero = GetCallingComponent<HeroHealth>();
        if (deadHero == null) return;
        
        int deadPlayerActorNumber = deadHero.photonView.Owner.ActorNumber;
        
        // Actualizar estadísticas
        if (playerDeaths.ContainsKey(deadPlayerActorNumber))
        {
            playerDeaths[deadPlayerActorNumber]++;
        }
        else
        {
            playerDeaths[deadPlayerActorNumber] = 1;
        }
        
        if (killerActorNumber > 0) // Si hay un asesino válido
        {
            if (playerKills.ContainsKey(killerActorNumber))
            {
                playerKills[killerActorNumber]++;
            }
            else
            {
                playerKills[killerActorNumber] = 1;
            }
            
            // Notificar muerte
            OnPlayerKill?.Invoke(killerActorNumber, deadPlayerActorNumber);
            
            // Sincronizar estadísticas
            photonView.RPC("RPC_SyncKillStats", RpcTarget.All, killerActorNumber, deadPlayerActorNumber);
        }
        
        // Notificar muerte en general
        OnPlayerDeath?.Invoke(deadPlayerActorNumber);
    }
    
    private void HandleDamageTaken(float amount, int attackerActorNumber)
    {
        // No se requiere acción centralizada para daño
    }
    
    [PunRPC]
    private void RPC_SyncKillStats(int killerActorNumber, int victimActorNumber)
    {
        // Actualizar estadísticas locales
        if (playerKills.ContainsKey(killerActorNumber))
        {
            playerKills[killerActorNumber]++;
        }
        else
        {
            playerKills[killerActorNumber] = 1;
        }
        
        if (playerDeaths.ContainsKey(victimActorNumber))
        {
            playerDeaths[victimActorNumber]++;
        }
        else
        {
            playerDeaths[victimActorNumber] = 1;
        }
        
        // Notificar a los listeners
        OnPlayerKill?.Invoke(killerActorNumber, victimActorNumber);
        OnPlayerDeath?.Invoke(victimActorNumber);
    }
    
    // Utilidad para obtener el componente que llamó a un evento
    private T GetCallingComponent<T>() where T : Component
    {
        // Obtener el objeto que llamó
        var stackTrace = new System.Diagnostics.StackTrace();
        var callingMethod = stackTrace.GetFrame(2).GetMethod();
        var callingType = callingMethod.DeclaringType;
        
        // Encontrar todos los componentes de ese tipo
        T[] components = FindObjectsOfType<T>();
        
        foreach (T component in components)
        {
            if (component.GetType() == callingType || component.GetType().IsSubclassOf(callingType))
            {
                return component;
            }
        }
        
        return null;
    }
    
    // Métodos públicos
    
    public int GetPlayerKills(int actorNumber)
    {
        if (playerKills.ContainsKey(actorNumber))
        {
            return playerKills[actorNumber];
        }
        return 0;
    }
    
    public int GetPlayerDeaths(int actorNumber)
    {
        if (playerDeaths.ContainsKey(actorNumber))
        {
            return playerDeaths[actorNumber];
        }
        return 0;
    }
    
    public Dictionary<int, int> GetAllKills()
    {
        return new Dictionary<int, int>(playerKills);
    }
    
    public Dictionary<int, int> GetAllDeaths()
    {
        return new Dictionary<int, int>(playerDeaths);
    }
    
    // Limpiar al salir de la partida
    public void ResetStats()
    {
        playerKills.Clear();
        playerDeaths.Clear();
    }
} 