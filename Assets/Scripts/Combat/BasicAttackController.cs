using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Pun.Demo.Asteroids;

[RequireComponent(typeof(HeroBase))]
public class BasicAttackController : MonoBehaviourPun
{
    [Header("Configuración de Proyectil")]
    public GameObject projectilePrefab;
    public Transform projectileSpawnPoint;
    
    [Header("Efectos")]
    public GameObject muzzleFlashPrefab;
    public AudioClip attackSound;
    public GameObject meleeAttackEffectPrefab;
    
    // Referencias internas
    private HeroBase heroBase;
    private Animator animator;
    private AudioSource audioSource;
    
    // Control de ataque
    private float attackCooldown = 0f;
    private bool canAttack = true;
    private Transform currentTarget;
    private Transform currentAttackTarget;
    
    private const string PROJECTILE_PATH = "Prefabs/Combat/BasicAttackProjectile";
    
    private void Awake()
    {
        heroBase = GetComponent<HeroBase>();
        animator = GetComponentInChildren<Animator>();
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f; // 3D sound
            audioSource.minDistance = 5.0f;
            audioSource.maxDistance = 20.0f;
        }
        
        // Crear punto de spawn si no existe
        if (projectileSpawnPoint == null)
        {
            GameObject spawnPoint = new GameObject("ProjectileSpawnPoint");
            spawnPoint.transform.SetParent(transform);
            spawnPoint.transform.localPosition = new Vector3(0, 1.5f, 0.5f);
            projectileSpawnPoint = spawnPoint.transform;
        }

