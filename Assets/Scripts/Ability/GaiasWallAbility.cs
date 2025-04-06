using UnityEngine;
using System.Collections;
using Photon.Pun;
using System.Collections.Generic;
using System.Collections.Generic;

namespace Photon.Pun.Demo.Asteroids
{
    /// <summary>
    /// Implementación de la habilidad Gaia's Wall para Gaius, the guardian of Gaia.
    /// Permite colocar una muralla que crece desde el suelo.
    /// Implementación basada en el patrón de ScarecrowAbility para mantener consistencia.
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class GaiasWallAbility : AbilityBase // Cambiado a heredar de AbilityBase para evitar problemas con AOEAbility
    {
        [Header("Wall Settings")]
        public float wallWidth = 5f;          // Ancho de la muralla
        public float wallHeight = 3f;         // Altura máxima de la muralla
        public float growDuration = 1.5f;     // Duración de la animación de crecimiento
        public float wallDuration = 15f;      // Duración de la muralla en segundos (0 = permanente)
        public GameObject wallPrefab;         // Prefab de la muralla
        
        [Header("Placement Settings")]
        public LayerMask groundLayer;         // Capa del suelo para detectar colisiones
        public float maxPlacementRange = 10f; // Rango máximo para colocar la muralla
        
        [Header("Visual Feedback")]
        public GameObject placementIndicatorPrefab; // Indicador de posición para colocación
        public Material validPlacementMaterial;     // Material para posición válida
        public Material invalidPlacementMaterial;   // Material para posición inválida
        public Material wallGrowMaterial;           // Material para efecto de crecimiento
        public Color glowColor = Color.green;       // Color del brillo al crecer
        
        // Variables privadas
        private GameObject placementIndicator;      // Instancia del indicador de posición
        private bool isPlacing = false;             // Si está en modo de colocación
        private Camera mainCamera;                  // Referencia a la cámara
        private bool wallPlaced = false;            // Si ya se colocó la muralla
        private bool abilityActive = true;          // Si la habilidad sigue activa
        
        // Override de OnAbilityInitialized de AbilityBase
        protected override void OnAbilityInitialized()
        {
            base.OnAbilityInitialized(); // Llamar a la base para inicialización estándar
            
            Debug.Log($"[GaiasWallAbility] Inicializando - photonView.IsMine: {photonView.IsMine}, ViewID: {photonView.ViewID}");
            
            // Verificar componentes críticos
            if (wallPrefab == null)
            {
                Debug.LogError("[GaiasWallAbility] wallPrefab no asignado. Asigna un prefab en el inspector.");
                return;
            }
            
            // Obtener la cámara principal
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                // Buscar por el nombre específico de tu cámara
                Camera[] allCameras = FindObjectsOfType<Camera>();
                foreach (Camera cam in allCameras)
                {
                    if (cam.gameObject.name.Contains("PhotonLocalCamera"))
                    {
                        mainCamera = cam;
                        Debug.Log($"[GaiasWallAbility] Encontrada PhotonLocalCamera: {cam.name}");
                        break;
                    }
                }
                
                if (mainCamera == null)
                {
                    Debug.LogError("[GaiasWallAbility] No se pudo encontrar una cámara. La habilidad no funcionará correctamente.");
                    return;
                }
            }
            
            // IMPORTANTE: Desactivar la auto-destrucción por tiempo de vida mientras estamos colocando
            // Cancela cualquier invocación previa para evitar destrucción prematura
            CancelInvoke("DestroyAbility");
            
            // Si somos el propietario de la habilidad, entramos en modo de colocación
            if (photonView.IsMine)
            {
                StartCoroutine(EnterPlacementMode());
            }
        }
        
        // Corrutina para entrar en modo de colocación con un pequeño retardo
        private IEnumerator EnterPlacementMode()
        {
            // Pequeño retraso para asegurar que todos los sistemas están inicializados
            yield return new WaitForSeconds(0.1f);
            
            // Entrar en modo de colocación
            isPlacing = true;
            abilityActive = true;
            
            // Crear indicador visual
            CreatePlacementIndicator();
            
            Debug.Log("[GaiasWallAbility] Modo de colocación activado");
        }
        
