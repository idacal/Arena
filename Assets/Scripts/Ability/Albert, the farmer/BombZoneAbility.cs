using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;
using System.Collections;

namespace Photon.Pun.Demo.Asteroids
{
    public class BombZoneAbility : AOEAbility
    {
        [Header("BombZone Settings")]
        public int bombCount = 8;
        public float bombDamage = 50f;
        public float bombLifetime = 5f;
        public float bombDetonationRadius = 1f;
        
        [Header("Visual Settings")]
        public Color areaColor = new Color(1f, 0.9f, 0.2f, 0.3f);
        public bool usePulseEffect = true;
        
        [Header("Prefab References")]
        public GameObject bombZonePrefab;
        
        private GameObject bombZoneInstance;
        private List<BombEffect> activeBombs = new List<BombEffect>();
        
        protected override void OnAbilityInitialized()
        {
            base.OnAbilityInitialized();
            
            // Asegurarnos de que el PhotonView tenga un ID válido
            if (photonView.ViewID == 0)
            {
                PhotonNetwork.AllocateViewID(photonView);
            }
            
            // Crear visualización usando el prefab
            photonView.RPC("RPC_CreateBombZoneVisual", RpcTarget.All);
            
            // Programar destrucción después del tiempo de vida
            Invoke("DetonateRemainingBombs", bombLifetime);
        }
        
