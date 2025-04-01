using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
// using System.Collections; // Ya no se necesita

/// <summary>
/// Script para los arbustos que detectan unidades dentro y notifican para ocultarlas/revelarlas.
/// El cambio de capa real se hace vía RPC en la entidad misma.
/// </summary>
public class BushVisibility : MonoBehaviourPunCallbacks // Sigue siendo PunCallbacks por si se necesita lógica de red aquí en el futuro
{
    [Header("Configuración")]
    [Tooltip("Habilitar mensajes de debug en consola")]
    public bool showDebugMessages = false;

    // Lista de GameObjects (identificados por su PhotonView ID) que están físicamente en el trigger
    private HashSet<int> entitiesInTrigger = new HashSet<int>();
    
    // Diccionario para recordar la capa original de cada unidad (PhotonView ID -> Layer ID)
    private Dictionary<int, int> originalLayers = new Dictionary<int, int>();

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
        
        LogMessage($"Arbusto {gameObject.name} inicializado.");
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
        // Llamar a método especial de salida (más directo) 
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
                
                // Quitar de la lista
                entitiesInTrigger.Remove(viewID);
                
                // Notificar al tracker localmente
                entityTracker.ExitBush(this);
                
                // Determinar la capa original
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
                
                // Llamar inmediatamente al RPC para restaurar la capa
                entityPhotonView.RPC("RPC_SetVisibilityLayer", RpcTarget.AllBuffered, originalLayer);
                
                // Para casos críticos, también intentar forzar la capa
                entityPhotonView.RPC("RPC_ForceLayer", RpcTarget.AllBuffered, originalLayer, false);
                
                LogMessage($"📤 FORZANDO restauración a capa {LayerMask.LayerToName(originalLayer)} ({originalLayer})");
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
    /// Aplica un pequeño retraso antes de solicitar el cambio de capa para evitar condiciones de carrera con OnTriggerStay.
    /// </summary>
    private System.Collections.IEnumerator DelayedLayerChange(PhotonView targetView, int layerId, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Verificación adicional: Asegurarse que la entidad ya no está en el trigger
        if (targetView != null && !entitiesInTrigger.Contains(targetView.ViewID))
        {
            LogMessage($"🕒 Ejecutando cambio de capa retrasado para {targetView.gameObject.name} a {LayerMask.LayerToName(layerId)} ({layerId})");
            RequestLayerChange(targetView, layerId);
        }
        else if (targetView != null)
        {
            LogMessage($"⚠️ Cancelando cambio de capa retrasado porque {targetView.gameObject.name} volvió a entrar al trigger");
        }
        else
        {
            LogMessage($"⚠️ Cancelando cambio de capa retrasado porque el objetivo es nulo");
        }
    }

    /// <summary>
    /// Solicita a la entidad que cambie su capa a través de un RPC.
    /// </summary>
    private void RequestLayerChange(PhotonView targetView, int layerId)
    {
        if (targetView != null)
        {
             LogMessage($"📤 Solicitando RPC_SetVisibilityLayer en {targetView.gameObject.name} (ID: {targetView.ViewID}) para capa {LayerMask.LayerToName(layerId)} ({layerId})");
             targetView.RPC("RPC_SetVisibilityLayer", RpcTarget.AllBuffered, layerId);
        }
         else
        {
            LogMessage("❌ Error: Se intentó solicitar cambio de capa en un PhotonView nulo.");
        }
    }

    /// <summary>
    /// Helper para verificar si un Collider pertenece a una entidad válida (con PhotonView y equipo)
    /// </summary>
    private bool IsValidEntity(Collider other)
    {
        // Necesita PhotonView y pertenecer a un equipo
        bool hasPhotonView = other.GetComponent<PhotonView>() != null;
        bool belongsToTeam = other.CompareTag(LayerManager.TAG_RED_TEAM) || other.CompareTag(LayerManager.TAG_BLUE_TEAM);
        // Podríamos añadir más condiciones si fuera necesario (ej. no ser un proyectil)
        return hasPhotonView && belongsToTeam;
    }
    
    // Ya no se usan HideEntity ni RevealEntity directamente aquí
    // private void HideEntity(GameObject entity) { ... }
    // private void RevealEntity(GameObject entity) { ... }
    
    // --- Métodos antiguos que ya no aplican con la nueva lógica --- 
    // private void NotifyEntityEnter(GameObject entity) { ... } 
    // private void NotifyEntityExit(GameObject entity) { ... }

    /// <summary>
    /// Comprueba si una entidad específica (por su PhotonView ID) está actualmente en el trigger de este arbusto.
    /// </summary>
    public bool IsEntityInside(int viewID)
    {
        return entitiesInTrigger.Contains(viewID);
    }
    
    /// <summary>
    /// Método de utilidad para logging condicional
    /// </summary>
    private void LogMessage(string message)
    {
        if (showDebugMessages)
        {
            Debug.Log($"[BushVisibility: {gameObject.name}] {message}", this);
        }
    }
}
