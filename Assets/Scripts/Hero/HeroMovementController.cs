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
        private bool syncMadeRigidbodyKinematic = true; // Para sincronizar el estado del Rigidbody
        
        // Collider and Rigidbody references
        private Collider characterCollider;
        private Rigidbody characterRigidbody;
        
        // Ground check
        private Vector3 lastValidPosition;
        private float groundCheckTimer = 0f;
        private float groundCheckInterval = 0.05f; // Check 20 times per second
        private int failedGroundChecks = 0;
        private const int MAX_FAILED_CHECKS = 5;
        
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
            
            // Solo controlar si es el jugador local
            if (!photonView.IsMine)
            {
                // Desactivar el NavMeshAgent para jugadores remotos
                navAgent.enabled = false;
                return;
            }
            
            // Obtener la cámara principal
            mainCamera = Camera.main;
            
            // Inicializar posición objetivo
            targetPosition = transform.position;
            latestTargetPosition = targetPosition;
            lastValidPosition = transform.position;
            
            // Ensure we start grounded
            CheckGrounded();
            
            // Sync initial state
            syncMadeRigidbodyKinematic = true;
            photonView.RPC("RPC_ForceKinematic", RpcTarget.Others, true);
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
                characterRigidbody.constraints = RigidbodyConstraints.FreezeRotation; // Another way to freeze rotation
                
                // Start with kinematic enabled to prevent initial falling
                characterRigidbody.isKinematic = true;
                characterRigidbody.velocity = Vector3.zero;
                
                // Also increase collision detection quality
                characterRigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
                
                // Add some drag to prevent sliding
                characterRigidbody.drag = 5f;
                
                if (debugMode)
                {
                    Debug.Log($"Rigidbody configured: isKinematic=true, " +
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
        /// Ensures the Rigidbody is kinematic when not moving to prevent falling
        /// </summary>
        private void EnsureKinematicWhenStill()
        {
            if (characterRigidbody != null && !isMoving)
            {
                // When not moving, force kinematic to prevent falling
                if (!characterRigidbody.isKinematic)
                {
                    if (debugMode)
                        Debug.Log("Setting Rigidbody to kinematic while standing still");
                        
                    characterRigidbody.velocity = Vector3.zero;
                    characterRigidbody.isKinematic = true;
                    
                    // Send RPC to all other clients to make their Rigidbody kinematic too
                    if (syncMadeRigidbodyKinematic != characterRigidbody.isKinematic)
                    {
                        syncMadeRigidbodyKinematic = characterRigidbody.isKinematic;
                        photonView.RPC("RPC_ForceKinematic", RpcTarget.Others, true);
                    }
                }
            }
        }
        
        /// <summary>
        /// Prepares the Rigidbody for movement if needed
        /// </summary>
        private void PrepareRigidbodyForMovement()
        {
            if (characterRigidbody != null && isMoving)
            {
                // For this implementation, we keep the Rigidbody kinematic even during movement
                // The NavMeshAgent will handle movement.
                characterRigidbody.isKinematic = true;
                characterRigidbody.velocity = Vector3.zero;
                
                // Sync with other clients
                if (syncMadeRigidbodyKinematic != characterRigidbody.isKinematic)
                {
                    syncMadeRigidbodyKinematic = characterRigidbody.isKinematic;
                    photonView.RPC("RPC_ForceKinematic", RpcTarget.Others, true);
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
                    characterRigidbody.isKinematic = true;
                    
                    // Sync with other clients
                    syncMadeRigidbodyKinematic = true;
                    photonView.RPC("RPC_ForceKinematic", RpcTarget.Others, true);
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
                // For this implementation, we'll keep it kinematic to be safe
                characterRigidbody.isKinematic = true;
                
                // Sync with other clients
                syncMadeRigidbodyKinematic = true;
                photonView.RPC("RPC_ForceKinematic", RpcTarget.Others, true);
                
                if (debugMode)
                {
                    Debug.Log($"Kept Rigidbody kinematic for stability");
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
                characterRigidbody.isKinematic = true;
                
                // Sync with other clients
                syncMadeRigidbodyKinematic = true;
                photonView.RPC("RPC_ForceKinematic", RpcTarget.Others, true);
            }
        }
        
        /// <summary>
        /// Maneja la entrada para movimiento (click derecho para movimiento estilo MOBA)
        /// </summary>
        private void HandleMovementInput()
        {
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
            
            // IMPORTANT: Keep the remote character's Rigidbody kinematic to prevent falling
            if (characterRigidbody != null && !characterRigidbody.isKinematic)
            {
                characterRigidbody.velocity = Vector3.zero;
                characterRigidbody.isKinematic = true;
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
                    }
                }
            }
        }
        
        /// <summary>
        /// Detiene el movimiento del héroe
        /// </summary>
        public void StopMovement()
        {
            // Solo aplicar si es el jugador local
            if (photonView.IsMine)
            {
                bool wasMoving = isMoving;
                
                if (navAgent.enabled && navAgent.isOnNavMesh)
                {
                    navAgent.ResetPath();
                }
                
                isMoving = false;
                
                // Actualizar animación
                if (animator != null)
                {
                    animator.SetFloat(moveSpeedParameter, 0f);
                }
                
                // Store position when we stop
                lastValidPosition = transform.position;
                
                // Force kinematic when stopping
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
                // When stopping, force kinematic mode
                characterRigidbody.velocity = Vector3.zero;
                characterRigidbody.isKinematic = true;
            }
        }
        
        [PunRPC]
        private void RPC_ForceKinematic(bool kinematic)
        {
            // Force Rigidbody to be kinematic on remote clients
            if (characterRigidbody != null)
            {
                characterRigidbody.velocity = Vector3.zero;
                characterRigidbody.isKinematic = kinematic;
                
                if (debugMode)
                {
                    Debug.Log($"RPC_ForceKinematic: Set Rigidbody.isKinematic = {kinematic}");
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
            
            // Reset velocity and ensure kinematic
            if (characterRigidbody != null)
            {
                characterRigidbody.velocity = Vector3.zero;
                characterRigidbody.isKinematic = true;
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
                
                // Make Rigidbody kinematic when stunned
                if (characterRigidbody != null)
                {
                    characterRigidbody.velocity = Vector3.zero;
                    characterRigidbody.isKinematic = true;
                    
                    // Sync with other clients
                    syncMadeRigidbodyKinematic = true;
                    photonView.RPC("RPC_ForceKinematic", RpcTarget.Others, true);
                }
                
                // Update the remote movement state too
                photonView.RPC("RPC_SetMovingState", RpcTarget.Others, false);
            }
            else
            {
                // Even on remote clients, ensure kinematic
                if (characterRigidbody != null)
                {
                    characterRigidbody.velocity = Vector3.zero;
                    characterRigidbody.isKinematic = true;
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
                
                // Make Rigidbody kinematic when rooted
                if (characterRigidbody != null)
                {
                    characterRigidbody.velocity = Vector3.zero;
                    characterRigidbody.isKinematic = true;
                    
                    // Sync with other clients
                    syncMadeRigidbodyKinematic = true;
                    photonView.RPC("RPC_ForceKinematic", RpcTarget.Others, true);
                }
                
                // Update the remote movement state too
                photonView.RPC("RPC_SetMovingState", RpcTarget.Others, false);
            }
            else
            {
                // Even on remote clients, ensure kinematic
                if (characterRigidbody != null)
                {
                    characterRigidbody.velocity = Vector3.zero;
                    characterRigidbody.isKinematic = true;
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
    }
}