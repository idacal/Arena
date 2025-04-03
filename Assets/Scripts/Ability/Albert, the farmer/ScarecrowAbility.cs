using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine.UI;

namespace Photon.Pun.Demo.Asteroids
{
    /// <summary>
    /// Implementación optimizada del espantapájaros que maneja sus propias visualizaciones
    /// </summary>
    [RequireComponent(typeof(PhotonView))]
    public class ScarecrowAbility : AOEAbility
    {
        [Header("Espantapájaros - Configuración")]
        public float fearDuration = 2f;
        public float scarecrowHealth = 100f;
        
        [Header("Espantapájaros - Referencias")]
        [Tooltip("Referencia directa al prefab del espantapájaros")]
        public GameObject scarecrowPrefab;
        
        [Header("Espantapájaros - Visuales")]
        [Tooltip("Color del área de efecto para aliados")]
        public Color allyAreaColor = new Color(1f, 0.7f, 0f, 0.3f);
        [Tooltip("Color del área de efecto para enemigos")]
        public Color enemyAreaColor = new Color(1f, 0.3f, 0.3f, 0.3f); // Rojo más intenso para enemigos
        
        // Variables privadas
        private GameObject scarecrowInstance;
        private float currentHealth;
        private Dictionary<int, float> lastFearTimes = new Dictionary<int, float>();
        private PulsingAOEVisualEffect areaEffect;
        private bool visualsCreated = false;
        
        protected override void OnAbilityInitialized()
        {
            base.OnAbilityInitialized();
            
            // Inicializar salud
            currentHealth = scarecrowHealth;
            
            // Crear visuales solo una vez
            if (!visualsCreated)
            {
                CreateVisuals();
                visualsCreated = true;
            }
            
            // Reproducir sonido explícitamente
            if (abilitySound != null && photonView.IsMine)
            {
                PlayAbilitySound();
                Debug.Log("[ScarecrowAbility] Reproduciendo sonido del espantapájaros");
            }
            
            // Programar destrucción con el lifetime del AOEAbility heredado
            if (photonView.IsMine)
            {
                Invoke("OnAbilityEnd", lifetime);
            }
        }
        
        private void CreateVisuals()
        {
            // Crear el área de efecto si no existe
            CreateAreaEffect();
            
            // Crear el espantapájaros si no existe
                CreateScarecrowVisual();
            
            // Sincronizar las visualizaciones a todos los clientes
            if (photonView.IsMine)
            {
                photonView.RPC("RPC_EnsureVisibility", RpcTarget.Others);
            }
        }
        
        private void CreateAreaEffect()
        {
            // Eliminar área existente si hay alguna
            Transform existingArea = transform.Find("AreaEffect");
            if (existingArea != null)
            {
                Destroy(existingArea.gameObject);
            }
            
            // Crear un nuevo objeto para el área
            GameObject areaEffectObj = new GameObject("AreaEffect");
            areaEffectObj.transform.SetParent(transform);
            areaEffectObj.transform.localPosition = new Vector3(0, 0.05f, 0);
            
            // Determinar qué color usar basado en el equipo
            Color areaColor = DetermineAreaColor();
            
            // 1. Crear el borde circular con LineRenderer
            LineRenderer lineRenderer = areaEffectObj.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = true;
            lineRenderer.positionCount = 60;
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            
            // Crear material para el LineRenderer
            Material lineMaterial = CreateSafeMaterial("Sprites/Default");
            lineRenderer.material = lineMaterial;
            lineRenderer.startColor = areaColor;
            lineRenderer.endColor = areaColor;
            
            // Generar puntos del círculo
            float deltaTheta = (2f * Mathf.PI) / (lineRenderer.positionCount - 1);
            float theta = 0f;
            
            for (int i = 0; i < lineRenderer.positionCount; i++)
            {
                float x = radius * Mathf.Cos(theta);
                float z = radius * Mathf.Sin(theta);
                Vector3 pos = new Vector3(x, 0, z);
                lineRenderer.SetPosition(i, pos);
                theta += deltaTheta;
            }
            
            // 2. Crear área sombreada con un cilindro
            GameObject areaShadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            areaShadow.name = "AreaShadow";
            areaShadow.transform.SetParent(areaEffectObj.transform);
            areaShadow.transform.localPosition = Vector3.zero;
            areaShadow.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);
            
            // Eliminar collider del cilindro (solo necesitamos el visual)
            if (areaShadow.GetComponent<Collider>() != null)
            {
                Destroy(areaShadow.GetComponent<Collider>());
            }
            
            // Material para el área sombreada
            Material areaMaterial = CreateSafeMaterial("Transparent/Diffuse");
            Color shadowColor = new Color(areaColor.r, areaColor.g, areaColor.b, 0.15f);
            areaMaterial.color = shadowColor;
            
            if (areaShadow.GetComponent<Renderer>() != null)
            {
                areaShadow.GetComponent<Renderer>().material = areaMaterial;
            }
            
            // 3. Agregar SphereCollider para detección de trigger
            SphereCollider triggerCollider = areaEffectObj.AddComponent<SphereCollider>();
            triggerCollider.radius = radius;
            triggerCollider.isTrigger = true;
            
            // IMPORTANTE: Configurar la capa del collider para que ignore los raycast
            // Intentar usar una capa existente como "Ignore Raycast" (capa 2)
            areaEffectObj.layer = 2; // Capa "Ignore Raycast" por defecto en Unity
            
