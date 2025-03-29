using UnityEngine;
using FischlWorks_FogWar;
using Photon.Pun;
using System.Collections.Generic;
using Photon.Pun.Demo.Asteroids;
using UnityEditor;

public class MOBAFogWar : MonoBehaviourPunCallbacks
{
    public static MOBAFogWar Instance { get; private set; }

    [Header("Configuración")]
    public GameObject fogWarPrefab;    // Prefab que contiene el componente csFogWar
    public float sightRange = 10f;     // Rango de visión para los reveladores
    public Transform levelMidPoint;     // Punto medio del nivel para el sistema de niebla
    public Vector2 levelSize = new Vector2(100f, 100f); // Tamaño del plano de niebla (X, Z)

    private csFogWar redTeamFogWar;
    private csFogWar blueTeamFogWar;
    private Dictionary<HeroBase, int> heroRevealerIndices = new Dictionary<HeroBase, int>();
    private int localPlayerTeam = -1;   // Equipo del jugador local

    private void Awake()
    {
        Instance = this;
        if (levelMidPoint == null)
        {
            Debug.LogError("[MOBAFogWar] No se ha asignado el levelMidPoint en el inspector!");
            return;
        }
        CreateTeamFogWars();
    }

    private void CreateTeamFogWars()
    {
        // Crear FogWar para equipo rojo
        GameObject redFogObj = Instantiate(fogWarPrefab, Vector3.zero, Quaternion.identity);
        redFogObj.name = "RedTeam_FogWar";
        redTeamFogWar = redFogObj.GetComponent<csFogWar>();
        if (redTeamFogWar != null)
        {
            redTeamFogWar.SetLevelMidPoint(levelMidPoint);
            // Ajustar el tamaño del plano
            var fogPlane = FindFogPlane(redFogObj);
            if (fogPlane != null)
            {
                Vector3 mapScale = levelMidPoint.localScale;
                fogPlane.localScale = new Vector3(
                    mapScale.x * 5f, // Multiplicamos por 5 para que coincida con la escala del mapa
                    1f,
                    mapScale.z * 5f
                );
                Debug.Log($"[MOBAFogWar] Plano de niebla rojo escalado a: {fogPlane.localScale}");
            }
            else
            {
                Debug.LogError("[MOBAFogWar] No se encontró el plano de niebla rojo");
            }
            // Ocultar el panel de niebla del equipo rojo inicialmente
            redTeamFogWar.gameObject.SetActive(false);
        }

        // Crear FogWar para equipo azul
        GameObject blueFogObj = Instantiate(fogWarPrefab, Vector3.zero, Quaternion.identity);
        blueFogObj.name = "BlueTeam_FogWar";
        blueTeamFogWar = blueFogObj.GetComponent<csFogWar>();
        if (blueTeamFogWar != null)
        {
            blueTeamFogWar.SetLevelMidPoint(levelMidPoint);
            // Ajustar el tamaño del plano
            var fogPlane = FindFogPlane(blueFogObj);
            if (fogPlane != null)
            {
                Vector3 mapScale = levelMidPoint.localScale;
                fogPlane.localScale = new Vector3(
                    mapScale.x * 5f, // Multiplicamos por 5 para que coincida con la escala del mapa
                    1f,
                    mapScale.z * 5f
                );
                Debug.Log($"[MOBAFogWar] Plano de niebla azul escalado a: {fogPlane.localScale}");
            }
            else
            {
                Debug.LogError("[MOBAFogWar] No se encontró el plano de niebla azul");
            }
            // Ocultar el panel de niebla del equipo azul inicialmente
            blueTeamFogWar.gameObject.SetActive(false);
        }

        Debug.Log($"Sistemas de niebla de guerra creados para ambos equipos");
    }

    private Transform FindFogPlane(GameObject fogObj)
    {
        // Buscar en todos los hijos
        var allChildren = fogObj.GetComponentsInChildren<Transform>(true);
        foreach (var child in allChildren)
        {
            // Buscar por nombre que contenga "Fog" o "Plane"
            if (child.name.Contains("Fog") || child.name.Contains("Plane"))
            {
                Debug.Log($"[MOBAFogWar] Encontrado objeto de niebla: {child.name}");
                return child;
            }
        }
        return null;
    }

    public void SetLocalPlayerTeam(int teamId)
    {
        localPlayerTeam = teamId;
        UpdateFogWarVisibility();
    }

    private void UpdateFogWarVisibility()
    {
        if (redTeamFogWar != null)
        {
            redTeamFogWar.gameObject.SetActive(localPlayerTeam == 0);
        }
        if (blueTeamFogWar != null)
        {
            blueTeamFogWar.gameObject.SetActive(localPlayerTeam == 1);
        }
        Debug.Log($"[MOBAFogWar] Visibilidad actualizada para el equipo {localPlayerTeam}");
    }

    // Este método será llamado desde HeroBase.Start()
    public void RegisterHero(HeroBase hero)
    {
        if (hero == null) return;

        try
        {
            csFogWar targetFogWar = (hero.teamId == 0) ? redTeamFogWar : blueTeamFogWar;
            if (targetFogWar == null)
            {
                Debug.LogError($"No se encontró el sistema de niebla para el equipo {hero.teamId}");
                return;
            }

            var revealer = new csFogWar.FogRevealer(
                hero.transform,
                Mathf.RoundToInt(sightRange),
                true
            );
            targetFogWar.AddFogRevealer(revealer);
            
            // Guardar el índice del revelador para este héroe
            heroRevealerIndices[hero] = targetFogWar._FogRevealers.Count - 1;
            
            Debug.Log($"Héroe {hero.heroName} registrado en el equipo {(hero.teamId == 0 ? "Rojo" : "Azul")}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error al registrar héroe en fog of war: {e.Message}");
        }
    }

    public void UnregisterHero(HeroBase hero)
    {
        if (hero == null) return;

        csFogWar targetFogWar = (hero.teamId == 0) ? redTeamFogWar : blueTeamFogWar;
        if (targetFogWar == null) return;

        if (heroRevealerIndices.TryGetValue(hero, out int revealerIndex))
        {
            targetFogWar.RemoveFogRevealer(revealerIndex);
            heroRevealerIndices.Remove(hero);
            Debug.Log($"[MOBAFogWar] Revelador eliminado para el héroe {hero.heroName}");
        }
    }
} 