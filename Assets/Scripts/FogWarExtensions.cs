using UnityEngine;
using FischlWorks_FogWar;

public static class FogWarExtensions
{
    public static void SetLevelMidPoint(this csFogWar fogWar, Transform midPoint)
    {
        if (fogWar == null || midPoint == null) return;

        // Asignar el punto medio usando reflexión
        var field = typeof(csFogWar).GetField("levelMidPoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(fogWar, midPoint);
            Debug.Log($"[FogWarExtensions] LevelMidPoint asignado correctamente a {fogWar.name}");
        }
        else
        {
            Debug.LogError("[FogWarExtensions] No se pudo encontrar el campo levelMidPoint en csFogWar");
        }
    }
} 