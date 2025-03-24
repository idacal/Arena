using UnityEngine;
using Photon.Pun;
using System.Collections;
using Photon.Pun.Demo.Asteroids;

/// <summary>
/// Clase que maneja el comportamiento de los proyectiles de ataque básico
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BasicAttackProjectile : MonoBehaviourPun, IPunObservable
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private float lifetime = 5f;
    [SerializeField] private bool useGravity = false;
    [SerializeField] private LayerMask collisionMask;
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject hitEffectPrefab;
    [SerializeField] private TrailRenderer trailEffect;
    
    // Propiedades privadas
    private float damage;
    private int attackerViewID;
    private int targetViewID;
    private Vector3 direction;
    private Rigidbody rb;
    private bool hasHit = false;
    
    // Inicialización
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    
    // Configuramos el proyectil basado en los datos de instantiación
    private void Start()
    {
        // Inicializar físicas
        rb.useGravity = useGravity;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        
        // Obtener datos de instantiación (pasados cuando se crea el proyectil)
        object[] instantiationData = photonView.InstantiationData;
        if (instantiationData != null && instantiationData.Length >= 3)
        {
            damage = (float)instantiationData[0];
            attackerViewID = (int)instantiationData[1];
            targetViewID = (int)instantiationData[2];
            
            // Buscar objetivo para establecer dirección
            PhotonView targetView = PhotonView.Find(targetViewID);
            if (targetView != null)
            {
                // Establecer dirección hacia el objetivo
                direction = (targetView.transform.position - transform.position).normalized;
                transform.forward = direction;
            }
            
            // Establecer velocidad inicial
            rb.velocity = direction * speed;
        }
        else
        {
            Debug.LogError("BasicAttackProjectile: No se recibieron datos de instantiación correctos");
            PhotonNetwork.Destroy(gameObject);
            return;
        }
        
        // Auto-destrucción después de tiempo máximo
        Destroy(gameObject, lifetime);
        
        // Si tenemos trail, asegurarnos que esté activo
        if (trailEffect != null)
        {
            trailEffect.enabled = true;
            trailEffect.emitting = true;
        }
    }
    
    private void Update()
    {
        // Si no ha golpeado nada, actualizar su posición/rotación
        if (!hasHit && photonView.IsMine)
        {
            // Opcional: Corregir trayectoria hacia el objetivo si se mueve
            if (targetViewID != 0)
            {
                PhotonView targetView = PhotonView.Find(targetViewID);
                if (targetView != null)
                {
                    // Ajustar levemente la dirección hacia el objetivo (homing suave)
                    Vector3 newDirection = (targetView.transform.position - transform.position).normalized;
                    direction = Vector3.Lerp(direction, newDirection, Time.deltaTime * 2f);
                    transform.forward = direction;
                    rb.velocity = direction * speed;
                }
            }
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        // Solo el dueño del proyectil procesa la colisión
        if (!photonView.IsMine || hasHit) return;
        
        // Evitar golpear al lanzador
        PhotonView attackerView = PhotonView.Find(attackerViewID);
        if (attackerView != null && collision.gameObject == attackerView.gameObject) return;
        
        // Verificar si golpeamos a un héroe
        HeroBase hitHero = collision.gameObject.GetComponent<HeroBase>();
        
        // Si golpeamos a un héroe, aplicar daño
        if (hitHero != null)
        {
            // Solo aplicar daño si el objetivo es diferente al lanzador
            if (attackerView != null && hitHero.photonView.ViewID != attackerViewID)
            {
                // Obtener el punto de impacto
                Vector3 hitPoint = collision.contacts[0].point;
                
                // Intentar aplicar daño
                hitHero.TakeDamage(damage, attackerView.GetComponent<HeroBase>());
                
                // Crear efecto visual de impacto
                CreateHitEffect(hitPoint);
                
                // Marcar como golpeado para evitar múltiples hits
                hasHit = true;
                
                // Destruir el proyectil
                DestroyProjectile();
            }
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        ProcessHit(other.gameObject, other.ClosestPoint(transform.position));
    }
    
    // Procesar colisión/trigger con algo
    private void ProcessHit(GameObject hitObject, Vector3 hitPoint)
    {
        // Solo el dueño del proyectil procesa la colisión
        if (!photonView.IsMine || hasHit) return;
        
        // Evitar golpear al lanzador
        PhotonView attackerView = PhotonView.Find(attackerViewID);
        if (attackerView != null && hitObject == attackerView.gameObject) return;
        
        // Verificar si golpeamos a un héroe
        HeroBase hitHero = hitObject.GetComponent<HeroBase>();
        
        // Si golpeamos a un héroe, aplicar daño
        if (hitHero != null)
        {
            // Solo aplicar daño si el objetivo es diferente al lanzador
            if (attackerView != null && hitHero.photonView.ViewID != attackerViewID)
            {
                // Intentar aplicar daño
                hitHero.TakeDamage(damage, attackerView.GetComponent<HeroBase>());
            }
        }
        
        // Marcar como golpeado para evitar múltiples hits
        hasHit = true;
        
        // Crear efecto visual de impacto
        CreateHitEffect(hitPoint);
        
        // Destruir el proyectil
        DestroyProjectile();
    }
    
    // Crea el efecto visual de impacto
    private void CreateHitEffect(Vector3 hitPoint)
    {
        // RPC para que todos los clientes vean el efecto
        photonView.RPC("RPC_CreateHitEffect", RpcTarget.All, hitPoint);
    }
    
    // Destruye el proyectil
    private void DestroyProjectile()
    {
        // Detener movimiento
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
        }
        
        // Desactivar colliders
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
        
        // Desactivar trail
        if (trailEffect != null)
        {
            trailEffect.emitting = false;
        }
        
        // Destruir objeto en red
        StartCoroutine(DestroyAfterTrail());
    }
    
    // Espera a que el trail se complete antes de destruir
    private IEnumerator DestroyAfterTrail()
    {
        // Esperar tiempo para que el trail termine
        yield return new WaitForSeconds(0.5f);
        
        // Destruir en red
        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
    
    #region Photon RPCs
    
    [PunRPC]
    private void RPC_CreateHitEffect(Vector3 hitPoint)
    {
        // Crear efecto visual en el punto de impacto
        if (hitEffectPrefab != null)
        {
            Instantiate(hitEffectPrefab, hitPoint, Quaternion.identity);
        }
        else
        {
            // Efecto básico si no hay prefab
            GameObject basicEffect = new GameObject("BasicHitEffect");
            basicEffect.transform.position = hitPoint;
            
            // Crear partículas básicas
            ParticleSystem particles = basicEffect.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startSize = 0.3f;
            main.startLifetime = 0.5f;
            main.startSpeed = 2f;
            
            // Añadir luz temporal
            Light light = basicEffect.AddComponent<Light>();
            light.color = Color.yellow;
            light.intensity = 2f;
            light.range = 3f;
            
            // Destruir efecto después de un tiempo
            Destroy(basicEffect, 1f);
        }
    }
    
    #endregion
    
    // Implementación de IPunObservable para sincronización
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Datos que enviamos
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(rb.velocity);
            stream.SendNext(hasHit);
        }
        else
        {
            // Datos que recibimos
            transform.position = (Vector3)stream.ReceiveNext();
            transform.rotation = (Quaternion)stream.ReceiveNext();
            
            Vector3 receivedVelocity = (Vector3)stream.ReceiveNext();
            if (rb != null && !rb.isKinematic)
            {
                rb.velocity = receivedVelocity;
            }
            
            hasHit = (bool)stream.ReceiveNext();
            
            // Si el proyectil ya impactó, desactivar sistemas visuales
            if (hasHit)
            {
                if (trailEffect != null)
                {
                    trailEffect.emitting = false;
                }
            }
        }
    }
} 