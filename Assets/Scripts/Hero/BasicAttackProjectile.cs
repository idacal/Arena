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
        
        // Configurar el Rigidbody
        if (rb != null)
        {
            rb.useGravity = useGravity;
            rb.isKinematic = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezeRotation; // Evitar rotaciones no deseadas
            Debug.Log($"[BasicAttackProjectile] Rigidbody configurado - useGravity: {useGravity}, isKinematic: {rb.isKinematic}");
        }
    }
    
    // Configuramos el proyectil basado en los datos de instantiación
    private void Start()
    {
        // Ya no necesitamos configurar el Rigidbody aquí, se hace en Awake
        
        // Obtener datos de instantiación (pasados cuando se crea el proyectil)
        object[] instantiationData = photonView.InstantiationData;
        if (instantiationData != null && instantiationData.Length >= 3)
        {
            damage = (float)instantiationData[0];
            attackerViewID = (int)instantiationData[1];
            targetViewID = (int)instantiationData[2];
            
            Debug.Log($"[BasicAttackProjectile] Inicializado con - Daño: {damage}, AttackerID: {attackerViewID}, TargetID: {targetViewID}");
            
            // Buscar objetivo para establecer dirección
            PhotonView targetView = PhotonView.Find(targetViewID);
            if (targetView != null)
            {
                // Establecer dirección hacia el objetivo
                direction = (targetView.transform.position - transform.position).normalized;
                transform.forward = direction;
                
                // Establecer velocidad inicial
                if (rb != null)
                {
                    rb.velocity = direction * speed;
                    Debug.Log($"[BasicAttackProjectile] Velocidad inicial establecida: {rb.velocity}, Dirección: {direction}");
                }
            }
            else
            {
                Debug.LogWarning("[BasicAttackProjectile] No se encontró el objetivo para el proyectil");
            }
        }
        else
        {
            Debug.LogError("[BasicAttackProjectile] No se recibieron datos de instantiación correctos");
            if (photonView.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
            }
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
        Debug.Log($"[BasicAttackProjectile] Colisión detectada con: {collision.gameObject.name}");
        
        // Solo el dueño del proyectil procesa la colisión
        if (!photonView.IsMine)
        {
            Debug.Log("[BasicAttackProjectile] Colisión ignorada - No es el dueño del proyectil");
            return;
        }
        
        if (hasHit)
        {
            Debug.Log("[BasicAttackProjectile] Colisión ignorada - Ya ha impactado antes");
            return;
        }
        
        // Evitar golpear al lanzador
        PhotonView attackerView = PhotonView.Find(attackerViewID);
        if (attackerView != null && collision.gameObject == attackerView.gameObject)
        {
            Debug.Log("[BasicAttackProjectile] Colisión ignorada - Golpeó al lanzador");
            return;
        }
        
        // Obtener el punto de impacto
        Vector3 hitPoint = collision.contacts[0].point;
        
        // Verificar si golpeamos a un héroe
        HeroBase hitHero = collision.gameObject.GetComponent<HeroBase>();
        
        // Si golpeamos a un héroe, aplicar daño
        if (hitHero != null)
        {
            Debug.Log($"[BasicAttackProjectile] Golpeó a un héroe: {hitHero.name}");
            // Solo aplicar daño si el objetivo es diferente al lanzador
            if (attackerView != null && hitHero.photonView.ViewID != attackerViewID)
            {
                // Aplicar daño usando RPC a todos los clientes
                photonView.RPC("RPC_ApplyDamage", RpcTarget.All, hitHero.photonView.ViewID, damage);
            }
        }
        
        // Notificar a todos los clientes sobre el impacto
        photonView.RPC("RPC_OnProjectileHit", RpcTarget.All, hitPoint);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Solo el dueño del proyectil procesa la colisión
        if (!photonView.IsMine || hasHit) return;
        
        // Evitar golpear al lanzador
        PhotonView attackerView = PhotonView.Find(attackerViewID);
        if (attackerView != null && other.gameObject == attackerView.gameObject) return;
        
        Vector3 hitPoint = other.ClosestPoint(transform.position);
        
        // Verificar si golpeamos a un héroe
        HeroBase hitHero = other.gameObject.GetComponent<HeroBase>();
        
        // Si golpeamos a un héroe, aplicar daño
        if (hitHero != null)
        {
            // Solo aplicar daño si el objetivo es diferente al lanzador
            if (attackerView != null && hitHero.photonView.ViewID != attackerViewID)
            {
                // Aplicar daño usando RPC a todos los clientes
                photonView.RPC("RPC_ApplyDamage", RpcTarget.All, hitHero.photonView.ViewID, damage);
            }
        }
        
        // Notificar a todos los clientes sobre el impacto
        photonView.RPC("RPC_OnProjectileHit", RpcTarget.All, hitPoint);
    }
    
    [PunRPC]
    private void RPC_OnProjectileHit(Vector3 hitPoint)
    {
        Debug.Log($"[BasicAttackProjectile] RPC_OnProjectileHit recibido en {(photonView.IsMine ? "dueño" : "cliente")}");
        
        // Marcar como golpeado y desactivar inmediatamente
        hasHit = true;
        
        // Detener el movimiento y la física
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        
        // Desactivar colliders
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
        
        // Desactivar renderer para que el proyectil sea invisible inmediatamente
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = false;
        }
        
        // Desactivar trail
        if (trailEffect != null)
        {
            trailEffect.emitting = false;
        }
        
        // Crear efecto visual
        if (hitEffectPrefab != null)
        {
            GameObject effect = Instantiate(hitEffectPrefab, hitPoint, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        // Si soy el dueño, destruir el objeto después de un pequeño delay
        if (photonView.IsMine)
        {
            // Destruir el objeto en red después de un pequeño delay
            photonView.RPC("RPC_DestroyProjectile", RpcTarget.All);
        }
    }
    
    [PunRPC]
    private void RPC_DestroyProjectile()
    {
        Debug.Log($"[BasicAttackProjectile] RPC_DestroyProjectile recibido en {(photonView.IsMine ? "dueño" : "cliente")}");
        
        // Si soy el dueño, destruir el objeto en la red
        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
        else
        {
            // Si no soy el dueño, destruir localmente
            Destroy(gameObject);
        }
    }
    
    #region Photon RPCs
    
    [PunRPC]
    private void RPC_ApplyDamage(int targetViewID, float damageAmount)
    {
        // Buscar el objetivo por su ViewID
        PhotonView targetView = PhotonView.Find(targetViewID);
        if (targetView == null) return;
        
        // Obtener componente HeroBase
        HeroBase targetHero = targetView.GetComponent<HeroBase>();
        if (targetHero == null) return;
        
        // Aplicar daño usando el ViewID del atacante
        targetHero.TakeDamage(damageAmount, attackerViewID);
        
        // Debug log
        Debug.Log($"[BasicAttackProjectile] Aplicando {damageAmount} de daño al ViewID {targetViewID} desde el atacante {attackerViewID}");
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
            stream.SendNext(gameObject.activeSelf);
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
            bool isActive = (bool)stream.ReceiveNext();
            
            // Aplicar estado activo/inactivo
            if (gameObject.activeSelf != isActive)
            {
                gameObject.SetActive(isActive);
            }
        }
    }
} 