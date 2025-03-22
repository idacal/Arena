using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

namespace Photon.Pun.Demo.Asteroids
{
    /// <summary>
    /// Implementación de una habilidad tipo proyectil
    /// </summary>
    public class ProjectileAbility : AbilityBehaviour
    {
        [Header("Projectile Settings")]
        public float speed = 15f;                // Velocidad del proyectil
        public bool useGravity = false;          // Si el proyectil es afectado por gravedad
        public float gravityMultiplier = 1f;     // Multiplicador de gravedad
        public LayerMask collisionLayers;        // Capas con las que colisiona
        public bool penetratesTargets = false;   // Si atraviesa a los objetivos o se detiene al primer impacto
        public int maxPenetrations = 3;          // Máximo número de penetraciones si atraviesa
        public float collisionRadius = 0.5f;     // Radio de colisión del proyectil
        
        [Header("Projectile Visuals")]
        public GameObject projectileModel;       // Modelo visual del proyectil
        public TrailRenderer trailEffect;        // Efecto de estela (opcional)
        public ParticleSystem flyingParticles;   // Partículas en vuelo (opcional)
        
        // Variables protegidas para que las clases derivadas puedan acceder
        protected Vector3 initialPosition;       // Posición inicial del proyectil
        protected Vector3 initialDirection;      // Dirección inicial del proyectil
        protected Vector3 currentDirection;      // Dirección actual del proyectil
        protected int penetrationCount = 0;      // Contador de penetraciones actuales
        protected bool isMoving = true;          // Control de si el proyectil está en movimiento
        
        // Variables privadas
        private Rigidbody projectileRigidbody;   // Rigidbody para física (opcional)
        private bool isInitialized = false;      // Flag para evitar inicializar múltiples veces
        
        protected override void OnAbilityInitialized()
        {
            if (isInitialized) return;
            isInitialized = true;
            
            // Guardar posición y dirección iniciales
            initialPosition = transform.position;
            initialDirection = transform.forward.normalized;
            currentDirection = initialDirection;
            
            // Obtener componente rigidbody si existe
            projectileRigidbody = GetComponent<Rigidbody>();
            
            // Configurar rigidbody si existe
            if (projectileRigidbody != null)
            {
                // Asegurarnos de que esté en modo no-kinematic para que se mueva
                projectileRigidbody.isKinematic = false;
                projectileRigidbody.useGravity = useGravity;
                projectileRigidbody.velocity = currentDirection * speed;
                
                // Si no usamos física completa, desactivar rotación
                if (!useGravity)
                {
                    projectileRigidbody.freezeRotation = true;
                }
            }
            
            // Inicializar en red
            if (photonView.IsMine)
            {
                photonView.RPC("RPC_InitializeProjectile", RpcTarget.Others, 
                    initialPosition, initialDirection, speed);
            }
            
            // Activar efectos visuales
            ActivateVisualEffects();
        }
        
        // Añade estos métodos RPC a tu clase ProjectileAbility

[PunRPC]
private void RPC_InitializeProjectile(Vector3 position, Vector3 direction, float projectileSpeed)
{
    // Configurar posición y dirección para clientes remotos
    transform.position = position;
    initialPosition = position;
    initialDirection = direction.normalized;
    currentDirection = initialDirection;
    speed = projectileSpeed;
    
    // Si hay rigidbody, actualizar su velocidad
    if (projectileRigidbody != null && !projectileRigidbody.isKinematic)
    {
        projectileRigidbody.velocity = currentDirection * speed;
    }
    
    // Orientar en la dirección correcta
    transform.rotation = Quaternion.LookRotation(currentDirection);
    
    // Activar efectos visuales
    ActivateVisualEffects();
    
    isInitialized = true;
    isMoving = true;
}

[PunRPC]
private void RPC_OnHitEnvironment(Vector3 hitPoint, Vector3 hitNormal)
{
    // Crear efecto de impacto en la posición y normal del hit
    if (impactEffectPrefab != null)
    {
        Instantiate(impactEffectPrefab, hitPoint, Quaternion.LookRotation(hitNormal));
    }
    
    // Detener el movimiento
    isMoving = false;
}

private void ActivateVisualEffects()
{
    // Activar modelo si existe
    if (projectileModel != null)
    {
        projectileModel.SetActive(true);
    }
    
    // Activar trail si existe
    if (trailEffect != null)
    {
        trailEffect.enabled = true;
        trailEffect.Clear(); // Limpiar cualquier punto previo
    }
    
    // Activar partículas si existen
    if (flyingParticles != null)
    {
        flyingParticles.gameObject.SetActive(true);
        if (!flyingParticles.isPlaying)
        {
            flyingParticles.Play();
        }
    }
}
        
