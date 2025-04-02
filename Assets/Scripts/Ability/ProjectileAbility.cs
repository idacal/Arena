using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

namespace Photon.Pun.Demo.Asteroids
{
    /// <summary>
    /// Implementación de una habilidad tipo proyectil
    /// </summary>
    public class ProjectileAbility : AbilityBase
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
        
        // Lista de objetivos ya impactados para evitar múltiples impactos
        protected List<int> hitTargets = new List<int>();
        
        // Variables privadas
        private Rigidbody projectileRigidbody;   // Rigidbody para física (opcional)
        private bool projectileInitialized = false;      // Flag para evitar inicializar múltiples veces
        
        protected override void OnAbilityInitialized()
        {
            if (projectileInitialized) return;
            projectileInitialized = true;
            
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
        
        // Usar Update en lugar de AbilityUpdate
        protected virtual void Update()
        {
            // Si no estamos en movimiento, no procesar la física
            if (!isMoving || !projectileInitialized) return;
            
            // Actualizar movimiento
            UpdateMovement();
            
            // Verificar colisiones
            CheckCollisions();
        }
        
        /// <summary>
        /// Actualiza el movimiento del proyectil
        /// </summary>
        protected virtual void UpdateMovement()
        {
            // Si tenemos Rigidbody, usamos física para el movimiento
            if (projectileRigidbody != null && !projectileRigidbody.isKinematic)
            {
                // Para proyectiles con gravedad, ajustar la dirección incluyendo gravedad
                if (useGravity)
                {
                    projectileRigidbody.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);
                    
                    // Actualizar la dirección actual basado en la velocidad del rigidbody
                    if (projectileRigidbody.velocity.sqrMagnitude > 0.1f)
                    {
                        currentDirection = projectileRigidbody.velocity.normalized;
                        transform.rotation = Quaternion.LookRotation(currentDirection);
                    }
                }
                else
                {
                    // Para proyectiles sin gravedad, mantener velocidad constante
                    projectileRigidbody.velocity = currentDirection * speed;
                }
            }
            else
            {
                // Si no tenemos Rigidbody, mover usando Transform directamente
                transform.position += currentDirection * speed * Time.deltaTime;
            }
        }
        
        /// <summary>
        /// Verifica colisiones con objetos en la escena
        /// </summary>
        protected virtual void CheckCollisions()
        {
            if (!photonView.IsMine) return;
            
            // Usar Physics.SphereCast para detectar colisiones a lo largo de la trayectoria
            RaycastHit hit;
            float rayDistance = speed * Time.deltaTime; // Distancia a verificar basada en velocidad
            
            if (Physics.SphereCast(transform.position, collisionRadius, currentDirection, out hit, rayDistance, collisionLayers))
            {
                // Verificar si es un héroe
                HeroBase hitHero = hit.collider.GetComponent<HeroBase>();
                
                // Si es un héroe y no es el lanzador, procesar impacto
                if (hitHero != null && (!caster || hitHero != caster))
                {
                    // Evitar impactos múltiples con el mismo objetivo
                    int targetId = hitHero.photonView.ViewID;
                    if (!hitTargets.Contains(targetId))
                    {
                        // Marcar como impactado
                        hitTargets.Add(targetId);
                        
                        // Procesar el impacto
                        ProcessImpact(hitHero);
                        
                        // Destruir si no atraviesa objetivos
                        if (!penetratesTargets)
                        {
                            isMoving = false;
                            DestroyAbility();
                        }
                        else
                        {
                            // Incrementar contador de penetraciones
                            penetrationCount++;
                            
                            // Si alcanzamos el límite, destruir
                            if (penetrationCount >= maxPenetrations)
                            {
                                isMoving = false;
                                DestroyAbility();
                            }
                        }
                    }
                }
                // Si impactamos con algo que no es un héroe
                else if (hitHero == null)
                {
                    // Crear efecto de impacto
                    if (impactEffectPrefab != null)
                    {
                        Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                    }
                    
                    // Informar a todos los clientes sobre el impacto con el entorno
                    photonView.RPC("RPC_OnHitEnvironment", RpcTarget.All, hit.point, hit.normal);
                    
                    // Detener movimiento y destruir
                    isMoving = false;
                    DestroyAbility();
                }
            }
        }
        
        /// <summary>
        /// Método para activar efectos visuales
        /// </summary>
        protected virtual void ActivateVisualEffects()
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
        
