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
    
    // Diccionario para almacenar las corrutinas de salida
    private Dictionary<GameObject, Coroutine> exitCoroutines = new Dictionary<GameObject, Coroutine>();
    
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
        if (other.CompareTag(LayerManager.TAG_RED_TEAM) || other.CompareTag(LayerManager.TAG_BLUE_TEAM))
        {
            GameObject entity = other.gameObject;
            
            // Añadir a la lista si no está ya
            if (!entitiesInBush.Contains(entity))
            {
                entitiesInBush.Add(entity);
                
                // Notificar al EntityVisibilityTracker de la unidad
                NotifyEntityEnter(entity);
                
                // Ocultar la unidad cambiando su capa
                HideEntity(entity);
                
                LogMessage($"Unidad {entity.name} (Equipo: {other.tag}) entró al arbusto.");
            }
        }
    }
    
    /// <summary>
    /// Cuando una unidad sale del arbusto
    /// </summary>
    void OnTriggerExit(Collider other)
    {
        // Verificar si el objeto es una unidad válida (pertenece a un equipo)
        if (other.CompareTag(LayerManager.TAG_RED_TEAM) || other.CompareTag(LayerManager.TAG_BLUE_TEAM))
        {
            GameObject entity = other.gameObject;
            
            // Verificar que estaba en la lista
            if (entitiesInBush.Contains(entity))
            {
                // Iniciar una corrutina con tiempo de gracia antes de procesar la salida
                if (exitCoroutines.ContainsKey(entity))
                {
                    StopCoroutine(exitCoroutines[entity]);
                }
                
                exitCoroutines[entity] = StartCoroutine(DelayedExit(entity, other));
            }
        }
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
        }
        
        // Usar las funciones de utilidad de LayerManager para ocultar la unidad
        LayerManager.HideUnitInBush(entity);
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
        }
        
        // Usar las funciones de utilidad de LayerManager para revelar la unidad
        LayerManager.RevealUnitFromBush(entity, originalLayer);
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
            Debug.Log($"[BushVisibility] {message}", this);
        }
    }
    
    private IEnumerator DelayedExit(GameObject entity, Collider other)
    {
        // Esperar un pequeño tiempo de gracia (0.2 segundos)
        yield return new WaitForSeconds(0.2f);
        
        // Verificar si la entidad aún está en nuestra lista (podría haber vuelto a entrar)
        if (entitiesInBush.Contains(entity))
        {
            // Notificar al EntityVisibilityTracker de la unidad
            NotifyEntityExit(entity);
            
            // Revelar la unidad restaurando su capa original
            RevealEntity(entity);
            
            // Eliminar de la lista
            entitiesInBush.Remove(entity);
            originalLayers.Remove(entity);
            
            LogMessage($"Unidad {entity.name} (Equipo: {other.tag}) salió del arbusto.");
        }
        
        // Limpiar la referencia a la corrutina
        if (exitCoroutines.ContainsKey(entity))
        {
            exitCoroutines.Remove(entity);
        }
    }
}
