using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

namespace Photon.Pun.Demo.Asteroids
{
    public class ScarecrowAbility : AOEAbility
    {
        [Header("Scarecrow Settings")]
        public GameObject scarecrowPrefab;     // Prefab del espantapájaros
        public float fearDuration = 2f;        // Duración del efecto de miedo
        public float scarecrowHealth = 100f;   // Vida del espantapájaros
        
        [Header("Visual Settings")]
        public Color areaColor = new Color(1f, 0.7f, 0f, 0.3f);  // Color del área (naranja por defecto)
        public bool forceCreateVisual = true;  // Forzar creación manual del círculo visual
        
        private GameObject spawnedScarecrow;
        private Dictionary<int, float> lastFearTimes = new Dictionary<int, float>();
        private GameObject circleVisual;
        
        protected override void OnAbilityInitialized()
        {
            Debug.Log("ScarecrowAbility: Inicializando...");
            
            // Llamar al inicializador de la clase base (AOEAbility)
            base.OnAbilityInitialized();
            
            // Intentar actualizar o crear la visualización del círculo
            if (!UpdateAreaColor() && forceCreateVisual)
            {
                CreateCircleVisual();
            }
            
            // Crear el espantapájaros en el centro
            CreateScarecrow();
        }
        
        private bool UpdateAreaColor()
        {
            // Primer intento: Buscar directamente un objeto hijo llamado "AreaVisual"
            Transform areaVisualTransform = transform.Find("AreaVisual");
            GameObject areaVisual = areaVisualTransform?.gameObject;
            
            if (areaVisual != null)
            {
                Debug.Log("ScarecrowAbility: AreaVisual encontrado, actualizando color");
                LineRenderer lineRenderer = areaVisual.GetComponent<LineRenderer>();
                if (lineRenderer != null)
                {
                    lineRenderer.startColor = areaColor;
                    lineRenderer.endColor = areaColor;
                    return true;
                }
            }
            
            // Segundo intento: Buscar recursivamente cualquier LineRenderer en hijos
            LineRenderer[] renderers = GetComponentsInChildren<LineRenderer>();
            if (renderers.Length > 0)
            {
                Debug.Log($"ScarecrowAbility: Encontrados {renderers.Length} LineRenderers en hijos");
                foreach (LineRenderer lr in renderers)
                {
                    lr.startColor = areaColor;
                    lr.endColor = areaColor;
                }
                return true;
            }
            
            Debug.LogWarning("ScarecrowAbility: No se encontró ninguna visualización del área para actualizar");
            return false;
        }
        
        private void CreateCircleVisual()
        {
            Debug.Log("ScarecrowAbility: Creando visualización del círculo manualmente");
            
            // Crear un objeto para el círculo
            circleVisual = new GameObject("CircleVisual");
            circleVisual.transform.SetParent(transform);
            circleVisual.transform.localPosition = new Vector3(0, 0.05f, 0);
            
            // Usar nuestro helper para crear un LineRenderer seguro
            LineRenderer lineRenderer = ShaderSafetyUtility.CreateSafeLineRenderer(
                circleVisual, areaColor, 0.1f, 60);
            
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
            
            // Crear también un área sombreada
            GameObject areaShadow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            areaShadow.name = "AreaShadow";
            areaShadow.transform.SetParent(circleVisual.transform);
            areaShadow.transform.localPosition = Vector3.zero;
            areaShadow.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);
            
            // Eliminar collider
            DestroyImmediate(areaShadow.GetComponent<Collider>());
            
            // Usar nuestro helper para crear un material seguro
            Material areaMaterial = ShaderSafetyUtility.CreateSafeMaterial(
                "Transparent/Diffuse", 
                new Color(areaColor.r, areaColor.g, areaColor.b, 0.15f));
            
            if (areaMaterial != null && areaShadow.GetComponent<Renderer>() != null)
            {
                areaShadow.GetComponent<Renderer>().material = areaMaterial;
            }
            
            Debug.Log("ScarecrowAbility: Visualización del círculo creada con éxito");
        }
        
        private void CreateScarecrow()
        {
            Debug.Log("ScarecrowAbility: Creando espantapájaros...");
            
            if (photonView.IsMine)
            {
                photonView.RPC("RPC_CreateScarecrow", RpcTarget.AllBuffered);
            }
        }
        
