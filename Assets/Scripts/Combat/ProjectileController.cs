using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Pun.Demo.Asteroids;

public class ProjectileController : MonoBehaviourPun
{
    [Header("Proyectil Configuración")]
    public float speed = 15f;
    public float lifetime = 5f;
    public bool useGravity = false;
    public LayerMask collisionMask;
    
    [Header("Efectos")]
    public GameObject hitEffectPrefab;
    public GameObject trailEffect;
    
    private Rigidbody rb;
    private float damage;
    private Transform shooter;
    private int shooterActorNumber;
    private int attackerViewID;
    
    private void Awake()
    {
        Debug.Log("[ProjectileController] Awake iniciado");
        rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.useGravity = useGravity;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            Debug.Log($"[ProjectileController] Rigidbody configurado: useGravity={useGravity}, interpolation={rb.interpolation}, collisionDetection={rb.collisionDetectionMode}");
        }
        else
        {
            Debug.LogError("[ProjectileController] No se encontró el componente Rigidbody");
        }
        
        // Destruir después del tiempo de vida
        Destroy(gameObject, lifetime);
        Debug.Log($"[ProjectileController] Programado para destruirse en {lifetime} segundos");
    }
    
    private void Start()
    {
        Debug.Log("[ProjectileController] Start iniciado");
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
        {
            Debug.Log("[ProjectileController] Cliente remoto - Desactivando física");
            // Desactivar la física en clientes remotos
            if (rb) rb.isKinematic = true;
            return;
        }
        
        // Iniciar movimiento
        if (rb)
        {
            rb.velocity = transform.forward * speed;
            Debug.Log($"[ProjectileController] Velocidad inicial establecida: {rb.velocity.magnitude}");
        }
        else
        {
            Debug.LogError("[ProjectileController] No se pudo establecer la velocidad inicial - Rigidbody no encontrado");
        }
    }
    
    /// <summary>
    /// Inicializa el proyectil con los datos necesarios
    /// </summary>
    /// <param name="damage">Daño que causará el proyectil</param>
    /// <param name="shooter">Referencia al transform del disparador (para identificar al atacante)</param>
    /// <param name="shooterActorNumber">Número de actor Photon del disparador</param>
    public void Initialize(float damage, Transform shooter, int shooterActorNumber, int attackerViewID)
    {
        Debug.Log($"[ProjectileController] Inicializando proyectil - Daño: {damage}, Disparador: {shooter.name}, ActorNumber: {shooterActorNumber}");
        this.damage = damage;
        this.shooter = shooter;
        this.shooterActorNumber = shooterActorNumber;
        this.attackerViewID = attackerViewID;
        
        // Verificar configuración de colisiones
        if (collisionMask.value == 0)
        {
            Debug.LogWarning("[ProjectileController] La máscara de colisión está vacía - El proyectil no podrá colisionar");
        }
        else
        {
            Debug.Log($"[ProjectileController] Máscara de colisión configurada: {collisionMask.value}");
        }
        
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
        
        // Configuración de física
        if (rb != null)
        {
            rb.useGravity = useGravity;
            rb.velocity = transform.forward * speed;
            rb.drag = 0.1f; // Añadir un poco de resistencia al aire
            rb.angularDrag = 0.1f; // Prevenir rotaciones extrañas
        }
        
        // Destruir después del tiempo de vida
        Destroy(gameObject, lifetime);
    }
    
    [PunRPC]
    private void RPC_SetShooterInfo(int actorNumber)
    {
        shooterActorNumber = actorNumber;
        // Buscar el jugador que disparó por su actor number
        PhotonView[] photonViews = FindObjectsOfType<PhotonView>();
        foreach (PhotonView view in photonViews)
        {
            if (view.Owner != null && view.Owner.ActorNumber == actorNumber)
            {
                if (view.gameObject.GetComponent<HeroBase>() != null)
                {
                    shooter = view.transform;
                    break;
                }
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Proyectil golpeó a: {other.gameObject.name}, tag: {other.gameObject.tag}");
        
        // Verificar si golpeó a un héroe
        HeroBase hitHero = other.GetComponent<HeroBase>();
        
        // Verificar si golpeó a una criatura neutral
        NeutralCreep hitCreep = other.GetComponent<NeutralCreep>();
        
        // Obtener el atacante original
        PhotonView attackerView = PhotonView.Find(attackerViewID);
        HeroBase attacker = null;
        if (attackerView != null)
        {
            attacker = attackerView.GetComponent<HeroBase>();
        }
        
        if (hitHero != null)
        {
            Debug.Log("Golpeó a un héroe");
            
            if (attacker != null)
            {
                // Establecer el atacante como currentTarget antes de aplicar el daño
                hitHero.currentTarget = attacker;
                Debug.Log($"[ProjectileController] Establecido currentTarget a {attacker.heroName} para {hitHero.heroName}");
            }
            
            // Aplicar el daño
            hitHero.TakeDamage(damage, shooterActorNumber);
            
            // Destruir el proyectil
            if (photonView.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }
        else if (hitCreep != null)
        {
            Debug.Log($"[ProjectileController] Golpeó a una criatura neutral: {hitCreep.creepName}");
            
            // Aplicar el daño directamente a la criatura neutral
            hitCreep.TakeDamage(damage, attacker);
            
            // Destruir el proyectil
            if (photonView.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"[ProjectileController] Colisión detectada con: {collision.gameObject.name}");
        
        // Verificar si golpeó a un héroe
        HeroBase hitHero = collision.gameObject.GetComponent<HeroBase>();
        
        // Verificar si golpeó a una criatura neutral
        NeutralCreep hitCreep = collision.gameObject.GetComponent<NeutralCreep>();
        
        // Obtener el atacante original
        PhotonView attackerView = PhotonView.Find(attackerViewID);
        HeroBase attacker = null;
        if (attackerView != null)
        {
            attacker = attackerView.GetComponent<HeroBase>();
        }
        
        if (hitHero != null)
        {
            Debug.Log("[ProjectileController] Golpeó a un héroe en colisión física");
            
            if (attacker != null)
            {
                // Establecer el atacante como currentTarget antes de aplicar el daño
                hitHero.currentTarget = attacker;
                Debug.Log($"[ProjectileController] Establecido currentTarget a {attacker.heroName} para {hitHero.heroName}");
            }
            
            // Aplicar el daño
            hitHero.TakeDamage(damage, shooterActorNumber);
            
            // Destruir el proyectil
            if (photonView.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }
        else if (hitCreep != null)
        {
            Debug.Log($"[ProjectileController] Golpeó a una criatura neutral en colisión física: {hitCreep.creepName}");
            
            // Aplicar el daño directamente a la criatura neutral
            hitCreep.TakeDamage(damage, attacker);
            
            // Destruir el proyectil
            if (photonView.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        // Obtener los datos de inicialización
        object[] instantiationData = info.photonView.InstantiationData;
        if (instantiationData != null && instantiationData.Length >= 4)
        {
            float damage = (float)instantiationData[0];
            int shooterViewID = (int)instantiationData[1];
            int targetViewID = (int)instantiationData[2];
            int attackerViewID = (int)instantiationData[3];
            
            // Obtener el transform del disparador
            PhotonView shooterView = PhotonView.Find(shooterViewID);
            Transform shooter = shooterView != null ? shooterView.transform : null;
            
            // Inicializar el proyectil
            Initialize(damage, shooter, info.Sender.ActorNumber, attackerViewID);
        }
        else
        {
            Debug.LogError("[ProjectileController] No se recibieron los datos de inicialización correctos");
        }
    }
} 