        // Crear el indicador de posición
        private void CreatePlacementIndicator()
        {
            Debug.Log($"[GaiasWallAbility] Intentando crear indicador. Prefab asignado: {(placementIndicatorPrefab != null ? placementIndicatorPrefab.name : "NULL")}");
            
            if (placementIndicatorPrefab != null && placementIndicator == null)
            {
                // Crear en una posición fuera de la vista inicialmente
                placementIndicator = Instantiate(placementIndicatorPrefab, new Vector3(0, -100, 0), Quaternion.identity);
                
                // Aplicar escala adecuada al indicador
                placementIndicator.transform.localScale = new Vector3(wallWidth, 0.1f, 1f);
                
                // Agregar componente para efectos visuales adicionales si es necesario
                var indicatorEffect = placementIndicator.GetComponent<PlacementIndicator>();
                if (indicatorEffect == null)
                {
                    indicatorEffect = placementIndicator.AddComponent<PlacementIndicator>();
                    Debug.Log("[GaiasWallAbility] PlacementIndicator componente añadido");
                }
                
                // Ocultar inicialmente hasta tener una posición válida
                placementIndicator.SetActive(false);
                
                Debug.Log($"[GaiasWallAbility] Indicador de posición creado: {placementIndicator.name}");
            }
            else if (placementIndicatorPrefab == null)
            {
                // Intentar cargar el prefab desde Resources si no está asignado
                var indicatorPrefab = Resources.Load<GameObject>("Abilities/Gaius, the guardian of gaia/GaiasWall Indicator");
                if (indicatorPrefab != null)
                {
                    placementIndicatorPrefab = indicatorPrefab;
                    Debug.Log("[GaiasWallAbility] Indicador cargado desde Resources: " + indicatorPrefab.name);
                    CreatePlacementIndicator(); // Intentar crear de nuevo
                    return;
                }
                
                Debug.LogError("[GaiasWallAbility] placementIndicatorPrefab no asignado. La colocación no tendrá feedback visual.");
            }
            else
            {
                Debug.Log("[GaiasWallAbility] El indicador ya existe, no se creará uno nuevo");
            }
        }
        