            // Alternativamente, si el juego usa un sistema de capas personalizado
            // buscar una capa similar o consultar al desarrollador para usar la adecuada
            // int ignoreRaycastLayer = LayerMask.NameToLayer("IgnoreRaycast");
            // if (ignoreRaycastLayer != -1)
            //     areaEffectObj.layer = ignoreRaycastLayer;
            
            // Necesitamos un Rigidbody para que el trigger funcione correctamente
            Rigidbody rb = areaEffectObj.AddComponent<Rigidbody>();
            rb.isKinematic = true;      // No afectado por física
            rb.useGravity = false;      // Sin gravedad
            
            // 4. Añadir detector de trigger
            ScarecrowAreaTrigger areaTrigger = areaEffectObj.AddComponent<ScarecrowAreaTrigger>();
            areaTrigger.Initialize(this);
            
            Debug.Log($"[ScarecrowAbility] Área de efecto creada correctamente con color {(IsAlly() ? "aliado" : "enemigo")} y configurada para ignorar raycast");
        }
        
        /// <summary>
        /// Determina si el jugador local es aliado del dueño del espantapájaros
        /// </summary>
        private bool IsAlly()
        {
            // Verificar que tenemos un PhotonView válido
            if (photonView == null || !PhotonNetwork.IsConnected)
                return true; // Por defecto, mostrar como aliado en modo single player
            
            // Obtener el héroe local
            HeroBase localHero = FindLocalHero();
            if (localHero == null)
                return true; // Si no hay héroe local, asumir aliado
            
            // Comparar equipos
            int ownerTeamId = photonView.Owner.CustomProperties.TryGetValue("PlayerTeam", out object teamObj) 
                ? (int)teamObj 
                : 0;
            
            return localHero.teamId == ownerTeamId;
        }
        
        /// <summary>
        /// Busca el héroe controlado por el jugador local
        /// </summary>
        private HeroBase FindLocalHero()
        {
            // Buscar todos los héroes en la escena
            HeroBase[] allHeroes = FindObjectsOfType<HeroBase>();
            
            // Encontrar el controlado localmente
            foreach (HeroBase hero in allHeroes)
            {
                if (hero.photonView.IsMine)
                    return hero;
            }
            
            return null;
        }
        
        /// <summary>
        /// Determina qué color usar para el área basado en si es aliado o enemigo
        /// </summary>
        private Color DetermineAreaColor()
        {
            return IsAlly() ? allyAreaColor : enemyAreaColor;
        }
        
        // Método auxiliar para crear materiales de manera segura
        private Material CreateSafeMaterial(string shaderName)
        {
            // Intenta encontrar el shader por nombre
            Shader shader = Shader.Find(shaderName);
            
            // Si no lo encuentra, usa uno predeterminado
            if (shader == null)
            {
                Debug.LogWarning($"[ScarecrowAbility] No se pudo encontrar el shader '{shaderName}', usando Standard");
                shader = Shader.Find("Standard");
                
                // Si ni siquiera se encuentra el Standard, usar el material por defecto
                if (shader == null)
                {
                    Debug.LogError("[ScarecrowAbility] No se pudo encontrar ningún shader válido");
                    return new Material(Shader.Find("Default-Material"));
                }
            }
            
            return new Material(shader);
        }
        
        private void CreateScarecrowVisual()
        {
            // Si ya hay una instancia, no crear otra
            if (scarecrowInstance != null)
            {
                return;
            }
            
            // Verificar que tenemos el prefab
            if (scarecrowPrefab == null)
            {
                Debug.LogError("[ScarecrowAbility] No se ha asignado el prefab del espantapájaros");
                return;
            }
            
            // Crear la instancia con rotación inicial de 180 grados en Y para que mire hacia delante
            Quaternion initialRotation = Quaternion.Euler(0, 180, 0);
            scarecrowInstance = Instantiate(scarecrowPrefab, transform.position, initialRotation);
            scarecrowInstance.transform.SetParent(transform);
            scarecrowInstance.transform.localPosition = Vector3.zero;
            
            // Configurar el componente de salud si existe
            ScarecrowHealth health = scarecrowInstance.GetComponent<ScarecrowHealth>();
            if (health != null)
            {
                health.Initialize(scarecrowHealth);
            }
            
            // Iniciar la animación de aparición con giro 360
            StartCoroutine(PerformInitialSpinAnimation(scarecrowInstance.transform));
            
            Debug.Log("[ScarecrowAbility] Espantapájaros creado correctamente con rotación inicial de 180 grados");
        }
        
