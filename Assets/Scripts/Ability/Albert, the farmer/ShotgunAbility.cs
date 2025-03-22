using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

namespace Photon.Pun.Demo.Asteroids
{
    public class ShotgunAbility : ProjectileAbility
    {
        [Header("Shotgun Settings")]
        public float knockbackForce = 5f;      // Fuerza del empuje
        public float knockbackDuration = 0.5f;  // Duración del empuje
        
        // Variables para efectos visuales específicos de la escopeta
        [Header("Shotgun Visual Effects")]
        public ParticleSystem muzzleFlash;      // Efecto de fogonazo
        public AudioClip shotgunSound;          // Sonido de disparo
        public float shellEjectionDelay = 0.2f; // Retraso para eyección de cartucho
        
        // Contador interno de impactos
        private int hitCounter = 0;
        private bool hasHitSomething = false;
        
        // Variable propia para rastrear inicialización
        private bool shotgunInitialized = false;
        
        protected override void OnAbilityInitialized()
        {
            Debug.Log($"ShotgunAbility: Inicializando proyectil. IsMine={photonView.IsMine}");
            
            // Llamar a la inicialización base para configurar el proyectil
            base.OnAbilityInitialized();
            
            // Configuración específica de la escopeta
            if (photonView.IsMine)
            {
                // Forzar un valor alto de velocidad para la escopeta
                speed = Mathf.Max(speed, 20f);
                
                // Actualizar la velocidad del rigidbody si existe
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    rb.velocity = initialDirection * speed;
                }
                
                // Sincronizar datos específicos de la escopeta
                photonView.RPC("RPC_InitializeShotgun", RpcTarget.AllBuffered);
            }
            
            // Reproducir efectos visuales de disparo
            PlayShotgunEffects();
            
            // Marcar como inicializado
            shotgunInitialized = true;
        }
        
        [PunRPC]
        private void RPC_InitializeShotgun()
        {
            Debug.Log("ShotgunAbility: RPC_InitializeShotgun recibido");
            
            // Asegurar que el modelo del proyectil sea visible
            if (projectileModel != null)
            {
                projectileModel.SetActive(true);
            }
            
            // Asegurar que el trail esté activo
            if (trailEffect != null)
            {
                trailEffect.Clear(); // Limpiar puntos anteriores
                trailEffect.enabled = true;
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
            
            // Reproducir efectos iniciales
            PlayShotgunEffects();
            
            // Marcar como inicializado
            shotgunInitialized = true;
        }
        
        // Añadir el método RPC faltante
        [PunRPC]
        private void RPC_InitializeProjectile(Vector3 position, Vector3 direction, float projectileSpeed)
        {
            Debug.Log("ShotgunAbility: RPC_InitializeProjectile recibido");
            
            // Configurar posición y dirección para clientes remotos
            transform.position = position;
            initialPosition = position;
            initialDirection = direction.normalized;
            currentDirection = initialDirection;
            speed = projectileSpeed;
            
            // Si hay rigidbody, actualizar su velocidad
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null && !rb.isKinematic)
            {
                rb.velocity = currentDirection * speed;
            }
            
            // Orientar en la dirección correcta
            transform.rotation = Quaternion.LookRotation(currentDirection);
            
            // Asegurar que el modelo del proyectil sea visible
            if (projectileModel != null)
            {
                projectileModel.SetActive(true);
            }
            
            // Asegurar que el trail esté activo
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
            
            // Reproducir efectos de la escopeta también
            PlayShotgunEffects();
            
            // Marcar como inicializado con nuestra propia variable
            shotgunInitialized = true;
            isMoving = true;
        }
        
        private void PlayShotgunEffects()
        {
            // Reproducir fogonazo si existe
            if (muzzleFlash != null && !muzzleFlash.isPlaying)
            {
                muzzleFlash.Play();
            }
            
            // Reproducir sonido si existe
            if (shotgunSound != null)
            {
                AudioSource audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
                audioSource.PlayOneShot(shotgunSound);
            }
            
            // Programar eyección de cartucho (podría ser otra partícula)
            if (shellEjectionDelay > 0)
            {
                Invoke("EjectShell", shellEjectionDelay);
            }
        }
        
        private void EjectShell()
        {
            // Aquí podríamos instanciar un efecto de eyección de cartucho
            // (Este método es un placeholder para futuras mejoras)
        }
        
        protected override void AbilityUpdate()
        {
            // Verificar si está inicializado
            if (!shotgunInitialized || !isMoving)
                return;
                
            // Llamar al update base para movimiento y colisiones
            base.AbilityUpdate();
            
            // Lógica específica de la escopeta (si la hubiera)
            // Por ejemplo, podríamos añadir una dispersión de perdigones
            // o efectos de partículas que sigan al proyectil
        }
        
