using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using System.Collections; // Para corrutinas y WaitForSeconds

/// <summary>
/// Script para los arbustos que detectan unidades dentro y notifican para ocultarlas/revelarlas.
/// El cambio de capa real se hace vía RPC en la entidad misma.
/// Ahora también gestiona la visibilidad visual del arbusto para el jugador local.
/// </summary>
public class BushVisibility : MonoBehaviourPunCallbacks
{
    [Header("Configuración")]
    [Tooltip("Habilitar mensajes de debug en consola")]
    public bool showDebugMessages = false;

    [Header("Configuración Visual")]
    [Tooltip("Si es true, ajusta la visibilidad del arbusto para el jugador local que está dentro")]
    public bool adjustLocalPlayerVisibility = true;
    
    [Tooltip("Porcentaje de reducción de renderers cuando el jugador está dentro (ej: 0.5 deshabilita la mitad de los renderers)")]
    [Range(0.0f, 0.9f)]
    public float cullingPercentage = 0.7f;
    
    // Lista de GameObjects (identificados por su PhotonView ID) que están físicamente en el trigger
    private HashSet<int> entitiesInTrigger = new HashSet<int>();
    
    // Diccionario para recordar la capa original de cada unidad (PhotonView ID -> Layer ID)
    private Dictionary<int, int> originalLayers = new Dictionary<int, int>();
    
    // Contadores para jugadores locales dentro del arbusto
    private int localPlayersInside = 0;
    
    // Referencias a los renderers del arbusto
    private List<Renderer> bushRenderers = new List<Renderer>();
    private List<bool> originalRendererStates = new List<bool>();
    private LODGroup bushLodGroup;
    
    // Flag para controlar si la visualización está inicializada
    private bool visualsInitialized = false;
    
    void Start()
    {
        // Configurar el tag por si no lo tiene
        if (!gameObject.CompareTag(LayerManager.TAG_BUSH))
        {
            gameObject.tag = LayerManager.TAG_BUSH;
        }
        
        // Verificar que tenemos un Collider y es Trigger
        Collider bushCollider = GetComponent<Collider>();
        if (bushCollider == null)
        {
            Debug.LogError($"¡El arbusto {gameObject.name} no tiene un Collider!", this);
            this.enabled = false;
            return;
        }
        
        if (!bushCollider.isTrigger)
        {
            Debug.LogError($"¡El Collider del arbusto {gameObject.name} no es Trigger!", this);
            this.enabled = false;
            return;
        }
        
        // Verificar que la capa HiddenInBush existe
        if (LayerManager.HiddenInBushLayerID == -1)
        {
            Debug.LogError("La capa 'HiddenInBush' no existe.", this);
            this.enabled = false;
            return;
        }
        
        // Inicializar componentes de renderizado si queremos ajustar la visibilidad
        if (adjustLocalPlayerVisibility)
        {
            InitializeVisualComponents();
        }
        
        LogMessage($"Arbusto {gameObject.name} inicializado.");
    }
    
    /// <summary>
    /// Inicializa los componentes visuales del arbusto para poder modificarlos después
    /// </summary>
    private void InitializeVisualComponents()
    {
        // Buscar el LODGroup para controlar el nivel de detalle
        bushLodGroup = GetComponentInChildren<LODGroup>();
        if (bushLodGroup != null)
        {
            LogMessage($"LODGroup encontrado. Niveles de LOD: {bushLodGroup.lodCount}");
        }
        
        // Encontrar todos los renderers del arbusto
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length > 0)
        {
            // Almacenar referencias a todos los renderers
            foreach (Renderer renderer in renderers)
            {
                bushRenderers.Add(renderer);
                originalRendererStates.Add(renderer.enabled);
            }
            
            LogMessage($"Encontrados {bushRenderers.Count} renderers en el arbusto.");
        }
        else
        {
            LogMessage("No se encontraron renderers en el arbusto.");
        }
        
