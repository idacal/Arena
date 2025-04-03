using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

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
            
            // Necesitamos un Rigidbody para que el trigger funcione correctamente
            Rigidbody rb = areaEffectObj.AddComponent<Rigidbody>();
            rb.isKinematic = true;      // No afectado por física
            rb.useGravity = false;      // Sin gravedad
            
            // 4. Añadir detector de trigger
            ScarecrowAreaTrigger areaTrigger = areaEffectObj.AddComponent<ScarecrowAreaTrigger>();
            areaTrigger.Initialize(this);
            
            Debug.Log($"[ScarecrowAbility] Área de efecto creada correctamente con color {(IsAlly() ? "aliado" : "enemigo")}");
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
            
            // Crear la instancia
            scarecrowInstance = Instantiate(scarecrowPrefab, transform.position, transform.rotation);
            scarecrowInstance.transform.SetParent(transform);
            scarecrowInstance.transform.localPosition = Vector3.zero;
            
            // Configurar el componente de salud si existe
            ScarecrowHealth health = scarecrowInstance.GetComponent<ScarecrowHealth>();
            if (health != null)
            {
                health.Initialize(scarecrowHealth);
            }
            
            Debug.Log("[ScarecrowAbility] Espantapájaros creado correctamente");
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
        
        // Método público para ser llamado por el detector de trigger
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
            PhotonView targetView = PhotonView.Find(targetViewID);
            if (targetView == null) return;
            
            HeroBase target = targetView.GetComponent<HeroBase>();
            if (target == null) return;
            
            // Aplicar efecto de miedo
            ApplyFearEffect(target);
            
            // Reproducir sonido de impacto
            if (impactSound != null)
            {
                PlayImpactSound();
                Debug.Log("[ScarecrowAbility] Reproduciendo sonido de miedo");
            }
            
            // Animar el espantapájaros
            AnimateScarecrow();
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
                // Aplicar el estado de miedo
                fearable.ApplyFear(fearDuration);
                Debug.Log($"[ScarecrowAbility] Aplicando miedo a {target.name} durante {fearDuration} segundos");
                
                // Crear efecto visual de miedo
                CreateFearVisualEffect(target.transform);
                return;
            }
            
            // Alternativa: usar el controlador de movimiento para simular miedo
            // (Esta parte es un fallback por si acaso)
            HeroMovementController moveController = target.GetComponent<HeroMovementController>();
            if (moveController != null)
            {
                // Calcular dirección de huida (alejarse del espantapájaros)
                Vector3 fleeDirection = (target.transform.position - transform.position).normalized;
                Vector3 fleePosition = target.transform.position + fleeDirection * 10f;
                
                // Aplicar efecto de huida
                moveController.SetDestination(fleePosition);
                moveController.ApplyStun(0.5f);
                
                // Crear efecto visual
                CreateFearVisualEffect(target.transform);
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
}