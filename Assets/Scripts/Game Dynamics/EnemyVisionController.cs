using UnityEngine;

/// <summary>
/// Controlador para determinar si un enemigo puede ver o no a un objetivo basado en reglas de visibilidad,
/// como si el objetivo está dentro de un arbusto.
/// </summary>
public class EnemyVisionController : MonoBehaviour
{
    private EntityVisibilityTracker myVisibilityTracker; // Referencia al tracker de este enemigo
    private int hiddenLayer; // Número de la capa "HiddenInBush"
    
    [Tooltip("Radio de visión del enemigo")]
    public float visionRadius = 10f;
    
    [Tooltip("Ángulo de visión del enemigo (en grados)")]
    public float visionAngle = 120f;
    
    [Tooltip("Layers a detectar como objetivos")]
    public LayerMask targetLayers;
    
    [Tooltip("Layers que bloquean la visión (como paredes)")]
    public LayerMask obstacleLayers;

    void Start()
    {
        // Obtener referencia al tracker de visibilidad en este mismo GameObject
        myVisibilityTracker = GetComponent<EntityVisibilityTracker>();
        if (myVisibilityTracker == null)
        {
            Debug.LogError("¡El enemigo no tiene el componente EntityVisibilityTracker! Añadiéndolo...", this.gameObject);
            myVisibilityTracker = gameObject.AddComponent<EntityVisibilityTracker>();
        }

        // Obtener número de la capa oculta
        hiddenLayer = LayerMask.NameToLayer("HiddenInBush");
        if (hiddenLayer == -1)
        {
            Debug.LogError("¡La capa 'HiddenInBush' no está definida en Project Settings! La función de ocultación no funcionará correctamente.", this.gameObject);
        }
    }

    /// <summary>
    /// Determina si el enemigo puede ver al objetivo proporcionado,
    /// teniendo en cuenta la capa "HiddenInBush" y las reglas de visibilidad de arbustos.
    /// </summary>
    /// <param name="target">GameObject objetivo a comprobar</param>
    /// <returns>true si puede ver al objetivo, false en caso contrario</returns>
    public bool CanSeeTarget(GameObject target)
    {
        if (target == null) 
        {
            return false; // No hay objetivo que ver
        }

        // 1. Verificar distancia primero (optimización)
        float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
        if (distanceToTarget > visionRadius)
        {
            return false; // Fuera de rango
        }

        // 2. Verificar si el objetivo está en la capa oculta (HiddenInBush)
        if (target.layer == hiddenLayer)
        {
            // El objetivo está escondido en un arbusto
            // ¿Estoy yo (enemigo) también en un arbusto?
            if (!myVisibilityTracker.IsInBush)
            {
                // No estoy en un arbusto, NO PUEDO ver al objetivo escondido
                return false;
            }
            else
            {
                // Sí estoy en un arbusto. ¿Estamos en el MISMO arbusto?
                EntityVisibilityTracker targetTracker = target.GetComponent<EntityVisibilityTracker>();
                
                if (targetTracker != null && 
                    targetTracker.CurrentBush == myVisibilityTracker.CurrentBush && 
                    myVisibilityTracker.CurrentBush != null)
                {
                    // Estamos en el mismo arbusto, ahora se aplican las comprobaciones normales de visión
                    // (continúa a la verificación de ángulo y línea de visión)
                }
                else
                {
                    // Estamos en arbustos diferentes, NO PUEDO ver al objetivo
                    return false;
                }
            }
        }

        // 3. Verificar ángulo de visión
        Vector3 directionToTarget = (target.transform.position - transform.position).normalized;
        float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
        
        if (angleToTarget > visionAngle / 2f)
        {
            return false; // Fuera del cono de visión
        }

        // 4. Verificar si hay obstáculos que bloquean la línea de visión
        if (Physics.Raycast(transform.position, directionToTarget, distanceToTarget, obstacleLayers))
        {
            return false; // Hay un obstáculo bloqueando la visión
        }

        // Si llegamos aquí, el objetivo es visible
        return true;
    }

    /// <summary>
    /// Encuentra el objetivo más cercano visible dentro del radio y ángulo de visión.
    /// </summary>
    /// <returns>El GameObject del objetivo o null si no hay ninguno visible</returns>
    public GameObject FindVisibleTarget()
    {
        // Buscar todos los colliders dentro del radio de visión que estén en las capas objetivo
        Collider[] targetsInRadius = Physics.OverlapSphere(transform.position, visionRadius, targetLayers);
        
        GameObject closestVisibleTarget = null;
        float closestDistance = float.MaxValue;

        foreach (Collider targetCollider in targetsInRadius)
        {
            GameObject targetObject = targetCollider.gameObject;
            
            // Verificar si podemos ver este objeto según reglas de arbustos y visión
            if (CanSeeTarget(targetObject))
            {
                float distance = Vector3.Distance(transform.position, targetObject.transform.position);
                
                // Si es más cercano que el actual más cercano, actualizamos
                if (distance < closestDistance)
                {
                    closestVisibleTarget = targetObject;
                    closestDistance = distance;
                }
            }
        }

        return closestVisibleTarget;
    }

    // Método para dibujar gizmos en el editor para visualizar el cono de visión
    private void OnDrawGizmosSelected()
    {
        // Dibujar radio de visión
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRadius);

        // Dibujar cono de visión
        float halfAngle = visionAngle / 2f;
        Quaternion leftRayRotation = Quaternion.AngleAxis(-halfAngle, Vector3.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(halfAngle, Vector3.up);
        Vector3 leftRayDirection = leftRayRotation * transform.forward;
        Vector3 rightRayDirection = rightRayRotation * transform.forward;
        
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, leftRayDirection * visionRadius);
        Gizmos.DrawRay(transform.position, rightRayDirection * visionRadius);
    }
} 