using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;
using Photon.Realtime;

namespace Photon.Pun.Demo.Asteroids
{
    [RequireComponent(typeof(HeroBase))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class HeroMovementController : MonoBehaviourPun, IPunObservable
    {
        [Header("Movement Settings")]
        public float rotationSpeed = 5f;
        public float targetDistance = 0.1f;
        
        [Header("Animation")]
        public Animator animator;
        public string moveSpeedParameter = "MoveSpeed";
        public string attackTrigger = "Attack";
        public string dieTrigger = "Die";
        public string respawnTrigger = "Respawn";
        
        [Header("Ground Check Settings")]
        public float groundCheckDistance = 1.0f;     // Distance to check below the character
        public LayerMask groundLayer;                // Layer(s) considered as ground
        
        [Header("Click Indicator")]
        public GameObject clickIndicatorPrefab;      // Opcional: Prefab del indicador de clic
        
        [Header("Debug")]
        public bool debugMode = false;
        
        // Referencias privadas
        private HeroBase heroBase;
        private HeroAnimationSync animSync;
        
        // Cambiado de privado a público para permitir acceso desde BuffAbility
        public NavMeshAgent navAgent;
        
        private Camera mainCamera;
        private Vector3 targetPosition;
        private bool isMoving = false;
        
        // Estados
        private bool isStunned = false;
        private bool isRooted = false;
        private bool isGrounded = true;
        
        // Variables para sincronización
        private Vector3 latestTargetPosition;
        private bool latestIsMoving;
        private float timeSinceLastPositionUpdate = 0f;
        private float positionUpdateInterval = 0.2f; // Actualizar cada 200ms
        private bool syncMadeRigidbodyKinematic = false; // Para sincronizar el estado del Rigidbody
        
        // Collider and Rigidbody references
        private Collider characterCollider;
        private Rigidbody characterRigidbody;
        
        // Ground check
        private Vector3 lastValidPosition;
        private float groundCheckTimer = 0f;
        private float groundCheckInterval = 0.05f; // Check 20 times per second
        private int failedGroundChecks = 0;
        private const int MAX_FAILED_CHECKS = 5;
        
        // Indicador de clic
        private ClickIndicator playerClickIndicator;
        
        void Awake()
        {
            // Obtener componentes
            heroBase = GetComponent<HeroBase>();
            navAgent = GetComponent<NavMeshAgent>();
            characterCollider = GetComponent<Collider>();
            characterRigidbody = GetComponent<Rigidbody>();
            
            if (animator == null)
            {
                animator = GetComponent<Animator>();
                if (animator == null)
                {
                    // Buscar animator en hijos si no está en el objeto principal
                    animator = GetComponentInChildren<Animator>();
                }
            }
            
            // Set the ground layer if not specified
            if (groundLayer.value == 0)
            {
                groundLayer = LayerMask.GetMask("Default", "Ground", "Terrain");
                
                if (debugMode)
                    Debug.Log($"Set groundLayer to {groundLayer.value}");
            }
            
            // Buscar HeroAnimationSync
            animSync = GetComponent<HeroAnimationSync>();
            if (animSync == null)
            {
                animSync = GetComponentInParent<HeroAnimationSync>();
            }
            
            // Debug message
            if (animator == null && photonView.IsMine)
            {
                Debug.LogWarning("No se encontró un componente Animator en " + gameObject.name + ". Las animaciones no funcionarán.");
            }
        }
        
        void Start()
        {
            // Make sure we configure Rigidbody for both local and remote players
            SafeConfigureRigidbody();
            
            // Ensure NavMeshAgent is assigned
            if (navAgent == null)
            {
                navAgent = GetComponent<NavMeshAgent>();
                Debug.Log($"[HeroMovementController] NavMeshAgent obtenido manualmente: {(navAgent != null ? "éxito" : "FALLO")}");
            }
            
            // Solo controlar si es el jugador local
            if (!photonView.IsMine)
            {
                // MODIFICADO: No desactivar completamente el NavMeshAgent para permitir colisiones
                // Configurar NavMeshAgent para clientes remotos (importante para colisiones)
                if (navAgent != null)
                {
                    // Mantener el NavMeshAgent habilitado, pero con algunas propiedades especiales
                    navAgent.updatePosition = false;   // No actualizar la posición basada en NavMesh
                    navAgent.updateRotation = false;   // No actualizar la rotación
                    navAgent.isStopped = true;         // Detener el movimiento automático
                    navAgent.updateUpAxis = false;     // No actualizar el eje Y
                    
                    // MODIFICADO: Permitir colisiones físicas activando rigidbody
                    if (characterRigidbody != null)
                    {
                        characterRigidbody.isKinematic = false;
                        characterRigidbody.detectCollisions = true;  // Asegurar que las colisiones sigan activas
                        characterRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
                    }
                    
                    Debug.Log($"[HeroMovementController] Cliente remoto - NavMeshAgent configurado en modo colisión para {gameObject.name}");
                }
                return;
            }
            
            // Este código solo se ejecuta para el jugador LOCAL
            
            // Asegurarse de que el NavMeshAgent está activado para el jugador local
            if (navAgent != null)
            {
                if (!navAgent.enabled)
                {
                    Debug.LogWarning($"[HeroMovementController] NavMeshAgent estaba desactivado para jugador local, activándolo ahora en {gameObject.name}");
                    navAgent.enabled = true;
                }
                
                // Asegurarse que está configurado correctamente para el jugador local
                navAgent.updatePosition = true;
                navAgent.updateRotation = true;
                navAgent.isStopped = false;
                navAgent.updateUpAxis = true;
                
                Debug.Log($"[HeroMovementController] Estado de NavMeshAgent para jugador local: {(navAgent.enabled ? "ACTIVO" : "INACTIVO")} - {gameObject.name}");
                
                // Verificar si está en un NavMesh válido
                if (!navAgent.isOnNavMesh)
                {
                    Debug.LogWarning($"[HeroMovementController] ¡ADVERTENCIA! NavMeshAgent no está en un NavMesh válido - {gameObject.name}");
                }
            }
            else
            {
                Debug.LogError($"[HeroMovementController] ¡ERROR CRÍTICO! NavMeshAgent es NULL para jugador local - {gameObject.name}");
            }
            
            // Obtener la cámara principal
            mainCamera = Camera.main;
            
            // Inicializar posición objetivo
            targetPosition = transform.position;
            latestTargetPosition = targetPosition;
            lastValidPosition = transform.position;
            
            // Ensure we start grounded
            CheckGrounded();
            
            // MODIFICADO: No hacemos kinematic el Rigidbody para permitir colisiones
            syncMadeRigidbodyKinematic = false;
            photonView.RPC("RPC_ForceKinematic", RpcTarget.Others, false);
            
            // Inicializar el indicador de clic (solo para el jugador local)
            InitializeClickIndicator();
            
            // Doble verificación después de 1 segundo
            Invoke("VerifyNavMeshAgentState", 1.0f);
        }
        
        /// <summary>
        /// Verifica el estado del NavMeshAgent después de un pequeño retraso
        /// </summary>
        private void VerifyNavMeshAgentState()
        {
            if (!photonView.IsMine) return;
            
            if (navAgent != null)
            {
                if (!navAgent.enabled)
                {
                    Debug.LogWarning($"[HeroMovementController] NavMeshAgent se desactivó después de Start, reactivándolo - {gameObject.name}");
                    navAgent.enabled = true;
                }
                
                // Verificar si está en un NavMesh válido
                if (!navAgent.isOnNavMesh)
                {
                    Debug.LogWarning($"[HeroMovementController] Después de 1s, NavMeshAgent sigue sin estar en NavMesh válido - {gameObject.name}");
                    
                    // Intentar warp a una posición cercana
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(transform.position, out hit, 5f, NavMesh.AllAreas))
                    {
                        Debug.Log($"[HeroMovementController] Intentando warp a posición válida en NavMesh - {gameObject.name}");
                        navAgent.Warp(hit.position);
                    }
                }
            }
        }
        
        /// <summary>
        /// Inicializa el indicador de clic para este jugador
        /// </summary>
        private void InitializeClickIndicator()
        {
            // Si tenemos un prefab, instanciarlo
            if (clickIndicatorPrefab != null)
            {
                GameObject indicatorObj = Instantiate(clickIndicatorPrefab);
                playerClickIndicator = indicatorObj.GetComponent<ClickIndicator>();
                
                // Si el objeto no tiene el componente, añadirlo
                if (playerClickIndicator == null)
                {
                    playerClickIndicator = indicatorObj.AddComponent<ClickIndicator>();
                }
            }
            else
            {
                // Crear un indicador básico si no hay prefab
                GameObject indicatorObj = new GameObject("ClickIndicator");
                playerClickIndicator = indicatorObj.AddComponent<ClickIndicator>();
            }
            
            // Configurar el color según el equipo
            if (playerClickIndicator != null && heroBase != null)
            {
                // Definir color según el equipo (rojo o azul)
                Color teamColor = (heroBase.teamId == 0) ? 
                    new Color(1f, 0.3f, 0.3f, 0.5f) : // Rojo para equipo 0
                    new Color(0.3f, 0.5f, 1f, 0.5f);  // Azul para equipo 1
                
                // Si el campo es privado, usar reflection para acceder a él
                var colorField = typeof(ClickIndicator).GetField("indicatorColor", 
                    System.Reflection.BindingFlags.Instance | 
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Public);
                    
                if (colorField != null)
                {
                    colorField.SetValue(playerClickIndicator, teamColor);
                }
            }
            
            // Inicialmente desactivado
            if (playerClickIndicator != null)
            {
                playerClickIndicator.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Configure Rigidbody for stability without changing NavMeshAgent settings
        /// </summary>
        private void SafeConfigureRigidbody()
        {
            if (characterRigidbody != null)
            {
                // Make sure the Rigidbody has these critical settings
                characterRigidbody.freezeRotation = true; // Prevent tipping over
                characterRigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ; // Free Y rotation
                
                // MODIFICADO: Permitir colisiones físicas estableciendo isKinematic = false
                characterRigidbody.isKinematic = false;
                characterRigidbody.velocity = Vector3.zero;
                
                // Also increase collision detection quality
                characterRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
                
                // Add some drag to prevent sliding
                characterRigidbody.drag = 5f;
                characterRigidbody.angularDrag = 0.5f;
                
                // Permitir detección de colisiones
                characterRigidbody.detectCollisions = true;
                
                if (debugMode)
                {
                    Debug.Log($"Rigidbody configurado: isKinematic=false, " +
                             $"constraints={characterRigidbody.constraints}, " +
                             $"detection={characterRigidbody.collisionDetectionMode}");
                }
            }
        }
        
        void Update()
        {
            // Actualizar sincronización para jugadores remotos
            if (!photonView.IsMine)
            {
                UpdateRemotePlayer();
                return;
            }
            
            // No mover si está aturdido o muerto
            if (isStunned || heroBase.IsDead)
                return;
                
            // Regular ground checking
            groundCheckTimer += Time.deltaTime;
            if (groundCheckTimer >= groundCheckInterval)
            {
                groundCheckTimer = 0f;
                CheckGrounded();
            }
            
            // Leer entrada para movimiento
            HandleMovementInput();
            
            // Actualizar animaciones
            UpdateAnimation();
            
            // Ensure Rigidbody is kinematic when not moving
            EnsureKinematicWhenStill();
            
            // If we're not on ground, try to recover
            if (!isGrounded)
            {
                RecoverFromFalling();
            }
            
            // Test de animaciones (solo para debug)
            if (debugMode && Input.GetKeyDown(KeyCode.T))
            {
                TestAnimations();
            }
            
            // Actualizar datos para sincronización
            timeSinceLastPositionUpdate += Time.deltaTime;
            if (timeSinceLastPositionUpdate >= positionUpdateInterval)
            {
                timeSinceLastPositionUpdate = 0;
                latestTargetPosition = targetPosition;
                latestIsMoving = isMoving;
            }
        }
        
        void LateUpdate()
        {
            // Apply additional safety check to ensure the hero stays on the NavMesh
            if (photonView.IsMine && navAgent.enabled && !navAgent.isOnNavMesh)
            {
                TryRecoverNavMeshPosition();
            }
        }
        
        /// <summary>
        /// Ensures the Rigidbody is properly configured when not moving
        /// </summary>
        private void EnsureKinematicWhenStill()
        {
            if (characterRigidbody != null && !isMoving)
            {
                // MODIFICADO: Mantenemos isKinematic=false para permitir colisiones
                // pero minimizamos el efecto de la física en el personaje
                if (characterRigidbody.isKinematic)
                {
                    characterRigidbody.isKinematic = false;
                    characterRigidbody.velocity = Vector3.zero;
                    
                    // Sync with other clients
                    if (syncMadeRigidbodyKinematic != characterRigidbody.isKinematic)
                    {
                        syncMadeRigidbodyKinematic = characterRigidbody.isKinematic;
                        photonView.RPC("RPC_ForceKinematic", RpcTarget.Others, false);
                    }
                }
                
                // Reducir la velocidad a cero cuando no se mueve
                characterRigidbody.velocity = Vector3.zero;
            }
        }
        
        /// <summary>
        /// Prepares the Rigidbody for movement if needed
        /// </summary>
        private void PrepareRigidbodyForMovement()
        {
            if (characterRigidbody != null && isMoving)
            {
                // MODIFICADO: Mantenemos isKinematic=false para permitir colisiones
                // El NavMeshAgent moverá el personaje y las colisiones físicas seguirán funcionando
                characterRigidbody.isKinematic = false;
                characterRigidbody.velocity = Vector3.zero; // Reset velocity
                
                // Sync with other clients
                if (syncMadeRigidbodyKinematic != characterRigidbody.isKinematic)
                {
                    syncMadeRigidbodyKinematic = characterRigidbody.isKinematic;
                    photonView.RPC("RPC_ForceKinematic", RpcTarget.Others, false);
                }
            }
        }
        
        /// <summary>
        /// Checks if the character is grounded
        /// </summary>
        private void CheckGrounded()
        {
            if (!photonView.IsMine) return;
            
            // Calculate ray origin (bottom center of collider)
            Vector3 rayOrigin = transform.position;
            if (characterCollider != null)
            {
                // Adjust based on collider height
                rayOrigin = transform.position + Vector3.up * 0.1f; // Slightly above ground
            }
            
            // Cast a ray downward to check for ground
            bool wasGrounded = isGrounded;
            isGrounded = Physics.Raycast(rayOrigin, Vector3.down, groundCheckDistance, groundLayer);
            
            // If we're grounded, store the position as valid
            if (isGrounded && navAgent.enabled && navAgent.isOnNavMesh)
            {
                lastValidPosition = transform.position;
                failedGroundChecks = 0; // Reset counter
                
                // If we've just become grounded after being in the air
                if (!wasGrounded && debugMode)
                {
                    Debug.Log($"Hero regained ground contact at {transform.position}");
                }
            }
            else if (!isGrounded)
            {
                // Track consecutive failed checks
                failedGroundChecks++;
                
                if (wasGrounded && debugMode)
                {
                    Debug.LogWarning($"Hero lost ground contact at {transform.position}");
                }
                
                // If we've had too many failed checks in a row, take action
                if (failedGroundChecks >= MAX_FAILED_CHECKS)
                {
                    if (debugMode)
                    {
                        Debug.LogWarning($"Multiple ground check failures detected - taking action");
                    }
                    
                    RecoverFromFalling();
                }
            }
            
            // Debug visualization
            if (debugMode)
            {
                Debug.DrawRay(rayOrigin, Vector3.down * groundCheckDistance, isGrounded ? Color.green : Color.red, 0.1f);
            }
        }
        
        /// <summary>
        /// Tries to recover when the hero is falling or not on valid ground
        /// </summary>
        private void RecoverFromFalling()
        {
            if (!photonView.IsMine) return;
            
            // If we've fallen below a certain threshold or have too many failed checks
            if (transform.position.y < lastValidPosition.y - 2.0f || failedGroundChecks >= MAX_FAILED_CHECKS)
            {
                if (debugMode)
                {
                    Debug.LogWarning($"Hero falling detected, teleporting back to {lastValidPosition}");
                }
                
                // Try to use the NavMeshAgent to warp if possible
                if (navAgent.enabled)
                {
                    if (navAgent.isOnNavMesh)
                    {
                        navAgent.Warp(lastValidPosition);
                        
                        // Sync position with all clients through RPC
                        photonView.RPC("RPC_SyncPosition", RpcTarget.Others, lastValidPosition);
                        
                        // Reset failed checks after recovery
                        failedGroundChecks = 0;
                    }
                    else
                    {
                        TryRecoverNavMeshPosition();
                    }
                }
                else
                {
                    // Direct teleport if NavMeshAgent is not available
                    transform.position = lastValidPosition;
                    
                    // Sync position with all clients through RPC
                    photonView.RPC("RPC_SyncPosition", RpcTarget.Others, lastValidPosition);
                    
                    failedGroundChecks = 0;
                }
                
                // Force the Rigidbody to stop falling
                if (characterRigidbody != null)
                {
                    characterRigidbody.velocity = Vector3.zero;
                    characterRigidbody.isKinematic = false;
                    
                    // Sync with other clients
                    syncMadeRigidbodyKinematic = false;
                    photonView.RPC("RPC_ForceKinematic", RpcTarget.Others, false);
                }
            }
        }
        
        /// <summary>
        /// Coroutine to restore Rigidbody state after a brief delay
        /// </summary>
        private System.Collections.IEnumerator RestoreRigidbodyState(bool originalState)
        {
            // Wait a moment for the character to settle
            yield return new WaitForSeconds(0.2f);
            
            // Only restore non-kinematic state if we're safely grounded
            if (isGrounded && characterRigidbody != null)
            {
                characterRigidbody.isKinematic = false;
                
                // Sync with other clients
                syncMadeRigidbodyKinematic = false;
                photonView.RPC("RPC_ForceKinematic", RpcTarget.Others, false);
                
                if (debugMode)
                {
                    Debug.Log($"Restaurado Rigidbody no-kinematic para permitir colisiones");
                }
            }
        }
        
        /// <summary>
        /// Try to recover and place the agent back on NavMesh when it loses contact
        /// </summary>
        private void TryRecoverNavMeshPosition()
        {
            // First try to find nearest position on NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 10f, NavMesh.AllAreas))
            {
                // If we got a valid position, try to teleport there
                if (navAgent.enabled)
                {
                    if (!navAgent.isOnNavMesh)
                    {
                        // Can't warp directly, need to disable and re-enable the agent
                        navAgent.enabled = false;
                        transform.position = hit.position;
                        
                        // Sync position with all clients
                        photonView.RPC("RPC_SyncPosition", RpcTarget.Others, hit.position);
                        
                        navAgent.enabled = true;
                        
                        // Reset failed checks counter
                        failedGroundChecks = 0;
                        
                        if (debugMode)
                            Debug.Log($"Recovered hero position to NavMesh at {hit.position}");
                    }
                    else
                    {
                        navAgent.Warp(hit.position);
                        
                        // Sync position with all clients
                        photonView.RPC("RPC_SyncPosition", RpcTarget.Others, hit.position);
                    }
                }
                else
                {
                    transform.position = hit.position;
                    
                    // Sync position with all clients
                    photonView.RPC("RPC_SyncPosition", RpcTarget.Others, hit.position);
                }
            }
            else
            {
                // If we can't find NavMesh nearby, use last valid position
                if (navAgent.enabled)
                {
                    navAgent.enabled = false;
                    transform.position = lastValidPosition;
                    
                    // Sync position with all clients
                    photonView.RPC("RPC_SyncPosition", RpcTarget.Others, lastValidPosition);
                    
                    navAgent.enabled = true;
                    
                    // Reset failed checks counter
                    failedGroundChecks = 0;
                    
                    if (debugMode)
                        Debug.Log($"Teleported hero back to last valid position: {lastValidPosition}");
                }
                else
                {
                    transform.position = lastValidPosition;
                    
                    // Sync position with all clients
                    photonView.RPC("RPC_SyncPosition", RpcTarget.Others, lastValidPosition);
                }
            }
            
            // Make sure character rigidbody velocity is zeroed out
            if (characterRigidbody != null)
            {
                characterRigidbody.velocity = Vector3.zero;
                characterRigidbody.isKinematic = false;
                
                // Sync with other clients
                syncMadeRigidbodyKinematic = false;
                photonView.RPC("RPC_ForceKinematic", RpcTarget.Others, false);
            }
        }
        
        /// <summary>
        /// Maneja la entrada para movimiento (click derecho para movimiento estilo MOBA)
        /// </summary>
        private void HandleMovementInput()
        {
            if (!photonView.IsMine || isStunned || isRooted) return;
            
            // No mover si está atacando
            if (IsAttacking())
            {
                return;
            }
            
            // Click derecho para mover
            if (Input.GetMouseButtonDown(1))
            {
                Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                
                // Raycast al terreno
                if (Physics.Raycast(ray, out hit, 100f, groundLayer))
                {
                    // Si no está enraizado, establecer destino
                    if (!isRooted)
                    {
                        targetPosition = hit.point;
                        
                        // Ensure NavMeshAgent is on NavMesh before setting destination
                        if (navAgent.enabled && navAgent.isOnNavMesh)
                        {
                            navAgent.SetDestination(targetPosition);
                            isMoving = true;
                            
                            // Prepare Rigidbody for movement
                            PrepareRigidbodyForMovement();
                            
                            // Send moving state to all
                            photonView.RPC("RPC_SetMovingState", RpcTarget.Others, true);
                            
                            // Mostrar indicador de clic
                            if (playerClickIndicator != null)
                            {
                                playerClickIndicator.ShowAt(hit.point);
                            }
                        }
                        else
                        {
                            // Try to recover NavMesh position first
                            TryRecoverNavMeshPosition();
                            
                            // Then set destination if successful
                            if (navAgent.enabled && navAgent.isOnNavMesh)
                            {
                                navAgent.SetDestination(targetPosition);
                                isMoving = true;
                                
                                // Prepare Rigidbody for movement
                                PrepareRigidbodyForMovement();
                                
                                // Send moving state to all
                                photonView.RPC("RPC_SetMovingState", RpcTarget.Others, true);
                                
                                // Mostrar indicador de clic
                                if (playerClickIndicator != null)
                                {
                                    playerClickIndicator.ShowAt(hit.point);
                                }
                            }
                        }
                    }
                }
            }
            
            // Verificar si hemos llegado al destino
            if (isMoving && navAgent.enabled && !navAgent.pathPending)
            {
                if (navAgent.remainingDistance <= navAgent.stoppingDistance)
                {
                    if (!navAgent.hasPath || navAgent.velocity.sqrMagnitude < 0.1f)
                    {
                        bool wasMoving = isMoving;
                        isMoving = false;
                        
                        // When we stop, store position and make sure we're grounded
                        if (navAgent.isOnNavMesh)
                        {
                            lastValidPosition = transform.position;
                        }
                        
                        // Make Rigidbody kinematic when stopping
                        if (characterRigidbody != null)
                        {
                            characterRigidbody.velocity = Vector3.zero;
                            characterRigidbody.isKinematic = true;
                            
                            // Sync with other clients
                            syncMadeRigidbodyKinematic = true;
                            photonView.RPC("RPC_ForceKinematic", RpcTarget.Others, true);
                        }
                        
                        // Only send RPC if we were moving before
                        if (wasMoving)
                        {
                            // Send moving state to all
                            photonView.RPC("RPC_SetMovingState", RpcTarget.Others, false);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Actualiza las animaciones del héroe según el movimiento
        /// </summary>
        private void UpdateAnimation()
        {
            if (animator != null)
            {
                // Calcular velocidad normalizada para la animación (0-1)
                float moveSpeed = 0f;
                
                if (navAgent.enabled && navAgent.isOnNavMesh)
                {
                    moveSpeed = navAgent.velocity.magnitude / navAgent.speed;
                }
                
                // Para evitar pequeñas fluctuaciones, redondea valores muy pequeños a cero
                if (moveSpeed < 0.05f) moveSpeed = 0f;
                
                animator.SetFloat(moveSpeedParameter, moveSpeed);
                
                // Debug opcional
                if (debugMode && photonView.IsMine && Time.frameCount % 60 == 0)
                {
                    Debug.Log($"MoveSpeed: {moveSpeed}, Velocity: {navAgent.velocity.magnitude}, Grounded: {isGrounded}");
                }
            }
        }
        
        /// <summary>
        /// Método para probar animaciones manualmente (activado con tecla T si debugMode es true)
        /// </summary>
        private void TestAnimations()
        {
            PlayAttackAnimation();
            Debug.Log("Probando animación de ataque...");
        }
        
        /// <summary>
        /// Actualiza el jugador remoto basado en datos recibidos
        /// </summary>
        private void UpdateRemotePlayer()
        {
            // Actualizar animación basada en si está moviéndose
            if (animator != null)
            {
                // Calcular velocidad aproximada basada en la distancia al destino
                float distance = Vector3.Distance(transform.position, latestTargetPosition);
                float approxMoveSpeed = latestIsMoving && distance > 0.1f ? 1.0f : 0f;
                
                animator.SetFloat(moveSpeedParameter, approxMoveSpeed);
            }
            
            // Mover suavemente hacia la posición objetivo
            if (latestIsMoving)
            {
                transform.position = Vector3.Lerp(transform.position, latestTargetPosition, Time.deltaTime * 5f);
                
                // Rotar hacia la dirección de movimiento
                if ((latestTargetPosition - transform.position).sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(latestTargetPosition - transform.position);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
                }
            }
            
            // MODIFICADO: Permitir colisiones físicas en clientes remotos
            if (characterRigidbody != null && characterRigidbody.isKinematic)
            {
                characterRigidbody.isKinematic = false;
                characterRigidbody.detectCollisions = true;
            }
        }
        
        /// <summary>
        /// Establece el destino del héroe (para uso de habilidades o IA)
        /// </summary>
        public void SetDestination(Vector3 destination)
        {
            // Solo aplicar si es el jugador local y no está enraizado
            if (photonView.IsMine && !isRooted)
            {
                targetPosition = destination;
                
                // Check NavMeshAgent status before setting destination
                if (navAgent.enabled && navAgent.isOnNavMesh)
                {
                    navAgent.SetDestination(targetPosition);
                    isMoving = true;
                    
                    // Prepare Rigidbody for movement
                    PrepareRigidbodyForMovement();
                    
                    // Send moving state to all
                    photonView.RPC("RPC_SetMovingState", RpcTarget.Others, true);
                    
                    // Mostrar indicador de clic
                    if (playerClickIndicator != null)
                    {
                        playerClickIndicator.ShowAt(destination);
                    }
                }
                else
                {
                    // Try to recover first
                    TryRecoverNavMeshPosition();
                    
                    if (navAgent.enabled && navAgent.isOnNavMesh)
                    {
                        navAgent.SetDestination(targetPosition);
                        isMoving = true;
                        
                        // Prepare Rigidbody for movement
                        PrepareRigidbodyForMovement();
                        
                        // Send moving state to all
                        photonView.RPC("RPC_SetMovingState", RpcTarget.Others, true);
                        
                        // Mostrar indicador de clic
                        if (playerClickIndicator != null)
                        {
                            playerClickIndicator.ShowAt(destination);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Detiene el movimiento del héroe
        /// </summary>
        public void StopMovement()
        {
            if (!photonView.IsMine) return;
            
            // Detener el NavMeshAgent
            if (navAgent != null && navAgent.enabled)
            {
                navAgent.isStopped = true;
                navAgent.velocity = Vector3.zero;
                navAgent.ResetPath();
                Debug.Log($"[HeroMovementController] Deteniendo movimiento de {gameObject.name}");
            }
            
            // Actualizar estado de movimiento
            isMoving = false;
            
            // Notificar a otros clientes
            photonView.RPC("RPC_SetMovingState", RpcTarget.Others, false);
            
            // Actualizar animación
            if (animator != null)
            {
                animator.SetFloat(moveSpeedParameter, 0f);
            }
        }
        
        /// <summary>
        /// Reproduce la animación de ataque
        /// </summary>
        public void PlayAttackAnimation()
        {
            if (animSync != null)
            {
                animSync.TriggerAnimationForAll(attackTrigger);
            }
            else if (animator != null)
            {
                animator.SetTrigger(attackTrigger);
            }
        }
        
        /// <summary>
        /// Reproduce la animación de muerte
        /// </summary>
        public void PlayDeathAnimation()
        {
            if (animSync != null)
            {
                animSync.TriggerAnimationForAll(dieTrigger);
            }
            else if (animator != null)
            {
                animator.SetTrigger(dieTrigger);
            }
        }
        
        /// <summary>
        /// Reproduce la animación de respawn
        /// </summary>
        public void PlayRespawnAnimation()
        {
            if (animSync != null)
            {
                animSync.TriggerAnimationForAll(respawnTrigger);
            }
            else if (animator != null)
            {
                animator.SetTrigger(respawnTrigger);
            }
        }
        
        /// <summary>
        /// Aplica un efecto de aturdimiento al héroe
        /// </summary>
        public void ApplyStun(float duration)
        {
            // Sincronizar con todos los clientes
            photonView.RPC("RPC_ApplyStun", RpcTarget.All, duration);
        }
        
        /// <summary>
        /// Aplica un efecto de enraizamiento al héroe
        /// </summary>
        public void ApplyRoot(float duration)
        {
            // Sincronizar con todos los clientes
            photonView.RPC("RPC_ApplyRoot", RpcTarget.All, duration);
        }
        
        /// <summary>
        /// Implementación de IPunObservable
        /// </summary>
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // Datos que se envían al resto de jugadores
                stream.SendNext(latestTargetPosition);
                stream.SendNext(latestIsMoving);
                stream.SendNext(isStunned);
                stream.SendNext(isRooted);
                stream.SendNext(isGrounded);
                
                // Send Rigidbody kinematic state for better sync
                stream.SendNext(characterRigidbody != null ? characterRigidbody.isKinematic : true);
                
                // Send position for better sync
                stream.SendNext(transform.position);
            }
            else
            {
                // Datos que se reciben de otros jugadores
                latestTargetPosition = (Vector3)stream.ReceiveNext();
                latestIsMoving = (bool)stream.ReceiveNext();
                isStunned = (bool)stream.ReceiveNext();
                isRooted = (bool)stream.ReceiveNext();
                isGrounded = (bool)stream.ReceiveNext();
                
                // Receive Rigidbody kinematic state
                bool remoteIsKinematic = (bool)stream.ReceiveNext();
                if (characterRigidbody != null && !remoteIsKinematic)
                {
                    // Remote player's rigidbody is non-kinematic, but we'll keep ours kinematic
                    // to prevent falling on the client side
                    characterRigidbody.isKinematic = true;
                }
                
                // Receive precise position updates
                Vector3 precisePosition = (Vector3)stream.ReceiveNext();
                
                // If not moving or difference is significant, teleport
                if (!latestIsMoving || Vector3.Distance(transform.position, precisePosition) > 2.0f)
                {
                    transform.position = precisePosition;
                }
            }
        }
        
        #region PHOTON RPC
        
        [PunRPC]
        private void RPC_SetMovingState(bool moving)
        {
            // This is called on remote clients to sync movement state
            latestIsMoving = moving;
            
            if (!moving && characterRigidbody != null)
            {
                // MODIFICADO: Mantener isKinematic=false para permitir colisiones pero detener movimiento
                characterRigidbody.velocity = Vector3.zero;
                characterRigidbody.isKinematic = false;
            }
        }
        
        [PunRPC]
        private void RPC_ForceKinematic(bool kinematic)
        {
            // MODIFICADO: Ignorar valores true, siempre mantener isKinematic=false para permitir colisiones
            if (characterRigidbody != null)
            {
                characterRigidbody.velocity = Vector3.zero;
                characterRigidbody.isKinematic = false; // Siempre false para asegurar colisiones
                characterRigidbody.detectCollisions = true;
                
                if (debugMode)
                {
                    Debug.Log($"RPC_ForceKinematic: Manteniendo Rigidbody.isKinematic = false para permitir colisiones");
                }
            }
        }
        
        [PunRPC]
        private void RPC_SyncPosition(Vector3 position)
        {
            // Instantly update position on remote clients
            transform.position = position;
            
            // Also update our lastValidPosition
            lastValidPosition = position;
            
            // Reset velocity and ensure kinematic is false
            if (characterRigidbody != null)
            {
                characterRigidbody.velocity = Vector3.zero;
                characterRigidbody.isKinematic = false;
            }
            
            if (debugMode)
            {
                Debug.Log($"RPC_SyncPosition: Teleported to {position}");
            }
        }
        
        [PunRPC]
        private void RPC_ApplyStun(float duration)
        {
            // Aplicar aturdimiento
            isStunned = true;
            
            // Detener movimiento
            if (photonView.IsMine)
            {
                if (navAgent.enabled && navAgent.isOnNavMesh)
                {
                    navAgent.ResetPath();
                }
                isMoving = false;
                
                // MODIFICADO: Mantener isKinematic=false para permitir colisiones
                if (characterRigidbody != null)
                {
                    characterRigidbody.velocity = Vector3.zero;
                    characterRigidbody.isKinematic = false;
                    
                    // Sync with other clients
                    syncMadeRigidbodyKinematic = false;
                    photonView.RPC("RPC_ForceKinematic", RpcTarget.Others, false);
                }
                
                // Update the remote movement state too
                photonView.RPC("RPC_SetMovingState", RpcTarget.Others, false);
            }
            else
            {
                // MODIFICADO: Mantener isKinematic=false para clientes remotos
                if (characterRigidbody != null)
                {
                    characterRigidbody.velocity = Vector3.zero;
                    characterRigidbody.isKinematic = false;
                }
                
                // Update local state
                latestIsMoving = false;
            }
            
            // Mostrar efectos visuales de aturdimiento (puedes añadir aquí)
            
            // Programar remoción del aturdimiento
            CancelInvoke("RemoveStun");
            Invoke("RemoveStun", duration);
        }
        
        private void RemoveStun()
        {
            isStunned = false;
        }
        
        [PunRPC]
        private void RPC_ApplyRoot(float duration)
        {
            // Aplicar enraizamiento
            isRooted = true;
            
            // Detener movimiento
            if (photonView.IsMine)
            {
                if (navAgent.enabled && navAgent.isOnNavMesh)
                {
                    navAgent.ResetPath();
                }
                isMoving = false;
                
                // MODIFICADO: Mantener isKinematic=false para permitir colisiones
                if (characterRigidbody != null)
                {
                    characterRigidbody.velocity = Vector3.zero;
                    characterRigidbody.isKinematic = false;
                    
                    // Sync with other clients
                    syncMadeRigidbodyKinematic = false;
                    photonView.RPC("RPC_ForceKinematic", RpcTarget.Others, false);
                }
                
                // Update the remote movement state too
                photonView.RPC("RPC_SetMovingState", RpcTarget.Others, false);
            }
            else
            {
                // MODIFICADO: Mantener isKinematic=false para clientes remotos
                if (characterRigidbody != null)
                {
                    characterRigidbody.velocity = Vector3.zero;
                    characterRigidbody.isKinematic = false;
                }
                
                // Update local state
                latestIsMoving = false;
            }
            
            // Mostrar efectos visuales de enraizamiento (puedes añadir aquí)
            
            // Programar remoción del enraizamiento
            CancelInvoke("RemoveRoot");
            Invoke("RemoveRoot", duration);
        }
        
        private void RemoveRoot()
        {
            isRooted = false;
        }
        
        #endregion
        
        void OnDestroy()
        {
            // Clean up
            CancelInvoke();
            StopAllCoroutines();
            
            // Destruir el indicador de clic
            if (playerClickIndicator != null)
            {
                Destroy(playerClickIndicator.gameObject);
            }
        }
        
        /// <summary>
        /// Visualización de debug en el editor
        /// </summary>
        void OnDrawGizmos()
        {
            if (navAgent != null && navAgent.enabled && Application.isPlaying)
            {
                // Dibuja una esfera en el punto de destino
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(navAgent.destination, 0.3f);
                
                // Dibuja una línea desde la posición actual hasta el destino
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, navAgent.destination);
                
                // Draw the last valid position
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(lastValidPosition, 0.2f);
                
                // Draw ground check ray
                Gizmos.color = isGrounded ? Color.green : Color.red;
                Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
                Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * groundCheckDistance);
            }
        }

        private bool IsAttacking()
        {
            BasicAttackController attackController = GetComponent<BasicAttackController>();
            if (attackController != null)
            {
                return !attackController.CanAttack();
            }
            return false;
        }

        public void ResetMovement()
        {
            if (!photonView.IsMine) return;

            // Detener el movimiento actual
            if (navAgent != null)
            {
                navAgent.ResetPath();
                navAgent.velocity = Vector3.zero;
                navAgent.isStopped = true;
            }
            
            // Resetear variables de movimiento
            isMoving = false;
            targetPosition = transform.position;
            latestTargetPosition = transform.position;
            
            // Actualizar animación
            if (animator != null)
            {
                animator.SetFloat(moveSpeedParameter, 0f);
            }
            
            // Notificar a otros clientes
            photonView.RPC("RPC_SetMovingState", RpcTarget.Others, false);
            
            // Ocultar indicador de clic si existe
            if (playerClickIndicator != null)
            {
                playerClickIndicator.HideIndicator();
            }
            
            if (debugMode)
            {
                Debug.Log($"[HeroMovementController] Movimiento reseteado para {gameObject.name}");
            }
        }
    }
}