        // Update para gestionar la colocación de la muralla
        private void Update()
        {
            // Solo el propietario de la habilidad maneja la colocación
            if (!photonView.IsMine) return;
            
            // Solo procesar en modo de colocación
            if (!isPlacing) return;
            
            // Si ya se colocó la muralla, no hacer nada más
            if (wallPlaced) return;
            
            // Si la habilidad no está activa, no hacer nada
            if (!abilityActive) return;
            
            // Verificar que tenemos una cámara válida
            if (mainCamera == null)
            {
                Debug.LogError("[GaiasWallAbility] No hay cámara principal disponible");
                mainCamera = Camera.main;
                if (mainCamera == null) return;
            }
            
            // Raycast para encontrar posición en el suelo
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit, 100f, groundLayer))
            {
                // Mostrar el indicador si estaba oculto
                if (placementIndicator != null && !placementIndicator.activeSelf)
                {
                    placementIndicator.SetActive(true);
                    Debug.Log("[GaiasWallAbility] Indicador activado, posición válida encontrada");
                }
                
                Vector3 hitPoint = hit.point;
                
                // Verificar si está dentro del rango de colocación
                bool isValidPosition = true;
                if (caster != null)
                {
                    isValidPosition = Vector3.Distance(caster.transform.position, hitPoint) <= maxPlacementRange;
                }
                
                // Actualizar posición del indicador
                if (placementIndicator != null)
                {
                    // Posicionar indicador ligeramente por encima del suelo
                    placementIndicator.transform.position = hitPoint + new Vector3(0, 0.05f, 0);
                    
                    // Calcular dirección hacia el caster para orientar la muralla
                    Vector3 dirToCaster = Vector3.forward;
                    if (caster != null)
                    {
                        dirToCaster = (caster.transform.position - hitPoint).normalized;
                        dirToCaster.y = 0; // Mantener en plano horizontal
                    }
                    
                    // Rotar para que la muralla mire hacia el caster
                    placementIndicator.transform.forward = -dirToCaster;
                    
                    // Cambiar material según validez
                    Renderer renderer = placementIndicator.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        if (isValidPosition)
                        {
                            if (validPlacementMaterial != null)
                                renderer.material = validPlacementMaterial;
                        }
                        else
                        {
                            if (invalidPlacementMaterial != null)
                                renderer.material = invalidPlacementMaterial;
                        }
                    }
                }
                
                // Click izquierdo para colocar la muralla
                if (Input.GetMouseButtonDown(0))
                {
                    Debug.Log($"[GaiasWallAbility] Click detectado en posición: {hitPoint}, válida: {isValidPosition}");
                    
                    if (isValidPosition)
                    {
                        // Calcular dirección
                        Vector3 wallDirection = Vector3.forward;
                        if (caster != null)
                        {
                            wallDirection = -(caster.transform.position - hitPoint).normalized;
                            wallDirection.y = 0;
                        }
                        
                        // Colocar la muralla
                        Debug.Log($"[GaiasWallAbility] Enviando RPC para colocar muralla en posición {hitPoint}");
                        photonView.RPC("RPC_PlaceWall", RpcTarget.AllBuffered, hitPoint, wallDirection);
                        
                        // Marcar que ya se colocó
                        wallPlaced = true;
                        isPlacing = false;
                        
                        // Destruir el indicador
                        if (placementIndicator != null)
                        {
                            Destroy(placementIndicator);
                            placementIndicator = null;
                            Debug.Log("[GaiasWallAbility] Indicador destruido después de colocar muralla");
                        }
                        
                        // Destruir la habilidad después de crear la muralla
                        // Programar con un pequeño retraso para asegurar que el RPC se envía
                        Invoke("DestroyAbility", 0.5f);
                    }
                    else
                    {
                        Debug.Log("[GaiasWallAbility] Posición inválida: demasiado lejos");
                    }
                }
                
                // Click derecho para cancelar
                if (Input.GetMouseButtonDown(1))
                {
                    // Destruir la habilidad
                    Debug.Log("[GaiasWallAbility] Habilidad cancelada por click derecho");
                    CancelPlacement();
                }
            }
            else
            {
                // Ocultar indicador cuando no hay hit válido
                if (placementIndicator != null)
                {
                    placementIndicator.SetActive(false);
                }
            }
            
            // También verificar la tecla Escape para cancelar
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Debug.Log("[GaiasWallAbility] Habilidad cancelada por Escape");
                CancelPlacement();
            }
        }
        
        // Método para cancelar la colocación
        private void CancelPlacement()
        {
            isPlacing = false;
            abilityActive = false;
            
            // Destruir el indicador
            if (placementIndicator != null)
            {
                Destroy(placementIndicator);
                placementIndicator = null;
                Debug.Log("[GaiasWallAbility] Indicador destruido por cancelación");
            }
            
            // Destruir la habilidad
            DestroyAbility();
        }
        
        // Método para colocar la muralla en todos los clientes
        [PunRPC]
        private void RPC_PlaceWall(Vector3 position, Vector3 direction)
        {
            try
            {
                Debug.Log($"[GaiasWallAbility] RPC_PlaceWall llamado en posición {position}, dirección {direction}");
                
                // Verificar disponibilidad del prefab
                if (wallPrefab == null)
                {
                    // Intentar cargar desde Resources
                    wallPrefab = Resources.Load<GameObject>("Prefabs/Abilities/Gaia'sWall");
                    
                    if (wallPrefab == null)
                    {
                        Debug.LogError("[GaiasWallAbility] wallPrefab es NULL y no se pudo cargar desde Resources, no se puede crear la muralla");
                        return;
                    }
                    Debug.Log("[GaiasWallAbility] Prefab de muralla cargado desde Resources: " + wallPrefab.name);
                }
                
                Debug.Log($"[GaiasWallAbility] wallPrefab encontrado: {wallPrefab.name}");
                
                // Colocar ligeramente por encima del suelo para evitar penetración
                position.y += 0.05f;
                
                // Crear la muralla - usar el prefab directamente
                GameObject wall = Instantiate(wallPrefab, position, Quaternion.identity);
                
                if (wall == null)
                {
                    Debug.LogError("[GaiasWallAbility] Fallo al instanciar la muralla");
                    return;
                }
                
                Debug.Log($"[GaiasWallAbility] Muralla instanciada: {wall.name} en posición {wall.transform.position}");
                
                // Orientar la muralla
                wall.transform.forward = direction;
                
                // Ajustar tamaño inicial (aplastado para efecto de crecimiento)
                wall.transform.localScale = new Vector3(wallWidth, 0.1f, 1f);
                
                // Registrar estructura de jerarquía para depuración
                string hierarchyInfo = "Jerarquía de la muralla:\n";
                foreach (Transform child in wall.transform)
                {
                    hierarchyInfo += $"- {child.name}\n";
                    
                    Renderer renderer = child.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        hierarchyInfo += $"  (Tiene Renderer con material: {renderer.material.name})\n";
                    }
                }
                Debug.Log(hierarchyInfo);
                
                // Aplicar material de crecimiento si está disponible
                if (wallGrowMaterial != null)
                {
                    Renderer[] renderers = wall.GetComponentsInChildren<Renderer>(true);
                    Debug.Log($"[GaiasWallAbility] Aplicando material {wallGrowMaterial.name} a {renderers.Length} renderers");
                    
                    if (renderers.Length == 0)
                    {
                        Debug.LogWarning("[GaiasWallAbility] No se encontraron renderers en la muralla");
                    }
                    
                    foreach (Renderer renderer in renderers)
                    {
                        if (renderer != null)
                        {
                            // Crear instancia única del material para poder modificarlo
                            Material growMaterial = new Material(wallGrowMaterial);
                            growMaterial.SetColor("_GlowColor", glowColor);
                            
                            // Inicializar valores del shader
                            if (growMaterial.HasProperty("_GrowProgress"))
                                growMaterial.SetFloat("_GrowProgress", 0f);
                                
                            renderer.material = growMaterial;
                            Debug.Log($"[GaiasWallAbility] Material aplicado a {renderer.gameObject.name}");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("[GaiasWallAbility] wallGrowMaterial es NULL, se usará el material por defecto");
                }
                
                // Verificar si hay colliders para interacción física
                Collider[] colliders = wall.GetComponentsInChildren<Collider>(true);
                Debug.Log($"[GaiasWallAbility] La muralla tiene {colliders.Length} colliders");
                
                // Iniciar animación de crecimiento
                StartCoroutine(GrowWallEffect(wall));
                
                // Reproducir sonido
                PlayAbilitySound();
                
                // Programar destrucción si tiene duración
                if (wallDuration > 0f)
                {
                    Destroy(wall, wallDuration);
                    Debug.Log($"[GaiasWallAbility] Muralla programada para destruirse en {wallDuration} segundos");
                }
                
                Debug.Log("[GaiasWallAbility] Muralla creada correctamente");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GaiasWallAbility] Error al crear muralla: {e.Message}\n{e.StackTrace}");
            }
        }
        
        // Corrutina para efecto de crecimiento de la muralla
        private IEnumerator GrowWallEffect(GameObject wall)
        {
            Debug.Log($"[GaiasWallAbility] Iniciando animación de crecimiento para {wall.name}");
            
            float elapsedTime = 0f;
            Vector3 initialPosition = wall.transform.position;
            Renderer[] renderers = wall.GetComponentsInChildren<Renderer>(true);
            
            // Obtener todos los transforms hijos para ajustar su escala
            List<Transform> childTransforms = new List<Transform>();
            foreach (Transform child in wall.transform)
            {
                childTransforms.Add(child);
            }
            
            // Guardar escalas iniciales de los hijos
            Dictionary<Transform, Vector3> initialScales = new Dictionary<Transform, Vector3>();
            foreach (Transform child in childTransforms)
            {
                initialScales[child] = child.localScale;
            }
            
            Debug.Log($"[GaiasWallAbility] Comenzando animación con {renderers.Length} renderers y {childTransforms.Count} hijos");
            
            while (elapsedTime < growDuration)
            {
                // Calcular progreso de crecimiento
                float progress = elapsedTime / growDuration;
                float smoothProgress = Mathf.SmoothStep(0, 1, progress);
                float currentHeight = Mathf.Lerp(0.1f, wallHeight, smoothProgress);
                
                // Actualizar escala del contenedor principal
                wall.transform.localScale = new Vector3(wallWidth, currentHeight, 1f);
                
                // Actualizar posición Y para que crezca desde el suelo
                wall.transform.position = new Vector3(
                    initialPosition.x,
                    initialPosition.y + (currentHeight / 2f),
                    initialPosition.z
                );
                
                // Actualizar shader de crecimiento
                foreach (Renderer renderer in renderers)
                {
                    if (renderer != null && renderer.material != null && 
                        renderer.material.HasProperty("_GrowProgress"))
                    {
                        renderer.material.SetFloat("_GrowProgress", smoothProgress);
                    }
                }
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // Asegurar valores finales
            wall.transform.localScale = new Vector3(wallWidth, wallHeight, 1f);
            wall.transform.position = new Vector3(
                initialPosition.x,
                initialPosition.y + (wallHeight / 2f),
                initialPosition.z
            );
            
            // Valor final del shader
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null && renderer.material != null && 
                    renderer.material.HasProperty("_GrowProgress"))
                {
                    renderer.material.SetFloat("_GrowProgress", 1.0f);
                }
            }
            
            // Sonido de completado
            PlayImpactSound();
            
            Debug.Log("[GaiasWallAbility] Animación de crecimiento completada");
        }
        
        // Al destruir la habilidad, limpiar recursos
        private void OnDestroy()
        {
            // Limpiar indicador de posición si existe
            if (placementIndicator != null)
            {
                Destroy(placementIndicator);
                placementIndicator = null;
                Debug.Log("[GaiasWallAbility] Indicador destruido en OnDestroy");
            }
        }
    }
} 