        /// <summary>
        /// Realiza una animación de giro de 360 grados al inicio del espantapájaros
        /// </summary>
        private System.Collections.IEnumerator PerformInitialSpinAnimation(Transform scarecrowTransform)
        {
            if (scarecrowTransform == null) yield break;
            
            // Guardar la rotación final deseada (rotación actual)
            Quaternion originalRotation = scarecrowTransform.rotation;
            
            // Crear la rotación inicial inclinada (30 grados en X para inclinarse hacia adelante)
            Quaternion startingRotation = originalRotation * Quaternion.Euler(30f, 0, 0);
            
            // Aplicar la rotación inicial inclinada
            scarecrowTransform.rotation = startingRotation;
            
            // Duración de la animación
            float spinDuration = 1.5f;
            float elapsed = 0f;
            
            // Rotación a completar en Y (360 grados)
            float targetYRotation = 360f;
            
            // Inclinación inicial
            float initialTiltX = 30f;
            
            // Valor para la curva de animación (hacer que empiece y termine lento)
            AnimationCurve speedCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            
            // Realizar el giro
            while (elapsed < spinDuration)
            {
                // Calcular progreso normalizado (0-1)
                float normalizedTime = elapsed / spinDuration;
                
                // Aplicar curva de animación para suavizar el movimiento
                float curvedTime = speedCurve.Evaluate(normalizedTime);
                
                // Calcular ángulo de giro en Y actual (0 a 360)
                float currentYRotation = curvedTime * targetYRotation;
                
                // Calcular inclinación en X actual (comenzando en 30 y terminando en 0)
                float currentTiltX = initialTiltX * (1 - curvedTime);
                
                // Crear rotación actual
                Quaternion currentRotation = originalRotation * Quaternion.Euler(currentTiltX, currentYRotation, 0);
                
                // Aplicar rotación
                scarecrowTransform.rotation = currentRotation;
                
                // Actualizar tiempo
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Asegurar que termina exactamente en la rotación original
            scarecrowTransform.rotation = originalRotation;
            
            // Pequeño efecto de "asentamiento" al final
            StartCoroutine(SettlingEffect(scarecrowTransform, originalRotation));
            
            Debug.Log("[ScarecrowAbility] Animación inicial de giro completada");
        }
        
        /// <summary>
        /// Pequeño efecto de "asentamiento" después del giro inicial
        /// </summary>
        private System.Collections.IEnumerator SettlingEffect(Transform target, Quaternion baseRotation)
        {
            // Series de pequeñas inclinaciones que se van reduciendo
            float[] tiltAngles = new float[] { 5f, -3f, 2f, -1f, 0 };
            float tiltDuration = 0.1f;
            
            foreach (float tiltAngle in tiltAngles)
            {
                // Calcular rotación objetivo con la inclinación
                Quaternion targetRotation = baseRotation * Quaternion.Euler(tiltAngle, 0, 0);
                
                // Tiempo para esta inclinación
                float elapsed = 0;
                
                // Rotación inicial para este paso
                Quaternion startRotation = target.rotation;
                
                // Interpolar hacia la rotación objetivo
                while (elapsed < tiltDuration)
                {
                    float t = elapsed / tiltDuration;
                    target.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                
                // Asegurar que llega a la rotación objetivo
                target.rotation = targetRotation;
            }
            
            // Asegurar que termina en la rotación base
            target.rotation = baseRotation;
        }
        
        /// <summary>
        /// Hace que el espantapájaros gire hacia el enemigo con una animación suave
        /// </summary>
        private void RotateScarecrowTowardsEnemy(HeroBase enemy)
        {
            if (scarecrowInstance == null || enemy == null) return;
            
            // Calcular dirección hacia el enemigo (solo en el plano horizontal)
            Vector3 targetPosition = enemy.transform.position;
            Vector3 direction = targetPosition - scarecrowInstance.transform.position;
            direction.y = 0; // Mantener la rotación en el plano horizontal
            
            if (direction.magnitude < 0.1f) return; // Evitar rotaciones con direcciones muy pequeñas
            
            // Crear rotación mirando hacia el enemigo
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            
            // Crear efecto de inclinación hacia adelante (asustando)
            Quaternion forwardTilt = Quaternion.Euler(15f, 0, 0); // 15 grados de inclinación en X
            targetRotation *= forwardTilt; // Combinar rotaciones
            
            // Iniciar la corrutina para rotación suave
            StartCoroutine(SmoothRotateTowards(scarecrowInstance.transform, targetRotation, 0.3f));
            
            Debug.Log($"[ScarecrowAbility] Girando espantapájaros hacia {enemy.name}");
        }
        
        /// <summary>
        /// Corrutina para rotar suavemente hacia una rotación objetivo
        /// </summary>
        private System.Collections.IEnumerator SmoothRotateTowards(Transform objectToRotate, Quaternion targetRotation, float duration)
        {
            Quaternion startRotation = objectToRotate.rotation;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                // Interpolar suavemente entre rotación inicial y objetivo
                objectToRotate.rotation = Quaternion.Slerp(startRotation, targetRotation, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Asegurar rotación final exacta
            objectToRotate.rotation = targetRotation;
            
            // Añadir pequeño efecto de "rebote" al final
            StartCoroutine(SlightBouncingEffect(objectToRotate));
        }
        
        /// <summary>
        /// Crea un pequeño efecto de rebote después de la rotación
        /// </summary>
        private System.Collections.IEnumerator SlightBouncingEffect(Transform target)
        {
            Quaternion originalRotation = target.rotation;
            Quaternion slightlyMore = originalRotation * Quaternion.Euler(5f, 0, 0); // 5 grados más de inclinación
            
            // Ir un poco más allá
            float overshootTime = 0.1f;
            float elapsed = 0f;
            
            while (elapsed < overshootTime)
            {
                target.rotation = Quaternion.Slerp(originalRotation, slightlyMore, elapsed / overshootTime);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Volver a la posición original
            elapsed = 0f;
            float returnTime = 0.2f;
            
            while (elapsed < returnTime)
            {
                target.rotation = Quaternion.Slerp(slightlyMore, originalRotation, elapsed / returnTime);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Asegurar rotación final
            target.rotation = originalRotation;
        }
        
        public void OnAbilityEnd()
        {
            if (photonView.IsMine)
            {
                DestroyAbility();
            }
        }
        
        protected override void DestroyAbility()
        {
            if (photonView.IsMine)
            {
                Debug.Log("[ScarecrowAbility] Destruyendo la habilidad del espantapájaros");
                base.DestroyAbility();
            }
        }
        
        public void TakeDamage(float damage)
        {
            if (!photonView.IsMine) return;
            
            currentHealth -= damage;
            Debug.Log($"[ScarecrowAbility] Espantapájaros recibe {damage} de daño. Salud restante: {currentHealth}");
            
            if (currentHealth <= 0)
            {
                Debug.Log("[ScarecrowAbility] Espantapájaros destruido por daño");
                DestroyAbility();
            }
        }
        
        protected override void Update()
        {
            base.Update();
            
            // Asegurarse que las visualizaciones siempre estén disponibles
            if (!visualsCreated)
            {
                CreateVisuals();
                visualsCreated = true;
            }
        }
        
        [PunRPC]
        private void RPC_EnsureVisibility()
        {
            // Asegurarse que las visualizaciones existen en todos los clientes
            if (!visualsCreated)
            {
                CreateVisuals();
                visualsCreated = true;
            }
            
            // Activar todos los renderers
            ActivateAllRenderers();
        }
        
        private void ActivateAllRenderers()
        {
            // Activar renderers
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (!renderer.enabled)
                {
                    renderer.enabled = true;
                }
            }
            
            // Activar partículas
            ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem ps in particles)
            {
                if (!ps.isPlaying)
                {
                    ps.gameObject.SetActive(true);
                    ps.Play(true);
                }
            }
        }
        
        public void OnEnemyEnterArea(HeroBase hero)
        {
            if (!photonView.IsMine) return;
            
            // Solo procesar héroes válidos
            if (hero == null) return;
            
            // Verificar que sea de otro equipo
            if (hero.photonView.Owner.ActorNumber == photonView.Owner.ActorNumber)
            {
                return; // Es del mismo equipo, ignorar
            }
            
            // Hacer que el espantapájaros gire hacia el enemigo
            RotateScarecrowTowardsEnemy(hero);
            
            int targetId = hero.photonView.ViewID;
            
            // Controlar el tiempo entre aplicaciones del miedo
                    float lastHitTime = 0f;
                    lastFearTimes.TryGetValue(targetId, out lastHitTime);
                    
                    if (Time.time >= lastHitTime + fearDuration)
                    {
                        lastFearTimes[targetId] = Time.time;
                
                // Aplicar el efecto de miedo
                        photonView.RPC("RPC_ApplyFearEffect", RpcTarget.All, targetId);
                Debug.Log($"[ScarecrowAbility] Aplicando miedo a {hero.name} (ID: {targetId})");
                
                // Programar la destrucción del espantapájaros después de un pequeño retraso
                // para permitir que se vea la animación y se escuche el sonido
                Invoke("DestroyAfterActivation", 0.5f);
            }
        }
        
        // Método para destruir el espantapájaros después de activarse
        private void DestroyAfterActivation()
        {
            if (!photonView.IsMine) return;
            
            Debug.Log("[ScarecrowAbility] Espantapájaros desapareciendo después de activarse");
            DestroyAbility();
        }
        
        [PunRPC]
        private void RPC_ApplyFearEffect(int targetViewID)
        {
            // Buscar el objetivo por su ViewID
            PhotonView targetView = PhotonView.Find(targetViewID);
            if (targetView == null)
            {
                Debug.LogWarning($"[ScarecrowAbility] No se encontró el objetivo con ViewID {targetViewID}");
                return;
            }
            
            // Obtener el componente HeroBase
            HeroBase target = targetView.GetComponent<HeroBase>();
            if (target == null)
            {
                Debug.LogWarning($"[ScarecrowAbility] El objeto con ViewID {targetViewID} no tiene un componente HeroBase");
                return;
            }
            
            // Intentar girar el espantapájaros hacia el enemigo con RPC
            if (photonView.IsMine && scarecrowInstance != null)
            {
                // Solo sincronizar la rotación en clientes remotos
                photonView.RPC("RPC_RotateScarecrow", RpcTarget.Others, targetViewID);
            }
            
            // Aplicar el efecto de miedo
            ApplyFearEffect(target);
            
            // Animar el espantapájaros
            AnimateScarecrow();
            
            // Crear efecto de onda expansiva
            CreateExpandingWaveEffect(transform.position);
            
            // Reproducir sonido de miedo (controlado para evitar duplicación)
            PlayFearSoundForEveryone();
        }
        
        /// <summary>
        /// Método sobrecargado para crear un efecto de onda expansiva con el radio predeterminado
        /// </summary>
        private void CreateExpandingWaveEffect(Vector3 center)
        {
            // Usar el radio de esta habilidad como radio máximo para la onda
            StartCoroutine(CreateExpandingWaveEffect(center, radius));
        }
        
        /// <summary>
        /// Reproduce el sonido de miedo garantizando que se escuche en todos los clientes
        /// </summary>
        private void PlayFearSoundForEveryone()
        {
            // Solo el dueño del espantapájaros maneja la reproducción del sonido
            if (!photonView.IsMine) return;
            
            Debug.Log("[ScarecrowAbility] Intentando reproducir sonido de miedo");
            
            // 1. Reproducir localmente
            PlayFearSoundLocally();
            
            // 2. Enviar a otros clientes
            photonView.RPC("RPC_PlayFearSound", RpcTarget.Others);
        }
        
        // Método para reproducir el sonido localmente
        private void PlayFearSoundLocally()
        {
            if (impactSound != null) 
            {
                // Crear AudioSource temporal para reproducción garantizada
                GameObject audioObj = new GameObject("FearSoundTemp");
                audioObj.transform.position = transform.position;
                
                AudioSource tempAudio = audioObj.AddComponent<AudioSource>();
                tempAudio.clip = impactSound;
                tempAudio.volume = 1.0f;
                tempAudio.spatialBlend = 0.5f;
                tempAudio.minDistance = 10f;
                tempAudio.maxDistance = 50f;
                tempAudio.rolloffMode = AudioRolloffMode.Linear;
                tempAudio.Play();
                
                // Destruir después de reproducir
                Destroy(audioObj, impactSound.length + 0.1f);
                
                Debug.Log("[ScarecrowAbility] Reproduciendo sonido de miedo localmente");
            }
        }
        
        [PunRPC]
        private void RPC_PlayFearSound()
        {
            PlayFearSoundLocally();
        }
        
        [PunRPC]
        private void RPC_RotateScarecrow(int targetViewID)
        {
            // Solo para clientes remotos
            if (photonView.IsMine) return;
            
            // Buscar el objetivo
            PhotonView targetView = PhotonView.Find(targetViewID);
            if (targetView == null) return;
            
            HeroBase target = targetView.GetComponent<HeroBase>();
            if (target == null) return;
            
            // Rotar espantapájaros hacia el objetivo
            RotateScarecrowTowardsEnemy(target);
        }
        
        private void AnimateScarecrow()
        {
            if (scarecrowInstance == null) return;
            
            // Intentar obtener un animator
            Animator animator = scarecrowInstance.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                // Trigger de animación "Scare"
                animator.SetTrigger("Scare");
            }
            else
            {
                // Animación simple de escala si no hay animator
                StartCoroutine(SimpleScareAnimation(scarecrowInstance.transform));
            }
        }
        
        private System.Collections.IEnumerator SimpleScareAnimation(Transform target)
        {
            // Guardar escala original
            Vector3 originalScale = target.localScale;
            
            // Escala expandida
            Vector3 expandedScale = originalScale * 1.2f;
            
            // Tiempo de animación
            float animTime = 0.5f;
            float elapsed = 0f;
            
            // Expandir
            while (elapsed < animTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animTime;
                target.localScale = Vector3.Lerp(originalScale, expandedScale, t);
                yield return null;
            }
            
            // Contraer
            elapsed = 0f;
            while (elapsed < animTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / animTime;
                target.localScale = Vector3.Lerp(expandedScale, originalScale, t);
                yield return null;
            }
            
            // Restaurar escala original
            target.localScale = originalScale;
        }
        
        private void ApplyFearEffect(HeroBase target)
        {
            // Verificar si implementa la interfaz IFearable
            IFearable fearable = target.GetComponent<IFearable>();
            if (fearable != null)
            {
                // Aplicar el estado de miedo personalizado con comportamiento errático
                ApplyErraticFearMovement(target);
                
                // Aplicar el estado de miedo
                fearable.ApplyFear(fearDuration);
                Debug.Log($"[ScarecrowAbility] Aplicando miedo a {target.name} durante {fearDuration} segundos");
                
                // Crear efecto visual de miedo
                CreateFearVisualEffect(target.transform);
                
                // Aplicar oscurecimiento de pantalla al jugador afectado
                photonView.RPC("RPC_ApplyScreenDarkening", target.photonView.Owner, fearDuration);
                
                return;
            }
            
            // Alternativa: usar el controlador de movimiento para simular miedo
            HeroMovementController moveController = target.GetComponent<HeroMovementController>();
            if (moveController != null)
            {
                // Aplicar movimiento errático de miedo
                ApplyErraticFearMovement(target);
                
                // Crear efecto visual
                CreateFearVisualEffect(target.transform);
                
                // Aplicar oscurecimiento de pantalla al jugador afectado
                photonView.RPC("RPC_ApplyScreenDarkening", target.photonView.Owner, fearDuration);
            }
        }
        
        /// <summary>
        /// Aplica un movimiento errático al jugador con miedo
        /// </summary>
        private void ApplyErraticFearMovement(HeroBase target)
        {
            // Solo ejecutar en el cliente que es dueño del objeto
            if (!target.photonView.IsMine) return;
            
            HeroMovementController moveController = target.GetComponent<HeroMovementController>();
            if (moveController == null) return;
            
            // Iniciar el comportamiento errático
            target.StartCoroutine(ErraticMovementCoroutine(moveController, fearDuration));
        }
        
        /// <summary>
        /// Coroutine para movimiento errático de miedo
        /// </summary>
        private System.Collections.IEnumerator ErraticMovementCoroutine(HeroMovementController moveController, float duration)
        {
            // Guardar la posición inicial
            Vector3 initialPosition = moveController.transform.position;
            float elapsedTime = 0;
            
            // Usar la velocidad del héroe con un incremento para el efecto de miedo
            HeroBase heroBase = moveController.GetComponent<HeroBase>();
            float moveSpeed = (heroBase != null) ? heroBase.moveSpeed * 1.3f : 5f; // 30% más rápido
            
            // Bucle durante la duración del efecto
            while (elapsedTime < duration)
            {
                // Genera una nueva dirección aleatoria cada 0.4-0.7 segundos
                float changeTime = Random.Range(0.4f, 0.7f);
                Vector3 randomDirection = Random.insideUnitSphere;
                randomDirection.y = 0; // Mantener en plano horizontal
                randomDirection.Normalize();
                
                // Radio máximo para el movimiento errático (3 unidades)
                float maxRadius = 3f;
                Vector3 boundedDirection = randomDirection * maxRadius;
                
                // Posición objetivo dentro del radio máximo desde la posición inicial
                Vector3 targetPosition = initialPosition + boundedDirection;
                
                // Aplicar movimiento durante este segmento
                float segmentTime = 0;
                while (segmentTime < changeTime && elapsedTime < duration)
                {
                    // Mover hacia la dirección actual (rotación)
                    moveController.transform.rotation = Quaternion.Slerp(
                        moveController.transform.rotation,
                        Quaternion.LookRotation(randomDirection),
                        Time.deltaTime * 5f);
                    
                    // Mover con velocidad más rápida pero errática (posición)
                    moveController.transform.position = Vector3.MoveTowards(
                        moveController.transform.position,
                        targetPosition,
                        moveSpeed * Time.deltaTime);
                    
                    segmentTime += Time.deltaTime;
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }
            }
        }
        
        private void CreateFearVisualEffect(Transform targetTransform)
        {
            // Eliminar iconos de miedo anteriores si existen
            Transform existingFear = targetTransform.Find("FearEffect");
            if (existingFear != null)
            {
                Destroy(existingFear.gameObject);
            }
            
            // Crear objeto para el efecto visual de miedo
            GameObject fearEffect = new GameObject("FearEffect");
            fearEffect.transform.position = targetTransform.position + Vector3.up * 2f;
            fearEffect.transform.SetParent(targetTransform);
            
            // Opciones para visualización (elige una de ellas):
            
            // Opción 1: Texto "!" (signo de exclamación)
            TextMesh textMesh = fearEffect.AddComponent<TextMesh>();
            textMesh.text = "!";
            textMesh.fontSize = 25;
            textMesh.color = Color.red;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
            
            // Opción 2: Sprite de miedo (si has creado uno)
            // (Comenta esta sección si no tienes un sprite de miedo)
            /*
            SpriteRenderer renderer = fearEffect.AddComponent<SpriteRenderer>();
            Sprite fearSprite = Resources.Load<Sprite>("Textures/fear_icon");
            if (fearSprite != null)
            {
                renderer.sprite = fearSprite;
                renderer.color = Color.white;
            }
            else
            {
                // Si no existe el sprite, usamos el texto como fallback
                Destroy(renderer);
                TextMesh textMesh = fearEffect.AddComponent<TextMesh>();
                textMesh.text = "!";
                textMesh.fontSize = 25;
                textMesh.color = Color.red;
                textMesh.alignment = TextAlignment.Center;
                textMesh.anchor = TextAnchor.MiddleCenter;
            }
            */
            
            // Añadir el comportamiento de animación
            FearEffectBehavior behavior = fearEffect.AddComponent<FearEffectBehavior>();
            
            // Destruir después de la duración del efecto
            Destroy(fearEffect, fearDuration);
            
            Debug.Log($"[ScarecrowAbility] Creado efecto visual de miedo sobre {targetTransform.name}");
        }
        
        /// <summary>
        /// Crea un efecto visual cuando el espantapájaros se activa
        /// </summary>
        private void CreateActivationEffect()
        {
            // Buscar el objeto de área
            Transform areaObj = transform.Find("AreaEffect");
            if (areaObj == null) return;
            
            // 1. Cambiar color del área a negro (efecto oscuro)
            LineRenderer lineRenderer = areaObj.GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                // Guardar colores originales para restaurar
                Color originalStartColor = lineRenderer.startColor;
                Color originalEndColor = lineRenderer.endColor;
                
                // Cambiar a color negro/púrpura oscuro
                Color darkColor = new Color(0.2f, 0, 0.3f, 0.8f); // Púrpura oscuro
                lineRenderer.startColor = darkColor;
                lineRenderer.endColor = darkColor;
                
                // Restaurar después de un tiempo
                StartCoroutine(RestoreColorAfterDelay(lineRenderer, originalStartColor, originalEndColor, 0.5f));
            }
            
            // Buscar el cilindro del área
            Transform areaShadow = areaObj.Find("AreaShadow");
            if (areaShadow != null && areaShadow.GetComponent<Renderer>() != null)
            {
                Renderer shadowRenderer = areaShadow.GetComponent<Renderer>();
                Color originalColor = shadowRenderer.material.color;
                
                // Cambiar a color oscuro
                Color darkAreaColor = new Color(0.1f, 0, 0.2f, 0.6f);
                shadowRenderer.material.color = darkAreaColor;
                
                // Restaurar después de un tiempo
                StartCoroutine(RestoreRendererColorAfterDelay(shadowRenderer, originalColor, 0.5f));
            }
            
            // 2. Crear un efecto de onda expansiva
            StartCoroutine(CreateExpandingWaveEffect(areaObj.position, radius));
            
            // 3. Crear sistema de partículas para el efecto
            CreateActivationParticles(areaObj.position);
        }
        
        /// <summary>
        /// Crea un efecto de onda expansiva
        /// </summary>
        private System.Collections.IEnumerator CreateExpandingWaveEffect(Vector3 center, float maxRadius)
        {
            // Crear objeto para la onda
            GameObject waveObj = new GameObject("ScarecrowWave");
            waveObj.transform.position = center;
            waveObj.transform.SetParent(transform); // Importante: hacer hijo para que se destruya con el padre
            
            // Añadir LineRenderer para la onda
            LineRenderer waveRenderer = waveObj.AddComponent<LineRenderer>();
            waveRenderer.useWorldSpace = false;
            waveRenderer.loop = true;
            waveRenderer.positionCount = 60;
            waveRenderer.startWidth = 0.1f;
            waveRenderer.endWidth = 0.05f;
            
            // Color de la onda (púrpura brillante)
            Color waveColor = new Color(0.7f, 0.2f, 1f, 0.8f); 
            waveRenderer.startColor = waveColor;
            waveRenderer.endColor = new Color(waveColor.r, waveColor.g, waveColor.b, 0);
            
            // Material para la onda
            Material waveMaterial = CreateSafeMaterial("Particles/Additive");
            waveRenderer.material = waveMaterial;
            
            // Animación de expansión
            float duration = 0.5f;
            float elapsedTime = 0;
            
            while (elapsedTime < duration)
            {
                float t = elapsedTime / duration;
                float currentRadius = maxRadius * t;
                
                // Actualizar puntos del círculo
                float deltaTheta = (2f * Mathf.PI) / (waveRenderer.positionCount);
                float theta = 0f;
                
                for (int i = 0; i < waveRenderer.positionCount; i++)
                {
                    float x = currentRadius * Mathf.Cos(theta);
                    float z = currentRadius * Mathf.Sin(theta);
                    Vector3 pos = new Vector3(x, 0.05f, z);
                    waveRenderer.SetPosition(i, pos);
                    theta += deltaTheta;
                }
                
                // Actualizar alpha basado en progreso
                Color fadeColor = waveColor;
                fadeColor.a = waveColor.a * (1 - t);
                waveRenderer.startColor = fadeColor;
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            // Asegurar que se destruya el objeto
            Destroy(waveObj);
            
            Debug.Log("[ScarecrowAbility] Efecto de onda expansiva completado y destruido");
        }
        
        /// <summary>
        /// Crea partículas para el efecto de activación
        /// </summary>
        private void CreateActivationParticles(Vector3 center)
        {
            // Crear objeto para partículas
            GameObject particlesObj = new GameObject("ActivationParticles");
            particlesObj.transform.position = center;
            
            // Añadir sistema de partículas
            ParticleSystem particles = particlesObj.AddComponent<ParticleSystem>();
            
            // Configurar sistema de partículas
            var main = particles.main;
            main.startSpeed = 2f;
            main.startSize = 0.3f;
            main.startLifetime = 1f;
            main.maxParticles = 100;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            // Color de partículas: morado oscuro a claro
            var colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(new Color(0.7f, 0.2f, 1f), 0.0f),
                    new GradientColorKey(new Color(0.9f, 0.5f, 1f), 1.0f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(0.0f, 1.0f) 
                }
            );
            colorOverLifetime.color = gradient;
            
            // Emisión en forma de cono
            var shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.1f;
            shape.rotation = new Vector3(90f, 0f, 0f); // Apuntar hacia arriba
            
            // Emisión inicial
            var emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { 
                new ParticleSystem.Burst(0.0f, 50) 
            });
            
            // Destruir después de completar
            Destroy(particlesObj, 2f);
        }
        
