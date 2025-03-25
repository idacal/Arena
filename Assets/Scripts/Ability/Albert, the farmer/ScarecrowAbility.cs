using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

namespace Photon.Pun.Demo.Asteroids
{
    public class ScarecrowAbility : AOEAbility
    {
        [Header("Scarecrow Settings")]
        public float fearDuration = 2f;
        public float scarecrowHealth = 100f;
        
        [Header("Prefab References")]
        public GameObject scarecrowPrefab;
        
        [Header("Visual Settings")]
        public Color areaColor = new Color(1f, 0.7f, 0f, 0.3f);
        
        private GameObject scarecrowInstance;
        private float currentHealth;
        private Dictionary<int, float> lastFearTimes = new Dictionary<int, float>();
        
        public void OnPhotonInstantiate(PhotonMessageInfo info)
        {
            if (photonView.IsMine)
            {
                CreateScarecrowVisual();
            }
        }
        
        private void CreateScarecrowVisual()
        {
            if (scarecrowPrefab != null)
            {
                // Instanciar el prefab del espantapájaros
                scarecrowInstance = Instantiate(scarecrowPrefab, transform.position, Quaternion.identity);
                scarecrowInstance.transform.SetParent(transform);
                
                // Configurar el ScarecrowPrefabSetup
                var setup = scarecrowInstance.GetComponent<ScarecrowPrefabSetup>();
                if (setup != null)
                {
                    setup.areaRadius = radius;
                    setup.areaColor = areaColor;
                    setup.SetupAreaEffect();
                    setup.SetupScarecrowVisual();
                    
                    // Inicializar la salud
                    var healthComponent = scarecrowInstance.GetComponent<ScarecrowHealth>();
                    if (healthComponent != null)
                    {
                        healthComponent.Initialize(scarecrowHealth);
                    }
                }
                
                currentHealth = scarecrowHealth;
            }
            else
            {
                Debug.LogError("ScarecrowPrefab no está asignado en " + gameObject.name);
            }
        }
        
        public void TakeDamage(float damage)
        {
            if (!photonView.IsMine) return;
            
            currentHealth -= damage;
            
            if (currentHealth <= 0)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }
        
        protected void OnTriggerEnter(Collider other)
        {
            if (!photonView.IsMine) return;
            
            IFearable fearable = other.GetComponent<IFearable>();
            if (fearable != null)
            {
                int targetId = other.GetComponent<PhotonView>()?.ViewID ?? -1;
                if (targetId != -1)
                {
            float lastHitTime = 0f;
            lastFearTimes.TryGetValue(targetId, out lastHitTime);
            
            if (Time.time >= lastHitTime + fearDuration)
            {
                lastFearTimes[targetId] = Time.time;
                    photonView.RPC("RPC_ApplyFearEffect", RpcTarget.All, targetId);
                    }
                }
            }
        }
        
        [PunRPC]
        private void RPC_ApplyFearEffect(int targetViewID)
        {
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
            HeroMovementController moveController = target.GetComponent<HeroMovementController>();
            if (moveController != null)
            {
                Vector3 fleeDirection = (target.transform.position - transform.position).normalized;
                Vector3 fleePosition = target.transform.position + fleeDirection * 10f;
                
                moveController.SetDestination(fleePosition);
                moveController.ApplyStun(0.5f);
                
                CreateFearVisualEffect(target.transform);
            }
        }
        
        private void CreateFearVisualEffect(Transform targetTransform)
        {
            GameObject fearEffect = new GameObject("FearEffect");
            fearEffect.transform.position = targetTransform.position + Vector3.up * 2f;
            
            TextMesh textMesh = fearEffect.AddComponent<TextMesh>();
            textMesh.text = "!";
            textMesh.fontSize = 20;
            textMesh.color = Color.red;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
            
            fearEffect.AddComponent<FearEffectBehavior>();
            
            Destroy(fearEffect, 1.0f);
        }
        
        public void OnAbilityEnd()
        {
            if (scarecrowInstance != null)
            {
                // Asegurarnos de que se destruya todo el prefab y sus hijos
                if (photonView.IsMine)
                {
                    PhotonNetwork.Destroy(scarecrowInstance);
                }
                else
                {
                    Destroy(scarecrowInstance);
                }
                scarecrowInstance = null;
            }
        }
        
        protected void OnDestroy()
        {
            if (scarecrowInstance != null)
            {
                // Asegurarnos de que se destruya todo el prefab y sus hijos
                if (photonView.IsMine)
                {
                    PhotonNetwork.Destroy(scarecrowInstance);
                }
                else
                {
                    Destroy(scarecrowInstance);
                }
                scarecrowInstance = null;
            }
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