        protected override void ProcessImpact(HeroBase target)
        {
            // Incrementar contador de impactos
            hitCounter++;
            hasHitSomething = true;
            
            // Aplicar daño base
            base.ProcessImpact(target);
            
            // Calcular dirección de knockback
            Vector3 knockbackDirection = (target.transform.position - transform.position).normalized;
            knockbackDirection.y = 0; // Para evitar empujar hacia arriba o abajo
            
            // Si somos el dueño, sincronizar el knockback
            if (photonView.IsMine)
            {
                photonView.RPC("RPC_ApplyKnockback", RpcTarget.All, target.photonView.ViewID, knockbackDirection);
            }
            
            // Crear efecto de impacto
            if (impactEffectPrefab != null)
            {
                Instantiate(impactEffectPrefab, target.transform.position + Vector3.up, Quaternion.identity);
            }
            
            // Si no penetra o ha alcanzado el máximo, destruir
            if (!penetratesTargets || hitCounter >= maxPenetrations)
            {
                DestroyAbility();
            }
        }
        
        [PunRPC]
        private void RPC_ApplyKnockback(int targetViewID, Vector3 knockbackDirection)
        {
            // Encontrar el objetivo por su ViewID
            PhotonView targetView = PhotonView.Find(targetViewID);
            if (targetView != null)
            {
                HeroBase target = targetView.GetComponent<HeroBase>();
                if (target != null)
                {
                    // Aplicar knockback
                    Rigidbody targetRb = target.GetComponent<Rigidbody>();
                    if (targetRb != null && !targetRb.isKinematic)
                    {
                        targetRb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
                    }
                    
                    // Mover al personaje directamente
                    target.transform.position += knockbackDirection * knockbackForce * 0.2f;
                    
                    // Pausar el NavMeshAgent brevemente para el efecto de aturdimiento
                    HeroMovementController targetMovement = target.GetComponent<HeroMovementController>();
                    if (targetMovement != null)
                    {
                        targetMovement.ApplyStun(knockbackDuration);
                    }
                }
            }
        }
        
        [PunRPC]
        private void RPC_OnHitEnvironment(Vector3 hitPoint, Vector3 hitNormal)
        {
            Debug.Log("ShotgunAbility: RPC_OnHitEnvironment recibido");
            
            // Crear efecto de impacto en la posición y normal del hit
            if (impactEffectPrefab != null)
            {
                GameObject impactEffect = Instantiate(impactEffectPrefab, hitPoint, Quaternion.LookRotation(hitNormal));
                
                // Destruir después de un tiempo
                Destroy(impactEffect, 2f);
            }
            
            // Detener el movimiento del objeto
            // Primero comprobar si el Rigidbody es kinematic antes de cambiar su velocidad
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                if (rb.isKinematic)
                {
                    // Si ya es kinematic, no intentamos cambiar su velocidad
                    isMoving = false;
                }
                else
                {
                    // Si no es kinematic, podemos cambiar su velocidad y luego hacerlo kinematic
                    rb.velocity = Vector3.zero;
                    rb.isKinematic = true;
                }
            }
            else
            {
                // Si no hay rigidbody, solo marcar que no está en movimiento
                isMoving = false;
            }
            
            // Desactivar efectos visuales
            TrailRenderer trail = GetComponent<TrailRenderer>();
            if (trail != null)
            {
                trail.enabled = false;
            }
            
            ParticleSystem particles = GetComponent<ParticleSystem>();
            if (particles != null && particles.isPlaying)
            {
                particles.Stop();
            }
        }
        
        // Implementamos nuestro propio OnCollisionEnter para evitar problemas de acceso protegido
        void OnCollisionEnter(Collision collision)
        {
            // Solo procesar si tenemos control sobre este proyectil y está en movimiento
            if (!photonView.IsMine || !isMoving)
                return;
                
            // Verificar si impactamos con un héroe
            HeroBase hitHero = collision.gameObject.GetComponent<HeroBase>();
            
            // Marcar que hemos golpeado algo
            hasHitSomething = true;
            
            // Si es un héroe y no es el lanzador, procesar impacto
            if (hitHero != null && (!caster || hitHero != caster))
            {
                ProcessImpact(hitHero);
                
                // Incrementar contador de impactos
                hitCounter++;
                
                // Si no penetra o alcanzó el máximo, destruir
                if (!penetratesTargets || hitCounter >= maxPenetrations)
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
                    
                    // Crear efecto de impacto si existe
                    if (impactEffectPrefab != null)
                    {
                        Instantiate(impactEffectPrefab, hitPoint, Quaternion.LookRotation(hitNormal));
                    }
                    
                    // Informar colisión mediante RPC
                    if (photonView.IsMine)
                    {
                        photonView.RPC("RPC_OnHitEnvironment", RpcTarget.All, hitPoint, hitNormal);
                    }
                }
                
                // Destruir proyectil
                DestroyAbility();
            }
        }
        
        // También implementamos OnTriggerEnter por si necesitamos colisiones con triggers
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
                
                // Incrementar contador de impactos
                hitCounter++;
                
                // Si no penetra o alcanzó el máximo, destruir
                if (!penetratesTargets || hitCounter >= maxPenetrations)
                {
                    DestroyAbility();
                }
            }
        }
        
        // Sobrescribir DestroyAbility para manejar limpieza específica de la escopeta
        protected override void DestroyAbility()
        {
            // Limpiar efectos específicos de la escopeta
            if (muzzleFlash != null)
            {
                muzzleFlash.Stop();
            }
            
            // Cancelar cualquier Invoke pendiente
            CancelInvoke();
            
            // Llamar a la implementación base para la destrucción general
            base.DestroyAbility();
        }
    }
}