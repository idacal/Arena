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
    
    [SerializeField] private Sprite firstBloodIcon;
    [SerializeField] private Sprite doubleKillIcon;
    [SerializeField] private Sprite tripleKillIcon;
    [SerializeField] private Sprite quadraKillIcon;
    [SerializeField] private Sprite pentaKillIcon;
    [SerializeField] private Sprite killIcon;
    
    private Dictionary<string, int> playerKillStreaks = new Dictionary<string, int>();
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
        
        bool isFirstBlood = !firstBloodOccurred;
        if (isFirstBlood)
        {
            firstBloodOccurred = true;
        }
        
        if (!playerKillStreaks.ContainsKey(killerName))
        {
            playerKillStreaks[killerName] = 0;
        }
        playerKillStreaks[killerName]++;
        
        Sprite streakIcon = null;
        string streakText = "";
        
        if (isFirstBlood)
        {
            streakIcon = firstBloodIcon;
            streakText = "First Blood!";
        }
        else
        {
            int streak = playerKillStreaks[killerName];
            switch (streak)
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
        }
        
        CreateKillFeedEntry(killerName, victimName, killIcon, streakIcon, streakText, entryDuration);
    }
    
    private void CreateKillFeedEntry(string killerName, string victimName, Sprite killIcon, Sprite streakIcon, string streakText, float duration)
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
            killFeedEntry.Setup(killerName, victimName, streakIcon, streakText, duration);
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
    }
} 