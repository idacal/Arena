using UnityEngine;
using Photon.Pun;
using System.Collections;

namespace Photon.Pun.Demo.Asteroids
{
    [RequireComponent(typeof(PhotonView))]
    [RequireComponent(typeof(PhotonTransformView))]
    public class NeutralCreep : MonoBehaviourPunCallbacks, IDamageable, IPunObservable
    {
        [Header("Información Básica")]
        public string creepName = "Creep";
        public string creepType = "Neutral"; // Tipo de creep (ej: "Fungus", "Bat", etc.)
        public int level = 1;
        
        [Header("Estadísticas")]
        public float maxHealth = 100f;
        public float currentHealth;
        public float attackDamage = 10f;
        public float attackSpeed = 1f;
        public float moveSpeed = 3f;
        public float armor = 0f;
        public float magicResistance = 0f;
        
        [Header("Recompensas")]
        public float experienceReward = 25f;
        public float goldReward = 10f;
        public float healthRestore = 0f;    // Vida restaurada al matar
        public float manaRestore = 0f;      // Maná restaurado al matar
        
        [Header("Comportamiento")]
        public float aggroRange = 5f;       // Rango para detectar enemigos
        public float attackRange = 1.5f;    // Rango de ataque
        public float respawnTime = 60f;     // Tiempo para respawnear
        public float patrolRadius = 5f;    // Radio de patrulla
        public float minPatrolTime = 3f;   // Tiempo mínimo de patrulla
        public float maxPatrolTime = 8f;   // Tiempo máximo de patrulla
        
        [Header("Efectos")]
        public GameObject deathEffectPrefab;
        public AudioClip deathSound;
        public float deathSoundVolume = 1f;
        private AudioSource audioSource;
        
        [Header("Animaciones")]
        public Animator animator;
        
        [Header("UI")]
        public GameObject healthBarPrefab;
        private HealthBar healthBar;
        
        // Eventos
        public delegate void CreepDeathDelegate(NeutralCreep creep);
        public event CreepDeathDelegate OnDeath;
        
        // Variables privadas
        private bool isDead = false;
        private float attackCooldown = 0f;
        private Transform currentTarget;
        private Vector3 spawnPosition;
        private float timeSinceDeath = 0f;
        
        // Variables de patrulla
        private Vector3 patrolTarget;
        private float patrolTimer;
        private bool isPatrolling = true;
        
        // Componentes de red
        private PhotonTransformView photonTransformView;
        
        // Nombres de los parámetros de animación
        private const string ANIM_IS_WALKING = "IsWalking";
        private const string ANIM_IS_ATTACKING = "IsAttacking";
        private const string ANIM_DIE = "Die";
        private const string ANIM_RESPAWN = "Respawn";
        
        // Variables para sincronización de posición
        private Vector3 lastSyncPosition = Vector3.zero;
        
        void Awake()
        {
            // Inicializar lastSyncPosition
            lastSyncPosition = transform.position;
            
            // Obtener el PhotonTransformView
            photonTransformView = GetComponent<PhotonTransformView>();
            if (photonTransformView == null)
            {
                photonTransformView = gameObject.AddComponent<PhotonTransformView>();
                Debug.Log($"[NeutralCreep] Se ha añadido PhotonTransformView automáticamente a {creepName}");
            }
            
            // Configurar PhotonTransformView siempre
            photonTransformView.m_SynchronizePosition = true;
            photonTransformView.m_SynchronizeRotation = true;
            photonTransformView.m_SynchronizeScale = false;
            
            // Asegurarse de que el PhotonView observe el PhotonTransformView
            PhotonView pv = GetComponent<PhotonView>();
            if (pv != null)
            {
                // Asegurar que el PhotonView tiene un componente para observar
                if (pv.ObservedComponents == null)
                {
                    pv.ObservedComponents = new System.Collections.Generic.List<Component>();
                }
                
                // Añadir PhotonTransformView si no está en la lista
                if (!pv.ObservedComponents.Contains(photonTransformView))
                {
                    pv.ObservedComponents.Add(photonTransformView);
                    pv.Synchronization = ViewSynchronization.UnreliableOnChange;
                    Debug.Log($"[NeutralCreep] Configurado PhotonView para observar PhotonTransformView en {creepName}");
                }
            }
            
            // Configurar Rigidbody
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezePositionY;
            
            // Configurar Collider
            CapsuleCollider capsuleCollider = GetComponent<CapsuleCollider>();
            if (capsuleCollider == null)
            {
                capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
            }
            capsuleCollider.isTrigger = false;
            capsuleCollider.height = 2f; // Ajusta según el tamaño de tu modelo
            capsuleCollider.radius = 0.5f; // Ajusta según el ancho de tu modelo
            capsuleCollider.center = new Vector3(0, 1f, 0); // Ajusta según el centro de tu modelo
            
            // Configurar Layer
            gameObject.layer = LayerMask.NameToLayer("NeutralCreep");
            
            // Obtener el Animator
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
            
            // Configurar AudioSource
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f; // Sonido 3D
                audioSource.minDistance = 5f;
                audioSource.maxDistance = 20f;
                audioSource.rolloffMode = AudioRolloffMode.Linear;
            }
        }
        
