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
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.useGravity = useGravity;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
        
        // Destruir después del tiempo de vida
        Destroy(gameObject, lifetime);
    }
    
    private void Start()
    {
        if (PhotonNetwork.IsConnected && !photonView.IsMine)
        {
            // Desactivar la física en clientes remotos
            if (rb) rb.isKinematic = true;
            return;
        }
        
        // Iniciar movimiento
        if (rb) rb.velocity = transform.forward * speed;
    }
    
    /// <summary>
    /// Inicializa el proyectil con los datos necesarios
    /// </summary>
    /// <param name="damage">Daño que causará el proyectil</param>
    /// <param name="shooter">Referencia al transform del disparador (para identificar al atacante)</param>
    /// <param name="shooterActorNumber">Número de actor Photon del disparador</param>
    public void Initialize(float damage, Transform shooter, int shooterActorNumber)
    {
        Debug.Log($"Proyectil inicializado: Daño={damage}, Shooter={shooter.name}, ActorNumber={shooterActorNumber}");
        this.damage = damage;
        this.shooter = shooter;
        this.shooterActorNumber = shooterActorNumber;
        
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
        // Ignorar colisiones con el disparador
        if (shooter != null && other.gameObject == shooter.gameObject)
            return;
            
        // Ignorar colisiones con otros proyectiles
        if (other.gameObject.layer == gameObject.layer)
            return;
            
        Debug.Log($"Proyectil golpeó a: {other.gameObject.name}, tag: {other.gameObject.tag}");
        
        // Verificar si es un hero
        HeroHealth targetHealth = other.GetComponent<HeroHealth>();
        GameObject hitObject = other.gameObject;
        
        if (targetHealth != null)
        {
            Debug.Log("Golpeó a un héroe con salud");
            
            // Debe tener componente HeroBase para verificar el equipo
            HeroBase targetHero = hitObject.GetComponent<HeroBase>();
            HeroBase shooterHero = shooter?.GetComponent<HeroBase>();
            
            if (targetHero != null && shooterHero != null)
            {
                // Solo dañar a enemigos (usando el sistema de tags)
                if (LayerManager.IsEnemy(shooter.gameObject, hitObject))
                {
                    Debug.Log($"Aplicando daño de {damage} a {hitObject.name}");
                    targetHealth.TakeDamage(damage, shooterActorNumber);
                }
                else
                {
                    Debug.Log($"No se aplica daño a {hitObject.name} porque no es enemigo");
                }
            }
        }
        
        // Crear efecto de impacto
        if (hitEffectPrefab != null && PhotonNetwork.IsConnected)
        {
            if (photonView.IsMine)
            {
                PhotonNetwork.Instantiate(hitEffectPrefab.name, transform.position, Quaternion.identity);
            }
        }
        else if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // Destruir el proyectil al impactar
        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
} 