        /// <summary>
        /// Restaura los colores originales después de un retraso
        /// </summary>
        private System.Collections.IEnumerator RestoreColorAfterDelay(LineRenderer lineRenderer, Color originalStart, Color originalEnd, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (lineRenderer != null)
            {
                lineRenderer.startColor = originalStart;
                lineRenderer.endColor = originalEnd;
            }
        }
        
        /// <summary>
        /// Restaura el color original de un renderer después de un retraso
        /// </summary>
        private System.Collections.IEnumerator RestoreRendererColorAfterDelay(Renderer renderer, Color originalColor, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (renderer != null)
            {
                renderer.material.color = originalColor;
            }
        }

        [PunRPC]
        private void RPC_ApplyScreenDarkening(float duration)
        {
            // Este RPC solo debe ejecutarse en el cliente afectado
            if (!photonView.IsMine) return;
            
            Debug.Log("[ScarecrowAbility] Iniciando efecto de oscurecimiento de pantalla");
            
            // Encontrar la cámara principal
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("[ScarecrowAbility] No se encontró la cámara principal para el efecto de oscurecimiento");
                return;
            }
            
            // Comprobar si hay un PhotonMOBACamera en la cámara
            PhotonMOBACamera mobaCamera = mainCamera.GetComponent<PhotonMOBACamera>();
            if (mobaCamera != null)
            {
                Debug.Log("[ScarecrowAbility] Cámara MOBA encontrada: " + mobaCamera.name);
            }
            
