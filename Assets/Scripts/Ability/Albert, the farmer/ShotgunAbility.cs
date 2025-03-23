using UnityEngine;
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
        
        // Contador interno de impactos
        private int hitCounter = 0;
        
        protected override void OnAbilityInitialized()
        {
            // IMPORTANTE: No llamar a base.OnAbilityInitialized() para evitar 
            // conflictos con la inicialización. En su lugar, replicamos el código necesario.
            
            if (photonView.IsMine) 
            {
                Debug.Log($"ShotgunAbility: Inicializando proyectil. IsMine={photonView.IsMine}");
                
                // Guardar posición y dirección iniciales
                initialPosition = transform.position;
                initialDirection = transform.forward.normalized;
                currentDirection = initialDirection;
                
                // Forzar un valor alto de velocidad para la escopeta
                speed = Mathf.Max(speed, 20f);
                
                // Obtener y configurar rigidbody
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.useGravity = useGravity;
                    rb.velocity = currentDirection * speed;
                    
                    if (!useGravity)
                    {
                        rb.freezeRotation = true;
                    }
                }
                
                // Enviar un solo RPC para sincronizar la inicialización
                photonView.RPC("RPC_InitializeShotgunProjectile", RpcTarget.AllBuffered, 
                    initialPosition, initialDirection, speed);
            }
            
            // Activar efectos visuales (se hace en todos los clientes)
            PlayShotgunEffects();
            
            // Marca que la habilidad está en movimiento
            isMoving = true;
        }
        
        [PunRPC]
        private void RPC_InitializeShotgunProjectile(Vector3 position, Vector3 direction, float projectileSpeed)
        {
            // Esta función combina la inicialización del proyectil y los efectos de la escopeta
            Debug.Log("ShotgunAbility: RPC_InitializeShotgunProjectile recibido");
            
            // No re-inicializar si es el dueño (ya lo hizo en OnAbilityInitialized)
            if (photonView.IsMine)
                return;
                
            // Configurar posición y dirección
            transform.position = position;
            initialPosition = position;
            initialDirection = direction.normalized;
            currentDirection = initialDirection;
            speed = projectileSpeed;
            
            // Configurar rigidbody si existe
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
                trailEffect.Clear(); // Limpiar puntos anteriores
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
            
            // Reproducir efectos de la escopeta
            PlayShotgunEffects();
            
            // Marca que está en movimiento
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
        }
        
        protected override void ProcessImpact(HeroBase target)
        {
            // Incrementar contador de impactos
            hitCounter++;
            
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
            // Crear efecto de impacto en la posición y normal del hit
            if (impactEffectPrefab != null)
            {
                GameObject impactEffect = Instantiate(impactEffectPrefab, hitPoint, Quaternion.LookRotation(hitNormal));
                
                // Destruir después de un tiempo
                Destroy(impactEffect, 2f);
            }
            
            // Detener el movimiento
            isMoving = false;
            
            // Desactivar efectos visuales
            if (trailEffect != null)
            {
                trailEffect.enabled = false;
            }
            
            if (flyingParticles != null && flyingParticles.isPlaying)
            {
                flyingParticles.Stop();
            }
        }
        
        // Nos ceñimos a la implementación de colisiones de la clase base
        // No sobrescribimos OnCollisionEnter/OnTriggerEnter
        
        // Sobrescribir DestroyAbility para manejar limpieza específica de la escopeta
        protected override void DestroyAbility()
        {
            // Detener efectos de la escopeta
            if (muzzleFlash != null)
            {
                muzzleFlash.Stop();
            }
            
            // Llamar a la implementación base para la destrucción general
            base.DestroyAbility();
        }
    }
}