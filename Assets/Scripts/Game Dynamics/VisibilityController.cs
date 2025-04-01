using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

/// <summary>
/// Controla la visibilidad de otras unidades para esta unidad, aplicando reglas como
/// las de los arbustos (no se puede ver unidades enemigas dentro de arbustos a menos que
/// esta unidad también esté dentro del mismo arbusto).
/// </summary>
public class VisibilityController : MonoBehaviourPunCallbacks
{
    [Header("Debug")]
    [Tooltip("Mostrar mensajes de debug en la consola")]
    public bool showDebugMessages = false;
    
    [Header("Configuración")]
    [Tooltip("Intervalo en segundos para actualizar la visibilidad")]
    public float updateInterval = 0.25f;
    
    // Propiedades públicas para obtener información desde otros scripts
    public bool IsLocalPlayer { get; private set; }
    
    // Referencias privadas
    private EntityVisibilityTracker visibilityTracker;
    private float updateTimer = 0f;
    private bool initialized = false;
    
    void Start()
    {
        // Verificar si es el jugador local o no
        IsLocalPlayer = photonView != null && photonView.IsMine;
        
        // Obtener el componente EntityVisibilityTracker (o añadirlo si no existe)
        visibilityTracker = GetComponent<EntityVisibilityTracker>();
        if (visibilityTracker == null)
        {
            visibilityTracker = gameObject.AddComponent<EntityVisibilityTracker>();
            LogMessage("EntityVisibilityTracker añadido automáticamente.");
        }
        
        // Verificar que la capa HiddenInBush existe
        if (LayerManager.HiddenInBushLayerID == -1)
        {
            Debug.LogError("¡La capa 'HiddenInBush' no existe! Crea esta capa en Edit > Project Settings > Tags & Layers");
        }
        else
        {
            Debug.Log($"[VisibilityController] Capa HiddenInBush encontrada con ID: {LayerManager.HiddenInBushLayerID}");
        }
        
        LogMessage($"VisibilityController inicializado. IsLocalPlayer: {IsLocalPlayer}");
        
        // Aplicar visibilidad inicial después de un pequeño retraso
        Invoke("InitialVisibilityUpdate", 0.5f);
    }
    
    void InitialVisibilityUpdate()
    {
        // Aplicar la visibilidad inicial a todas las unidades
        ApplyVisibilityToAllUnits();
        initialized = true;
        Debug.Log("[VisibilityController] Visibilidad inicial aplicada");
    }
    
    void Update()
    {
        // Solo funciona para el jugador local
        if (!IsLocalPlayer || !initialized) return;
        
        // Actualizar la visibilidad periódicamente
        updateTimer -= Time.deltaTime;
        if (updateTimer <= 0)
        {
            ApplyVisibilityToAllUnits();
            updateTimer = updateInterval;
        }
    }
    