        // Verificar que el prefab está en Resources
        if (PhotonNetwork.IsConnected)
        {
            var resourceCheck = Resources.Load<GameObject>(PROJECTILE_PATH);
            if (resourceCheck == null)
            {
                Debug.LogError($"[BasicAttackController] ADVERTENCIA: El prefab no se encuentra en Resources/{PROJECTILE_PATH}. Los ataques a distancia no funcionarán en red.");
            }
            else if (projectilePrefab == null)
            {
                // Si no hay referencia directa pero existe en Resources, usarla
                projectilePrefab = resourceCheck;
            }
        }
    }
    
    private void Update()
    {
        if (!photonView.IsMine) return;
        
        // Implementar ataque con clic derecho
        if (Input.GetMouseButtonDown(1))
        {
            Debug.Log("Clic derecho detectado");
            
            // Raycast para detectar objetivo bajo el cursor
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            // Usar la máscara de layer para jugadores y creeps
            int targetLayerMask = LayerManager.GetPlayerLayerMask() | LayerManager.GetCreepLayerMask();
            Debug.Log($"Buscando objetivos con layer mask: {targetLayerMask}, mi tag: {gameObject.tag}");
            
            if (Physics.Raycast(ray, out hit, 100f, targetLayerMask))
            {
                Debug.Log($"Hit detectado en: {hit.collider.gameObject.name}, layer: {hit.collider.gameObject.layer}, tag: {hit.collider.gameObject.tag}");
                
                // Verificar que no soy yo mismo
                if (hit.collider.gameObject == gameObject)
                {
                    Debug.Log("No puedo atacarme a mí mismo");
                    return;
                }
                
                // Verificar si es un objetivo válido
                if (!IsValidTarget(hit.collider.gameObject))
                {
                    Debug.Log($"No puedo atacar a {hit.collider.gameObject.name} porque no es un objetivo válido");
                    return;
                }
                
                // Intentar obtener el componente HeroBase o NeutralCreep
                HeroBase targetHero = hit.collider.GetComponent<HeroBase>();
                NeutralCreep targetCreep = hit.collider.GetComponent<NeutralCreep>();
                
                if (targetHero != null || targetCreep != null)
                {
                    Debug.Log($"Objetivo detectado: {(targetHero != null ? targetHero.heroName : targetCreep.creepName)}, tag: {hit.collider.gameObject.tag}");
                    // Atacar al objetivo
                    bool attackSuccess = TryAttack(hit.collider.transform);
                    Debug.Log($"Resultado del ataque: {(attackSuccess ? "ÉXITO" : "FALLIDO")}");
                    
                    // También establecerlo como objetivo actual en el sistema de targeting
                    TargetingSystem targetingSystem = GetComponent<TargetingSystem>();
                    if (targetingSystem != null)
                    {
                        bool targetSuccess = targetingSystem.SetTarget(hit.collider.transform);
                        Debug.Log($"Establecer como objetivo: {(targetSuccess ? "ÉXITO" : "FALLIDO")}");
                    }
                }
                else
                {
                    Debug.Log("El objeto golpeado no tiene componente HeroBase ni NeutralCreep");
                }
            }
            else
            {
                Debug.Log("El raycast no golpeó ningún objetivo válido");
            }
        }
        
        // Actualizar cooldown
        if (!canAttack)
        {
            attackCooldown -= Time.deltaTime;
            if (attackCooldown <= 0)
            {
                canAttack = true;
            }
        }
    }
    
    private bool IsValidTarget(GameObject target)
    {
        if (target == null) return false;
        
        // Verificar si es un héroe enemigo
        HeroBase targetHero = target.GetComponent<HeroBase>();
        if (targetHero != null)
        {
            return LayerManager.IsEnemy(gameObject, target);
        }
        
        // Verificar si es un creep neutral
        NeutralCreep targetCreep = target.GetComponent<NeutralCreep>();
        if (targetCreep != null)
        {
            return true; // Siempre podemos atacar a creeps neutrales
        }
        
        return false;
    }
    
    /// <summary>
    /// Intenta realizar un ataque básico al objetivo especificado
    /// </summary>
    /// <param name="target">Transform del objetivo a atacar</param>
    /// <returns>True si se pudo atacar, False si está en cooldown</returns>
    public bool TryAttack(Transform target)
    {
        if (!photonView.IsMine) return false;
        if (!canAttack) {
            Debug.Log("No puede atacar: en cooldown");
            return false;
        }
        if (target == null) {
            Debug.Log("No puede atacar: objetivo nulo");
            return false;
        }
        
        currentTarget = target;
        
        // Usar la propiedad AttackRange de HeroBase
        float attackRange = heroBase.AttackRange;
        
        // Comprobar distancia
        float distance = Vector3.Distance(transform.position, target.position);
        Debug.Log($"Distancia al objetivo: {distance}, Rango de ataque: {attackRange}");
        
        // Si está fuera de rango, mover al personaje
        if (distance > attackRange)
        {
            Debug.Log("Objetivo fuera de rango, moviendo al personaje");
            HeroMovementController movementController = GetComponent<HeroMovementController>();
            if (movementController != null)
            {
                // Calcular punto de destino (justo dentro del rango de ataque)
                Vector3 directionToTarget = (target.position - transform.position).normalized;
                Vector3 attackPosition = target.position - directionToTarget * (attackRange * 0.9f); // 90% del rango para dar un poco de margen
                movementController.SetDestination(attackPosition);
            }
            return false;
        }
        
        // Si está en rango, detener el movimiento y atacar
        HeroMovementController movement = GetComponent<HeroMovementController>();
        if (movement != null)
        {
            movement.StopMovement();
        }
        
        // Orientar hacia el objetivo
        Vector3 targetDirection = (target.position - transform.position).normalized;
        targetDirection.y = 0; // Mantener la rotación solo en el eje Y
        if (targetDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(targetDirection);
        }
        
        // Realizar el ataque según el tipo
        if (heroBase.HeroAttackType == HeroBase.AttackType.Ranged)
        {
            RangedAttack(target);
        }
        else
        {
            MeleeAttack(target);
        }
        
        // Actualizar cooldown
        canAttack = false;
        attackCooldown = 1f / heroBase.AttackSpeed;
        
        return true;
    }
    
    private void RangedAttack(Transform target)
    {
        Debug.Log($"[BasicAttackController] Iniciando ataque a distancia contra {target.name}");
        
        // Guardar el objetivo actual
        currentAttackTarget = target;
        
        // Activar animación si está disponible
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        
        // Iniciar corrutina para disparar después de un delay
        StartCoroutine(ShootWithDelay(0.2f)); // Ajusta este valor según la animación
    }
    
    private IEnumerator ShootWithDelay(float delay)
    {
        // Esperar el tiempo especificado
        yield return new WaitForSeconds(delay);
        
        // Llamar al método de disparo
        OnShoot();
    }
    
    private void OnShoot()
    {
        if (currentAttackTarget == null) return;
        
        // Verificar si tenemos spawnPoint
        if (projectileSpawnPoint == null)
        {
            Debug.LogError("[BasicAttackController] Error: No hay punto de origen para el proyectil");
            return;
        }
        
        // Verificar si tenemos prefab de proyectil (ya sea directo o en Resources)
        if (projectilePrefab == null && Resources.Load<GameObject>(PROJECTILE_PATH) == null)
        {
            Debug.LogError("[BasicAttackController] Error: No se encuentra el prefab del proyectil");
            return;
        }
        
        // Reproducir sonido
        if (attackSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(attackSound);
        }
        
        // Calcular dirección hacia el objetivo
        Vector3 targetDirection = (currentAttackTarget.position - projectileSpawnPoint.position).normalized;
        Debug.Log($"[BasicAttackController] Dirección del proyectil: {targetDirection}, Origen: {projectileSpawnPoint.position}, Destino: {currentAttackTarget.position}");
        
        try
        {
            // Instanciar proyectil
            GameObject projectile = null;
            if (PhotonNetwork.IsConnected)
            {
                Debug.Log($"[BasicAttackController] Instanciando proyectil en red desde {PROJECTILE_PATH}");
                projectile = PhotonNetwork.Instantiate(PROJECTILE_PATH, projectileSpawnPoint.position, Quaternion.LookRotation(targetDirection));
            }
            else
            {
                Debug.Log("[BasicAttackController] Instanciando proyectil localmente");
                if (projectilePrefab != null)
                {
                    projectile = Instantiate(projectilePrefab, projectileSpawnPoint.position, Quaternion.LookRotation(targetDirection));
                }
                else
                {
                    // Cargar desde Resources para modo single player
                    GameObject prefab = Resources.Load<GameObject>(PROJECTILE_PATH);
                    projectile = Instantiate(prefab, projectileSpawnPoint.position, Quaternion.LookRotation(targetDirection));
                }
            }
            
            if (projectile != null)
            {
                Debug.Log($"[BasicAttackController] Proyectil creado: {projectile.name}");
                ProjectileController projectileController = projectile.GetComponent<ProjectileController>();
                if (projectileController != null)
                {
                    projectileController.Initialize(heroBase.AttackDamage, transform, PhotonNetwork.IsConnected ? photonView.Owner.ActorNumber : 0);
                    Debug.Log($"[BasicAttackController] Proyectil inicializado con daño: {heroBase.AttackDamage}");
                }
                else
                {
                    Debug.LogError("[BasicAttackController] El proyectil no tiene ProjectileController!");
                }
            }
            else
            {
                Debug.LogError("[BasicAttackController] No se pudo crear el proyectil!");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[BasicAttackController] Error al instanciar el proyectil: {e.Message}\n{e.StackTrace}");
        }
        
        // Limpiar la referencia al objetivo
        currentAttackTarget = null;
    }
    
    private void MeleeAttack(Transform target)
    {
        // Activar animación
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        
        // Reproducir sonido
        if (attackSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(attackSound);
        }
        
        // Efecto de ataque melee
        if (meleeAttackEffectPrefab != null)
        {
            Vector3 midPoint = (transform.position + target.position) * 0.5f;
            midPoint.y = transform.position.y + 1f; // Ajustar altura
            
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Instantiate(meleeAttackEffectPrefab.name, midPoint, transform.rotation);
            }
            else
            {
                Instantiate(meleeAttackEffectPrefab, midPoint, transform.rotation);
            }
        }
        
        // Aplicar daño según el tipo de objetivo
        HeroBase targetHero = target.GetComponent<HeroBase>();
        NeutralCreep targetCreep = target.GetComponent<NeutralCreep>();
        
        if (targetHero != null)
        {
            // Solo dañar a enemigos (equipos diferentes)
            if (targetHero.teamId != heroBase.teamId)
            {
                // Usar HeroBase para el ataque directo
                heroBase.TryBasicAttack(targetHero);
            }
        }
        else if (targetCreep != null)
        {
            // Aplicar daño al creep
            targetCreep.TakeDamage(heroBase.AttackDamage, heroBase);
        }
        
        // Notificar a través de RPC que se realizó un ataque
        if (PhotonNetwork.IsConnected)
        {
            photonView.RPC("RPC_OnAttackPerformed", RpcTarget.Others, target.GetComponent<PhotonView>().ViewID);
        }
    }
    
    [PunRPC]
    private void RPC_OnAttackPerformed(int targetViewID)
    {
        // Buscar el objetivo por su ViewID
        PhotonView targetView = PhotonView.Find(targetViewID);
        if (targetView != null)
        {
            currentTarget = targetView.transform;
            
            // Activar animación si está disponible
            if (animator != null)
            {
                animator.SetTrigger("Attack");
            }
            
            // Reproducir sonido
            if (attackSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(attackSound);
            }
            
            // Mostrar efectos visuales (pero sin aplicar daño, eso lo hace el dueño)
            if (heroBase.HeroAttackType == HeroBase.AttackType.Ranged)
            {
                // Efecto de disparo
                if (muzzleFlashPrefab != null && projectileSpawnPoint != null)
                {
                    Instantiate(muzzleFlashPrefab, projectileSpawnPoint.position, projectileSpawnPoint.rotation);
                }
            }
            else
            {
                // Efecto de ataque melee
                if (meleeAttackEffectPrefab != null && currentTarget != null)
                {
                    Vector3 midPoint = (transform.position + currentTarget.position) * 0.5f;
                    midPoint.y = transform.position.y + 1f; // Ajustar altura
                    
                    Instantiate(meleeAttackEffectPrefab, midPoint, transform.rotation);
                }
            }
        }
    }
    
    // Métodos públicos para ayudar al control del héroe
    
    public bool IsInRange(Transform target)
    {
        if (target == null) return false;
        float distance = Vector3.Distance(transform.position, target.position);
        return distance <= heroBase.AttackRange;
    }
    
    public bool CanAttack()
    {
        return canAttack;
    }
    
    public float GetAttackCooldownRemaining()
    {
        return attackCooldown;
    }
} 