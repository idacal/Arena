using UnityEngine;
using System.Collections;

namespace Photon.Pun.Demo.Asteroids
{
    /// <summary>
    /// Controlador de cámara estilo MOBA optimizado para Photon PUN
    /// </summary>
    public class PhotonMOBACamera : MonoBehaviour
    {
        [Header("Camera Configuration")]
        public Transform target;
        public float cameraHeight = 10f;
        public float cameraDistance = 10f;
        public float cameraPitch = 60f;
        
        [Header("Movement")]
        public float edgeScrollSpeed = 25.0f;
        public float edgeScrollThreshold = 20.0f;
        public bool useEdgeScrolling = true;
        public float mouseWheelZoomSpeed = 5.0f;
        public Vector2 heightZoomRange = new Vector2(7f, 23f);
        public Vector2 distanceZoomRange = new Vector2(7f, 19f);
        public float snapToTargetSpeed = 8.0f;
        
        [Header("Map Boundaries")]
        public bool useBoundaries = true;
        public float mapMinX = -500f;
        public float mapMaxX = 500f;
        public float mapMinZ = -500f;
        public float mapMaxZ = 500f;
        
        [Header("Controls")]
        public KeyCode centerOnPlayerKey = KeyCode.C;
        public KeyCode cameraDragKey = KeyCode.Mouse2; // Middle mouse button (wheel)
        public KeyCode toggleFollowKey = KeyCode.V; // Tecla V para alternar seguimiento
        
        // Variables internas
        private Vector3 cameraTargetPosition;
        private float currentZoomFactor = 0.5f; // Normalized zoom factor (0-1)
        private bool isDragging = false;
        private Vector3 dragStartPosition;
        private Vector3 dragCurrentPosition;
        private bool snapToPlayer = false;
        private bool isFollowing = false; // Por defecto, no sigue al jugador
        
        // ID único de esta cámara (para depuración)
        private string cameraId;
        
        void Awake()
        {
            // Generar ID único para esta cámara
            cameraId = System.Guid.NewGuid().ToString().Substring(0, 8);
            
            // Asegurar que esta cámara NO se sincroniza por red
            Camera cam = GetComponent<Camera>();
            if (cam != null)
            {
                cam.tag = "MainCamera";
            }
            
            // Nombre único para depuración
            gameObject.name = $"PhotonLocalCamera_{cameraId}";
            
            Debug.Log($"[PhotonMOBACamera] Inicializando cámara con ID único: {cameraId}");
        }
        
        void Start()
        {
            // Rotación inicial
            transform.rotation = Quaternion.Euler(cameraPitch, 0f, 0f);
            
            // Inicializar posición de la cámara
            if (target != null)
            {
                cameraTargetPosition = target.position;
                snapToPlayer = true;
            }
            else
            {
                // Sin target, usar posición específica
                cameraTargetPosition = new Vector3(0f, 0f, 0f);
            }
            
            UpdateCameraPosition(false); // Posicionamiento inmediato
            
            // Mostrar mensaje de ayuda
            Debug.Log("[PhotonMOBACamera] Controles de cámara:\n" +
                     "- V: Alternar seguimiento del jugador\n" +
                     "- C: Centrar en jugador\n" +
                     "- RUEDA DEL RATÓN: Arrastrar cámara\n" +
                     "- BORDES DE PANTALLA: Mover cámara\n" +
                     "- SCROLL: Zoom");
        }
        
