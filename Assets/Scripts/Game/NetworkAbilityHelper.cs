using UnityEngine;
using Photon.Pun;

namespace Photon.Pun.Demo.Asteroids
{
    // ¡Importante! Añade este script a TODOS los prefabs de habilidades
    public class NetworkAbilityHelper : MonoBehaviourPun
    {
        [Tooltip("Si está marcado, se mostrarán mensajes de depuración detallados")]
        public bool debugMode = false;
        
        void Awake()
        {
            // Asegurarse de que todos los renderers están activos al inicio
            ActivateAllRenderers();
        }
        
        void Start()
        {
            if (debugMode) {
                Debug.Log($"[NetworkAbilityHelper] Inicializado para {gameObject.name}. IsMine: {photonView.IsMine}, ViewID: {photonView.ViewID}");
            }
            
            // Sincronizar la visualización en todos los clientes
            if (photonView.IsMine)
            {
                photonView.RPC("RPC_EnsureVisibility", RpcTarget.AllBuffered);
            }
            
            // Segunda comprobación con retraso
            Invoke("DelayedVisibilityCheck", 0.2f);
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
            if (areaVisual != null) return;
            
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
            if (circleVisual == null)
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
    }
}