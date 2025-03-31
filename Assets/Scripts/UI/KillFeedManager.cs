using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Pun.Demo.Asteroids;

public class KillFeedManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject killFeedEntryPrefab;
    [SerializeField] private Transform killFeedContainer;
    [SerializeField] private float entryDuration = 5f;
    
    [Header("Configuración de Multi-Kills")]
    [SerializeField] private float multiKillTimeWindow = 3f; // Tiempo máximo entre kills para contar como multi-kill
    
    [Header("Iconos de Multi-Kills")]
    [SerializeField] private Sprite firstBloodIcon;
    [SerializeField] private Sprite doubleKillIcon;
    [SerializeField] private Sprite tripleKillIcon;
    [SerializeField] private Sprite quadraKillIcon;
    [SerializeField] private Sprite pentaKillIcon;
    
    [Header("Iconos de Kill Streaks")]
    [SerializeField] private Sprite killStreakIcon;
    [SerializeField] private Sprite dominatingIcon;
    [SerializeField] private Sprite unstoppableIcon;
    [SerializeField] private Sprite godlikeIcon;
    [SerializeField] private Sprite legendaryIcon;
    
    private Dictionary<string, int> playerKillStreaks = new Dictionary<string, int>();
    private Dictionary<string, int> playerMultiKills = new Dictionary<string, int>();
    private Dictionary<string, float> playerLastKillTime = new Dictionary<string, float>();
    private bool firstBloodOccurred = false;
    
    private void Start()
    {
        Debug.Log("[KillFeedManager] Iniciando...");
        
        if (killFeedContainer == null)
        {
            Debug.LogError("[KillFeedManager] No se ha asignado el contenedor del kill feed!");
        }
        
        if (killFeedEntryPrefab == null)
        {
            Debug.LogError("[KillFeedManager] No se ha asignado el prefab de entrada del kill feed!");
        }
    }
    
    public void HandlePlayerKill(int killerActorNumber, int victimActorNumber)
    {
        Debug.Log($"[KillFeedManager] Manejando muerte: {killerActorNumber} mató a {victimActorNumber}");
        
        if (!PhotonNetwork.IsConnected) return;
        
        string killerName = GetPlayerName(killerActorNumber);
        string victimName = GetPlayerName(victimActorNumber);
        
        Debug.Log($"[KillFeedManager] Nombres: {killerName} mató a {victimName}");
        
        if (string.IsNullOrEmpty(killerName) || string.IsNullOrEmpty(victimName))
        {
            Debug.LogError("[KillFeedManager] No se pudieron obtener los nombres de los jugadores");
            return;
        }
        
        float currentTime = Time.time;
        
        // Manejar First Blood
        bool isFirstBlood = !firstBloodOccurred;
        if (isFirstBlood)
        {
            firstBloodOccurred = true;
            CreateKillFeedEntry(killerName, victimName, firstBloodIcon, "First Blood!", entryDuration, true, 0, false);
            return;
        }
        
        // Manejar Multi-Kills
        if (!playerLastKillTime.ContainsKey(killerName))
        {
            playerLastKillTime[killerName] = 0;
            playerMultiKills[killerName] = 0;
        }
        
        float timeSinceLastKill = currentTime - playerLastKillTime[killerName];
        playerLastKillTime[killerName] = currentTime;
        
        if (timeSinceLastKill <= multiKillTimeWindow)
        {
            playerMultiKills[killerName]++;
            HandleMultiKill(killerName, victimName);
        }
        else
        {
            playerMultiKills[killerName] = 1;
            HandleKillStreak(killerName, victimName);
            
            // Mostrar muerte normal si no hay racha
            if (playerKillStreaks[killerName] < 5)
            {
                CreateKillFeedEntry(killerName, victimName, null, "", entryDuration, false, 0, false);
            }
        }
    }
    
    private void HandleMultiKill(string killerName, string victimName)
    {
        Sprite streakIcon = null;
        string streakText = "";
        int multiKillCount = playerMultiKills[killerName];
        
        switch (multiKillCount)
        {
            case 2:
                streakIcon = doubleKillIcon;
                streakText = "Double Kill!";
                break;
            case 3:
                streakIcon = tripleKillIcon;
                streakText = "Triple Kill!";
                break;
            case 4:
                streakIcon = quadraKillIcon;
                streakText = "Quadra Kill!";
                break;
            case 5:
                streakIcon = pentaKillIcon;
                streakText = "PENTA KILL!";
                break;
        }
        
        if (streakIcon != null)
        {
            CreateKillFeedEntry(killerName, victimName, streakIcon, streakText, entryDuration, false, multiKillCount, true);
        }
    }
    
    private void HandleKillStreak(string killerName, string victimName)
    {
        if (!playerKillStreaks.ContainsKey(killerName))
        {
            playerKillStreaks[killerName] = 0;
        }
        playerKillStreaks[killerName]++;
        
        Sprite streakIcon = null;
        string streakText = "";
        int streakCount = playerKillStreaks[killerName];
        
        if (streakCount >= 5)
        {
            streakIcon = killStreakIcon;
            streakText = $"Kill Streak x{streakCount}!";
        }
        if (streakCount >= 10)
        {
            streakIcon = dominatingIcon;
            streakText = "Dominating!";
        }
        if (streakCount >= 15)
        {
            streakIcon = unstoppableIcon;
            streakText = "Unstoppable!";
        }
        if (streakCount >= 20)
        {
            streakIcon = godlikeIcon;
            streakText = "Godlike!";
        }
        if (streakCount >= 25)
        {
            streakIcon = legendaryIcon;
            streakText = "Legendary!";
        }
        
        if (streakIcon != null)
        {
            CreateKillFeedEntry(killerName, victimName, streakIcon, streakText, entryDuration, false, streakCount, false);
        }
    }
    
    private void CreateKillFeedEntry(string killerName, string victimName, Sprite streakIcon, string streakText, float duration, bool isFirstBlood, int streakCount, bool isMultiKill)
    {
        if (killFeedEntryPrefab == null || killFeedContainer == null)
        {
            Debug.LogError("[KillFeedManager] Faltan referencias necesarias para crear la entrada del kill feed!");
            return;
        }
        
        Debug.Log($"[KillFeedManager] Creando entrada: {killerName} -> {victimName}");
        
        GameObject entry = Instantiate(killFeedEntryPrefab, killFeedContainer);
        KillFeedEntry killFeedEntry = entry.GetComponent<KillFeedEntry>();
        
        if (killFeedEntry != null)
        {
            killFeedEntry.Setup(killerName, victimName, streakIcon, streakText, duration, isFirstBlood, streakCount, isMultiKill);
        }
        else
        {
            Debug.LogError("[KillFeedManager] No se encontró el componente KillFeedEntry en el prefab!");
        }
    }
    
    private string GetPlayerName(int actorNumber)
    {
        Photon.Realtime.Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
        return player?.NickName ?? "Unknown Player";
    }
    
    public void ResetKillStreak(string playerName)
    {
        if (playerKillStreaks.ContainsKey(playerName))
        {
            playerKillStreaks[playerName] = 0;
        }
        if (playerMultiKills.ContainsKey(playerName))
        {
            playerMultiKills[playerName] = 0;
        }
        if (playerLastKillTime.ContainsKey(playerName))
        {
            playerLastKillTime[playerName] = 0;
        }
    }
} 