using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using System;
using Photon.Pun.Demo.Asteroids;

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
        
        HeroBase[] heroes = FindObjectsOfType<HeroBase>();
        
        foreach (HeroBase hero in heroes)
        {
            // Suscribirse a eventos de daño y muerte
            hero.OnHeroDeath += HandleHeroDeath;
            hero.OnHealthChanged += HandleHealthChanged;
            
            // Inicializar estadísticas si este héroe es nuevo
            int actorNumber = hero.photonView.Owner.ActorNumber;
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
    
    private void HandleHeroDeath(HeroBase deadHero)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        
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
        
        // Notificar muerte en general
        OnPlayerDeath?.Invoke(deadPlayerActorNumber);
        
        // Sincronizar estadísticas
        photonView.RPC("RPC_SyncDeathStats", RpcTarget.All, deadPlayerActorNumber);
    }
    
    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        // No se requiere acción centralizada para cambios de salud
    }
    
    [PunRPC]
    private void RPC_SyncDeathStats(int victimActorNumber)
    {
        // Actualizar estadísticas locales
        if (playerDeaths.ContainsKey(victimActorNumber))
        {
            playerDeaths[victimActorNumber]++;
        }
        else
        {
            playerDeaths[victimActorNumber] = 1;
        }
        
        // Notificar a los listeners
        OnPlayerDeath?.Invoke(victimActorNumber);
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