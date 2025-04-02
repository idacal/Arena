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
    
    [Header("Configuración Visual")]
    [Tooltip("Si es true, ajusta la transparencia del arbusto para el jugador local que está dentro")]
    public bool adjustLocalPlayerVisibility = true;
    
    [Tooltip("Nivel de transparencia (0=invisible, 1=completamente opaco)")]
    [Range(0.1f, 0.7f)]
    public float bushTransparencyLevel = 0.4f;
    
    [Tooltip("Porcentaje de renderers a desactivar cuando el jugador está dentro (0.5 = desactivar 50%)")]
    [Range(0.0f, 0.9f)]
    public float cullingPercentage = 0.6f;

    // Lista de unidades que están actualmente dentro de este arbusto
    private List<GameObject> entitiesInBush = new List<GameObject>();
    
    // Diccionario para recordar la capa original de cada unidad
    private Dictionary<GameObject, int> originalLayers = new Dictionary<GameObject, int>();
    
    // Diccionario para almacenar las corrutinas de salida
    private Dictionary<GameObject, Coroutine> exitCoroutines = new Dictionary<GameObject, Coroutine>();
    
    // Para controlar la transparencia del arbusto
    private List<Renderer> bushRenderers = new List<Renderer>();
    private List<bool> originalRendererStates = new List<bool>();
    private List<Material> originalMaterials = new List<Material>();
    private List<Material> transparentMaterials = new List<Material>();
    private int localPlayersInside = 0;
    private bool visualsInitialized = false;
    private LODGroup bushLodGroup;
    
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
        
        // Inicializar componentes de renderizado si queremos ajustar la transparencia
        if (adjustLocalPlayerVisibility)
        {
            InitializeVisualComponents();
        }
        
        LogMessage($"Arbusto {gameObject.name} inicializado correctamente.");
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
                
                // Guardar materiales originales
                Material[] originalMats = renderer.materials;
                foreach (Material mat in originalMats)
                {
                    originalMaterials.Add(new Material(mat));
                    
                    // Crear versión transparente
                    Material transparentMat = new Material(mat);
                    Color color = transparentMat.color;
                    color.a = bushTransparencyLevel;
                    transparentMat.color = color;
                    
                    // Configurar para transparencia
                    transparentMat.SetFloat("_Mode", 3); // Transparent
                    transparentMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    transparentMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    transparentMat.SetInt("_ZWrite", 0);
                    transparentMat.DisableKeyword("_ALPHATEST_ON");
                    transparentMat.EnableKeyword("_ALPHABLEND_ON");
                    transparentMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    transparentMat.renderQueue = 3000;
                    
                    transparentMaterials.Add(transparentMat);
                }
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
                    else
                    {
                        // Para los renderers que mantengamos activos, aplicamos transparencia
                        bushRenderers[i].enabled = true;
                        
                        // Aplicar materiales transparentes
                        Material[] currentMaterials = bushRenderers[i].materials;
                        for (int j = 0; j < currentMaterials.Length; j++)
                        {
                            // Buscar el índice correspondiente en la lista de materiales transparentes
                            int materialIndex = i * currentMaterials.Length + j;
                            if (materialIndex < transparentMaterials.Count)
                            {
                                currentMaterials[j] = transparentMaterials[materialIndex];
                            }
                        }
                        bushRenderers[i].materials = currentMaterials;
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
                    
                    // Restaurar materiales originales
                    Material[] currentMaterials = bushRenderers[i].materials;
                    for (int j = 0; j < currentMaterials.Length; j++)
                    {
                        // Buscar el índice correspondiente en la lista de materiales originales
                        int materialIndex = i * currentMaterials.Length + j;
                        if (materialIndex < originalMaterials.Count)
                        {
                            currentMaterials[j] = originalMaterials[materialIndex];
                        }
                    }
                    bushRenderers[i].materials = currentMaterials;
                }
            }
            
            // Restaurar LOD original
            if (bushLodGroup != null)
            {
                bushLodGroup.ForceLOD(-1); // -1 para desactivar el forzado
            }
        }
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
                
                // Si es un jugador local, hacer el arbusto transparente
                PhotonView entityPV = entity.GetComponent<PhotonView>();
                if (entityPV != null && entityPV.IsMine && adjustLocalPlayerVisibility)
                {
                    SetLocalPlayerInside(true);
                }
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
                
                // Si es un jugador local, restaurar el arbusto a su estado normal
                PhotonView entityPV = entity.GetComponent<PhotonView>();
                if (entityPV != null && entityPV.IsMine && adjustLocalPlayerVisibility)
                {
                    SetLocalPlayerInside(false);
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
                    
                    // Restaurar materiales originales
                    Material[] currentMaterials = bushRenderers[i].materials;
                    for (int j = 0; j < currentMaterials.Length; j++)
                    {
                        // Buscar el índice correspondiente en la lista de materiales originales
                        int materialIndex = i * currentMaterials.Length + j;
                        if (materialIndex < originalMaterials.Count)
                        {
                            currentMaterials[j] = originalMaterials[materialIndex];
                        }
                    }
                    bushRenderers[i].materials = currentMaterials;
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
                    
                    // Restaurar materiales originales
                    Material[] currentMaterials = bushRenderers[i].materials;
                    for (int j = 0; j < currentMaterials.Length; j++)
                    {
                        // Buscar el índice correspondiente en la lista de materiales originales
                        int materialIndex = i * currentMaterials.Length + j;
                        if (materialIndex < originalMaterials.Count)
                        {
                            currentMaterials[j] = originalMaterials[materialIndex];
                        }
                    }
                    bushRenderers[i].materials = currentMaterials;
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
