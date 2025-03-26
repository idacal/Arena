using UnityEngine;
using Photon.Pun;
using System.Collections;

namespace Photon.Pun.Demo.Asteroids
{
    /// <summary>
    /// Implementación de la habilidad de escopeta para Albert
    /// </summary>
    public class AlbertShotgunAbility : ProjectileAbility
    {
        [Header("Shotgun Settings")]
        public float knockbackForce = 5f;        // Fuerza del empuje
        public float knockbackDuration = 0.5f;   // Duración del efecto de aturdimiento
        public float selfKnockbackForce = 8f;    // Aumentado de 3 a 8 para más impulso inicial
        public float selfKnockbackDuration = 0.8f; // Aumentado a 0.8 para que dure más el efecto
        public float selfKnockbackDrag = 2f;     // Reducido de 5 a 2 para menos fricción
        public AnimationCurve selfKnockbackCurve = new AnimationCurve(
            new Keyframe(0, 1),                  // Fuerza máxima al inicio
            new Keyframe(0.3f, 0.7f),           // Mantiene más fuerza por más tiempo
            new Keyframe(0.6f, 0.3f),           // Desacelera más gradualmente
            new Keyframe(1, 0)                   // Termina suavemente
        );
        public Color projectileColor = Color.red; // Color del proyectil de la escopeta
        
        [Header("Prefabs and Effects")]
        public GameObject projectilePrefab;      // Prefab del proyectil
        public GameObject muzzleFlashPrefab;     // Prefab del fogonazo
        public AudioClip shotgunSound;           // Sonido de disparo
        
        [Header("Debug Settings")]
        public bool showDebugLogs = false;       // Activar logs de debug
        
        // Variables internas
        private GameObject projectileVisual;     // Referencia al objeto visual del proyectil
        private bool hasHit = false;             // Flag para controlar si ya impactó
        
        /// <summary>
        /// Inicialización específica de la habilidad de escopeta
        /// </summary>
        protected override void OnAbilityInitialized()
        {
            base.OnAbilityInitialized();
            
            // Verificar que tenemos todos los prefabs necesarios
            if (projectilePrefab == null)
            {
                Debug.LogError("[AlbertShotgunAbility] ¡Falta asignar el prefab del proyectil!");
                return;
            }
            
            // Posicionar el proyectil frente a Albert
            if (caster != null)
            {
                transform.position = caster.transform.position + 
                                    caster.transform.forward * 1.5f + 
                                    Vector3.up * 1.0f;
                
                initialDirection = caster.transform.forward.normalized;
                currentDirection = initialDirection;
                transform.rotation = Quaternion.LookRotation(initialDirection);
            }
            
            // Configurar velocidad
            speed = 50f;
            
            // No usar gravedad y destruir al impactar
            useGravity = false;
            penetratesTargets = false;
            
            // Crear efectos visuales de disparo
            if (muzzleFlashPrefab != null)
            {
                GameObject muzzleFlash = Instantiate(
                    muzzleFlashPrefab,
                    transform.position,
                    transform.rotation
                );
                Destroy(muzzleFlash, 1f);
            }
            
            // Configurar el Rigidbody
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = false;
                rb.drag = 0f;
                rb.constraints = RigidbodyConstraints.FreezeRotation;
                rb.velocity = initialDirection * speed;
            }
            
            // Marcar como en movimiento
            isMoving = true;
            
            // Aplicar retroceso a Albert
            if (photonView.IsMine && caster != null)
            {
                Vector3 knockbackDirection = -caster.transform.forward;
                knockbackDirection.y = 0;
                photonView.RPC("RPC_ApplySelfKnockback", RpcTarget.All, caster.photonView.ViewID, knockbackDirection);
            }
        }
        
        /// <summary>
        /// Actualización específica para la habilidad
        /// </summary>
        protected override void AbilityUpdate()
        {
            // Llamar al update base para movimiento y colisiones
            base.AbilityUpdate();
            
            // Opcional: Añadir efectos visuales adicionales durante el vuelo
            if (isMoving && !hasHit)
            {
                // Por ejemplo, quizás estela de partículas adicionales
            }
        }
        
        /// <summary>
        /// Procesa el impacto con un objetivo
        /// </summary>
        protected override void ProcessImpact(HeroBase target)
        {
            if (hasHit) return;
            hasHit = true;
            
            base.ProcessImpact(target);
            
            // Aplicar knockback
            if (photonView.IsMine)
            {
                // Calcular dirección de knockback
                Vector3 knockbackDirection = (target.transform.position - transform.position).normalized;
                knockbackDirection.y = 0; // Mantener el empuje en el plano horizontal
                
                // Usar RPC para sincronizar el knockback en todos los clientes
                photonView.RPC("RPC_ApplyKnockback", RpcTarget.All, target.photonView.ViewID, knockbackDirection);
            }
            
            // Crear efecto de impacto usando el prefab
            if (impactEffectPrefab != null)
            {
                Instantiate(impactEffectPrefab, target.transform.position, Quaternion.identity);
            }
        }
        
