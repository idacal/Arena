using UnityEngine;
using Photon.Pun;

/// <summary>
/// Componente que se añade a cualquier objeto que deba revelar la niebla de guerra.
/// </summary>
[ExecuteInEditMode]
public class FogRevealer : MonoBehaviour
{
    [Tooltip("Radio de visión para este objeto")]
    [SerializeField] private float visionRadius = 5f;
    
    [Tooltip("Equipo al que pertenece (RedTeam o BlueTeam)")]
    [SerializeField] private string teamTag = "RedTeam";
    
    [Tooltip("Forma del revealer (0=Circular, 1=Cono)")]
    [SerializeField] private RevealerShape shape = RevealerShape.Circle;
    
    [Tooltip("Ángulo de visión (solo para forma de cono)")]
    [SerializeField] private float coneAngle = 60f;
    
    [Tooltip("Resolución del objeto de visión")]
    [SerializeField] private int resolution = 36;
    
    [Tooltip("¿La visión es bloqueada por obstáculos?")]
    [SerializeField] private bool blockedByObstacles = true;
    
    [Tooltip("Capas que bloquean la visión")]
    [SerializeField] private LayerMask obstacleMask = 0;
    
    // Componentes privados
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh visionMesh;
    private Material material;
    private FogOfWarSystem fogSystem;
    private PhotonView photonView;
    
    // Propiedades públicas
    public string TeamTag { get { return teamTag; } }
    public float VisionRadius { get { return visionRadius; } set { visionRadius = value; UpdateVisionMesh(); } }
    
    // Enumeración para forma del revealer
    public enum RevealerShape
    {
        Circle,
        Cone
    }
    
    private void OnEnable()
    {
        // Encontrar el sistema de niebla
        if (fogSystem == null)
        {
            fogSystem = FindObjectOfType<FogOfWarSystem>();
            if (fogSystem != null)
            {
                fogSystem.RegisterRevealer(this);
            }
        }
        
        // Componentes necesarios
        if (meshFilter == null)
        {
            meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                meshFilter = gameObject.AddComponent<MeshFilter>();
            }
        }
        