        visualsInitialized = true;
    }
    
    /// <summary>
    /// Ajusta la visualización del arbusto cuando un jugador local está dentro
    /// </summary>
    private void SetLocalPlayerInside(bool isInside)
    {
        if (!visualsInitialized || !adjustLocalPlayerVisibility) return;
        
        // Actualizar contador de jugadores locales dentro
        localPlayersInside += isInside ? 1 : -1;
        localPlayersInside = Mathf.Max(0, localPlayersInside); // Asegurar que no sea negativo
        
        if (localPlayersInside > 0)
        {
            // Al menos un jugador local está dentro del arbusto
            LogMessage($"Jugador local dentro del arbusto. Ajustando visualización para mejor visibilidad.");
            
            // Desactivar algunos renderers para mejorar la visibilidad
            int renderersToDisable = Mathf.FloorToInt(bushRenderers.Count * cullingPercentage);
            
            for (int i = 0; i < bushRenderers.Count; i++)
            {
                if (bushRenderers[i] != null)
                {
                    // Deshabilitamos solo un porcentaje de los renderers
                    // El porcentaje se configura con cullingPercentage
                    if (i < renderersToDisable)
                    {
                        bushRenderers[i].enabled = false;
                    }
                }
            }
            
            // Si hay LODGroup, aplicamos un nivel menos detallado
            if (bushLodGroup != null)
            {
                bushLodGroup.ForceLOD(1); // Usar LOD 1 (menos detallado que 0)
            }
        }
        else
        {
            // No hay jugadores locales dentro, restaurar visualización normal
            LogMessage($"No hay jugadores locales dentro del arbusto. Restaurando visualización normal.");
            
            // Restaurar estado original de todos los renderers
            for (int i = 0; i < bushRenderers.Count; i++)
            {
                if (bushRenderers[i] != null && i < originalRendererStates.Count)
                {
                    bushRenderers[i].enabled = originalRendererStates[i];
                }
            }
            
            // Restaurar LOD original
            if (bushLodGroup != null)
            {
                bushLodGroup.ForceLOD(-1); // -1 para desactivar el forzado
            }
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        HandleTriggerInteraction(other, true); // Entrando = true
    }

    void OnTriggerStay(Collider other)
    {
        HandleTriggerInteraction(other, true); // Permaneciendo = true
    }

    void OnTriggerExit(Collider other)
    {
        // Salida directa con máxima prioridad
        // Forzamos toda la lógica a ejecutarse en este frame
        HandlePlayerExitBush(other);
    }
    
    /// <summary>
    /// Maneja la salida de un jugador del arbusto de forma más directa
    /// </summary>
    private void HandlePlayerExitBush(Collider other)
    {
        // Verificar si es un jugador con componentes necesarios
        PhotonView entityPhotonView = other.GetComponent<PhotonView>();
        EntityVisibilityTracker entityTracker = other.GetComponent<EntityVisibilityTracker>();

        if (entityPhotonView != null && entityTracker != null && IsValidEntity(other))
        {
            int viewID = entityPhotonView.ViewID;
            GameObject entity = other.gameObject;

            // Verificar si estaba en nuestra lista de entidades dentro del arbusto
            if (entitiesInTrigger.Contains(viewID))
            {
                LogMessage($"🚨 Salida DIRECTA: {entity.name} (ID: {viewID}) salió del arbusto.");
                
                // Quitar de la lista INMEDIATAMENTE
                entitiesInTrigger.Remove(viewID);
                
                // Determinar la capa original primero para poder procesar RPCs lo antes posible
                int originalLayer = LayerManager.PlayerLayerID; // Por defecto Player
                if (originalLayers.TryGetValue(viewID, out int storedLayer))
        {
            originalLayer = storedLayer;
                    LogMessage($"⭐ Capa original recuperada: {LayerMask.LayerToName(originalLayer)} ({originalLayer})");
                    originalLayers.Remove(viewID);
                }
                else
                {
                    LogMessage($"⚠️ No se encontró capa original para {entity.name}. Usando Player por defecto.");
                }
                
                // PRIMERO: Enviar RPCs para cambio de capa con MÁXIMA PRIORIDAD
                // Usamos RpcTarget.All en lugar de AllBuffered para mayor velocidad
                // (Al salir del arbusto queremos priorizar velocidad sobre fiabilidad)
                entityPhotonView.RPC("RPC_ForceLayer", RpcTarget.All, originalLayer, false);
                LogMessage($"📤 FORZANDO restauración a capa {LayerMask.LayerToName(originalLayer)} ({originalLayer})");
                
                // LUEGO: Notificar al tracker localmente
                entityTracker.ExitBush(this);
                
                // Si es el jugador local, actualizar la visualización del arbusto
                if (entityPhotonView.IsMine)
                {
                    SetLocalPlayerInside(false);
                }
                
                // FINALMENTE: Llamar al RPC normal (con buffer) para garantizar consistencia a largo plazo
                entityPhotonView.RPC("RPC_SetVisibilityLayer", RpcTarget.AllBuffered, originalLayer);
                
                // Aplicar capa localmente de forma inmediata (técnica brutal pero efectiva)
                // Esto garantiza que incluso antes de que llegue el RPC, ya esté visible localmente
                if (entity != null)
                {
                    LogMessage($"🔧 Aplicando capa {LayerMask.LayerToName(originalLayer)} localmente de emergencia");
                    try {
                        entity.layer = originalLayer;
                        foreach (Transform child in entity.transform) {
                            if (child != null) child.gameObject.layer = originalLayer;
                        }
                    } catch (System.Exception ex) {
                        LogMessage($"Error al aplicar capa local: {ex.Message}");
                    }
                }
            }
        }
    }

    private void HandleTriggerInteraction(Collider other, bool isEnteringOrStaying)
    {
        // Intentar obtener PhotonView y EntityVisibilityTracker de la entidad
        PhotonView entityPhotonView = other.GetComponent<PhotonView>();
        EntityVisibilityTracker entityTracker = other.GetComponent<EntityVisibilityTracker>();

        if (entityPhotonView != null && entityTracker != null && IsValidEntity(other))
        {
            int viewID = entityPhotonView.ViewID;
            GameObject entity = other.gameObject; // Para logging y obtener capa inicial

            if (isEnteringOrStaying)
            {
                // Si entra o permanece en el trigger
                if (entitiesInTrigger.Add(viewID)) // .Add devuelve true si el elemento no estaba y se añadió
                {
                    // *** Recién Entrado ***
                    LogMessage($"Enter/Stay: {entity.name} (ID: {viewID}) detectado en trigger. Añadido a la lista.");
                    
                    // Guardar capa original SOLO si no la tenemos ya
                    if (!originalLayers.ContainsKey(viewID))
                    {
                        int layerToStore = entity.layer;
                        originalLayers.Add(viewID, layerToStore); 
                        LogMessage($"⭐ Guardada capa original {LayerMask.LayerToName(layerToStore)} ({layerToStore}) para {entity.name}");
                    }
                    else
                    {
                        LogMessage($"⚠️ La capa original para {entity.name} ya estaba guardada: {LayerMask.LayerToName(originalLayers[viewID])} ({originalLayers[viewID]})");
                    }

                    // Si es el jugador local, actualizar la visualización del arbusto
                    if (entityPhotonView.IsMine)
                    {
                        SetLocalPlayerInside(true);
                    }

                    // Notificar localmente al tracker de la entidad que entró
                    entityTracker.EnterBush(this);

                    // Solicitar cambio de capa a HiddenInBush vía RPC
                    RequestLayerChange(entityPhotonView, LayerManager.HiddenInBushLayerID);
                    LogMessage($"🔄 Entidad entrando ahora tiene capa: {LayerMask.LayerToName(entity.layer)} ({entity.layer})");
        }
        else
        {
                    // Ya está en la lista, verificamos si la capa es correcta (HiddenInBush)
                    if (entity.layer != LayerManager.HiddenInBushLayerID)
                    {
                        LogMessage($"⚠️ Entidad {entity.name} ya estaba en lista pero tiene capa incorrecta: {LayerMask.LayerToName(entity.layer)} ({entity.layer}). Debería ser {LayerMask.LayerToName(LayerManager.HiddenInBushLayerID)} ({LayerManager.HiddenInBushLayerID})");
                        
                        // Re-solicitar cambio de capa (puede haber sido restaurada incorrectamente)
                        RequestLayerChange(entityPhotonView, LayerManager.HiddenInBushLayerID);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Solicita un cambio de capa a través de un RPC
    /// </summary>
    private void RequestLayerChange(PhotonView targetPhotonView, int newLayerID)
    {
        if (targetPhotonView != null)
        {
            targetPhotonView.RPC("RPC_SetVisibilityLayer", RpcTarget.AllBuffered, newLayerID);
            LogMessage($"RPC solicitado: Cambiar capa de {targetPhotonView.name} (ID: {targetPhotonView.ViewID}) a {LayerMask.LayerToName(newLayerID)} ({newLayerID})");
        }
        else
        {
            LogMessage("⚠️ No se pudo solicitar cambio de capa: PhotonView es null");
        }
    }
    
    /// <summary>
    /// Verifica si una entidad es válida para cambio de visibilidad
    /// </summary>
    private bool IsValidEntity(Collider other)
    {
        if (other == null || other.gameObject == null) return false;

        // Necesita PhotonView y pertenecer a un equipo
        bool hasPhotonView = other.GetComponent<PhotonView>() != null;
        bool belongsToTeam = other.CompareTag(LayerManager.TAG_RED_TEAM) || other.CompareTag(LayerManager.TAG_BLUE_TEAM);
        // Podríamos añadir más condiciones si fuera necesario (ej. no ser un proyectil)
        return hasPhotonView && belongsToTeam;
    }
    
    /// <summary>
    /// Muestra mensajes de debug si están habilitados
    /// </summary>
    private void LogMessage(string message)
    {
        if (showDebugMessages)
        {
            Debug.Log($"[BushVisibility:{gameObject.name}] {message}");
        }
    }
    
    void OnDisable()
    {
        // Restaurar la visualización normal del arbusto al desactivar el script
        if (visualsInitialized && adjustLocalPlayerVisibility)
        {
            // Restaurar estado original de todos los renderers
            for (int i = 0; i < bushRenderers.Count; i++)
            {
                if (bushRenderers[i] != null && i < originalRendererStates.Count)
                {
                    bushRenderers[i].enabled = originalRendererStates[i];
                }
            }
            
            // Restaurar el LOD original
            if (bushLodGroup != null)
            {
                bushLodGroup.ForceLOD(-1); // Desactivar forzado de LOD
            }
        }
    }
    
    void OnDestroy()
    {
        // Asegurarse de restaurar la visualización normal del arbusto al destruir el script
        if (visualsInitialized && adjustLocalPlayerVisibility)
        {
            // Restaurar estado original de todos los renderers
            for (int i = 0; i < bushRenderers.Count; i++)
            {
                if (bushRenderers[i] != null && i < originalRendererStates.Count)
                {
                    bushRenderers[i].enabled = originalRendererStates[i];
                }
            }
            
            // Restaurar el LOD original
            if (bushLodGroup != null)
            {
                bushLodGroup.ForceLOD(-1); // Desactivar forzado de LOD
            }
        }
    }
}