        void Start()
        {
            currentHealth = maxHealth;
            spawnPosition = transform.position;
            SetNewPatrolTarget();
            
            // Crear la barra de vida
            if (healthBarPrefab != null)
            {
                GameObject healthBarObj = Instantiate(healthBarPrefab);
                healthBar = healthBarObj.GetComponent<HealthBar>();
                if (healthBar != null)
                {
                    healthBar.Initialize(transform, maxHealth, currentHealth);
                }
            }
            
            Debug.Log($"[NeutralCreep] {creepName} inicializado. PhotonView.IsMine: {photonView.IsMine}, ViewID: {photonView.ViewID}");
        }
        
        void Update()
        {
            if (isDead)
            {
                HandleDeath();
                return;
            }
            
            // Actualizar animaciones incluso en objetos remotos
            UpdateAnimations();
            
            // Solo el dueño del objeto ejecuta la lógica de movimiento
            if (!photonView.IsMine)
            {
                return;
            }
            
            // Actualizar cooldown de ataque
            if (attackCooldown > 0)
            {
                attackCooldown -= Time.deltaTime;
            }
            
            // Buscar y atacar objetivos
            if (currentTarget == null || !IsTargetValid(currentTarget))
            {
                FindTarget();
            }
            
            if (currentTarget != null)
            {
                // Comportamiento de combate
                HandleCombat();
            }
            else if (isPatrolling)
            {
                // Comportamiento de patrulla
                HandlePatrol();
            }
        }
        
        private void UpdateAnimations()
        {
            // Actualizar animaciones basadas en el estado actual
            if (isDead)
            {
                // Si está muerto, no actualizar más animaciones
                return;
            }
            
            if (currentTarget != null)
            {
                float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
                bool isAttacking = distanceToTarget <= attackRange;
                bool isWalking = distanceToTarget > attackRange;
                
                SetAnimationState(isWalking, isAttacking);
            }
            else if (isPatrolling)
            {
                // Si está patrullando, siempre debe estar caminando
                SetAnimationState(true, false);
            }
            else
            {
                // Estado por defecto: quieto
                SetAnimationState(false, false);
            }
        }
        
        private void HandleCombat()
        {
            if (!photonView.IsMine) return;
            
            isPatrolling = false;
            Vector3 direction = (currentTarget.position - transform.position).normalized;
            
            // Mover hacia el objetivo
            transform.position += direction * moveSpeed * Time.deltaTime;
            
            // Rotar hacia el objetivo
            transform.rotation = Quaternion.LookRotation(direction);
            
            // Atacar si está en rango
            if (Vector3.Distance(transform.position, currentTarget.position) <= attackRange)
            {
                Attack();
            }
            
            // Sincronizar la posición con otros clientes si ha cambiado significativamente
            if (Vector3.Distance(transform.position, lastSyncPosition) > 0.5f)
            {
                lastSyncPosition = transform.position;
                photonView.RPC("RPC_SyncTransform", RpcTarget.Others, transform.position, transform.rotation);
            }
        }
        
