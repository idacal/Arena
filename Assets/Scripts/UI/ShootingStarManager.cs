using UnityEngine;

namespace Photon.Pun.Demo.Asteroids
{
    public class ShootingStarManager : MonoBehaviour
    {
        [Header("Shooting Star Settings")]
        public GameObject shootingStarPrefab;
        public float minSpawnInterval = 5f;
        public float maxSpawnInterval = 15f;
        public float minSpeed = 10f;
        public float maxSpeed = 20f;
        public float trailDuration = 0.5f;
        public float trailWidth = 0.1f;
        public Color trailColor = Color.white;
        
        [Header("Depth Settings")]
        [Tooltip("Distancia mínima desde la cámara")]
        public float minDepth = 5f;
        [Tooltip("Distancia máxima desde la cámara")]
        public float maxDepth = 20f;
        
        [Header("Spawn Settings")]
        [Tooltip("Probabilidad de spawn desde los bordes (0-1)")]
        public float edgeSpawnProbability = 0.7f;
        [Tooltip("Distancia desde los bordes para spawn (en porcentaje de la pantalla)")]
        public float edgeSpawnDistance = 0.1f;
        
        private float nextSpawnTime;
        private Camera mainCamera;
        
        void Start()
        {
            mainCamera = Camera.main;
            // Calcular el próximo tiempo de spawn
            nextSpawnTime = Time.time + Random.Range(minSpawnInterval, maxSpawnInterval);
        }
        
        void Update()
        {
            if (Time.time >= nextSpawnTime)
            {
                SpawnShootingStar();
                // Calcular el próximo tiempo de spawn
                nextSpawnTime = Time.time + Random.Range(minSpawnInterval, maxSpawnInterval);
            }
        }
        
        private void SpawnShootingStar()
        {
            // Obtener los límites de la pantalla en coordenadas del mundo
            float screenHeight = 2f * mainCamera.orthographicSize;
            float screenWidth = screenHeight * mainCamera.aspect;
            
            Vector3 spawnPosition;
            Vector2 direction;
            
            // Decidir si spawnear desde los bordes o desde el centro
            if (Random.value < edgeSpawnProbability)
            {
                // Spawn desde los bordes
                spawnPosition = GetEdgeSpawnPosition(screenWidth, screenHeight);
                direction = GetDirectionFromEdge(spawnPosition, screenWidth, screenHeight);
            }
            else
            {
                // Spawn desde el centro
                spawnPosition = GetCenterSpawnPosition(screenWidth, screenHeight);
                direction = GetRandomDirection();
            }
            
            // Ajustar la profundidad Z aleatoriamente
            float randomDepth = Random.Range(minDepth, maxDepth);
            spawnPosition.z = -randomDepth;
            
            // Crear la estrella fugaz
            GameObject star = Instantiate(shootingStarPrefab, spawnPosition, Quaternion.identity);
            
            // Configurar el TrailRenderer
            TrailRenderer trail = star.GetComponent<TrailRenderer>();
            if (trail != null)
            {
                // Ajustar el ancho del trail según la profundidad
                float depthScale = 1f - (randomDepth - minDepth) / (maxDepth - minDepth);
                trail.time = trailDuration;
                trail.startWidth = trailWidth * depthScale;
                trail.endWidth = 0f;
                trail.startColor = trailColor;
                trail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
            }
            
            // Rotar la estrella para que apunte en la dirección del movimiento
            float rotationAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            star.transform.rotation = Quaternion.Euler(0, 0, rotationAngle);
            
            // Configurar la velocidad aleatoria en la dirección calculada
            Rigidbody2D rb = star.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                float speed = Random.Range(minSpeed, maxSpeed);
                // Ajustar la velocidad según la profundidad (las estrellas más lejanas se mueven más rápido)
                speed *= 1f + (randomDepth - minDepth) / (maxDepth - minDepth);
                rb.velocity = direction * speed;
            }
            
            // Destruir la estrella después de un tiempo
            Destroy(star, 3f);
        }
        
        private Vector3 GetEdgeSpawnPosition(float screenWidth, float screenHeight)
        {
            // Elegir un borde aleatorio (0: izquierda, 1: derecha, 2: abajo, 3: arriba)
            int edge = Random.Range(0, 4);
            float edgeDistance = Mathf.Max(screenWidth, screenHeight) * edgeSpawnDistance;
            
            switch (edge)
            {
                case 0: // Izquierda
                    return new Vector3(-screenWidth/2 - edgeDistance, Random.Range(-screenHeight/2, screenHeight/2), transform.position.z);
                case 1: // Derecha
                    return new Vector3(screenWidth/2 + edgeDistance, Random.Range(-screenHeight/2, screenHeight/2), transform.position.z);
                case 2: // Abajo
                    return new Vector3(Random.Range(-screenWidth/2, screenWidth/2), -screenHeight/2 - edgeDistance, transform.position.z);
                default: // Arriba
                    return new Vector3(Random.Range(-screenWidth/2, screenWidth/2), screenHeight/2 + edgeDistance, transform.position.z);
            }
        }
        
        private Vector3 GetCenterSpawnPosition(float screenWidth, float screenHeight)
        {
            return new Vector3(
                Random.Range(-screenWidth/2, screenWidth/2),
                Random.Range(-screenHeight/2, screenHeight/2),
                transform.position.z
            );
        }
        
        private Vector2 GetDirectionFromEdge(Vector3 spawnPos, float screenWidth, float screenHeight)
        {
            // Calcular dirección hacia el centro de la pantalla
            Vector2 center = Vector2.zero;
            Vector2 direction = (center - new Vector2(spawnPos.x, spawnPos.y)).normalized;
            
            // Añadir un poco de aleatoriedad a la dirección
            float randomAngle = Random.Range(-30f, 30f);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + randomAngle;
            return new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        }
        
        private Vector2 GetRandomDirection()
        {
            float angle = Random.Range(0f, 360f);
            return new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
        }
    }
} 