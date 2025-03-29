using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class FogOfWarController : MonoBehaviourPunCallbacks
{
    [Header("Fog of War Settings")]
    [SerializeField] private Material fogMaterial;
    [SerializeField] private string teamTag = "RedTeam"; // "RedTeam" o "BlueTeam" - Establecer directamente
    [SerializeField] private Vector2 mapSize = new Vector2(5f, 5f);
    [SerializeField] private float fogHeight = 10f;
    [SerializeField] private bool followPlayer = true;
    [SerializeField] private Transform playerToFollow;
    [SerializeField] private Color fogColor = new Color(0, 0, 0, 1);
    [SerializeField] private float fogSoftness = 0.5f;
    [SerializeField] private float fogBlend = 0.5f;
    
    [Header("Referencia a Objetos")]
    [SerializeField] private GameObject fogPlane; // Asignar desde el inspector
    [SerializeField] private LayerMask playerLayerMask; // Asignar la capa "player" desde el inspector
    
    [Header("Visibilidad")]
    [SerializeField] private float visionRadius = 2f; // Radio de visión reducido para pruebas
    [SerializeField] private bool debugMode = true; // Activar para ver información de depuración
    
    private Camera fogCamera;
    private RenderTexture fogTexture;
    private GameObject visionObject;
    private Dictionary<GameObject, int> teamObjectLayers = new Dictionary<GameObject, int>();
    private int tempLayer = 31; // Usar la última capa disponible
    private bool createdFogPlane = false; // Para saber si creamos el plano
    
    private void Start()
    {
        // Iniciar niebla de guerra
        CreateFogOfWar();
        
        // Si no se especificó un jugador a seguir, buscar uno
        if (playerToFollow == null && followPlayer)
        {
            // Buscar jugadores en la capa "Player"
            TryFindPlayer();
            
            // Si no encontramos jugador al inicio, configurar una búsqueda periódica
            if (playerToFollow == transform)
            {
                InvokeRepeating("TryFindPlayer", 0.5f, 1.0f);
            }
        }
        
        // Crear objeto de visión después de establecer el jugador
        CreateVisionObject();
        
        // Actualizamos la visibilidad una vez al inicio para configurar las capas
        UpdateTeamVisibility();
        
        // Y luego regularmente
        InvokeRepeating("UpdateTeamVisibility", 0.1f, 0.1f);
    }
    
    // Método para intentar encontrar un jugador
    private void TryFindPlayer()
    {
        GameObject[] allPlayers = FindPlayersInLayer();
        
        // Si no hay jugadores aún, salir
        if (allPlayers.Length == 0)
        {
            // Si anteriormente no teníamos un jugador, usar este objeto como fallback
            if (playerToFollow == null)
            {
                playerToFollow = transform;
                string layerName = LayerMask.LayerToName((int)Mathf.Log(playerLayerMask.value, 2));
                Debug.LogWarning("No se encontró ningún jugador en la capa '" + layerName + "'. Usando este objeto como referencia.");
            }
            return;
        }
        
        // Buscar un jugador con el tag de equipo correcto
        GameObject playerWithTag = null;
        foreach (GameObject player in allPlayers)
        {
            if (player.CompareTag(teamTag))
            {
                playerWithTag = player;
                break;
            }
        }
        
        // Si encontramos un jugador con el tag correcto, usarlo
        if (playerWithTag != null)
        {
            // Si ya estábamos siguiendo a este jugador, no hacer nada
            if (playerToFollow == playerWithTag.transform)
                return;
                
            playerToFollow = playerWithTag.transform;
            Debug.Log("¡JUGADOR ENCONTRADO! Siguiendo al jugador con tag " + teamTag + ": " + playerWithTag.name);
            
            // Si ya habíamos creado el objeto de visión, actualizarlo
            if (visionObject != null)
            {
                // Actualizar objeto de visión para que siga al nuevo jugador
                visionObject.transform.SetParent(playerToFollow, true);
                visionObject.transform.localPosition = Vector3.zero; // Asegurarse que esté centrado
                visionObject.transform.position = playerToFollow.position;
                visionObject.name = "VisionReveal_" + playerToFollow.name;
                
                // Actualizar escala
                UpdateVisionObjectScale();
            }
            
            // Si encontramos un jugador válido, dejar de buscar
            if (IsInvoking("TryFindPlayer"))
            {
                CancelInvoke("TryFindPlayer");
                Debug.Log("Búsqueda de jugador cancelada porque ya encontramos uno.");
            }
        }
        // Si no hay con el tag correcto pero hay jugadores, usar el primero
        else if (allPlayers.Length > 0 && playerToFollow == transform)
        {
            playerToFollow = allPlayers[0].transform;
            Debug.Log("Siguiendo al primer jugador encontrado: " + allPlayers[0].name);
            
            // Actualizar objeto de visión
            if (visionObject != null)
            {
                visionObject.transform.SetParent(playerToFollow, true);
                visionObject.transform.localPosition = Vector3.zero; // Asegurarse que esté centrado
                visionObject.transform.position = playerToFollow.position;
                visionObject.name = "VisionReveal_" + playerToFollow.name;
                
                // Actualizar escala
                UpdateVisionObjectScale();
            }
        }
    }
    
    // Encuentra todos los jugadores en la capa "player"
    private GameObject[] FindPlayersInLayer()
    {
        // Si no se asignó una capa, intentar obtener la capa "Player"
        if (playerLayerMask.value == 0)
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer != -1)
            {
                playerLayerMask = 1 << playerLayer;
                Debug.Log("Capa 'Player' detectada automáticamente.");
            }
            else
            {
                // Intentar con 'player' minúscula como fallback
                playerLayer = LayerMask.NameToLayer("player");
                if (playerLayer != -1)
                {
                    playerLayerMask = 1 << playerLayer;
                    Debug.Log("Capa 'player' detectada automáticamente.");
                }
                else
                {
                    Debug.LogError("No se encontró la capa 'Player' ni 'player'. Por favor asigna la capa correcta en el inspector.");
                    return new GameObject[0];
                }
            }
        }
        
        // Encontrar todos los objetos en la capa
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        List<GameObject> playersInLayer = new List<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            if (((1 << obj.layer) & playerLayerMask.value) != 0)
            {
                playersInLayer.Add(obj);
            }
        }
        
        string layerName = LayerMask.LayerToName((int)Mathf.Log(playerLayerMask.value, 2));
        Debug.Log($"Encontrados {playersInLayer.Count} objetos en la capa '{layerName}'");
        return playersInLayer.ToArray();
    }
    
    private void CreateFogOfWar()
    {
        // Limpiar cualquier instancia anterior
        CleanupFogSystem();
        
        // Crear la textura
        int textureSize = 1024; // Aumentar la resolución para mejor calidad
        
        fogTexture = new RenderTexture(textureSize, textureSize, 0);
        fogTexture.wrapMode = TextureWrapMode.Clamp;
        fogTexture.filterMode = FilterMode.Bilinear;
        fogTexture.Create();
        
        Debug.Log($"Fog texture created with size: {textureSize}x{textureSize}");
        
        // Crear y configurar la cámara
        GameObject cameraObj = new GameObject("FogCamera");
        fogCamera = cameraObj.AddComponent<Camera>();
        fogCamera.orthographic = true;
        fogCamera.orthographicSize = mapSize.y / 2;
        fogCamera.transform.position = new Vector3(0, fogHeight, 0);
        fogCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
        
        // Configurar la cámara para que solo renderice la capa temporal
        fogCamera.cullingMask = 1 << tempLayer;
        fogCamera.clearFlags = CameraClearFlags.SolidColor;
        fogCamera.backgroundColor = Color.black;
        fogCamera.targetTexture = fogTexture;
        fogCamera.depth = -100;
        
        // Usar el plano existente o crear uno si no existe
        if (fogPlane == null)
        {
            // Crear el plano de niebla si no se asignó uno
            fogPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            fogPlane.name = "FogPlane";
            fogPlane.transform.position = new Vector3(0, 0.1f, 0);
            fogPlane.transform.rotation = Quaternion.identity;
            fogPlane.transform.localScale = new Vector3(mapSize.x/2, 1, mapSize.y/2);
            
            // Desactivar el collider del plano
            Destroy(fogPlane.GetComponent<Collider>());
            
            createdFogPlane = true;
        }
        else
        {
            Debug.Log("Usando plano existente: " + fogPlane.name);
        }
        
        // Configurar el material
        if (fogMaterial != null)
        {
            // Clonar el material para no modificar el original
            Material instanceMaterial = new Material(fogMaterial);
            
            // Configurar propiedades
            instanceMaterial.SetTexture("_FogTexture", fogTexture);
            instanceMaterial.SetColor("_FogColor", fogColor);
            instanceMaterial.SetFloat("_FogSoftness", fogSoftness);
            instanceMaterial.SetFloat("_FogBlend", fogBlend);
            
            // Asignar el material al plano
            var renderer = fogPlane.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = instanceMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            else
            {
                Debug.LogError("El objeto de niebla no tiene un componente Renderer!");
            }
            
            // Hacer el material disponible para inspección
            fogMaterial = instanceMaterial;
        }
        else
        {
            Debug.LogError("No se ha asignado un material para la niebla!");
        }
        
        // Asegurarse de que tenemos un offset adecuado
        fogMaterial.SetVector("_WorldPosition", Vector4.zero);
        fogMaterial.SetVector("_WorldSize", new Vector4(mapSize.x, mapSize.y, 0, 0));
    }
    
    // Crear un objeto que revele la niebla
    private void CreateVisionObject()
    {
        if (playerToFollow == null) return;
        
        // Destruir objeto anterior si existe
        if (visionObject != null)
        {
            Destroy(visionObject);
        }
        
        // Crear una esfera blanca para la visión
        visionObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visionObject.name = "VisionReveal_" + (playerToFollow ? playerToFollow.name : "Unknown");
        visionObject.transform.position = playerToFollow.position;
        visionObject.transform.localScale = new Vector3(visionRadius * 2, 0.1f, visionRadius * 2);
        
        // Configurar el material para que sea completamente blanco
        Material visionMaterial = new Material(Shader.Find("Standard"));
        visionMaterial.color = Color.white;
        
        // Asignar el material al objeto de visión
        Renderer rend = visionObject.GetComponent<Renderer>();
        rend.material = visionMaterial;
        
        // En modo depuración mostramos el objeto visualmente
        if (debugMode)
        {
            rend.enabled = true;
        }
        else
        {
            rend.enabled = false; // Ocultar visualmente
        }
        
        // Asignar a la capa temporal
        visionObject.layer = tempLayer;
        
        // El objeto debe seguir al jugador
        if (followPlayer && playerToFollow != null)
        {
            visionObject.transform.SetParent(playerToFollow, true);
            visionObject.transform.localPosition = Vector3.zero; // Asegurar que esté centrado en el jugador
        }
        
        // Destruir el collider
        Destroy(visionObject.GetComponent<Collider>());
        
        Debug.Log("Objeto de visión creado: " + visionObject.name);
    }
    
    // Método para lanzar rayos y calcular visibilidad con oclusión
    private void UpdateVisibilityWithOcclusion()
    {
        if (playerToFollow == null) return;
        
        // Destruir cualquier objeto anterior de visión
        if (visionObject != null)
        {
            Destroy(visionObject);
            visionObject = null;
        }
        
        // Crear un nuevo objeto para la máscara de visión
        visionObject = new GameObject("VisionOcclusionMask_" + playerToFollow.name);
        visionObject.transform.position = playerToFollow.position;
        visionObject.transform.rotation = Quaternion.identity;
        visionObject.layer = tempLayer;
        
        // Crear un mesh para la visibilidad
        MeshFilter meshFilter = visionObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = visionObject.AddComponent<MeshRenderer>();
        
        // Crear un material más sofisticado para la máscara de visión
        Material visionMaterial = new Material(Shader.Find("Standard"));
        visionMaterial.color = Color.white;
        
        // Hacer el material más suave
        visionMaterial.SetFloat("_Glossiness", 0);
        visionMaterial.SetFloat("_Metallic", 0);
        meshRenderer.material = visionMaterial;
        
        // En modo de depuración, mostrar el mesh
        meshRenderer.enabled = debugMode;
        
        // Definir el mesh de visión
        Mesh visionMesh = new Mesh();
        meshFilter.mesh = visionMesh;
        
        // Definir la capa de obstáculos (árboles, paredes, etc.)
        // Excluir la capa del jugador y la temporal para evitar auto-oclusión
        LayerMask obstaclesMask = ~(1 << tempLayer) & ~(playerLayerMask.value);
        
        // Aumentar el número de rayos para mayor precisión
        int rayCount = 360; // Un rayo por grado para mayor suavidad
        float angleStep = 360f / rayCount;
        
        // Listas para construir el mesh
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        
        // Punto central
        vertices.Add(Vector3.zero);
        normals.Add(Vector3.up);
        uv.Add(new Vector2(0.5f, 0.5f));
        
        // Calcular puntos para el círculo exterior (para suavidad)
        Vector3 lastValidPoint = Vector3.zero;
        bool hasLastValidPoint = false;
        
        for (int i = 0; i <= rayCount; i++)
        {
            float angle = i * angleStep;
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            
            // Distancia por defecto (radio completo)
            float distance = visionRadius;
            
            // Origen del rayo ligeramente elevado para evitar colisiones con el suelo
            Vector3 origin = playerToFollow.position + Vector3.up * 0.5f;
            
            // Lanzar múltiples rayos en la misma dirección a diferentes alturas
            bool hitObstacle = false;
            
            // Niveles de altura para detectar obstáculos más precisamente
            float[] heightLevels = new float[] { 0.1f, 0.5f, 1.0f, 1.5f };
            
            foreach (float height in heightLevels)
            {
                Vector3 rayOrigin = playerToFollow.position + Vector3.up * height;
                RaycastHit hit;
                
                if (Physics.Raycast(rayOrigin, direction, out hit, visionRadius, obstaclesMask))
                {
                    // Si el obstáculo no es del mismo equipo, bloquea la visión
                    if (!hit.collider.CompareTag(teamTag))
                    {
                        // Ajustar distancia con un pequeño margen
                        float newDistance = hit.distance * 0.95f;
                        distance = Mathf.Min(distance, newDistance);
                        hitObstacle = true;
                        
                        if (debugMode)
                        {
                            // Mostrar los rayos que golpean obstáculos
                            Debug.DrawRay(rayOrigin, direction * distance, Color.red, 0.05f);
                        }
                    }
                }
            }
            
            if (!hitObstacle && debugMode)
            {
                // Rayos que no golpean nada
                Debug.DrawRay(origin, direction * distance, Color.green, 0.05f);
            }
            
            // Añadir punto al mesh
            Vector3 vertexPosition = direction * distance;
            vertexPosition.y = 0; // Mantener plano
            
            // Suavizar transiciones bruscas
            if (hasLastValidPoint && Vector3.Distance(lastValidPoint, vertexPosition) > visionRadius * 0.5f)
            {
                // Interpolar entre puntos distantes
                int subdivisions = 5;
                for (int s = 1; s < subdivisions; s++)
                {
                    float t = (float)s / subdivisions;
                    Vector3 smoothPoint = Vector3.Lerp(lastValidPoint, vertexPosition, t);
                    
                    // Ajustar distancia para que no exceda el radio
                    if (smoothPoint.magnitude > visionRadius)
                    {
                        smoothPoint = smoothPoint.normalized * visionRadius;
                    }
                    
                    vertices.Add(smoothPoint);
                    normals.Add(Vector3.up);
                    uv.Add(new Vector2((smoothPoint.x / visionRadius / 2) + 0.5f, 
                                      (smoothPoint.z / visionRadius / 2) + 0.5f));
                    
                    // Añadir triángulos para los puntos suavizados
                    if (vertices.Count > 2)
                    {
                        triangles.Add(0);
                        triangles.Add(vertices.Count - 2);
                        triangles.Add(vertices.Count - 1);
                    }
                }
            }
            
            vertices.Add(vertexPosition);
            normals.Add(Vector3.up);
            uv.Add(new Vector2((vertexPosition.x / visionRadius / 2) + 0.5f, 
                              (vertexPosition.z / visionRadius / 2) + 0.5f));
            
            // Añadir triángulos
            if (vertices.Count > 2)
            {
                triangles.Add(0);
                triangles.Add(vertices.Count - 2);
                triangles.Add(vertices.Count - 1);
            }
            
            // Actualizar último punto válido
            lastValidPoint = vertexPosition;
            hasLastValidPoint = true;
        }
        
        // Cerrar el loop conectando el último punto con el primero
        if (vertices.Count > 2)
        {
            triangles.Add(0);
            triangles.Add(vertices.Count - 1);
            triangles.Add(1);
        }
        
        // Asignar los datos al mesh
        visionMesh.Clear();
        visionMesh.vertices = vertices.ToArray();
        visionMesh.triangles = triangles.ToArray();
        visionMesh.normals = normals.ToArray();
        visionMesh.uv = uv.ToArray();
        
        // Optimizar el mesh
        visionMesh.RecalculateBounds();
        visionMesh.RecalculateNormals();
        visionMesh.Optimize();
        
        // El objeto debe seguir al jugador
        if (followPlayer && playerToFollow != null)
        {
            visionObject.transform.SetParent(playerToFollow, true);
            visionObject.transform.localPosition = Vector3.zero;
        }
        
        Debug.Log("Objeto de visión con oclusión mejorada creado para: " + playerToFollow.name);
    }
    
    private void UpdateTeamVisibility()
    {
        // Lista para rastrear objetos procesados en este ciclo
        List<GameObject> processedObjects = new List<GameObject>();
        
        // Si estamos siguiendo a un jugador específico, asegurarnos de que esté en la capa correcta
        if (playerToFollow != null)
        {
            // Guardar la capa original
            if (!teamObjectLayers.ContainsKey(playerToFollow.gameObject))
            {
                teamObjectLayers[playerToFollow.gameObject] = playerToFollow.gameObject.layer;
            }
            
            // Cambiar a la capa temporal
            playerToFollow.gameObject.layer = tempLayer;
            
            // Agregar a la lista de objetos procesados
            processedObjects.Add(playerToFollow.gameObject);
            
            // Actualizar la máscara de visión con oclusión
            UpdateVisibilityWithOcclusion();
        }
        
        // Validar que el tag no sea nulo ni vacío
        if (string.IsNullOrEmpty(teamTag))
        {
            // Usar un valor por defecto si el tag está vacío
            teamTag = "RedTeam";
            Debug.LogWarning("Tag de equipo vacío. Usando 'RedTeam' por defecto.");
        }
        
        // Comprobar si el tag existe
        try
        {
            // Además, intentar encontrar otros objetos con el tag del equipo
            GameObject[] teamObjs = GameObject.FindGameObjectsWithTag(teamTag);
            
            if (teamObjs.Length == 0 && debugMode)
            {
                Debug.LogWarning($"No se encontraron objetos con el tag: {teamTag}");
            }
            else if (debugMode)
            {
                Debug.Log($"Encontrados {teamObjs.Length} objetos con el tag: {teamTag}");
                
                foreach (GameObject obj in teamObjs)
                {
                    // No procesar el objeto que ya estamos siguiendo
                    if (playerToFollow != null && obj == playerToFollow.gameObject) continue;
                    
                    // Guardar la capa original
                    if (!teamObjectLayers.ContainsKey(obj))
                    {
                        teamObjectLayers[obj] = obj.layer;
                    }
                    
                    // Cambiar a la capa temporal
                    obj.layer = tempLayer;
                    
                    // Agregar a la lista de objetos procesados
                    processedObjects.Add(obj);
                }
            }
        }
        catch (System.ArgumentException e)
        {
            Debug.LogError($"Error con el tag '{teamTag}': {e.Message}. Verifica que exista este tag en Edit > Project Settings > Tags and Layers.");
        }
        
        // Programar la restauración de capas para después del renderizado
        StartCoroutine(RestoreLayersAfterRender(processedObjects));
    }
    
    // Corrutina para restaurar las capas originales después del renderizado
    private System.Collections.IEnumerator RestoreLayersAfterRender(List<GameObject> objectsToRestore)
    {
        // Esperar a que termine el frame actual (después del renderizado)
        yield return new WaitForEndOfFrame();
        
        // Restaurar las capas originales
        foreach (GameObject obj in objectsToRestore)
        {
            if (obj != null && teamObjectLayers.ContainsKey(obj))
            {
                obj.layer = teamObjectLayers[obj];
            }
        }
    }
    
    private void Update()
    {
        if (followPlayer && playerToFollow != null)
        {
            // Actualizar posición de la cámara para seguir al jugador
            Vector3 playerPos = playerToFollow.position;
            fogCamera.transform.position = new Vector3(playerPos.x, fogHeight, playerPos.z);
            
            // Actualizar posición del plano solo si fue creado dinámicamente o si está marcado para seguir
            if (fogPlane != null && (createdFogPlane || followPlayer))
            {
                fogPlane.transform.position = new Vector3(playerPos.x, 0.1f, playerPos.z);
            }
            
            // IMPORTANTE: Actualizar el offset del material para que coincida con la cámara de la niebla
            if (fogMaterial != null)
            {
                // Pasar la posición del jugador al shader para alinear la textura con el mundo
                fogMaterial.SetVector("_WorldPosition", new Vector4(playerPos.x, playerPos.z, 0, 0));
                fogMaterial.SetVector("_WorldSize", new Vector4(mapSize.x, mapSize.y, 0, 0));
            }
        }
        
        // Método para depuración que permite pulsar teclas para ajustar parámetros
        if (debugMode)
        {
            // Agrandar radio de visión
            if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus))
            {
                visionRadius += 0.5f;
                // Actualizar la máscara de visión para reflejar el nuevo radio
                if (playerToFollow != null)
                {
                    UpdateVisibilityWithOcclusion();
                }
                Debug.Log("Radio de visión aumentado a: " + visionRadius);
            }
            
            // Reducir radio de visión
            if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
            {
                visionRadius = Mathf.Max(0.5f, visionRadius - 0.5f);
                // Actualizar la máscara de visión para reflejar el nuevo radio
                if (playerToFollow != null)
                {
                    UpdateVisibilityWithOcclusion();
                }
                Debug.Log("Radio de visión reducido a: " + visionRadius);
            }
            
            // Tecla R para reconstruir el objeto de visión (útil para debugging)
            if (Input.GetKeyDown(KeyCode.R))
            {
                UpdateVisibilityWithOcclusion();
                Debug.Log("Objeto de visión reconstruido");
            }
            
            // Tecla C para limpiar y reconstruir todo el sistema de niebla
            if (Input.GetKeyDown(KeyCode.C))
            {
                CleanupFogSystem();
                CreateFogOfWar();
                if (playerToFollow != null)
                {
                    UpdateVisibilityWithOcclusion();
                }
                Debug.Log("Sistema de niebla reconstruido completamente");
            }
        }
    }
    
    // Método auxiliar para actualizar la escala del objeto de visión
    private void UpdateVisionObjectScale()
    {
        if (visionObject != null)
        {
            visionObject.transform.localScale = new Vector3(visionRadius * 2, 0.1f, visionRadius * 2);
        }
    }
    
    // Limpia todo el sistema de niebla
    private void CleanupFogSystem()
    {
        // Cancelar invocaciones pendientes
        CancelInvoke("UpdateTeamVisibility");
        CancelInvoke("TryFindPlayer");
        
        // Restaurar las capas originales
        foreach (var kvp in teamObjectLayers)
        {
            if (kvp.Key != null)
            {
                kvp.Key.layer = kvp.Value;
            }
        }
        
        // Limpiar completamente la textura de niebla
        if (fogTexture != null)
        {
            RenderTexture.active = fogTexture;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = null;
            
            fogTexture.Release();
            Destroy(fogTexture);
            fogTexture = null;
        }
        
        // Eliminar la cámara de niebla
        if (fogCamera != null)
        {
            Destroy(fogCamera.gameObject);
            fogCamera = null;
        }
        
        // Destruir todos los objetos de visión que puedan existir
        GameObject[] visionObjects = GameObject.FindObjectsOfType<GameObject>()
            .Where(obj => obj.name.StartsWith("VisionReveal_") || obj.name.StartsWith("VisionOcclusionMask_"))
            .ToArray();
            
        foreach (var obj in visionObjects)
        {
            Destroy(obj);
        }
        
        // Asegurarse de que nuestro objeto de visión se elimina
        if (visionObject != null)
        {
            Destroy(visionObject);
            visionObject = null;
        }
        
        // Limpiar el diccionario de capas
        teamObjectLayers.Clear();
    }
    
    private void OnDestroy()
    {
        CancelInvoke("UpdateTeamVisibility");
        
        // Restaurar las capas originales
        foreach (var kvp in teamObjectLayers)
        {
            if (kvp.Key != null)
            {
                kvp.Key.layer = kvp.Value;
            }
        }
        
        if (fogTexture != null)
        {
            fogTexture.Release();
            Destroy(fogTexture);
        }
        
        if (fogCamera != null)
            Destroy(fogCamera.gameObject);
            
        // Destruir el plano solo si lo creamos nosotros
        if (fogPlane != null && createdFogPlane)
            Destroy(fogPlane);
            
        if (visionObject != null)
            Destroy(visionObject);
    }
    
    // Para depuración
    private void OnGUI()
    {
        if (fogTexture != null && Input.GetKey(KeyCode.F))
        {
            GUI.DrawTexture(new Rect(10, 10, 200, 200), fogTexture, ScaleMode.ScaleToFit);
            
            GUI.Label(new Rect(10, 220, 300, 20), "Team Tag: " + teamTag);
            if (playerToFollow != null)
                GUI.Label(new Rect(10, 240, 300, 20), "Following: " + playerToFollow.name);
            
            GUI.Label(new Rect(10, 260, 300, 20), "Vision Radius: " + visionRadius);
            GUI.Label(new Rect(10, 280, 300, 20), "Debug Mode: " + (debugMode ? "ON" : "OFF"));
            
            if (playerLayerMask.value > 0)
                GUI.Label(new Rect(10, 300, 300, 20), "Player Layer: " + LayerMask.LayerToName((int)Mathf.Log(playerLayerMask.value, 2)));
        }
    }
} 