        private void HandlePatrol()
        {
            if (!photonView.IsMine) return;
            
            // Actualizar timer de patrulla
            patrolTimer -= Time.deltaTime;
            
            // Si llegamos al objetivo o se acabó el tiempo, elegir nuevo objetivo
            if (Vector3.Distance(transform.position, patrolTarget) < 0.1f || patrolTimer <= 0)
            {
                SetNewPatrolTarget();
                
                // Sincronizar el nuevo objetivo de patrulla a todos los clientes
                photonView.RPC("RPC_SetPatrolTarget", RpcTarget.OthersBuffered, patrolTarget, patrolTimer);
            }
            
            // Mover hacia el objetivo de patrulla
            Vector3 direction = (patrolTarget - transform.position).normalized;
            transform.position += direction * (moveSpeed * 0.5f) * Time.deltaTime;
            
            // Rotar hacia la dirección del movimiento
            transform.rotation = Quaternion.LookRotation(direction);
            
            // Sincronizar la posición con otros clientes si ha cambiado significativamente
            if (Vector3.Distance(transform.position, lastSyncPosition) > 0.5f)
            {
                lastSyncPosition = transform.position;
                photonView.RPC("RPC_SyncTransform", RpcTarget.Others, transform.position, transform.rotation);
            }
        }
        
        [PunRPC]
        private void RPC_SetPatrolTarget(Vector3 newPatrolTarget, float newPatrolTimer)
        {
            // Actualizar el objetivo de patrulla en clientes remotos
            patrolTarget = newPatrolTarget;
            patrolTimer = newPatrolTimer;
            Debug.Log($"[NeutralCreep] Objetivo de patrulla sincronizado: {patrolTarget}, Timer: {patrolTimer}");
        }
        
        [PunRPC]
        private void RPC_SyncTransform(Vector3 position, Quaternion rotation)
        {
            // Solo aplicar en clientes remotos
            if (!photonView.IsMine && !isDead)
            {
                // Aplicar posición y rotación con suavizado
                transform.position = Vector3.Lerp(transform.position, position, Time.deltaTime * 10);
                transform.rotation = Quaternion.Lerp(transform.rotation, rotation, Time.deltaTime * 10);
            }
        }
        
        private void SetNewPatrolTarget()
        {
            // Generar punto aleatorio dentro del radio de patrulla
            Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
            patrolTarget = spawnPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
            
            // Establecer tiempo aleatorio de patrulla
            patrolTimer = Random.Range(minPatrolTime, maxPatrolTime);
        }
        
        private void FindTarget()
        {
            // Buscar jugadores en el rango de aggro
            Collider[] colliders = Physics.OverlapSphere(transform.position, aggroRange);
            float closestDistance = aggroRange;
            
            foreach (Collider col in colliders)
            {
                HeroBase hero = col.GetComponent<HeroBase>();
                if (hero != null && !hero.IsDead)
                {
                    float distance = Vector3.Distance(transform.position, hero.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        currentTarget = hero.transform;
                    }
                }
            }
            
            // Si encontramos un objetivo, sincronizarlo con los demás clientes
            if (currentTarget != null)
            {
                photonView.RPC("RPC_SetTarget", RpcTarget.Others, currentTarget.GetComponent<PhotonView>().ViewID);
            }
        }
        
        [PunRPC]
        private void RPC_SetTarget(int targetViewID)
        {
            // Buscar el objetivo por su ViewID
            PhotonView targetView = PhotonView.Find(targetViewID);
            if (targetView != null)
            {
                currentTarget = targetView.transform;
                isPatrolling = false;
                Debug.Log($"[NeutralCreep] Objetivo sincronizado: {targetView.name}, ViewID: {targetViewID}");
            }
        }
        
        private bool IsTargetValid(Transform target)
        {
            if (target == null) return false;
            
            HeroBase hero = target.GetComponent<HeroBase>();
            if (hero == null || hero.IsDead) return false;
            
            return Vector3.Distance(transform.position, target.position) <= aggroRange;
        }
        
        private void Attack()
        {
            if (attackCooldown <= 0)
            {
                HeroBase targetHero = currentTarget.GetComponent<HeroBase>();
                if (targetHero != null)
                {
                    targetHero.TakeDamage(attackDamage, null); // Pasamos null como atacante
                    attackCooldown = 1f / attackSpeed;
                    
                    // Activar animación de ataque
                    SetAnimationState(false, true);
                }
            }
        }
        