    /// <summary>
    /// Determina si esta unidad puede ver a otra unidad, aplicando reglas de visibilidad como las de arbustos.
    /// </summary>
    public bool CanSeeUnit(GameObject targetUnit)
    {
        if (targetUnit == null)
            return false;
            
        // Verificar si estamos en la misma capa (para depuración)
        LogMessage($"Mi capa: {LayerMask.LayerToName(gameObject.layer)}, Capa objetivo: {LayerMask.LayerToName(targetUnit.layer)}");
            
        // Regla 1: Si el objetivo es un aliado, siempre es visible
        if (IsAlly(targetUnit))
        {
            LogMessage($"Veo a {targetUnit.name} porque es un aliado.");
            return true;
        }
        
        // Regla 2: Si el objetivo está en la capa oculta (HiddenInBush)
        if (targetUnit.layer == LayerManager.HiddenInBushLayerID)
        {
            // Mostrar siempre este mensaje aunque el debug esté desactivado para encontrar problemas
            Debug.Log($"[VisibilityController] Objetivo {targetUnit.name} está en capa HiddenInBush (ID: {LayerManager.HiddenInBushLayerID})");
            
            // Esta unidad (observador) no está en un arbusto, por lo tanto no puede ver al objetivo
            if (!visibilityTracker.IsInBush)
            {
                Debug.Log($"[VisibilityController] No puedo ver a {targetUnit.name} porque estoy fuera de arbustos y él dentro.");
                return false;
            }
            
            // Ambos están en arbustos. ¿Están en el mismo arbusto?
            EntityVisibilityTracker targetTracker = targetUnit.GetComponent<EntityVisibilityTracker>();
            if (targetTracker != null)
            {
                if (targetTracker.CurrentBush == visibilityTracker.CurrentBush && visibilityTracker.CurrentBush != null)
                {
                    // Están en el mismo arbusto, la unidad es visible
                    Debug.Log($"[VisibilityController] Veo a {targetUnit.name} porque ambos estamos en el mismo arbusto: {visibilityTracker.CurrentBush.name}");
                    return true;
                }
                else
                {
                    // Están en arbustos diferentes, la unidad no es visible
                    Debug.Log($"[VisibilityController] No puedo ver a {targetUnit.name} porque estamos en arbustos diferentes.");
                    return false;
                }
            }
            else
            {
                Debug.LogWarning($"[VisibilityController] El objeto {targetUnit.name} está en capa HiddenInBush pero no tiene EntityVisibilityTracker");
            }
        }
        
        // El objetivo no está en un arbusto o todas las verificaciones pasaron, es visible
        LogMessage($"Veo a {targetUnit.name} porque no está en un arbusto o pasó todas las verificaciones.");
        return true;
    }
    
    /// <summary>
    /// Aplica las reglas de visibilidad a todos los renderers de una unidad.
    /// Puede ocultar o mostrar la unidad según sea necesario.
    /// </summary>
    public void ApplyVisibilityToUnit(GameObject targetUnit)
    {
        if (targetUnit == null)
            return;
            
        // Obtener todos los renderers de la unidad objetivo
        Renderer[] renderers = targetUnit.GetComponentsInChildren<Renderer>();
        
        // Determinar si la unidad debería ser visible para esta unidad
        bool shouldBeVisible = CanSeeUnit(targetUnit);
        
        // Aplicar la visibilidad a todos los renderers
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = shouldBeVisible;
            }
        }
        
        // Siempre mostrar este mensaje para debug
        if (targetUnit.layer == LayerManager.HiddenInBushLayerID || !shouldBeVisible)
        {
            Debug.Log($"[VisibilityController] Aplicada visibilidad a {targetUnit.name}: visible = {shouldBeVisible}");
        }
        else
        {
            LogMessage($"Aplicada visibilidad a {targetUnit.name}: visible = {shouldBeVisible}");
        }
    }
    
    /// <summary>
    /// Aplica las reglas de visibilidad a todas las unidades de un equipo.
    /// </summary>
    public void ApplyVisibilityToTeam(string teamTag)
    {
        GameObject[] teamUnits = GameObject.FindGameObjectsWithTag(teamTag);
        LogMessage($"Encontradas {teamUnits.Length} unidades con tag {teamTag}");
        
        foreach (GameObject unit in teamUnits)
        {
            if (unit != gameObject) // No aplicar a sí mismo
            {
                ApplyVisibilityToUnit(unit);
            }
        }
    }
    
    /// <summary>
    /// Aplica las reglas de visibilidad a todas las unidades de ambos equipos.
    /// </summary>
    public void ApplyVisibilityToAllUnits()
    {
        LogMessage("Actualizando visibilidad para todas las unidades...");
        
        ApplyVisibilityToTeam(LayerManager.TAG_RED_TEAM);
        ApplyVisibilityToTeam(LayerManager.TAG_BLUE_TEAM);
    }
    
    /// <summary>
    /// Verifica si una unidad es aliada de esta unidad.
    /// </summary>
    private bool IsAlly(GameObject other)
    {
        return LayerManager.IsAlly(gameObject, other);
    }
    
    /// <summary>
    /// Método para logging condicional.
    /// </summary>
    private void LogMessage(string message)
    {
        if (showDebugMessages)
        {
            Debug.Log($"[VisibilityController] {message}", this);
        }
    }
} 