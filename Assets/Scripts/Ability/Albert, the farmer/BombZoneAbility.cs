using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

namespace Photon.Pun.Demo.Asteroids
{
    public class BombZoneAbility : AOEAbility
    {
        [Header("BombZone Settings")]
        public GameObject bombPrefab;           // Prefab de las bombas pequeñas
        public int bombCount = 8;               // Número de bombas a colocar
        public float bombDamage = 50f;          // Daño de cada bomba
        public float bombLifetime = 5f;         // Duración del campo minado
        public float bombDetonationRadius = 1f; // Radio de explosión de cada bomba
        
        [Header("Visual Settings")]
        public Color areaColor = new Color(1f, 0.9f, 0.2f, 0.3f);  // Color del área (amarillo por defecto)
        public bool usePulseEffect = true;                         // Usar efecto de pulso en el área
        
        private List<GameObject> spawnedBombs = new List<GameObject>();
        private GameObject areaVisual;
        
        protected override void OnAbilityInitialized()
        {
            base.OnAbilityInitialized();
            
            // Crear visualización del área
            CreateAreaVisual();
            
            // Crear bombas dentro del área
            CreateBombs();
            
            // Programar destrucción después del tiempo de vida
            Invoke("DetonateRemainingBombs", bombLifetime);
        }
        
        private void CreateAreaVisual()
        {
            // Usar la utilidad para crear materiales seguros
            GameObject circleObj = new GameObject("AreaVisual");
            circleObj.transform.SetParent(transform);
            circleObj.transform.localPosition = new Vector3(0, 0.05f, 0);
            
            LineRenderer lineRenderer = ShaderSafetyUtility.CreateSafeLineRenderer(circleObj, areaColor, 0.1f, 60);
            
            // Crear puntos del círculo
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
            
            // Método 2: Área sombreada (opcional)
            if (usePulseEffect)
            {
                GameObject areaObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                areaObj.name = "AreaShading";
                areaObj.transform.SetParent(transform);
                areaObj.transform.localPosition = new Vector3(0, 0.01f, 0);
                areaObj.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);
                
                // Eliminar collider
                DestroyImmediate(areaObj.GetComponent<Collider>());
                
                // Crear material seguro
                Material areaMaterial = ShaderSafetyUtility.CreateSafeMaterial("Transparent/Diffuse", 
                    new Color(areaColor.r, areaColor.g, areaColor.b, 0.2f));
                
                if (areaMaterial != null)
                {
                    Renderer renderer = areaObj.GetComponent<Renderer>();
                    renderer.material = areaMaterial;
                }
                
                // Añadir efecto de pulso
                AreaPulseEffect pulseEffect = areaObj.AddComponent<AreaPulseEffect>();
                pulseEffect.pulseSpeed = 2f;
                pulseEffect.pulseIntensity = 0.3f;
                pulseEffect.baseColor = new Color(areaColor.r, areaColor.g, areaColor.b, 0.2f);
                
                areaVisual = areaObj;
            }
        }
        
        private void CreateBombs()
        {
            // Usar el PhotonView para sincronizar la creación de bombas
            if (photonView.IsMine)
            {
                photonView.RPC("RPC_CreateBombs", RpcTarget.AllBuffered);
            }
        }
        
        [PunRPC]
        private void RPC_CreateBombs()
        {
            if (bombPrefab != null)
            {
                // Usar el prefab proporcionado
                SpawnBombsWithPrefab();
            }
            else
            {
                // Crear bombas básicas
                CreateBasicBombs();
            }
        }
        
        private void SpawnBombsWithPrefab()
        {
            float bombAreaRadius = radius * 0.9f;
            
            for (int i = 0; i < bombCount; i++)
            {
                // Posición aleatoria dentro del círculo
                Vector2 randomPoint = Random.insideUnitCircle * bombAreaRadius;
                Vector3 bombPosition = transform.position + new Vector3(randomPoint.x, 0.1f, randomPoint.y);
                
                // Instanciar bomba
                GameObject bomb = Instantiate(bombPrefab, bombPosition, Quaternion.identity);
                bomb.transform.SetParent(transform);
                
                // Configurar componente BombEffect
                BombEffect bombEffect = bomb.AddComponent<BombEffect>();
                bombEffect.Initialize(bombDamage, bombDetonationRadius, caster, bombLifetime);
                
                spawnedBombs.Add(bomb);
            }
        }
        
        private void CreateBasicBombs()
        {
            float bombAreaRadius = radius * 0.9f;
            
            for (int i = 0; i < bombCount; i++)
            {
                // Posición aleatoria dentro del círculo
                Vector2 randomPoint = Random.insideUnitCircle * bombAreaRadius;
                Vector3 bombPosition = transform.position + new Vector3(randomPoint.x, 0.1f, randomPoint.y);
                
                // Crear una bomba básica
                GameObject bomb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bomb.name = "Bomb_" + i;
                bomb.transform.position = bombPosition;
                bomb.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
                bomb.transform.SetParent(transform);
                
                // Añadir Trigger Collider
                SphereCollider col = bomb.GetComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 0.6f;
                
                // Crear material seguro para la bomba
                Material bombMaterial = ShaderSafetyUtility.CreateSafeMaterial("Standard", Color.yellow);
                
                if (bombMaterial != null)
                {
                    bombMaterial.EnableKeyword("_EMISSION");
                    bombMaterial.SetColor("_EmissionColor", new Color(1f, 0.8f, 0.2f) * 1.5f);
                    bomb.GetComponent<Renderer>().material = bombMaterial;
                }
                
                // Añadir efecto de intermitencia
                bomb.AddComponent<BombBlinkEffect>();
                
                // Configurar componente BombEffect
                BombEffect bombEffect = bomb.AddComponent<BombEffect>();
                bombEffect.Initialize(bombDamage, bombDetonationRadius, caster, bombLifetime);
                
                spawnedBombs.Add(bomb);
            }
        }
        
        protected override void DestroyAbility()
        {
            // Detonar bombas restantes al destruir la habilidad
            DetonateRemainingBombs();
            
            base.DestroyAbility();
        }
        
        private void DetonateRemainingBombs()
        {
            // Detonar todas las bombas restantes
            foreach (GameObject bomb in spawnedBombs)
            {
                if (bomb != null)
                {
                    BombEffect bombEffect = bomb.GetComponent<BombEffect>();
                    if (bombEffect != null)
                    {
                        bombEffect.Detonate();
                    }
                }
            }
            
            spawnedBombs.Clear();
        }
        
        protected override void AbilityUpdate()
        {
            base.AbilityUpdate();
            
            // Código adicional para animaciones o efectos durante la vida de la habilidad
        }
    }
    
    // Clase para controlar el comportamiento de cada bomba individual
    public class BombEffect : MonoBehaviour
    {
        private float damage;
        private float radius;
        private HeroBase owner;
        private float lifetime;
        private bool hasDetonated = false;
        
        public void Initialize(float bombDamage, float bombRadius, HeroBase caster, float bombLifetime)
        {
            damage = bombDamage;
            radius = bombRadius;
            owner = caster;
            lifetime = bombLifetime;
            
            // Autodestrucción después del tiempo de vida
            Invoke("Detonate", lifetime * Random.Range(0.8f, 1.0f)); // Variación para que no exploten todas al mismo tiempo
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