        private void SetAnimationState(bool isWalking, bool isAttacking)
        {
            if (animator != null)
            {
                animator.SetBool(ANIM_IS_WALKING, isWalking);
                animator.SetBool(ANIM_IS_ATTACKING, isAttacking);
            }
        }
        
        public void TakeDamage(float damage, object attacker)
        {
            if (isDead) return;
            
            Debug.Log($"[NeutralCreep] {creepName} recibe {damage} de daño. Atacante: {(attacker != null ? attacker.ToString() : "null")}");
            
            // Calcular daño real considerando la armadura
            float actualDamage = damage * (100 / (100 + armor));
            currentHealth -= actualDamage;
            
            // Actualizar la barra de vida
            if (healthBar != null)
            {
                healthBar.UpdateHealth(currentHealth);
            }
            
            // Si el daño viene de un héroe, establecerlo como objetivo
            if (attacker is HeroBase hero)
            {
                Debug.Log($"[NeutralCreep] El daño viene del héroe {hero.heroName}");
                currentTarget = hero.transform;
                isPatrolling = false; // Desactivar patrulla cuando es atacado
                
                // Sincronizar el objetivo con todos los clientes
                if (photonView.IsMine)
                {
                    photonView.RPC("RPC_SetTarget", RpcTarget.OthersBuffered, hero.photonView.ViewID);
                }
            }
            else
            {
                Debug.LogWarning($"[NeutralCreep] El atacante no es un héroe: {attacker?.GetType().Name ?? "null"}");
            }
            
            // Sincronizar la salud con todos los clientes
            if (photonView.IsMine)
            {
                photonView.RPC("RPC_SyncHealth", RpcTarget.OthersBuffered, currentHealth);
            }
            
            // Verificar muerte
            if (currentHealth <= 0 && !isDead)
            {
                Die(attacker as HeroBase);
            }
        }
        
        [PunRPC]
        private void RPC_SyncHealth(float newHealth)
        {
            currentHealth = newHealth;
            
            // Actualizar la barra de vida
            if (healthBar != null)
            {
                healthBar.UpdateHealth(currentHealth);
            }
            
            Debug.Log($"[NeutralCreep] Salud sincronizada: {currentHealth}");
            
            // Verificar muerte debido a la sincronización
            if (currentHealth <= 0 && !isDead)
            {
                // Morir sin asesino específico (solo se ejecutará en clientes que no son dueños)
                if (!photonView.IsMine)
                {
                    // No llamamos a Die() directamente para evitar bucles; esperamos a que el dueño nos diga
                    isDead = true;
                }
            }
        }
        
        private void Die(HeroBase killer)
        {
            if (isDead) return;
            
            isDead = true;
            currentHealth = 0;
            timeSinceDeath = 0f;
            
            Debug.Log($"[NeutralCreep] {creepName} está muriendo. Killer: {(killer != null ? killer.heroName : "null")}");
            
            // Reproducir efectos visuales localmente
            if (deathEffectPrefab != null)
            {
                Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            }
            
            // Activar animación de muerte
            if (animator != null)
            {
                animator.SetTrigger(ANIM_DIE);
            }
            
            // Desactivar el collider
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            
            // Detener movimiento
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.isKinematic = true;
            }
            