        /// <summary>
        /// Callback de Unity para colisiones físicas (complemento a la detección manual)
        /// </summary>
        protected virtual void OnCollisionEnter(Collision collision)
        {
            // Solo procesar si tenemos control sobre este proyectil y está en movimiento
            if (!photonView.IsMine || !isMoving)
                return;
                
            // Verificar si impactamos con un héroe
            HeroBase hitHero = collision.gameObject.GetComponent<HeroBase>();
            
            // Si es un héroe y no es el lanzador, procesar impacto
            if (hitHero != null && (!caster || hitHero != caster))
            {
                // Evitar impactos múltiples con el mismo objetivo
                int targetId = hitHero.photonView.ViewID;
                if (!hitTargets.Contains(targetId))
                {
                    // Marcar como impactado
                    hitTargets.Add(targetId);
                    
                    // Procesar el impacto
                    ProcessImpact(hitHero);
                    
                    // Destruir si no atraviesa objetivos
                    if (!penetratesTargets)
                    {
                        isMoving = false;
                        DestroyAbility();
                    }
                    else
                    {
                        // Incrementar contador de penetraciones
                        penetrationCount++;
                        
                        // Si alcanzamos el límite, destruir
                        if (penetrationCount >= maxPenetrations)
                        {
                            isMoving = false;
                            DestroyAbility();
                        }
                    }
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
                    
                    // Crear el efecto localmente
                    if (impactEffectPrefab != null)
                    {
                        Instantiate(impactEffectPrefab, hitPoint, Quaternion.LookRotation(hitNormal));
                    }
                    
                    // Informar colisión mediante RPC
                    photonView.RPC("RPC_OnHitEnvironment", RpcTarget.All, hitPoint, hitNormal);
                }
                
                // Detener movimiento y destruir
                isMoving = false;
                DestroyAbility();
            }
        }
        
        /// <summary>
        /// Callback de Unity para triggers (complemento a la detección manual)
        /// </summary>
        protected virtual void OnTriggerEnter(Collider other)
        {
            // Solo procesar si tenemos control sobre este proyectil y está en movimiento
            if (!photonView.IsMine || !isMoving)
                return;
                
            // Verificar si impactamos con un héroe
            HeroBase hitHero = other.GetComponent<HeroBase>();
            
            // Si es un héroe y no es el lanzador, procesar impacto
            if (hitHero != null && (!caster || hitHero != caster))
            {
                // Evitar impactos múltiples con el mismo objetivo
                int targetId = hitHero.photonView.ViewID;
                if (!hitTargets.Contains(targetId))
                {
                    // Marcar como impactado
                    hitTargets.Add(targetId);
                    
                    // Procesar el impacto
                    ProcessImpact(hitHero);
                    
                    // Destruir si no atraviesa objetivos
                    if (!penetratesTargets)
                    {
                        isMoving = false;
                        DestroyAbility();
                    }
                    else
                    {
                        // Incrementar contador de penetraciones
                        penetrationCount++;
                        
                        // Si alcanzamos el límite, destruir
                        if (penetrationCount >= maxPenetrations)
                        {
                            isMoving = false;
                            DestroyAbility();
                        }
                    }
                }
            }
        }
        
        // Añade estos métodos RPC para sincronización en red
        
        [PunRPC]
        protected virtual void RPC_InitializeProjectile(Vector3 position, Vector3 direction, float projectileSpeed)
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
            
            // Reproducir sonido de la habilidad
            PlayAbilitySound();
            
            projectileInitialized = true;
            isMoving = true;
        }
        
        [PunRPC]
        protected virtual void RPC_OnHitEnvironment(Vector3 hitPoint, Vector3 hitNormal)
        {
            // Crear efecto de impacto en la posición y normal del hit
            if (impactEffectPrefab != null)
            {
                Instantiate(impactEffectPrefab, hitPoint, Quaternion.LookRotation(hitNormal));
            }
            
            // Reproducir sonido de impacto
            PlayImpactSound();
            
            // Detener el movimiento
            isMoving = false;
        }
        
        /// <summary>
        /// Override de DestroyAbility para manejar partículas correctamente
        /// </summary>
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
                flyingParticles.transform.SetParent(null);
                flyingParticles.Stop();
                
                // Destruir sistema de partículas después de que termine
                var mainModule = flyingParticles.main;
                Destroy(flyingParticles.gameObject, mainModule.duration + mainModule.startLifetime.constantMax);
            }
            
            // Continuar con la destrucción normal
            base.DestroyAbility();
        }
    }
}