        void LateUpdate()
        {
            // Solo actualizar si tenemos un objetivo
            if (target == null)
                return;
                
            // Alternar seguimiento con la tecla V
            if (Input.GetKeyDown(toggleFollowKey))
            {
                isFollowing = !isFollowing;
                Debug.Log($"[PhotonMOBACamera] Seguimiento de cámara {(isFollowing ? "activado" : "desactivado")}");
            }
                
            // Zoom con rueda del ratón
            HandleZoom();
            
            // Tecla para centrar en jugador (C)
            if (Input.GetKeyDown(centerOnPlayerKey))
            {
                Debug.Log($"[PhotonMOBACamera] Centrando cámara en jugador {target.name}");
                cameraTargetPosition = target.position;
                snapToPlayer = true;
            }
            
            // Seguimiento del jugador solo si está activo
            if (isFollowing)
            {
                cameraTargetPosition = Vector3.Lerp(cameraTargetPosition, target.position, snapToTargetSpeed * Time.deltaTime);
            }
            
            // Scroll en bordes de pantalla solo si no está siguiendo
            if (useEdgeScrolling && !isFollowing)
            {
                HandleEdgeScrolling();
            }
            
            // Arrastrar con botón medio solo si no está siguiendo
            if (!isFollowing)
            {
                HandleMouseDrag();
            }
            
            // Aplicar límites del mapa
            if (useBoundaries)
            {
                cameraTargetPosition.x = Mathf.Clamp(cameraTargetPosition.x, mapMinX, mapMaxX);
                cameraTargetPosition.z = Mathf.Clamp(cameraTargetPosition.z, mapMinZ, mapMaxZ);
            }
            
            // Actualizar posición final
            UpdateCameraPosition(true);
        }
        
        private void HandleZoom()
        {
            // Obtener entrada de la rueda del ratón
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            
            if (scrollInput != 0)
            {
                // Ajustar el factor de zoom basado en la entrada
                float zoomDelta = scrollInput * mouseWheelZoomSpeed * 0.1f;
                
                // Actualizar factor de zoom (limitado entre 0 y 1)
                currentZoomFactor = Mathf.Clamp01(currentZoomFactor + zoomDelta);
            }
        }
        
        private void HandleEdgeScrolling()
        {
            if (isDragging) return; // No hacer edge scroll mientras arrastramos
            
            Vector3 moveDirection = Vector3.zero;
            
            // Bordes horizontales
            if (Input.mousePosition.x < edgeScrollThreshold)
            {
                moveDirection.x = -1;
            }
            else if (Input.mousePosition.x > Screen.width - edgeScrollThreshold)
            {
                moveDirection.x = 1;
            }
            
            // Bordes verticales
            if (Input.mousePosition.y < edgeScrollThreshold)
            {
                moveDirection.z = -1;
            }
            else if (Input.mousePosition.y > Screen.height - edgeScrollThreshold)
            {
                moveDirection.z = 1;
            }
            
            if (moveDirection != Vector3.zero)
            {
                // Convertir dirección según rotación de cámara
                moveDirection = Quaternion.Euler(0, transform.eulerAngles.y, 0) * moveDirection;
                
                // Aplicar movimiento
                Vector3 newPosition = cameraTargetPosition + moveDirection.normalized * edgeScrollSpeed * Time.deltaTime;
                cameraTargetPosition = newPosition;
            }
        }
        
        private void HandleMouseDrag()
        {
            // Iniciar arrastre con botón medio (rueda)
            if (Input.GetMouseButtonDown((int)cameraDragKey - (int)KeyCode.Mouse0))
            {
                isDragging = true;
                dragStartPosition = Input.mousePosition;
                dragCurrentPosition = dragStartPosition;
            }
            
            // Actualizar durante arrastre
            if (isDragging)
            {
                dragCurrentPosition = Input.mousePosition;
                Vector3 difference = dragStartPosition - dragCurrentPosition;
                
                if (difference.magnitude > 2) // Pequeño umbral para evitar movimientos accidentales
                {
                    // Convertir movimiento del ratón a dirección mundial
                    Vector3 dragDirection = new Vector3(difference.x, 0, difference.y) * 0.02f;
                    dragDirection = Quaternion.Euler(0, transform.eulerAngles.y, 0) * dragDirection;
                    
                    // Aplicar movimiento
                    cameraTargetPosition += dragDirection * edgeScrollSpeed * 2f;
                    
                    // Actualizar punto de inicio para el próximo frame
                    dragStartPosition = dragCurrentPosition;
                }
            }
            
            // Finalizar arrastre
            if (Input.GetMouseButtonUp((int)cameraDragKey - (int)KeyCode.Mouse0))
            {
                isDragging = false;
            }
        }
        
