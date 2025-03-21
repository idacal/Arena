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
        
        // Referencias privadas
        private HeroBase heroBase;
        private NavMeshAgent navAgent;
        private Camera mainCamera;
        private Vector3 targetPosition;
        private bool isMoving = false;
        
        // Estados
        private bool isStunned = false;
        private bool isRooted = false;
        
        // Variables para sincronización
        private Vector3 latestTargetPosition;
        private bool latestIsMoving;
        private float timeSinceLastPositionUpdate = 0f;
        private float positionUpdateInterval = 0.2f; // Actualizar cada 200ms
        
        void Awake()
        {
            // Obtener componentes
            heroBase = GetComponent<HeroBase>();
            navAgent = GetComponent<NavMeshAgent>();
            
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
        }
        
        void Start()
        {
            // Solo controlar si es el jugador local
            if (!photonView.IsMine)
            {
                // Desactivar el NavMeshAgent para jugadores remotos
                navAgent.enabled = false;
                return;
            }
            
            // Obtener la cámara principal
            mainCamera = Camera.main;
            
            // Configurar el NavMeshAgent con la velocidad del héroe
            navAgent.speed = heroBase.moveSpeed;
            navAgent.angularSpeed = rotationSpeed * 100;
            navAgent.stoppingDistance = targetDistance;
            
            // Inicializar posición objetivo
            targetPosition = transform.position;
            latestTargetPosition = targetPosition;
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
                
            // Leer entrada para movimiento
            HandleMovementInput();
            
            // Actualizar animaciones
            UpdateAnimation();
            
            // Actualizar datos para sincronización
            timeSinceLastPositionUpdate += Time.deltaTime;
            if (timeSinceLastPositionUpdate >= positionUpdateInterval)
            {
                timeSinceLastPositionUpdate = 0;
                latestTargetPosition = targetPosition;
                latestIsMoving = isMoving;
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
                if (Physics.Raycast(ray, out hit, 100f, LayerMask.GetMask("Ground")))
                {
                    // Si no está enraizado, establecer destino
                    if (!isRooted)
                    {
                        targetPosition = hit.point;
                        navAgent.SetDestination(targetPosition);
                        isMoving = true;
                    }
                }
            }
            
            // Verificar si hemos llegado al destino
            if (isMoving && !navAgent.pathPending)
            {
                if (navAgent.remainingDistance <= navAgent.stoppingDistance)
                {
                    if (!navAgent.hasPath || navAgent.velocity.sqrMagnitude == 0f)
                    {
                        isMoving = false;
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
                float moveSpeed = navAgent.velocity.magnitude / navAgent.speed;
                animator.SetFloat(moveSpeedParameter, moveSpeed);
            }
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
                navAgent.SetDestination(targetPosition);
                isMoving = true;
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
                navAgent.ResetPath();
                isMoving = false;
                
                // Actualizar animación
                if (animator != null)
                {
                    animator.SetFloat(moveSpeedParameter, 0f);
                }
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
            }
            else
            {
                // Datos que se reciben de otros jugadores
                latestTargetPosition = (Vector3)stream.ReceiveNext();
                latestIsMoving = (bool)stream.ReceiveNext();
                isStunned = (bool)stream.ReceiveNext();
                isRooted = (bool)stream.ReceiveNext();
            }
        }
        
        #region PHOTON RPC
        
        [PunRPC]
        private void RPC_ApplyStun(float duration)
        {
            // Aplicar aturdimiento
            isStunned = true;
            
            // Detener movimiento
            if (photonView.IsMine)
            {
                navAgent.ResetPath();
                isMoving = false;
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
                navAgent.ResetPath();
                isMoving = false;
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
    }
}