            // Crear objeto para el efecto de oscuridad
            GameObject darkeningScreen = new GameObject("FearDarkeningScreen");
            
            // Hacer que persista entre escenas y no se destruya fácilmente
            DontDestroyOnLoad(darkeningScreen);
            
            // Crear un canvas para mostrar el efecto en pantalla
            Canvas canvas = darkeningScreen.AddComponent<Canvas>();
            
            // CAMBIO IMPORTANTE: Usar ScreenSpaceCamera en lugar de ScreenSpaceOverlay
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = mainCamera;
            canvas.planeDistance = 1; // Muy cercano a la cámara
            canvas.sortingOrder = 999; // Asegurar que está por encima de todo
            
            // Añadir escalador para manejar diferentes resoluciones
            CanvasScaler scaler = darkeningScreen.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            // Añadir raycaster para que los eventos pasen a través
            GraphicRaycaster raycaster = darkeningScreen.AddComponent<GraphicRaycaster>();
            raycaster.blockingObjects = GraphicRaycaster.BlockingObjects.None;
            
            // Crear la imagen negra
            GameObject imageObj = new GameObject("DarkImage");
            imageObj.transform.SetParent(canvas.transform, false);
            
            // Configurar la imagen para cubrir toda la pantalla
            RectTransform rect = imageObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            
            // Añadir componente de imagen con color negro semi-transparente
            Image image = imageObj.AddComponent<Image>();
            image.color = new Color(0.05f, 0, 0.1f, 0.5f); // Negro/violeta oscuro semi-transparente
            image.raycastTarget = false; // Permitir clics a través de la imagen
            