        private void UpdateCameraPosition(bool smooth)
        {
            // Calcular altura y distancia basadas en el factor de zoom
            float currentHeight = Mathf.Lerp(heightZoomRange.x, heightZoomRange.y, currentZoomFactor);
            float currentDistance = Mathf.Lerp(distanceZoomRange.x, distanceZoomRange.y, currentZoomFactor);
            
            // Ajustar ángulo basado en el factor de zoom para un efecto más MOBA
            float currentPitch = Mathf.Lerp(35f, 75f, currentZoomFactor); // Más bajo = más horizontal (35°), más alto = más inclinado (75°)
            
            // Calcular posición deseada usando parámetros actuales
            Vector3 directionFromTarget = new Vector3(0, 0, -1); // Dirección base (hacia -Z)
            directionFromTarget = Quaternion.Euler(0, transform.eulerAngles.y, 0) * directionFromTarget;
            
            // Aplicar altura y distancia actuales
            Vector3 desiredPosition = cameraTargetPosition;
            desiredPosition.y = 0; // Ignorar altura del target
            desiredPosition += directionFromTarget * currentDistance;
            desiredPosition.y = currentHeight;
            
            // Aplicar movimiento suave o inmediato
            if (smooth)
            {
                transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 10f);
            }
            else
            {
                transform.position = desiredPosition;
            }
            
            // Ajustar también el ángulo con el zoom
            transform.rotation = Quaternion.Lerp(transform.rotation, 
                                             Quaternion.Euler(currentPitch, transform.eulerAngles.y, 0),
                                             Time.deltaTime * 5f);
        }
        
        /// <summary>
        /// Método público para cambiar el objetivo
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            
            if (target != null)
            {
                Debug.Log($"[PhotonMOBACamera] Cámara ahora sigue a {target.name}");
                
                // Actualizar posición inmediatamente
                cameraTargetPosition = target.position;
                snapToPlayer = true;
            }
        }
        
        /// <summary>
        /// Método público para centrar en el jugador
        /// </summary>
        public void CenterOnPlayer()
        {
            if (target != null)
            {
                Debug.Log($"[PhotonMOBACamera] Centrando manualmente en {target.name}");
                snapToPlayer = true;
            }
        }
        
        /// <summary>
        /// Método para agregar efecto de cámara shake
        /// </summary>
        public void ShakeCamera(float intensity = 0.5f, float duration = 0.5f)
        {
            StartCoroutine(DoCameraShake(intensity, duration));
        }
        
        private IEnumerator DoCameraShake(float intensity, float duration)
        {
            Vector3 originalPosition = transform.position;
            Quaternion originalRotation = transform.rotation;
            
            float elapsed = 0.0f;
            
            while (elapsed < duration)
            {
                // Generar offset aleatorio de shake
                float x = Random.Range(-1f, 1f) * intensity;
                float y = Random.Range(-1f, 1f) * intensity * 0.5f; // Menos shake vertical
                float z = Random.Range(-1f, 1f) * intensity;
                
                // Aplicar offset a la posición
                transform.position = originalPosition + new Vector3(x, y, z);
                
                // También aplicar algo de shake rotacional
                float pitchShake = Random.Range(-1f, 1f) * intensity * 5f;
                float yawShake = Random.Range(-1f, 1f) * intensity * 5f;
                transform.rotation = originalRotation * Quaternion.Euler(pitchShake, yawShake, 0);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Volver a la posición y rotación originales
            transform.position = originalPosition;
            transform.rotation = originalRotation;
        }
    }
}