        [PunRPC]
        private void RPC_CreateScarecrow()
        {
            // Verificar si ya existe un espantapájaros
            Transform existingScarecrow = transform.Find("BasicScarecrow");
            if (existingScarecrow != null)
            {
                return; // Ya existe, no crear otro
            }
            
            // Verificar si tenemos un prefab
            if (scarecrowPrefab != null)
            {
                Debug.Log($"ScarecrowAbility: Usando prefab {scarecrowPrefab.name}");
                spawnedScarecrow = Instantiate(scarecrowPrefab, transform.position, Quaternion.identity);
                spawnedScarecrow.transform.SetParent(transform);
            }
            else
            {
                Debug.Log("ScarecrowAbility: No hay prefab asignado, creando básico");
                // Crear un espantapájaros básico
                spawnedScarecrow = CreateBasicScarecrow();
            }
            
            // Añadir un componente de salud al espantapájaros
            ScarecrowHealth healthComp = spawnedScarecrow.AddComponent<ScarecrowHealth>();
            healthComp.Initialize(scarecrowHealth);
        }
        
        private GameObject CreateBasicScarecrow()
{
    // Crear un espantapájaros básico con primitivas (30% más pequeño)
    GameObject scarecrow = new GameObject("BasicScarecrow");
    scarecrow.transform.SetParent(transform);
    scarecrow.transform.localPosition = Vector3.zero;
    
    // Factor de escala (70% del tamaño original = 30% más pequeño)
    float scaleFactor = 0.7f;
    
    // Cuerpo
    GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
    body.transform.SetParent(scarecrow.transform);
    body.transform.localPosition = new Vector3(0, 1 * scaleFactor, 0);
    body.transform.localScale = new Vector3(0.5f * scaleFactor, 2 * scaleFactor, 0.5f * scaleFactor);
    
    // Brazos
    GameObject arms = GameObject.CreatePrimitive(PrimitiveType.Cube);
    arms.transform.SetParent(scarecrow.transform);
    arms.transform.localPosition = new Vector3(0, 1.5f * scaleFactor, 0);
    arms.transform.localScale = new Vector3(2 * scaleFactor, 0.2f * scaleFactor, 0.2f * scaleFactor);
    
    // Cabeza
    GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
    head.transform.SetParent(scarecrow.transform);
    head.transform.localPosition = new Vector3(0, 2.5f * scaleFactor, 0);
    head.transform.localScale = new Vector3(0.7f * scaleFactor, 0.7f * scaleFactor, 0.7f * scaleFactor);
    
    // Material seguro
    Material scarecrowMaterial = ShaderSafetyUtility.CreateSafeMaterial(
        "Standard", new Color(0.7f, 0.5f, 0.3f));
    
    if (scarecrowMaterial != null)
    {
        body.GetComponent<Renderer>().material = scarecrowMaterial;
        arms.GetComponent<Renderer>().material = scarecrowMaterial;
        head.GetComponent<Renderer>().material = scarecrowMaterial;
    }
    
    return scarecrow;
}
        
        // En lugar de override CheckTargetsInArea, usamos el método ProcessImpact
        protected override void ProcessImpact(HeroBase target)
        {
            // No aplicar daño base, solo el efecto de miedo
            
            int targetId = target.photonView.ViewID;
            float lastHitTime = 0f;
            
            // Verificar si ya pasó el intervalo para este objetivo específico
            lastFearTimes.TryGetValue(targetId, out lastHitTime);
            
            if (Time.time >= lastHitTime + fearDuration)
            {
                // Actualizar tiempo del último impacto para este objetivo
                lastFearTimes[targetId] = Time.time;
                
                // Aplicar el efecto de miedo a través de RPC para sincronizar el efecto en todos los clientes
                if (photonView.IsMine)
                {
                    photonView.RPC("RPC_ApplyFearEffect", RpcTarget.All, targetId);
                }
                
                Debug.Log($"ScarecrowAbility: Aplicado efecto de miedo a {target.heroName}");
            }
        }
        