        /// <summary>
        /// Aplica efecto de knockback al objetivo
        /// </summary>
        private void ApplyKnockback(HeroBase target)
        {
            if (!photonView.IsMine) return;
            
            // Calcular dirección de knockback
            Vector3 knockbackDirection = (target.transform.position - transform.position).normalized;
            knockbackDirection.y = 0; // Mantener el empuje en el plano horizontal
            
            // Usar RPC para sincronizar el knockback en todos los clientes
            photonView.RPC("RPC_ApplyKnockback", RpcTarget.All, target.photonView.ViewID, knockbackDirection);
        }
        
        /// <summary>
        /// Crea el efecto visual del proyectil
        /// </summary>
        private void CreateProjectileVisual()
        {
            // Log para depuración
            if (showDebugLogs)
            {
                Debug.Log($"[AlbertShotgunAbility] Creando visual del proyectil. ProjectileModel es null: {projectileModel == null}");
            }
            
            // Si existe el modelo de proyectil, no crear otro
            if (projectileModel != null)
            {
                // Solo asegurar que sea visible y tenga el color correcto
                Renderer modelRenderer = projectileModel.GetComponent<Renderer>();
                if (modelRenderer != null)
                {
                    Material mat = modelRenderer.material;
                    mat.color = projectileColor;
                }
                
                // Asegurar que esté visible
                projectileModel.SetActive(true);
                return;
            }
            
            // Crear una esfera roja para el proyectil
            projectileVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectileVisual.name = "ShotgunProjectile";
            projectileVisual.transform.SetParent(transform);
            projectileVisual.transform.localPosition = Vector3.zero;
            projectileVisual.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
            
            // Eliminar el collider de la esfera ya que usamos el sistema de colisiones de ProjectileAbility
            Collider visualCollider = projectileVisual.GetComponent<Collider>();
            if (visualCollider != null)
            {
                Destroy(visualCollider);
            }
            
            // Crear material con color rojo brillante
            Renderer projectileRenderer = projectileVisual.GetComponent<Renderer>();
            if (projectileRenderer != null)
            {
                // Intentar usar el shader Standard
                Shader standardShader = Shader.Find("Standard");
                if (standardShader != null)
                {
                    Material mat = new Material(standardShader);
                    mat.color = projectileColor;
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", projectileColor * 1.5f);
                    projectileRenderer.material = mat;
                }
                else
                {
                    // Si el shader Standard no está disponible, usar un shader más simple
                    Material fallbackMat = ShaderSafetyUtility.CreateSafeMaterial("Sprites/Default", projectileColor);
                    if (fallbackMat != null)
                    {
                        projectileRenderer.material = fallbackMat;
                    }
                }
            }
            
            // Para asegurar visibilidad, hacemos el proyectil más pequeño (50% del tamaño original)
            projectileVisual.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
            
            // Añadir efecto de luz para mejor visibilidad
            Light light = projectileVisual.AddComponent<Light>();
            light.color = projectileColor;
            light.intensity = 2.0f;
            light.range = 3.0f;
            
            // Asignar como modelo de proyectil
            projectileModel = projectileVisual;
            
            if (showDebugLogs)
            {
                Debug.Log($"[AlbertShotgunAbility] Proyectil visual creado. IsActive: {projectileVisual.activeSelf}");
            }
        }
        
        /// <summary>
        /// Crea el efecto de fogonazo en la posición inicial
        /// </summary>
        private void CreateMuzzleFlash()
        {
            if (muzzleFlashPrefab != null && caster != null)
            {
                // Crear en la posición del caster mirando en la dirección del disparo
                GameObject muzzleFlash = Instantiate(
                    muzzleFlashPrefab, 
                    caster.transform.position + caster.transform.forward * 1.5f + Vector3.up * 1.0f, 
                    Quaternion.LookRotation(initialDirection)
                );
                
                // Destruir después de un tiempo corto
                Destroy(muzzleFlash, 1f);
            }
            else
            {
                // Si no hay prefab, crear un efecto básico
                CreateBasicMuzzleFlash();
            }
        }
        
