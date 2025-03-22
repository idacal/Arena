using UnityEngine;
using Photon.Pun;

namespace Photon.Pun.Demo.Asteroids
{
    // Añade este script a TODOS los prefabs de habilidades
    public class NetworkAbilityHelper : MonoBehaviourPun, IPunObservable
    {
        [Tooltip("Si está marcado, se mostrarán mensajes de depuración detallados")]
        public bool debugMode = false;
        
        // Referencia al AbilityBehaviour para verificar propiedad
        private AbilityBehaviour abilityBehaviour;
        
        // Indicador de si este objeto fue creado por nuestro photonView
        private bool wasCreatedByMe = false;
        
        // Tick de vida para verificar si la habilidad debe destruirse
        private float lifetimeTick = 0;
        
        void Awake()
        {
            // Obtener referencia al AbilityBehaviour
            abilityBehaviour = GetComponent<AbilityBehaviour>();
            
            // Asegurarse de que todos los renderers están activos al inicio
            ActivateAllRenderers();
            
            // Verificar si somos el creador
            wasCreatedByMe = photonView.IsMine;
            
            if (debugMode) {
                Debug.Log($"[NetworkAbilityHelper] Awake para {gameObject.name}. IsMine: {photonView.IsMine}, ViewID: {photonView.ViewID}");
            }
        }
        
        void Start()
        {
            if (debugMode) {
                Debug.Log($"[NetworkAbilityHelper] Start para {gameObject.name}. IsMine: {photonView.IsMine}, ViewID: {photonView.ViewID}");
            }
            
            // Sincronizar la visualización en todos los clientes
            if (photonView.IsMine)
            {
                photonView.RPC("RPC_EnsureVisibility", RpcTarget.AllBuffered);
            }
            
            // Segunda comprobación con retraso para asegurar visibilidad
            Invoke("DelayedVisibilityCheck", 0.2f);
        }
        
        void Update()
        {
            // Si somos el dueño de la habilidad, controlar la destrucción
            if (wasCreatedByMe && abilityBehaviour != null)
            {
                lifetimeTick += Time.deltaTime;
                
                // Si hemos excedido el tiempo de vida, destruir en red
                if (lifetimeTick >= abilityBehaviour.lifetime && abilityBehaviour.lifetime > 0)
                {
                    if (debugMode) {
                        Debug.Log($"[NetworkAbilityHelper] Destruyendo {gameObject.name} por timeout");
                    }
                    
                    PhotonNetwork.Destroy(gameObject);
                }
            }
        }
        
        [PunRPC]
        private void RPC_EnsureVisibility()
        {
            ActivateAllRenderers();
            CreateAllVisuals();
        }
        
        private void DelayedVisibilityCheck()
        {
            ActivateAllRenderers();
            
            // Si somos el dueño, sincronizar de nuevo
            if (photonView.IsMine)
            {
                photonView.RPC("RPC_EnsureVisibility", RpcTarget.Others);
            }
        }
        
        private void ActivateAllRenderers()
        {
            // Activar todos los renderers
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (!renderer.enabled)
                {
                    renderer.enabled = true;
                    if (debugMode) {
                        Debug.Log($"[NetworkAbilityHelper] Activado renderer: {renderer.name}");
                    }
                }
            }
            
            // Activar todos los efectos de partículas
            ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem ps in particles)
            {
                if (!ps.isPlaying)
                {
                    ps.gameObject.SetActive(true);
                    ps.Play(true);
                    if (debugMode) {
                        Debug.Log($"[NetworkAbilityHelper] Activado ParticleSystem: {ps.name}");
                    }
                }
            }
            