        if (meshRenderer == null)
        {
            meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                meshRenderer = gameObject.AddComponent<MeshRenderer>();
            }
        }
        
        // Comprobar si tiene PhotonView
        if (photonView == null)
        {
            photonView = GetComponent<PhotonView>();
        }
        
        // Solo revelar la niebla si es propio o si no se usa Photon
        if (PhotonNetwork.IsConnected && photonView != null)
        {
            // Solo mostrar para el dueño de este objeto o si es un objeto de escena
            meshRenderer.enabled = photonView.IsMine || !photonView.IsOwnerActive;
        }
        else
        {
            meshRenderer.enabled = true;
        }
        
        // Crear malla inicial
        if (visionMesh == null)
        {
            visionMesh = new Mesh();
            visionMesh.name = "FogRevealer_" + gameObject.name;
            meshFilter.mesh = visionMesh;
        }
        
        // Actualizar la malla
        UpdateVisionMesh();
    }
    
    private void OnDisable()
    {
        // Desregistrar del sistema de niebla
        if (fogSystem != null)
        {
            fogSystem.UnregisterRevealer(this);
        }
    }
    
    private void OnDestroy()
    {
        // Limpiar
        if (visionMesh != null)
        {
            DestroyImmediate(visionMesh);
            visionMesh = null;
        }
    }
    
    // Actualizar la visión cuando el objeto se mueve
    private void Update()
    {
        // Actualizar la visión en cada frame para reflejar los cambios en los obstáculos cercanos
        UpdateVisionMesh();
        
        // También podemos optimizar haciendo la actualización solo cuando:
        // 1. El objeto se ha movido significativamente
        // 2. Ha pasado cierto tiempo desde la última actualización
        
        // Ejemplo de optimización (descomentar si se necesita mejor rendimiento):
        /*
        // Variable estática para llevar seguimiento del tiempo
        static float lastUpdateTime;
        
        // Actualizar solo cada X segundos (0.1 = 10 veces por segundo)
        if (Time.time - lastUpdateTime > 0.1f)
        {
            UpdateVisionMesh();
            lastUpdateTime = Time.time;
        }
        */
    }
    
    // Actualiza la malla de visión
    public void UpdateVisionMesh()
    {
        if (visionMesh == null) return;
        
        // Limpiar la malla
        visionMesh.Clear();
        
        // Crear según la forma
        if (shape == RevealerShape.Circle)
        {
            CreateCircleVision();
        }
        else
        {
            CreateConeVision();
        }
    }
    
    // Establece el material para este revealer
    public void SetRevealerMaterial(Material revealerMaterial)
    {
        if (meshRenderer != null && revealerMaterial != null)
        {
            material = new Material(revealerMaterial);
            meshRenderer.material = material;
            
            // Hacer invisible pero que siga renderizando a la textura
            material.color = new Color(1, 1, 1, 0);
        }
    }
    
    // Crear visión circular
    private void CreateCircleVision()
    {
        Vector3[] vertices = new Vector3[resolution + 2];
        int[] triangles = new int[resolution * 3];
        
        // Vértice central
        vertices[0] = Vector3.zero;
        
        // Crear vértices alrededor del círculo
        float angleStep = 360f / resolution;
        for (int i = 0; i < resolution; i++)
        {
            float angle = i * angleStep;
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            
            float distance = visionRadius;
            
            // Comprobar si hay obstáculos
            if (blockedByObstacles && obstacleMask != 0)
            {
                // Origen elevado para evitar colisiones con el suelo
                Vector3 origin = transform.position + Vector3.up * 0.5f;
                
                // Lanzar múltiples rayos a diferentes alturas para mayor precisión
                float[] heightOffsets = new float[] { 0.1f, 0.5f, 1.0f, 1.5f };
                bool hitObstacle = false;
                
                foreach (float heightOffset in heightOffsets)
                {
                    Vector3 rayOrigin = transform.position + Vector3.up * heightOffset;
                    RaycastHit hit;
                    
                    // Convertir la dirección local a global usando la rotación del objeto
                    Vector3 worldDirection = transform.TransformDirection(direction);
                    
                    // Debug ray (solo visible en la vista de escena)
                    if (Application.isEditor && i % 4 == 0)
                    {
                        Debug.DrawRay(rayOrigin, worldDirection * visionRadius, Color.yellow, 0.01f);
                    }
                    
                    if (Physics.Raycast(rayOrigin, worldDirection, out hit, visionRadius, obstacleMask))
                    {
                        // Asegurarse de que el obstáculo no es del mismo equipo
                        if (!hit.collider.CompareTag(teamTag))
                        {
                            // Añadir un pequeño margen para suavizar transiciones
                            float newDistance = hit.distance * 0.95f;
                            distance = Mathf.Min(distance, newDistance);
                            hitObstacle = true;
                            
                            if (Application.isEditor && i % 4 == 0)
                            {
                                Debug.DrawRay(rayOrigin, worldDirection * distance, Color.red, 0.01f);
                            }
                        }
                    }
                }
            }
            
            // Agregar vértice (Y=0 para mantenerlo plano)
            Vector3 vertex = direction * distance;
            vertex.y = 0;
            vertices[i + 1] = vertex;
            
            // Crear triángulos
            if (i < resolution - 1)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
            else
            {
                // Conectar el último vértice con el primero
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = 1;
            }
        }
        
        // Agregar un vértice más para cerrar el círculo
        vertices[resolution + 1] = vertices[1];
        
        // Asignar a la malla
        visionMesh.vertices = vertices;
        visionMesh.triangles = triangles;
        visionMesh.RecalculateNormals();
        visionMesh.RecalculateBounds();
    }
    
    // Crear visión de cono
    private void CreateConeVision()
    {
        int steps = Mathf.Max(3, resolution / 4); // Menos resolución para el cono
        Vector3[] vertices = new Vector3[steps + 2];
        int[] triangles = new int[steps * 3];
        
        // Vértice central
        vertices[0] = Vector3.zero;
        
        // Dirección hacia adelante basada en la rotación del objeto
        Vector3 forward = transform.forward;
        
        // Crear vértices alrededor del cono
        float halfAngle = coneAngle / 2f;
        float angleStep = coneAngle / steps;
        
        for (int i = 0; i <= steps; i++)
        {
            float angle = -halfAngle + i * angleStep;
            Vector3 direction = Quaternion.Euler(0, angle, 0) * forward;
            
            float distance = visionRadius;
            
            // Comprobar si hay obstáculos
            if (blockedByObstacles && obstacleMask != 0)
            {
                // Lanzar múltiples rayos a diferentes alturas para mayor precisión
                float[] heightOffsets = new float[] { 0.1f, 0.5f, 1.0f, 1.5f };
                bool hitObstacle = false;
                
                foreach (float heightOffset in heightOffsets)
                {
                    Vector3 rayOrigin = transform.position + Vector3.up * heightOffset;
                    RaycastHit hit;
                    
                    // Convertir la dirección local a global
                    Vector3 worldDirection = transform.TransformDirection(direction);
                    
                    // Debug ray (solo visible en la vista de escena)
                    if (Application.isEditor && i % 2 == 0)
                    {
                        Debug.DrawRay(rayOrigin, worldDirection * visionRadius, Color.yellow, 0.01f);
                    }
                    
                    if (Physics.Raycast(rayOrigin, worldDirection, out hit, visionRadius, obstacleMask))
                    {
                        // Asegurarse de que el obstáculo no es del mismo equipo
                        if (!hit.collider.CompareTag(teamTag))
                        {
                            // Añadir un pequeño margen para suavizar transiciones
                            float newDistance = hit.distance * 0.95f;
                            distance = Mathf.Min(distance, newDistance);
                            hitObstacle = true;
                            
                            if (Application.isEditor && i % 2 == 0)
                            {
                                Debug.DrawRay(rayOrigin, worldDirection * distance, Color.red, 0.01f);
                            }
                        }
                    }
                }
            }
            
            // Agregar vértice (Y=0 para mantenerlo plano)
            Vector3 vertex = direction * distance;
            vertex.y = 0;
            vertices[i + 1] = vertex;
            
            // Crear triángulos
            if (i < steps)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }
        
        // Asignar a la malla
        visionMesh.vertices = vertices;
        visionMesh.triangles = triangles;
        visionMesh.RecalculateNormals();
        visionMesh.RecalculateBounds();
    }
    
    // Dibujar gizmos para visualizar en el editor
    private void OnDrawGizmosSelected()
    {
        if (shape == RevealerShape.Circle)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, visionRadius);
        }
        else
        {
            Gizmos.color = Color.green;
            
            // Dibujar el cono de visión
            Vector3 forward = transform.forward;
            float halfAngle = coneAngle / 2f;
            Vector3 leftDir = Quaternion.Euler(0, -halfAngle, 0) * forward;
            Vector3 rightDir = Quaternion.Euler(0, halfAngle, 0) * forward;
            
            Gizmos.DrawLine(transform.position, transform.position + leftDir * visionRadius);
            Gizmos.DrawLine(transform.position, transform.position + rightDir * visionRadius);
            
            // Dibujar el arco
            int steps = 20;
            float angleStep = coneAngle / steps;
            Vector3 previousPos = transform.position + leftDir * visionRadius;
            
            for (int i = 1; i <= steps; i++)
            {
                float angle = -halfAngle + i * angleStep;
                Vector3 direction = Quaternion.Euler(0, angle, 0) * forward;
                Vector3 currentPos = transform.position + direction * visionRadius;
                
                Gizmos.DrawLine(previousPos, currentPos);
                previousPos = currentPos;
            }
        }
    }
} 