using UnityEngine;
using Photon.Pun;

namespace Photon.Pun.Demo.Asteroids
{
    public class ShadowStepAbility : AbilityBehaviour
    {
        [Header("Shadow Step Settings")]
        public float teleportRange = 10f;
        public float explosionDelay = 1.5f;
        public float explosionDamage = 150f;
        public float explosionRadius = 3f;
        public GameObject residualImagePrefab;
        
        private Vector3 targetPosition;
        private GameObject residualImage;
        
        protected override void OnAbilityInitialized()
        {
            // Determine target position (raycast from caster in the direction they're facing)
            if (caster != null && photonView.IsMine)
            {
                RaycastHit hit;
                if (Physics.Raycast(caster.transform.position, caster.transform.forward, out hit, teleportRange))
                {
                    targetPosition = hit.point;
                }
                else
                {
                    targetPosition = caster.transform.position + caster.transform.forward * teleportRange;
                }
                
                // Create residual image at original position
                Vector3 originalPosition = caster.transform.position;
                Quaternion originalRotation = caster.transform.rotation;
                
                // Synchronize residual image creation
                photonView.RPC("CreateResidualImage", RpcTarget.All, originalPosition, originalRotation);
                
                // Teleport the player
                caster.transform.position = targetPosition;
            }
        }
        
        [PunRPC]
        private void CreateResidualImage(Vector3 position, Quaternion rotation)
        {
            // Create the residual image
            if (residualImagePrefab != null)
            {
                residualImage = Instantiate(residualImagePrefab, position, rotation);
                
                // Set up the residual image script
                ResidualImage residualScript = residualImage.GetComponent<ResidualImage>();
                if (residualScript != null)
                {
                    residualScript.Initialize(explosionDelay, explosionDamage, explosionRadius, caster);
                }
            }
        }
    }
    
    // Script for the residual image
    public class ResidualImage : MonoBehaviour
    {
        private float explosionDelay;
        private float explosionDamage;
        private float explosionRadius;
        private HeroBase caster;
        private ParticleSystem explosionEffect;
        
        public void Initialize(float delay, float damage, float radius, HeroBase owner)
        {
            explosionDelay = delay;
            explosionDamage = damage;
            explosionRadius = radius;
            caster = owner;
            
            // Find the explosion effect
            explosionEffect = GetComponentInChildren<ParticleSystem>();
            
            // Schedule the explosion
            Invoke("Explode", explosionDelay);
        }
        
        private void Explode()
        {
            // Find enemies in radius and apply damage
            Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
            foreach (Collider col in colliders)
            {
                HeroBase hero = col.GetComponent<HeroBase>();
                if (hero != null && hero != caster)
                {
                    hero.TakeDamage(explosionDamage, caster, true);
                }
            }
            
            // Activate visual effects
            if (explosionEffect != null)
            {
                explosionEffect.transform.parent = null;
                explosionEffect.Play();
                Destroy(explosionEffect.gameObject, explosionEffect.main.duration);
            }
            
            // Destroy the residual image
            Destroy(gameObject);
        }
    }
}