            // Activar todos los Trail Renderers
            TrailRenderer[] trails = GetComponentsInChildren<TrailRenderer>(true);
            foreach (TrailRenderer trail in trails)
            {
                if (!trail.enabled)
                {
                    trail.enabled = true;
                    if (debugMode) {
                        Debug.Log($"[NetworkAbilityHelper] Activado TrailRenderer: {trail.name}");
                    }
                }
            }
        }
        
        private void CreateAllVisuals()
        {
            // Crear visuales para cada tipo de habilidad
            TryCreateVisualsForAOE();
            TryCreateVisualsForScarecrow();
            TryCreateVisualsForProjectile();
        }
        
        private void TryCreateVisualsForAOE()
        {
            AOEAbility aoeAbility = GetComponent<AOEAbility>();
            if (aoeAbility == null) return;
            
            // Verificar si ya existe una visualización
            Transform areaVisual = transform.Find("AreaVisual");
            Transform networkCreatedVisual = transform.Find("NetworkCreatedVisual");
            
            if (areaVisual != null || networkCreatedVisual != null) return;
            
            // Crear visualización del área manualmente
            CreateAOEVisual(aoeAbility.radius, new Color(1f, 1f, 0f, 0.5f));
            
            if (debugMode) {
                Debug.Log("[NetworkAbilityHelper] Creada visualización del área para AOE");
            }
        }
        
        private void TryCreateVisualsForScarecrow()
        {
            ScarecrowAbility scarecrow = GetComponent<ScarecrowAbility>();
            if (scarecrow == null) return;
            
            // Verificar si ya existe un espantapájaros
            Transform scarecrowObj = transform.Find("BasicScarecrow");
            if (scarecrowObj == null && scarecrow.scarecrowPrefab != null)
            {
                GameObject spawnedScarecrow = Instantiate(scarecrow.scarecrowPrefab, transform.position, Quaternion.identity);
                spawnedScarecrow.transform.SetParent(transform);
                if (debugMode) {
                    Debug.Log("[NetworkAbilityHelper] Creado espantapájaros manualmente");
                }
            }
            
            // Crear círculo visual si no existe
            Transform circleVisual = transform.Find("CircleVisual");
            Transform networkCreatedVisual = transform.Find("NetworkCreatedVisual");
            
            if (circleVisual == null && networkCreatedVisual == null)
            {
                CreateAOEVisual(scarecrow.radius, scarecrow.areaColor);
                if (debugMode) {
                    Debug.Log("[NetworkAbilityHelper] Creada visualización del área para espantapájaros");
                }
            }
        }
        
        private void TryCreateVisualsForProjectile()
        {
            ProjectileAbility projectile = GetComponent<ProjectileAbility>();
            if (projectile == null) return;
            
            // Verificar si ya existe un modelo de proyectil
            if (projectile.projectileModel != null)
            {
                projectile.projectileModel.SetActive(true);
            }
            
            // Activar efectos de partículas
            if (projectile.flyingParticles != null)
            {
                projectile.flyingParticles.gameObject.SetActive(true);
                if (!projectile.flyingParticles.isPlaying)
                {
                    projectile.flyingParticles.Play();
                }
            }
            
            // Activar trail
            if (projectile.trailEffect != null)
            {
                projectile.trailEffect.enabled = true;
            }
            
            if (debugMode) {
                Debug.Log("[NetworkAbilityHelper] Configurados elementos visuales del proyectil");
            }
        }
        
        private void CreateAOEVisual(float radius, Color color)
        {
            // Objeto para el círculo
            GameObject visualObj = new GameObject("NetworkCreatedVisual");
            visualObj.transform.SetParent(transform);
            visualObj.transform.localPosition = new Vector3(0, 0.05f, 0);
            
            // Añadir LineRenderer
            LineRenderer lineRenderer = visualObj.AddComponent<LineRenderer>();
            lineRenderer.useWorldSpace = false;
            lineRenderer.loop = true;
            lineRenderer.positionCount = 60;
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            
            // Configurar material seguro
            Material material = CreateSafeMaterial("Sprites/Default");
            lineRenderer.material = material;
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;
            
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
            
            // Área sombreada
            GameObject areaShadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            areaShadow.name = "AreaShadow";
            areaShadow.transform.SetParent(visualObj.transform);
            areaShadow.transform.localPosition = Vector3.zero;
            areaShadow.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);
            
            // Eliminar collider
            if (areaShadow.GetComponent<Collider>() != null)
            {
                Destroy(areaShadow.GetComponent<Collider>());
            }
            
            // Material para el área sombreada
            Material areaMaterial = CreateSafeMaterial("Transparent/Diffuse");
            Color shadowColor = new Color(color.r, color.g, color.b, 0.15f);
            areaMaterial.color = shadowColor;
            
            if (areaShadow.GetComponent<Renderer>() != null)
            {
                areaShadow.GetComponent<Renderer>().material = areaMaterial;
            }
        }
        
        private Material CreateSafeMaterial(string shaderName)
        {
            // Intenta encontrar el shader por nombre
            Shader shader = Shader.Find(shaderName);
            
            // Si no lo encuentra, usa uno predeterminado
            if (shader == null)
            {
                if (debugMode) {
                    Debug.LogWarning($"No se pudo encontrar el shader '{shaderName}', usando Standard");
                }
                shader = Shader.Find("Standard");
                
                // Si ni siquiera se encuentra el Standard, usar el material por defecto
                if (shader == null)
                {
                    Debug.LogError("No se pudo encontrar ningún shader válido.");
                    return new Material(Shader.Find("Default-Material"));
                }
            }
            
            return new Material(shader);
        }
        
        // Implementación para sincronización en red
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            // Si estamos enviando datos (somos el dueño del objeto)
            if (stream.IsWriting)
            {
                // Enviar tiempo de vida restante
                stream.SendNext(lifetimeTick);
            }
            // Si estamos recibiendo datos
            else
            {
                // Recibir tiempo de vida
                lifetimeTick = (float)stream.ReceiveNext();
            }
        }
    }
}