        [PunRPC]
        private void RPC_ApplyFearEffect(int targetViewID)
        {
            // Encontrar el objetivo por su ViewID
            PhotonView targetView = PhotonView.Find(targetViewID);
            if (targetView != null)
            {
                HeroBase target = targetView.GetComponent<HeroBase>();
                if (target != null)
                {
                    ApplyFearEffect(target);
                }
            }
        }
        
        private void ApplyFearEffect(HeroBase target)
        {
            // Aplicar efecto de miedo
            HeroMovementController moveController = target.GetComponent<HeroMovementController>();
            if (moveController != null)
            {
                // Dirección de huida (alejándose del espantapájaros)
                Vector3 fleeDirection = (target.transform.position - transform.position).normalized;
                Vector3 fleePosition = target.transform.position + fleeDirection * 10f;
                
                // Forzar movimiento
                moveController.SetDestination(fleePosition);
                
                // Aplicar un stun breve para interrumpir otras acciones
                moveController.ApplyStun(0.5f);
                
                // Mostrar efecto visual de miedo sobre el personaje
                CreateFearVisualEffect(target.transform);
            }
        }
        
        private void CreateFearVisualEffect(Transform targetTransform)
        {
            // Crear un objeto para el efecto
            GameObject fearEffect = new GameObject("FearEffect");
            fearEffect.transform.position = targetTransform.position + Vector3.up * 2f;
            
            // Añadir un TextMesh que muestre "!"
            TextMesh textMesh = fearEffect.AddComponent<TextMesh>();
            textMesh.text = "!";
            textMesh.fontSize = 20;
            textMesh.color = Color.red;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
            
            // Añadir un script para hacerlo mirar a la cámara y animarlo
            FearEffectBehavior behavior = fearEffect.AddComponent<FearEffectBehavior>();
            
            // Auto-destruir después de un tiempo
            Destroy(fearEffect, 1.0f);
        }
        
        protected override void DestroyAbility()
        {
            if (spawnedScarecrow != null)
            {
                Destroy(spawnedScarecrow);
            }
            
            base.DestroyAbility();
        }
        
        void OnDrawGizmos()
        {
            // Dibujar un gizmo para visualizar el área de miedo en el editor
            Gizmos.color = new Color(areaColor.r, areaColor.g, areaColor.b, 0.3f);
            Gizmos.DrawSphere(transform.position, radius);
        }
    }
    
    // Componente para gestionar la salud del espantapájaros
    public class ScarecrowHealth : MonoBehaviour
    {
        private float health;
        private float maxHealth;
        
        public void Initialize(float maxHealth)
        {
            this.health = maxHealth;
            this.maxHealth = maxHealth;
        }
        
        public void TakeDamage(float damage)
        {
            health -= damage;
            
            if (health <= 0)
            {
                // Buscar la habilidad padre y destruirla
                ScarecrowAbility parentAbility = GetComponentInParent<ScarecrowAbility>();
                if (parentAbility != null)
                {
                    // Si la habilidad tiene un PhotonView y es el dueño, usar PhotonNetwork.Destroy
                    if (parentAbility.photonView != null && parentAbility.photonView.IsMine)
                    {
                        PhotonNetwork.Destroy(parentAbility.gameObject);
                    }
                    else
                    {
                        // Si no somos el dueño, solo desactivar localmente
                        parentAbility.gameObject.SetActive(false);
                    }
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }
    }
    
    // Componente para comportamiento del efecto de miedo
    public class FearEffectBehavior : MonoBehaviour
    {
        private Transform cameraTransform;
        private Vector3 initialScale;
        private float animationSpeed = 2f;
        
        void Start()
        {
            cameraTransform = Camera.main.transform;
            initialScale = transform.localScale;
        }
        
        void Update()
        {
            // Hacer que el signo de exclamación mire a la cámara
            if (cameraTransform != null)
            {
                transform.LookAt(transform.position + cameraTransform.rotation * Vector3.forward,
                               cameraTransform.rotation * Vector3.up);
            }
            
            // Hacer que el signo de exclamación se haga grande y pequeño
            float pulse = (Mathf.Sin(Time.time * animationSpeed) + 1) * 0.5f; // Valor entre 0 y 1
            transform.localScale = initialScale * (1 + pulse * 0.5f);
            
            // También mover ligeramente hacia arriba
            transform.position += Vector3.up * Time.deltaTime * 0.5f;
        }
    }
}