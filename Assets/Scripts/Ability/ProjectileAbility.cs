using UnityEngine;
using System.Collections.Generic;

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
        
        // Variables privadas
        private Vector3 direction;               // Dirección del proyectil
        private Rigidbody projectileRigidbody;   // Rigidbody para física (opcional)
        private int penetrationCount = 0;        // Contador de penetraciones actuales
        
        protected override void OnAbilityInitialized()
        {
            // Configuración inicial
            direction = transform.forward;
            
            // Obtener componente rigidbody si existe
            projectileRigidbody = GetComponent<Rigidbody>();
            
            // Configurar rigidbody si existe
            if (projectileRigidbody != null)
            {
                projectileRigidbody.useGravity = useGravity;
                projectileRigidbody.velocity = direction * speed;
                
                // Si no usamos física completa, desactivar rotación
                if (!useGravity)
                {
                    projectileRigidbody.freezeRotation = true;
                }
            }
            
            // Activar efectos visuales
            if (trailEffect != null)
            {
                trailEffect.enabled = true;
            }
            
            if (flyingParticles != null)
            {
                flyingParticles.Play();
            }
        }
        
        protected override void AbilityUpdate()
        {
            // Si no usa rigidbody, mover manualmente
            if (projectileRigidbody == null || projectileRigidbody.isKinematic)
            {
                // Mover en la dirección con la velocidad configurada
                transform.position += direction * speed * Time.deltaTime;
                
                // Aplicar gravedad manual si está configurada
                if (useGravity)
                {
                    direction += Physics.gravity * gravityMultiplier * Time.deltaTime;
                }
            }
            
            // Orientar siempre en la dirección del movimiento
            if (projectileRigidbody == null || !useGravity)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
            else if (projectileRigidbody != null && projectileRigidbody.velocity.sqrMagnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(projectileRigidbody.velocity);
            }
            
            // Verificar colisiones manualmente (más preciso que OnCollisionEnter para proyectiles rápidos)
            CheckCollisions();
        }
        
        /// <summary>
        /// Verifica colisiones manualmente usando Physics.SphereCast
        /// </summary>
        private void CheckCollisions()
        {
            // Si estamos en un cliente remoto, no hacemos detección de colisiones
            if (photonView && !photonView.IsMine)
                return;
                
            RaycastHit hit;
            Vector3 movement = (projectileRigidbody != null && !projectileRigidbody.isKinematic) 
                ? projectileRigidbody.velocity.normalized 
                : direction;
                
            float moveDistance = (projectileRigidbody != null && !projectileRigidbody.isKinematic)
                ? projectileRigidbody.velocity.magnitude * Time.deltaTime
                : speed * Time.deltaTime;
                
            // Usar SphereCast para mejor detección de colisiones
            if (Physics.SphereCast(transform.position, collisionRadius, movement, out hit, moveDistance, collisionLayers))
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
                    // Crear efecto de impacto en la posición y normal del hit
                    if (impactEffectPrefab != null)
                    {
                        Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                    }
                    
                    // Destruir proyectil ya que impactó con el escenario
                    DestroyAbility();
                }
            }
        }
        
        /// <summary>
        /// Callback de Unity para colisiones físicas (complemento a la detección manual)
        /// </summary>
        void OnCollisionEnter(Collision collision)
        {
            // Solo procesar si tenemos control sobre este proyectil
            if (photonView && !photonView.IsMine)
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
                if (impactEffectPrefab != null && collision.contacts.Length > 0)
                {
                    Instantiate(impactEffectPrefab, collision.contacts[0].point, 
                        Quaternion.LookRotation(collision.contacts[0].normal));
                }
                
                // Destruir proyectil ya que impactó con el escenario
                DestroyAbility();
            }
        }
        
        /// <summary>
        /// Callback de Unity para triggers (complemento a la detección manual)
        /// </summary>
        void OnTriggerEnter(Collider other)
        {
            // Solo procesar si tenemos control sobre este proyectil
            if (photonView && !photonView.IsMine)
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
        
        protected override void DestroyAbility()
        {
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