        protected override void AbilityUpdate()
        {
            if (!isInitialized || !isMoving) return;
            
            // Si usa rigidbody y no es kinematic, dejar que la física lo maneje
            if (projectileRigidbody != null && !projectileRigidbody.isKinematic)
            {
                // Aplicar gravedad manual si está configurada
                if (useGravity)
                {
                    projectileRigidbody.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);
                }
                
                // Actualizar dirección basada en la velocidad del rigidbody
                if (projectileRigidbody.velocity.sqrMagnitude > 0.1f)
                {
                    currentDirection = projectileRigidbody.velocity.normalized;
                    transform.rotation = Quaternion.LookRotation(currentDirection);
                }
            }
            else
            {
                // Si no tiene rigidbody o es kinematic, mover manualmente
                
                // Aplicar gravedad manual si está configurada
                if (useGravity)
                {
                    currentDirection += Physics.gravity * gravityMultiplier * Time.deltaTime;
                    currentDirection.Normalize();
                }
                
                // Mover en la dirección con la velocidad configurada
                transform.position += currentDirection * speed * Time.deltaTime;
                
                // Orientar en la dirección del movimiento
                transform.rotation = Quaternion.LookRotation(currentDirection);
            }
            
            // Verificar colisiones manualmente
            CheckCollisions();
        }
        
        /// <summary>
        /// Verifica colisiones manualmente usando Physics.SphereCast
        /// </summary>
        private void CheckCollisions()
        {
            // Si estamos en un cliente remoto y el dueño está manejando las colisiones, salir
            if (photonView && !photonView.IsMine)
                return;
                
            // Solo continuar si estamos en movimiento
            if (!isMoving) return;
                
            RaycastHit hit;
            float moveDistance = speed * Time.deltaTime;
                
            // Usar SphereCast para mejor detección de colisiones
            if (Physics.SphereCast(transform.position, collisionRadius, currentDirection, out hit, moveDistance, collisionLayers))
            {
                // Verificar si impactamos con un héroe
                HeroBase hitHero = hit.collider.GetComponent<HeroBase>();
                
                // Si es un héroe y no es el lanzador, procesar impacto
                if (hitHero != null && (!caster || hitHero != caster))
                {
                    ProcessImpact(hitHero);
                    
                    // Incrementar contador de penetraciones
                    penetrationCount++;
                    
                    // Si no penetra o alcanzó el máximo, destruir
                    if (!penetratesTargets || penetrationCount >= maxPenetrations)
                    {
                        DestroyAbility();
                    }
                }
                // Si impactamos con algo que no es un héroe
                else if (hitHero == null)
                {
                    // Informar colisión
                    if (photonView.IsMine)
                    {
                        photonView.RPC("RPC_OnHitEnvironment", RpcTarget.All, 
                            hit.point, hit.normal);
                    }
                    
                    // Destruir proyectil
                    DestroyAbility();
                }
            }
        }
        
       
        /// <summary>
        /// Callback de Unity para colisiones físicas (complemento a la detección manual)
        /// </summary>
        void OnCollisionEnter(Collision collision)
        {
            // Solo procesar si tenemos control sobre este proyectil y está en movimiento
            if (!photonView.IsMine || !isMoving)
                return;
                
            // Verificar si impactamos con un héroe
            HeroBase hitHero = collision.gameObject.GetComponent<HeroBase>();
            
            // Si es un héroe y no es el lanzador, procesar impacto
            if (hitHero != null && (!caster || hitHero != caster))
            {
                ProcessImpact(hitHero);
                
                // Incrementar contador de penetraciones
                penetrationCount++;
                
                // Si no penetra o alcanzó el máximo, destruir
                if (!penetratesTargets || penetrationCount >= maxPenetrations)
                {
                    DestroyAbility();
                }
            }
            // Si impactamos con algo que no es un héroe
            else if (hitHero == null)
            {
                // Crear efecto de impacto en el punto de contacto
                if (collision.contacts.Length > 0)
                {
                    Vector3 hitPoint = collision.contacts[0].point;
                    Vector3 hitNormal = collision.contacts[0].normal;
                    
                    // Informar colisión
                    if (photonView.IsMine)
                    {
                        photonView.RPC("RPC_OnHitEnvironment", RpcTarget.All, 
                            hitPoint, hitNormal);
                    }
                }
                
                // Destruir proyectil
                DestroyAbility();
            }
        }
        
