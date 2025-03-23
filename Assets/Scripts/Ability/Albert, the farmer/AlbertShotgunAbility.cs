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
        public Color projectileColor = Color.red; // Color del proyectil de la escopeta
        
        [Header("Shotgun Visual Effects")]
        public GameObject muzzleFlashPrefab;     // Efecto de fogonazo
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
            // Activar logs para depuración
            showDebugLogs = true;
            
            if (showDebugLogs)
            {
                Debug.Log($"[AlbertShotgunAbility] Inicializando en posición {transform.position}");
            }
            
            // IMPORTANTE: Asegurarnos de que la posición inicial está frente a Albert
            if (caster != null)
            {
                // Posicionar el proyectil frente a Albert
                transform.position = caster.transform.position + 
                                    caster.transform.forward * 1.5f + 
                                    Vector3.up * 1.0f; // Altura ajustada
                
                // Establecer la dirección inicial como la dirección hacia donde mira Albert
                initialDirection = caster.transform.forward.normalized;
                currentDirection = initialDirection;
                
                // Orientar el proyectil en la dirección correcta
                transform.rotation = Quaternion.LookRotation(initialDirection);
                
                if (showDebugLogs)
                {
                    Debug.Log($"[AlbertShotgunAbility] Posición reposicionada a {transform.position}, frente a Albert");
                    Debug.Log($"[AlbertShotgunAbility] Dirección establecida a {initialDirection}");
                }
            }
            
            // Configurar la velocidad - Queremos un proyectil LENTO
            speed = 50f; // Muy lento
            
            // Llamar a la inicialización base para configurar el proyectil
            base.OnAbilityInitialized();
            
            // No queremos que use gravedad
            useGravity = false;
            
            // Queremos que se destruya al impactar
            penetratesTargets = false;
            
            // Crear efectos visuales de disparo
            CreateMuzzleFlash();
            PlayShotgunSound();
            
            // Crear modelo visual para el proyectil si no existe
            CreateProjectileVisual();
            
            // IMPORTANTE: Asegurar que tenemos un rigidbody y está configurado correctamente
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Configurar el rigidbody para el movimiento correcto
                rb.isKinematic = false;
                rb.useGravity = false;
                rb.drag = 0f; // Añadir resistencia para movimiento más lento
                rb.constraints = RigidbodyConstraints.FreezeRotation; // Evitar rotación no deseada
                
                // APLICAR VELOCIDAD INICIAL - SIN FUERZA ADICIONAL PARA QUE SEA MÁS LENTO
                rb.velocity = initialDirection * speed;
                
                // Sin AddForce adicional para no acelerar demasiado
                
                if (showDebugLogs)
                {
                    Debug.Log($"[AlbertShotgunAbility] Rigidbody configurado con velocidad: {rb.velocity} y drag: {rb.drag}");
                }
            }
            else
            {
                Debug.LogError("[AlbertShotgunAbility] ¡No se encontró Rigidbody en el proyectil!");
            }
            
            // Asegurar que está marcado como en movimiento
            isMoving = true;
            
            // Invocar un check de backup para asegurar el movimiento (pero sin acelerarlo)
            Invoke("EnsureSlowMovement", 0.1f);
        }
        
        /// <summary>
        /// Método de respaldo para asegurar que el proyectil se está moviendo (pero lentamente)
        /// </summary>
        private void EnsureSlowMovement()
        {
            if (!isMoving || hasHit)
                return;
                
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Si la velocidad es demasiado alta, la reducimos
                if (rb.velocity.magnitude > speed + 1f)
                {
                    rb.velocity = rb.velocity.normalized * speed;
                    if (showDebugLogs)
                    {
                        Debug.Log($"[AlbertShotgunAbility] Velocidad frenada a {rb.velocity.magnitude}");
                    }
                }
                // Si la velocidad es demasiado baja, la ajustamos al valor deseado
                else if (rb.velocity.magnitude < 0.5f)
                {
                    rb.velocity = initialDirection * speed;
                    if (showDebugLogs)
                    {
                        Debug.Log($"[AlbertShotgunAbility] Velocidad aumentada a {rb.velocity.magnitude}");
                    }
                }
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
            
            // Llamar al método base para aplicar daño
            base.ProcessImpact(target);
            
            // Aplicar knockback (empuje) al objetivo
            ApplyKnockback(target);
            
            // Crear efecto visual de impacto
            CreateImpactEffect(target.transform.position);
            
            // Reproducir sonido de impacto si existe
            PlayImpactSound();
            
            // Destruir la habilidad (ya controlada por base.ProcessImpact si destroyOnImpact es true)
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
            if (targetView != null)
            {
                HeroBase target = targetView.GetComponent<HeroBase>();
                if (target != null)
                {
                    // Aplicar movimiento directo para asegurar el knockback
                    target.transform.position += knockbackDirection * knockbackForce * 0.2f;
                    
                    // Aplicar aturdimiento temporalmente
                    HeroMovementController moveController = target.GetComponent<HeroMovementController>();
                    if (moveController != null)
                    {
                        moveController.ApplyStun(knockbackDuration);
                    }
                    
                    // Si tiene Rigidbody, también aplicar fuerza
                    Rigidbody targetRb = target.GetComponent<Rigidbody>();
                    if (targetRb != null && !targetRb.isKinematic)
                    {
                        targetRb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
                    }
                }
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
        
        #endregion
    }
}