            // Añadir comportamiento para que la oscuridad se desvanezca
            FearDarkeningEffect darkeningEffect = darkeningScreen.AddComponent<FearDarkeningEffect>();
            darkeningEffect.Initialize(image, duration);
            
            Debug.Log("[ScarecrowAbility] Efecto de oscurecimiento aplicado correctamente");
        }
    }
    
    /// <summary>
    /// Comportamiento para el efecto visual de miedo
    /// </summary>
    public class FearEffectBehavior : MonoBehaviour
    {
        private Transform cameraTransform;
        private Vector3 initialScale;
        private float animationSpeed = 2f;
        
        void Start()
        {
            // Obtener la cámara principal
            cameraTransform = Camera.main.transform;
            initialScale = transform.localScale;
        }
        
        void Update()
        {
            // Hacer que el icono mire siempre a la cámara (billboard)
            if (cameraTransform != null)
            {
                transform.LookAt(transform.position + cameraTransform.rotation * Vector3.forward,
                               cameraTransform.rotation * Vector3.up);
            }
            
            // Animación de pulsación
            float pulse = (Mathf.Sin(Time.time * animationSpeed) + 1) * 0.5f; // Valor entre 0 y 1
            transform.localScale = initialScale * (1 + pulse * 0.5f);
            
            // Movimiento ascendente
            transform.position += Vector3.up * Time.deltaTime * 0.5f;
        }
    }
    
    /// <summary>
    /// Componente detector de trigger para el área de espantapájaros
    /// </summary>
    public class ScarecrowAreaTrigger : MonoBehaviour
    {
        private ScarecrowAbility ownerAbility;
        
        public void Initialize(ScarecrowAbility ability)
        {
            ownerAbility = ability;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (ownerAbility == null) return;
            
            // Verificar si es un héroe
            HeroBase hero = other.GetComponent<HeroBase>();
            if (hero != null)
            {
                // Delegar al componente principal
                ownerAbility.OnEnemyEnterArea(hero);
            }
        }
    }

    /// <summary>
    /// Efecto de oscurecimiento de pantalla que se desvanece con el tiempo
    /// </summary>
    public class FearDarkeningEffect : MonoBehaviour
    {
        private Image darkeningImage;
        private float duration;
        private float startTime;
        private Color initialColor;
        private float pulsationSpeed = 2f;
        
        public void Initialize(Image image, float effectDuration)
        {
            darkeningImage = image;
            duration = effectDuration;
            startTime = Time.time;
            initialColor = image.color;
        }
        
        void Update()
        {
            if (darkeningImage == null)
            {
                Destroy(gameObject);
                return;
            }
            
            // Calcular tiempo transcurrido
            float elapsedTime = Time.time - startTime;
            
            if (elapsedTime >= duration)
            {
                // Destruir efecto cuando termina
                Destroy(gameObject);
                return;
            }
            
            // Calcular intensidad basada en el tiempo transcurrido
            float normalizedTime = elapsedTime / duration;
            
            // Añadir efecto de pulsación para que sea más dinámico
            float pulseFactor = Mathf.Sin(elapsedTime * pulsationSpeed) * 0.1f + 0.9f;
            
            // Calcular alpha basado en el tiempo restante (se desvanece gradualmente)
            float alpha = initialColor.a * (1f - normalizedTime) * pulseFactor;
            
            // Actualizar color de la imagen
            darkeningImage.color = new Color(
                initialColor.r,
                initialColor.g,
                initialColor.b,
                alpha
            );
        }
    }
}