        /// <summary>
        /// Callback de Unity para triggers (complemento a la detección manual)
        /// </summary>
        void OnTriggerEnter(Collider other)
        {
            // Solo procesar si tenemos control sobre este proyectil y está en movimiento
            if (!photonView.IsMine || !isMoving)
                return;
                
            // Verificar si impactamos con un héroe
            HeroBase hitHero = other.GetComponent<HeroBase>();
            
            // Si es un héroe y no es el lanzador, procesar impacto
            if (hitHero != null && (!caster || hitHero != caster))
            {
                ProcessImpact(hitHero);
                
                // Incrementar contador de penetraciones
                penetrationCount++;
                
                // Si no penetra o alcanzó el máximo, destruir
                if (!penetratesTargets || penetrationCount >= maxPenetrations)
                {
                    DestroyAbility();
                }
            }
        }
        
        public override void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            // Primero llamar a la implementación base
            base.OnPhotonSerializeView(stream, info);
            
            if (stream.IsWriting)
            {
                // Si somos el dueño, enviar datos adicionales
                stream.SendNext(transform.position);
                stream.SendNext(currentDirection);
                stream.SendNext(isMoving);
                stream.SendNext(speed);
                stream.SendNext(penetrationCount);
            }
            else
            {
                // Si somos un cliente remoto, recibir datos
                Vector3 networkPosition = (Vector3)stream.ReceiveNext();
                Vector3 networkDirection = (Vector3)stream.ReceiveNext();
                bool networkIsMoving = (bool)stream.ReceiveNext();
                float networkSpeed = (float)stream.ReceiveNext();
                int networkPenetrationCount = (int)stream.ReceiveNext();
                
                // Aplicar valores
                // En vez de establecer directamente la posición, usamos Lerp para suavizar
                transform.position = Vector3.Lerp(transform.position, networkPosition, 0.25f);
                currentDirection = networkDirection;
                isMoving = networkIsMoving;
                speed = networkSpeed;
                penetrationCount = networkPenetrationCount;
                
                // Orientar en la dirección recibida
                if (isMoving && currentDirection.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.LookRotation(currentDirection);
                }
                
                // Si tiene rigidbody, actualizar velocidad
                if (projectileRigidbody != null && !projectileRigidbody.isKinematic && isMoving)
                {
                    projectileRigidbody.velocity = currentDirection * speed;
                }
            }
        }
        
        protected override void DestroyAbility()
        {
            // Detener el movimiento
            isMoving = false;
            
            // Si tiene rigidbody, detener
            if (projectileRigidbody != null)
            {
                projectileRigidbody.velocity = Vector3.zero;
                projectileRigidbody.isKinematic = true;
            }
            
            // Detener partículas antes de destruir
            if (flyingParticles != null)
            {
                // Desacoplar sistema de partículas para que termine su animación
                flyingParticles.transform.parent = null;
                flyingParticles.Stop();
                
                // Destruir sistema de partículas después de que termine
                Destroy(flyingParticles.gameObject, flyingParticles.main.duration + flyingParticles.main.startLifetime.constantMax);
            }
            
            // Continuar con la destrucción normal
            base.DestroyAbility();
        }
    }
}