            // Si somos el dueño del objeto, sincronizar la muerte
            if (photonView.IsMine)
            {
                // Utilizar AllViaServer para garantizar que todos los clientes reciban la información
                int killerViewID = (killer != null && killer.photonView != null) ? killer.photonView.ViewID : -1;
                photonView.RPC("RPC_SyncDeath", RpcTarget.AllViaServer, killerViewID);
                
                // Otorgar recompensas al asesino (solo si es un jugador válido)
                if (killer != null)
                {
                    Debug.Log($"[NeutralCreep] Procesando recompensa para {killer.heroName}");
                    
                    // Si somos dueños del héroe, otorgar recompensas directamente
                    if (killer.photonView.IsMine)
                    {
                        GiveRewardsToKiller(killer);
                    }
                    else
                    {
                        // Enviar RPC al dueño del héroe para que reciba las recompensas
                        photonView.RPC("RPC_GiveRewards", killer.photonView.Owner, goldReward, experienceReward, creepName);
                    }
                }
                
                // Iniciar el proceso de destrucción después de un tiempo
                StartCoroutine(DestroyAfterDelay(2f));
            }
        }
        
        private void GiveRewardsToKiller(HeroBase killer)
        {
            Debug.Log($"[NeutralCreep] Otorgando recompensas directamente a {killer.heroName}");
            
            // Experiencia
            killer.AwardCreepKillExperience(this);
            
            // Oro con mensaje
            string creepKillText = $"¡{creepName} eliminado!";
            
            // Reproducir sonido de oro
            if (killer.goldSound != null)
            {
                killer.PlaySound(killer.goldSound, 1.5f, true);
            }
            
            // Añadir el oro
            killer.AddGold(goldReward, creepKillText, transform.position);
            
            // Restaurar vida/maná si corresponde
            if (healthRestore > 0)
            {
                killer.Heal(healthRestore);
            }
            if (manaRestore > 0)
            {
                killer.UseMana(-Mathf.RoundToInt(manaRestore));
            }
            
            Debug.Log($"[NeutralCreep] Recompensas otorgadas a {killer.heroName}: {goldReward} oro, {experienceReward} exp");
        }
        
        [PunRPC]
        private void RPC_SyncDeath(int killerViewID)
        {
            Debug.Log($"[NeutralCreep] RPC_SyncDeath recibido para {creepName}");
            
            // Marcar como muerto
            isDead = true;
            currentHealth = 0;
            timeSinceDeath = 0f;
            
            // Reproducir efectos visuales si no lo hemos hecho ya
            if (deathEffectPrefab != null)
            {
                Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            }
            
            // Reproducir sonido
            if (deathSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(deathSound, deathSoundVolume);
            }
            
            // Activar animación de muerte
            if (animator != null)
            {
                animator.SetTrigger(ANIM_DIE);
            }
            
            // Desactivar físicas
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.isKinematic = true;
            }
            
            // Desactivar collider
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            
            // Destruir la barra de vida
            if (healthBar != null)
            {
                Destroy(healthBar.gameObject);
            }
            
            // Notificar el evento de muerte
            OnDeath?.Invoke(this);
        }
        
        [PunRPC]
        private void RPC_GiveRewards(float gold, float experience, string creepName)
        {
            Debug.Log($"[NeutralCreep] RPC_GiveRewards recibido: {gold} oro, {experience} exp de {creepName}");
            
            // Este RPC se ejecuta en el cliente que posee al héroe que mató a la criatura
            HeroBase localHero = FindObjectOfType<HeroBase>();
            if (localHero != null && localHero.photonView.IsMine)
            {
                // Experiencia
                localHero.AwardCreepKillExperience(this);
                
                // Oro con mensaje
                string creepKillText = $"¡{creepName} eliminado!";
                
                // Reproducir sonido de oro
                if (localHero.goldSound != null)
                {
                    localHero.PlaySound(localHero.goldSound, 1.5f, true);
                }
                
                // Añadir el oro
                localHero.AddGold(gold, creepKillText, transform.position);
                
                // Restaurar vida/maná si corresponde
                if (healthRestore > 0)
                {
                    localHero.Heal(healthRestore);
                }
                if (manaRestore > 0)
                {
                    localHero.UseMana(-Mathf.RoundToInt(manaRestore));
                }
                
                Debug.Log($"[NeutralCreep] Recompensas otorgadas a {localHero.heroName}: {gold} oro, {experience} exp");
            }
            else
            {
                Debug.LogError("[NeutralCreep] No se encontró el héroe local para otorgar recompensas");
            }
        }
        
        private System.Collections.IEnumerator DestroyAfterDelay(float delay)
        {
            Debug.Log($"[NeutralCreep] Iniciando destrucción con retraso de {delay} segundos para {creepName}");
            
            // Esperar el tiempo especificado
            yield return new WaitForSeconds(delay);
            
            if (photonView.IsMine)
            {
                Debug.Log($"[NeutralCreep] Destruyendo objeto: {creepName}");
                
                try
                {
                    // Destruir en la red
                    PhotonNetwork.Destroy(gameObject);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[NeutralCreep] Error al destruir en red: {e.Message}");
                    // Plan B - destrucción local si falla la destrucción en red
                    Destroy(gameObject);
                }
            }
        }
        
        private void HandleDeath()
        {
            if (!isDead) return;
            
            // Incrementar el tiempo desde la muerte
            timeSinceDeath += Time.deltaTime;
            
            // Si ha pasado mucho tiempo y aún no se ha destruido
            if (timeSinceDeath > 5f && photonView.IsMine)
            {
                Debug.Log($"[NeutralCreep] Forzando destrucción por tiempo excedido: {creepName}");
                try
                {
                    PhotonNetwork.Destroy(gameObject);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[NeutralCreep] Error al forzar destrucción: {e.Message}");
                    Destroy(gameObject);
                }
            }
        }
        
        private void Respawn()
        {
            isDead = false;
            currentHealth = maxHealth;
            timeSinceDeath = 0f;
            
            // Reactivar el collider y el renderer
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = true;
            
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = true;
            
            // Volver a la posición de spawn
            transform.position = spawnPosition;
            currentTarget = null;
            
            // Activar animación de respawn
            if (animator != null)
            {
                animator.SetTrigger(ANIM_RESPAWN);
            }
            
            // Reiniciar patrulla
            SetNewPatrolTarget();
        }
        
        // Implementación de IPunObservable
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // Enviar datos más importantes primero
                stream.SendNext(isDead);
                stream.SendNext(currentHealth);
                
                // Solo sincronizar posición/rotación si está vivo
                if (!isDead)
                {
                    stream.SendNext(transform.position);
                    stream.SendNext(transform.rotation);
                }
                
                // Datos de comportamiento
                stream.SendNext(isPatrolling);
                stream.SendNext(patrolTarget);
                stream.SendNext(patrolTimer);
                
                // Enviar el ID del objetivo si existe
                int targetViewID = -1;
                if (currentTarget != null)
                {
                    PhotonView targetView = currentTarget.GetComponent<PhotonView>();
                    if (targetView != null)
                    {
                        targetViewID = targetView.ViewID;
                    }
                }
                stream.SendNext(targetViewID);
            }
            else
            {
                // Datos que recibimos - mantener mismo orden que al enviar
                bool prevIsDead = isDead;
                float prevHealth = currentHealth;
                
                isDead = (bool)stream.ReceiveNext();
                currentHealth = (float)stream.ReceiveNext();
                
                // Solo recibir posición/rotación si está vivo
                if (!isDead && !photonView.IsMine)
                {
                    Vector3 networkPosition = (Vector3)stream.ReceiveNext();
                    Quaternion networkRotation = (Quaternion)stream.ReceiveNext();
                    
                    // Aplicar suavizado
                    float lerpRate = Mathf.Clamp01(Time.deltaTime * 10); // 10x por segundo como máximo
                    transform.position = Vector3.Lerp(transform.position, networkPosition, lerpRate);
                    transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, lerpRate);
                }
                else if (!isDead) // Necesitamos avanzar el stream aunque no usemos los valores
                {
                    stream.ReceiveNext(); // Posición
                    stream.ReceiveNext(); // Rotación
                }
                
                // Actualizar variables de comportamiento
                isPatrolling = (bool)stream.ReceiveNext();
                patrolTarget = (Vector3)stream.ReceiveNext();
                patrolTimer = (float)stream.ReceiveNext();
                
                // Procesar ID del objetivo
                int targetViewID = (int)stream.ReceiveNext();
                if (targetViewID >= 0)
                {
                    PhotonView targetView = PhotonView.Find(targetViewID);
                    if (targetView != null)
                    {
                        currentTarget = targetView.transform;
                    }
                }
                else
                {
                    currentTarget = null;
                }
                
                // Actualizar barra de vida
                if (healthBar != null)
                {
                    healthBar.UpdateHealth(currentHealth);
                }
                
                // Si acaba de morir, asegurarse de que se apliquen los efectos
                if (!prevIsDead && isDead)
                {
                    Debug.Log($"[NeutralCreep] Estado de muerte sincronizado para {creepName}");
                    
                    // Solo ejecutar efectos de muerte en clientes remotos
                    if (!photonView.IsMine)
                    {
                        RPC_SyncDeath(-1); // -1 indica que no conocemos al killer
                    }
                }
            }
        }
    }
} 