        [PunRPC]
        private void RPC_CreateBombZoneVisual()
        {
            if (bombZonePrefab != null)
            {
                // Instanciar el prefab
                bombZoneInstance = Instantiate(bombZonePrefab, transform.position, Quaternion.identity);
                bombZoneInstance.transform.SetParent(transform);
                
                // Asegurarnos de que el área no tenga collider físico
                var areaCollider = bombZoneInstance.GetComponent<Collider>();
                if (areaCollider != null)
                {
                    areaCollider.isTrigger = true;
                    // Poner en la capa de habilidades para no interferir con el movimiento
                    bombZoneInstance.layer = LayerMask.NameToLayer("Ability");
                }
                
                // Configurar el BombZonePrefabSetup
                var setup = bombZoneInstance.GetComponent<BombZonePrefabSetup>();
                if (setup != null)
                {
                    setup.areaRadius = radius;
                    setup.bombCount = bombCount;
                    setup.areaColor = areaColor;
                    setup.SetupAreaEffect();
                    setup.SetupBombVisuals();
                    
                    // Añadir componentes de bomba a los visuales solo en el lado del lanzador
                    if (photonView.IsMine)
                    {
                        Transform bombsContainer = bombZoneInstance.transform.Find("BombsContainer");
                        if (bombsContainer != null)
                        {
                            foreach (Transform bombTransform in bombsContainer)
                            {
                                BombEffect bombEffect = bombTransform.gameObject.AddComponent<BombEffect>();
                                bombEffect.Initialize(bombDamage, bombDetonationRadius, caster, bombLifetime);
                                activeBombs.Add(bombEffect);
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.LogError("BombZonePrefab no está asignado en " + gameObject.name);
            }
        }
        
        private void DetonateRemainingBombs()
        {
            foreach (var bomb in activeBombs)
            {
                if (bomb != null && !bomb.hasDetonated)
                {
                    bomb.Detonate();
                }
            }
            
            activeBombs.Clear();
        }
        
        protected void OnDestroy()
        {
            if (bombZoneInstance != null)
            {
                Destroy(bombZoneInstance);
            }
        }
    }
    
    // Clase para controlar el comportamiento de cada bomba individual
    public class BombEffect : MonoBehaviour
    {
        private float damage;
        private float radius;
        private HeroBase owner;
        private float lifetime;
        public bool hasDetonated { get; private set; } = false;
        
        private void Awake()
        {
            // Asegurarnos de que el collider esté configurado correctamente
            var col = GetComponent<SphereCollider>();
            if (col == null)
            {
                col = gameObject.AddComponent<SphereCollider>();
            }
            col.isTrigger = true;
            
            // Asegurarnos de que esté en la capa correcta
            gameObject.layer = LayerMask.NameToLayer("Ability");
        }
        
        public void Initialize(float bombDamage, float bombRadius, HeroBase caster, float bombLifetime)
        {
            damage = bombDamage;
            radius = bombRadius;
            owner = caster;
            lifetime = bombLifetime;
            
            // Actualizar el radio del collider
            var col = GetComponent<SphereCollider>();
            if (col != null)
            {
                col.radius = radius;
            }
            
            // Autodestrucción después del tiempo de vida
            Invoke("Detonate", lifetime * Random.Range(0.8f, 1.0f));
        }
        
        void OnTriggerEnter(Collider other)
        {
            // Si un enemigo entra en contacto con la bomba, detonar
            HeroBase hero = other.GetComponent<HeroBase>();
            if (hero != null && hero != owner)
            {
                Detonate();
            }
        }
        
        public void Detonate()
        {
            if (hasDetonated) return;
            
            hasDetonated = true;
            
            // Encontrar enemigos en el radio de explosión
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius);
            foreach (Collider hitCollider in hitColliders)
            {
                HeroBase hitHero = hitCollider.GetComponent<HeroBase>();
                if (hitHero != null && hitHero != owner)
                {
                    // Aplicar daño
                    hitHero.TakeDamage(damage, owner, true);
                }
            }
            
            // Crear efecto de explosión
            CreateExplosionEffect();
            
            // Destruir la bomba
            Destroy(gameObject);
        }
        
        private void CreateExplosionEffect()
        {
            // Crear objeto para la explosión
            GameObject explosionObj = new GameObject("Explosion");
            explosionObj.transform.position = transform.position;
            
            // Onda expansiva (esfera que crece y se desvanece)
            GameObject shockwave = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shockwave.transform.SetParent(explosionObj.transform);
            shockwave.transform.localPosition = Vector3.zero;
            shockwave.transform.localScale = Vector3.one * 0.1f;
            
            // Eliminar collider
            DestroyImmediate(shockwave.GetComponent<Collider>());
            
            // Material seguro para la onda expansiva
            Material shockwaveMaterial = ShaderSafetyUtility.CreateSafeMaterial("Transparent/Diffuse", 
                new Color(1f, 0.5f, 0f, 0.7f));
                
            if (shockwaveMaterial != null)
            {
                shockwave.GetComponent<Renderer>().material = shockwaveMaterial;
            }
            
            // Añadir script para animar la explosión
            ShockwaveAnimation animation = explosionObj.AddComponent<ShockwaveAnimation>();
            animation.targetScale = Vector3.one * radius * 2f;
            animation.duration = 0.5f;
            animation.shockwave = shockwave;
            
            // Autodestruir después de la animación
            Destroy(explosionObj, 0.6f);
        }
        
        void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
    
    // Clase para animar el efecto de pulso del área
    public class AreaPulseEffect : MonoBehaviour
    {
        public float pulseSpeed = 1f;
        public float pulseIntensity = 0.2f;
        public Color baseColor = Color.yellow;
        
        private Renderer rend;
        private Material material;
        private float time = 0f;
        
        void Start()
        {
            rend = GetComponent<Renderer>();
            
            if (rend != null && rend.material != null)
            {
                // Crear material único para este objeto
                material = rend.material;
            }
        }
        
        void Update()
        {
            if (material != null)
            {
                time += Time.deltaTime * pulseSpeed;
                
                // Valor del pulso entre 0 y 1
                float pulse = Mathf.Sin(time) * 0.5f + 0.5f;
                
                // Aplicar el pulso al color
                Color pulseColor = baseColor * (1f + pulse * pulseIntensity);
                pulseColor.a = baseColor.a; // Mantener la transparencia original
                
                material.color = pulseColor;
            }
        }
    }
    
    // Clase para hacer parpadear las bombas
    public class BombBlinkEffect : MonoBehaviour
    {
        private Renderer rend;
        private float time = 0f;
        private float blinkSpeed;
        private Material material;
        
        void Start()
        {
            rend = GetComponent<Renderer>();
            if (rend != null && rend.material != null)
            {
                // Crear una copia del material para no afectar a otros objetos
                material = rend.material;
                blinkSpeed = Random.Range(3f, 5f); // Velocidad aleatoria para cada bomba
            }
        }
        
        void Update()
        {
            if (rend != null && material != null)
            {
                time += Time.deltaTime * blinkSpeed;
                
                // Valor del parpadeo entre 0 y 1
                float blink = Mathf.Sin(time) * 0.5f + 0.5f;
                
                // Color base amarillo
                Color baseColor = new Color(1f, 0.8f, 0.2f);
                
                // Aplicar el parpadeo a la emisión
                material.SetColor("_EmissionColor", baseColor * (1f + blink * 2f));
            }
        }
    }
    
    // Clase para animar la onda expansiva de la explosión
    public class ShockwaveAnimation : MonoBehaviour
    {
        public GameObject shockwave;
        public Vector3 targetScale = Vector3.one * 3f;
        public float duration = 0.5f;
        
        private float startTime;
        private Vector3 initialScale;
        private Material material;
        
        void Start()
        {
            startTime = Time.time;
            
            if (shockwave != null)
            {
                initialScale = shockwave.transform.localScale;
                material = shockwave.GetComponent<Renderer>().material;
            }
        }
        
        void Update()
        {
            float elapsedTime = Time.time - startTime;
            float progress = elapsedTime / duration;
            
            if (shockwave != null)
            {
                // Escalar la onda expansiva
                shockwave.transform.localScale = Vector3.Lerp(initialScale, targetScale, progress);
                
                // Desvanecer la onda
                if (material != null)
                {
                    Color color = material.color;
                    color.a = Mathf.Lerp(0.7f, 0f, progress);
                    material.color = color;
                }
            }
        }
    }
}