        /// <summary>
        /// Crea un efecto de fogonazo básico si no hay prefab
        /// </summary>
        private void CreateBasicMuzzleFlash()
        {
            if (caster == null) return;
            
            // Crear luces y partículas para simular el fogonazo
            GameObject flashObj = new GameObject("MuzzleFlash");
            flashObj.transform.position = caster.transform.position + caster.transform.forward * 1.5f + Vector3.up * 1.0f;
            flashObj.transform.rotation = Quaternion.LookRotation(initialDirection);
            
            // Añadir luz
            Light flashLight = flashObj.AddComponent<Light>();
            flashLight.color = new Color(1f, 0.7f, 0.3f);
            flashLight.intensity = 3f;
            flashLight.range = 5f;
            
            // Destruir después de un tiempo corto
            Destroy(flashObj, 0.2f);
        }
        
        /// <summary>
        /// Reproduce el sonido de disparo
        /// </summary>
        private void PlayShotgunSound()
        {
            if (shotgunSound != null)
            {
                // Usar AudioSource existente o crear uno temporal
                AudioSource audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.spatialBlend = 1.0f;  // 3D sound
                    audioSource.minDistance = 2.0f;
                    audioSource.maxDistance = 20.0f;
                }
                
                audioSource.PlayOneShot(shotgunSound);
            }
        }
        
        /// <summary>
        /// Crea un efecto visual en el punto de impacto
        /// </summary>
        private void CreateImpactEffect(Vector3 position)
        {
            // Si tenemos un prefab de impacto, usarlo
            if (impactEffectPrefab != null)
            {
                Instantiate(impactEffectPrefab, position, Quaternion.identity);
                return;
            }
            
            // Crear un efecto simple si no hay prefab
            GameObject impactObj = new GameObject("ShotgunImpact");
            impactObj.transform.position = position;
            
            // Añadir luz
            Light impactLight = impactObj.AddComponent<Light>();
            impactLight.color = projectileColor;
            impactLight.intensity = 2f;
            impactLight.range = 3f;
            
            // Animar la luz para que se desvanezca
            StartCoroutine(FadeOutLight(impactLight, 0.3f));
            
            // Destruir después de tiempo
            Destroy(impactObj, 0.5f);
        }
        
        /// <summary>
        /// Reproduce sonido de impacto
        /// </summary>
        private void PlayImpactSound()
        {
            // Implementar si se añade un sonido específico para el impacto
        }
        
        /// <summary>
        /// Corrutina para desvanecer una luz
        /// </summary>
        private IEnumerator FadeOutLight(Light light, float duration)
        {
            float initialIntensity = light.intensity;
            float elapsed = 0;
            
            while (elapsed < duration)
            {
                light.intensity = Mathf.Lerp(initialIntensity, 0, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            light.intensity = 0;
        }
        
        /// <summary>
        /// Override de DestroyAbility para manejar Rigidbody correctamente
        /// </summary>
        protected override void DestroyAbility()
        {
            if (showDebugLogs)
            {
                Debug.Log("[AlbertShotgunAbility] DestroyAbility llamado, gestionando todo el proceso...");
            }
            
            // Detener el movimiento
            isMoving = false;
            hasHit = true; // Marcar que ya impactó para evitar procesamiento adicional
            
            // Cancelar cualquier Invoke pendiente para evitar llamadas múltiples
            CancelInvoke();
            
            // IMPORTANTE: Si hay Rigidbody, NO modificar velocidad, solo hacerlo kinematic
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Asegurar que sea kinematic sin tocar su velocidad
                rb.isKinematic = true;
            }
            
            // Detener partículas antes de destruir
            if (flyingParticles != null)
            {
                // Desacoplar sistema de partículas para que termine su animación
                flyingParticles.transform.SetParent(null);
                flyingParticles.Stop();
                
                // Destruir sistema de partículas después de que termine
                try
                {
                    // Intentar obtener la duración y tiempo de vida
                    float duration = flyingParticles.main.duration;
                    float lifetime = flyingParticles.main.startLifetime.constant;
                    Destroy(flyingParticles.gameObject, duration + lifetime);
                }
                catch (System.Exception)
                {
                    // Si hay algún error, usar un tiempo fijo de destrucción
                    Destroy(flyingParticles.gameObject, 2f);
                }
            }
            
            // Apagar la luz si existe
            if (projectileVisual != null)
            {
                Light light = projectileVisual.GetComponent<Light>();
                if (light != null)
                {
                    light.enabled = false;
                }
            }
            
            // NO LLAMAR al método base para evitar problemas
            // base.DestroyAbility();
            
            // En su lugar, implementar directamente el comportamiento necesario:
            
            // Si somos el dueño del objeto, destruirlo en red
            if (photonView.IsMine)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"[AlbertShotgunAbility] Destruyendo {gameObject.name} en red (ViewID: {photonView.ViewID})");
                }
                
                // Usar PhotonNetwork.Destroy para objetos creados con PhotonNetwork.Instantiate
                PhotonNetwork.Destroy(gameObject);
            }
            else
            {
                // Si no somos el dueño, solo ocultar localmente
                if (showDebugLogs)
                {
                    Debug.Log($"[AlbertShotgunAbility] Ocultando {gameObject.name} localmente (no somos dueños)");
                }
                
                gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Override de OnCollisionEnter para manejar colisiones correctamente
        /// </summary>
        void OnCollisionEnter(Collision collision)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[AlbertShotgunAbility] Colisión detectada con {collision.gameObject.name}");
            }
            
            // Solo procesar si somos el dueño del objeto y está en movimiento
            if (!photonView.IsMine || !isMoving || hasHit)
                return;
                
            // Verificar si impactamos con un héroe
            HeroBase hitHero = collision.gameObject.GetComponent<HeroBase>();
            
            // Si es un héroe y no es el lanzador, procesar impacto
            if (hitHero != null && (!caster || hitHero != caster))
            {
                if (showDebugLogs)
                {
                    Debug.Log($"[AlbertShotgunAbility] Impacto con héroe {hitHero.heroName}");
                }
                
                // Procesar nuestro impacto personalizado
                ProcessImpact(hitHero);
            }
            // Si impactamos con algo que no es un héroe
            else if (hitHero == null)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"[AlbertShotgunAbility] Impacto con entorno");
                }
                
                // Marcar que ya impactamos
                hasHit = true;
                
                // Crear efecto de impacto en el punto de contacto
                if (collision.contacts.Length > 0)
                {
                    Vector3 hitPoint = collision.contacts[0].point;
                    Vector3 hitNormal = collision.contacts[0].normal;
                    
                    // Crear el efecto localmente
                    CreateImpactEffect(hitPoint);
                    
                    // Informar colisión mediante RPC
                    if (photonView.IsMine)
                    {
                        // Usamos nuestra propia llamada RPC, no la base
                        photonView.RPC("RPC_OnHitEnvironment", RpcTarget.All, hitPoint, hitNormal);
                    }
                }
                
                // Detener movimiento 
                isMoving = false;
                
                // Destruir proyectil (con retraso para asegurar efectos visuales)
                Invoke("DestroyAbility", 0.1f);
            }
        }
        
        #region Photon RPC
        
        [PunRPC]
        private void RPC_ApplyKnockback(int targetViewID, Vector3 knockbackDirection)
        {
            // Encontrar el objetivo por su ViewID
            PhotonView targetView = PhotonView.Find(targetViewID);
            if (targetView == null) return;
            
            HeroBase target = targetView.GetComponent<HeroBase>();
            if (target == null) return;
            
            // Aplicar knockback con Rigidbody
            Rigidbody targetRb = target.GetComponent<Rigidbody>();
            if (targetRb != null && !targetRb.isKinematic)
            {
                targetRb.AddForce(knockbackDirection * knockbackForce * 1.5f, ForceMode.Impulse);
            }
            
            // Mover al personaje directamente con más fuerza
            target.transform.position += knockbackDirection * knockbackForce * 0.5f;
            
            // Pausar el NavMeshAgent brevemente para el efecto de aturdimiento
            HeroMovementController moveController = target.GetComponent<HeroMovementController>();
            if (moveController != null)
            {
                moveController.ApplyStun(knockbackDuration);
            }
        }
        
        [PunRPC]
        private void RPC_OnHitEnvironment(Vector3 hitPoint, Vector3 hitNormal)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[AlbertShotgunAbility] RPC_OnHitEnvironment recibido en punto {hitPoint}");
            }
            
            // Crear efecto de impacto en la posición y normal del hit
            if (impactEffectPrefab != null)
            {
                GameObject impactEffect = Instantiate(impactEffectPrefab, hitPoint, Quaternion.LookRotation(hitNormal));
                
                // Destruir después de un tiempo
                Destroy(impactEffect, 2f);
            }
            else 
            {
                // Crear un efecto simple si no hay prefab
                GameObject impactObj = new GameObject("ShotgunImpact");
                impactObj.transform.position = hitPoint;
                
                // Añadir luz
                Light impactLight = impactObj.AddComponent<Light>();
                impactLight.color = projectileColor;
                impactLight.intensity = 2f;
                impactLight.range = 3f;
                
                // Destruir después de tiempo
                Destroy(impactObj, 0.5f);
            }
            
            // Detener el movimiento
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.isKinematic = true;
            }
            
            // Marcar como detenido
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
        
        [PunRPC]
        private void RPC_InitializeProjectile(Vector3 position, Vector3 direction, float projectileSpeed)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[AlbertShotgunAbility] RPC_InitializeProjectile recibido. Pos: {position}, Dir: {direction}, Speed: {projectileSpeed}");
            }
            
            // Configurar posición y dirección para clientes remotos
            transform.position = position;
            initialPosition = position;
            initialDirection = direction.normalized;
            currentDirection = initialDirection;
            speed = projectileSpeed;
            
            // Si hay rigidbody, actualizar su velocidad
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Configurar correctamente
                rb.isKinematic = false;
                rb.useGravity = false;
                rb.drag = 0f; // Misma resistencia que en la inicialización principal
                rb.constraints = RigidbodyConstraints.FreezeRotation;
                
                // Aplicar velocidad sin fuerza adicional
                rb.velocity = initialDirection * speed;
                
                if (showDebugLogs)
                {
                    Debug.Log($"[AlbertShotgunAbility] Rigidbody configurado con velocidad: {rb.velocity}");
                }
            }
            
            // Orientar en la dirección correcta
            transform.rotation = Quaternion.LookRotation(initialDirection);
            
            // Activar efectos visuales
            CreateProjectileVisual();
            
            // Crear efectos de disparo
            CreateMuzzleFlash();
            PlayShotgunSound();
            
            // Asegurarse de que el trail esté funcionando
            if (trailEffect != null)
            {
                trailEffect.enabled = true;
                trailEffect.Clear();
            }
            
            // Activar partículas si existen
            if (flyingParticles != null)
            {
                flyingParticles.gameObject.SetActive(true);
                flyingParticles.Play();
            }
            
            // Marcar como inicializado y en movimiento
            isMoving = true;
            
            // También verificar el movimiento lento en los clientes remotos
            Invoke("EnsureSlowMovement", 0.1f);
        }
        
        [PunRPC]
        private void RPC_ApplySelfKnockback(int casterViewID, Vector3 knockbackDirection)
        {
            // Encontrar a Albert por su ViewID
            PhotonView casterView = PhotonView.Find(casterViewID);
            if (casterView != null)
            {
                HeroBase albert = casterView.GetComponent<HeroBase>();
                if (albert != null)
                {
                    StartCoroutine(ApplySmoothKnockback(albert, knockbackDirection));
                }
            }
        }
        
        private IEnumerator ApplySmoothKnockback(HeroBase albert, Vector3 knockbackDirection)
        {
            float elapsedTime = 0f;
            Rigidbody albertRb = albert.GetComponent<Rigidbody>();
            HeroMovementController moveController = albert.GetComponent<HeroMovementController>();
            
            // Guardar valores originales
            float originalDrag = albertRb != null ? albertRb.drag : 0f;
            
            if (albertRb != null && !albertRb.isKinematic)
            {
                // Aplicar impulso inicial más fuerte
                albertRb.AddForce(knockbackDirection * selfKnockbackForce * 1.5f, ForceMode.Impulse);
                
                // Menos fricción para un deslizamiento más largo
                albertRb.drag = selfKnockbackDrag;
            }
            
            // Aplicar un mini-stun al inicio
            if (moveController != null)
            {
                moveController.ApplyStun(0.1f);
            }
            
            // Aplicar la fuerza gradualmente
            while (elapsedTime < selfKnockbackDuration)
            {
                float normalizedTime = elapsedTime / selfKnockbackDuration;
                float currentForce = selfKnockbackCurve.Evaluate(normalizedTime);
                
                if (albertRb != null && !albertRb.isKinematic)
                {
                    // Fuerza continua reducida para mantener el movimiento
                    albertRb.AddForce(knockbackDirection * selfKnockbackForce * currentForce * Time.deltaTime * 0.5f, ForceMode.Force);
                }
                else
                {
                    // Si no hay Rigidbody, mover directamente la posición
                    albert.transform.position += knockbackDirection * selfKnockbackForce * currentForce * Time.deltaTime * 0.1f;
                }
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // Restaurar valores originales gradualmente
            if (albertRb != null)
            {
                float dragRestoreTime = 0f;
                float dragRestoreDuration = 0.2f;
                float currentDrag = albertRb.drag;
                
                while (dragRestoreTime < dragRestoreDuration)
                {
                    albertRb.drag = Mathf.Lerp(currentDrag, originalDrag, dragRestoreTime / dragRestoreDuration);
                    dragRestoreTime += Time.deltaTime;
                    yield return null;
                }
                
                albertRb.drag = originalDrag;
            }
        }
        
        #endregion
    }
}