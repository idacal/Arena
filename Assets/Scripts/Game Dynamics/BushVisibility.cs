using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;

/// <summary>
/// Script para los arbustos que ocultan unidades dentro. 
/// Integrado con el sistema de equipos de Arena.
/// </summary>
public class BushVisibility : MonoBehaviourPunCallbacks
{
    [Header("Configuración")]
    [Tooltip("Habilitar mensajes de debug en consola")]
    public bool showDebugMessages = false;

    // Lista de unidades que están actualmente dentro de este arbusto
    private List<GameObject> entitiesInBush = new List<GameObject>();
    
    // Diccionario para recordar la capa original de cada unidad
    private Dictionary<GameObject, int> originalLayers = new Dictionary<GameObject, int>();
    
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
            Debug.LogError($"¡El arbusto {gameObject.name} no tiene un Collider! Añade un Box Collider o similar.", this);
            this.enabled = false;
            return;
        }
        
        if (!bushCollider.isTrigger)
        {
            Debug.LogError($"¡El Collider del arbusto {gameObject.name} no está configurado como Trigger! Marca 'Is Trigger' en el inspector.", this);
            this.enabled = false;
            return;
        }
        
        // Verificar que la capa HiddenInBush existe
        if (LayerManager.HiddenInBushLayerID == -1)
        {
            Debug.LogError("La capa 'HiddenInBush' no existe. Añádela en Edit > Project Settings > Tags & Layers", this);
            this.enabled = false;
            return;
        }
        
        LogMessage($"Arbusto {gameObject.name} inicializado correctamente.");
    }
    
    /// <summary>
    /// Cuando una unidad entra en el arbusto
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        // Verificar si el objeto es una unidad válida (pertenece a un equipo)
        if (IsValidEntity(other))
        {
            GameObject entity = other.gameObject;
            
            // Añadir a la lista y ocultar SOLO si no está ya
            if (!entitiesInBush.Contains(entity))
            {
                entitiesInBush.Add(entity);
                
                // Notificar al EntityVisibilityTracker de la unidad
                NotifyEntityEnter(entity);
                
                // Ocultar la unidad cambiando su capa
                HideEntity(entity);
                
                LogMessage($"OnTriggerEnter: Unidad {entity.name} (Equipo: {other.tag}) entró al arbusto.");
            }
             else
            {
                LogMessage($"OnTriggerEnter: Unidad {entity.name} ya estaba en la lista.");
            }
        }
    }

    /// <summary>
    /// Llamado cada frame de física mientras una unidad está DENTRO del trigger
    /// </summary>
    void OnTriggerStay(Collider other)
    {
        // Verificar si es una unidad válida
        if (IsValidEntity(other))
        {
            GameObject entity = other.gameObject;

            // Si por alguna razón la entidad está en el trigger pero no en nuestra lista
            // (podría pasar en casos raros o al inicio), la añadimos y ocultamos.
            if (!entitiesInBush.Contains(entity))
            {
                 LogMessage($"OnTriggerStay: Unidad {entity.name} detectada en Stay pero no en lista. Añadiendo y ocultando.");
                 entitiesInBush.Add(entity);
                 NotifyEntityEnter(entity);
                 HideEntity(entity);
            }
            // Si está en la lista, nos aseguramos de que esté oculta (redundancia segura)
            // Esto podría evitar casos donde algo externo la revela mientras sigue dentro.
            else if (entity.layer != LayerManager.HiddenInBushLayerID && originalLayers.ContainsKey(entity)) 
            {
                 LogMessage($"OnTriggerStay: Unidad {entity.name} está en lista pero no oculta. Re-ocultando.");
                 HideEntity(entity); // Asegura que permanezca oculta
            }
        }
    }

    /// <summary>
    /// Cuando una unidad sale del arbusto
    /// </summary>
    void OnTriggerExit(Collider other)
    {
        // Verificar si el objeto es una unidad válida
        if (IsValidEntity(other))
        {
            GameObject entity = other.gameObject;
            
            // Verificar que estaba en la lista antes de intentar quitarla
            if (entitiesInBush.Contains(entity))
            {
                // Eliminar de la lista
                entitiesInBush.Remove(entity);

                // Notificar al EntityVisibilityTracker de la unidad
                NotifyEntityExit(entity);
                
                // Revelar la unidad restaurando su capa original
                RevealEntity(entity); // Revelar inmediatamente al salir
                
                // Limpiar el registro de su capa original
                originalLayers.Remove(entity); 
                
                LogMessage($"OnTriggerExit: Unidad {entity.name} (Equipo: {other.tag}) salió del arbusto.");
            }
             else
            {
                LogMessage($"OnTriggerExit: Unidad {entity.name} salió pero no estaba en la lista.");
            }
        }
    }

    /// <summary>
    /// Helper para verificar si un Collider pertenece a una entidad válida
    /// </summary>
    private bool IsValidEntity(Collider other)
    {
        // Comprobamos los tags de equipo. Añade más tags si tienes otros tipos de unidades.
        return other.CompareTag(LayerManager.TAG_RED_TEAM) || other.CompareTag(LayerManager.TAG_BLUE_TEAM);
        // Podrías añadir aquí una comprobación de si tiene un componente específico,
        // como un 'HealthComponent' o 'PlayerController', para ser más robusto.
        // Ejemplo: return other.GetComponent<PlayerController>() != null; 
    }
    
    /// <summary>
    /// Oculta una unidad cambiando su capa a HiddenInBush
    /// </summary>
    private void HideEntity(GameObject entity)
    {
        // Guardar la capa original si no la tenemos ya
        if (!originalLayers.ContainsKey(entity))
        {
            originalLayers.Add(entity, entity.layer);
            LogMessage($"Guardada capa original {LayerMask.LayerToName(entity.layer)} para {entity.name}");
        }
        
        // Usar las funciones de utilidad de LayerManager para ocultar la unidad
        LayerManager.HideUnitInBush(entity);
        LogMessage($"Ocultando {entity.name} en capa {LayerMask.LayerToName(LayerManager.HiddenInBushLayerID)}");
    }
    
    /// <summary>
    /// Revela una unidad restaurando su capa original
    /// </summary>
    private void RevealEntity(GameObject entity)
    {
        // Recuperar la capa original
        int originalLayer = LayerManager.PlayerLayerID; // Valor por defecto si no se encuentra
        
        if (originalLayers.TryGetValue(entity, out int storedLayer))
        {
            originalLayer = storedLayer;
            LogMessage($"Recuperada capa original {LayerMask.LayerToName(storedLayer)} para {entity.name}");
        }
         else
        {
             LogMessage($"No se encontró capa original para {entity.name}. Usando capa por defecto {LayerMask.LayerToName(originalLayer)}.");
        }

        // Usar las funciones de utilidad de LayerManager para revelar la unidad
        LayerManager.RevealUnitFromBush(entity, originalLayer);
         LogMessage($"Revelando {entity.name} a capa {LayerMask.LayerToName(originalLayer)}");
    }
    
    /// <summary>
    /// Notifica al EntityVisibilityTracker que una unidad ha entrado al arbusto
    /// </summary>
    private void NotifyEntityEnter(GameObject entity)
    {
        // Intentar obtener el componente EntityVisibilityTracker
        EntityVisibilityTracker tracker = entity.GetComponent<EntityVisibilityTracker>();
        
        // Si tiene el componente, notificarle
        if (tracker != null)
        {
            tracker.EnterBush(this);
        }
        else
        {
            // Si no tiene el componente, añadírselo
            tracker = entity.AddComponent<EntityVisibilityTracker>();
            tracker.EnterBush(this);
            LogMessage($"Añadido EntityVisibilityTracker a {entity.name} porque no lo tenía.");
        }
    }
    
    /// <summary>
    /// Notifica al EntityVisibilityTracker que una unidad ha salido del arbusto
    /// </summary>
    private void NotifyEntityExit(GameObject entity)
    {
        // Intentar obtener el componente EntityVisibilityTracker
        EntityVisibilityTracker tracker = entity.GetComponent<EntityVisibilityTracker>();
        
        // Si tiene el componente, notificarle
        if (tracker != null)
        {
            tracker.ExitBush(this);
        }
    }
    
    /// <summary>
    /// Comprueba si una entidad específica está dentro de este arbusto
    /// </summary>
    public bool IsEntityInside(GameObject entity)
    {
        return entitiesInBush.Contains(entity);
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
