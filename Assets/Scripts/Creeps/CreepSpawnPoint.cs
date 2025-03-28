using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

namespace Photon.Pun.Demo.Asteroids
{
    public class CreepSpawnPoint : MonoBehaviourPunCallbacks
    {
        [Header("Spawn Configuration")]
        public string spawnPointName;
        public string creepType;
        public GameObject[] creepPrefabs;
        public int maxCreeps = 3;
        public float spawnInterval = 30f;
        public float spawnRadius = 2f;
        
        [Header("Debug")]
        public bool showGizmos = true;
        public Color gizmoColor = Color.yellow;
        
        private List<NeutralCreep> activeCreeps = new List<NeutralCreep>();
        private float nextSpawnTime;
        private bool isInitialized = false;
        
        private void Start()
        {
            InitializeSpawnPoint();
        }
        
        private void Update()
        {
            if (!isInitialized || !PhotonNetwork.IsMasterClient) return;
            
            // Limpiar criaturas muertas de la lista
            activeCreeps.RemoveAll(creep => creep == null);
            
            // Verificar si podemos spawnear más criaturas
            if (activeCreeps.Count < maxCreeps && Time.time >= nextSpawnTime)
            {
                SpawnCreep();
                nextSpawnTime = Time.time + spawnInterval;
            }
        }
        
        private void InitializeSpawnPoint()
        {
            isInitialized = true;
            
            // No necesitamos asignar photonView ya que es una propiedad de solo lectura
            // y se inicializa automáticamente por MonoBehaviourPunCallbacks
            
            // Configurar el PhotonView
            if (photonView != null)
            {
                photonView.ObservedComponents = new List<Component> { this };
                photonView.Synchronization = ViewSynchronization.Unreliable;
            }
            
            // Solo el MasterClient spawneará los creeps iniciales
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log($"[CreepSpawnPoint] Spawneando {maxCreeps} creeps iniciales en {spawnPointName}");
                
                // Spawnear todos los creeps iniciales
                for (int i = 0; i < maxCreeps; i++)
                {
                    SpawnCreep();
                }
            }
            
            // Inicializar el tiempo del próximo spawn
            nextSpawnTime = Time.time + spawnInterval;
        }
        
        private void SpawnCreep()
        {
            if (creepPrefabs == null || creepPrefabs.Length == 0)
            {
                Debug.LogError($"[CreepSpawnPoint] No hay prefabs configurados para el spawn point {spawnPointName}");
                return;
            }
            
            // Limpiar la lista de creeps muertos
            activeCreeps.RemoveAll(creep => creep == null);
            
            // Verificar si podemos spawnear más criaturas
            if (activeCreeps.Count >= maxCreeps)
            {
                return;
            }
            
            // Seleccionar un prefab aleatorio
            GameObject prefabToSpawn = creepPrefabs[Random.Range(0, creepPrefabs.Length)];
            
            // Calcular posición de spawn aleatoria dentro del radio
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPosition = transform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
            
            // Instanciar la criatura usando PhotonNetwork
            GameObject creepObject = PhotonNetwork.Instantiate(
                GetPrefabPathInResources(prefabToSpawn),
                spawnPosition,
                Quaternion.identity
            );
            
            // Obtener el componente NeutralCreep
            NeutralCreep creep = creepObject.GetComponent<NeutralCreep>();
            if (creep != null)
            {
                activeCreeps.Add(creep);
                creep.OnDeath += HandleCreepDeath;
                Debug.Log($"[CreepSpawnPoint] Spawneado nuevo creep: {creep.creepName}");
            }
            else
            {
                Debug.LogError($"[CreepSpawnPoint] El prefab {prefabToSpawn.name} no tiene el componente NeutralCreep");
            }
        }
        
        private void HandleCreepDeath(NeutralCreep creep)
        {
            if (creep != null)
            {
                activeCreeps.Remove(creep);
                creep.OnDeath -= HandleCreepDeath;
                
                // Programar el próximo spawn después del tiempo de respawn
                nextSpawnTime = Time.time + creep.respawnTime;
            }
        }
        
        private string GetPrefabPathInResources(GameObject prefab)
        {
            // Intentar encontrar el prefab en la carpeta Resources
            string[] possiblePaths = new string[]
            {
                $"Creeps/{prefab.name}",
                $"Prefabs/Creeps/{prefab.name}",
                prefab.name
            };
            
            foreach (string path in possiblePaths)
            {
                if (Resources.Load<GameObject>(path) != null)
                {
                    return path;
                }
            }
            
            Debug.LogError($"[CreepSpawnPoint] No se pudo encontrar el prefab {prefab.name} en Resources");
            return null;
        }
        
        private void OnDrawGizmos()
        {
            if (!showGizmos) return;
            
            // Dibujar el área de spawn
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, spawnRadius);
            
            // Dibujar el punto central
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position, 0.5f);
        }
        
        private void OnDestroy()
        {
            // Limpiar las suscripciones a eventos
            foreach (var creep in activeCreeps)
            {
                if (creep != null)
                {
                    creep.OnDeath -= HandleCreepDeath;
                }
            }
        }
    }
} 