using UnityEngine;
using System.Collections;

namespace Photon.Pun.Demo.Asteroids
{
    public class TwilightEdgeAbility : AbilityBehaviour
    {
        [Header("Twilight Blade Settings")]
        public float markDuration = 3f;
        public float criticalDamage = 350f;
        public float range = 10f;
        
        [Header("Visual Effects")]
        public GameObject markEffectPrefab;
        public GameObject intangibleEffectPrefab;
        public GameObject criticalAttackEffectPrefab;
        
        private HeroBase markedTarget;
        private GameObject markEffect;
        private GameObject intangibleEffect;
        
        protected override void OnAbilityInitialized()
        {
            // Only the owner selects a target
            if (photonView.IsMine && caster != null)
            {
                // Find a target in the player's line of sight
                RaycastHit hit;
                if (Physics.Raycast(caster.transform.position, caster.transform.forward, out hit, range))
                {
                    HeroBase target = hit.collider.GetComponent<HeroBase>();
                    if (target != null && target != caster)
                    {
                        markedTarget = target;
                        
                        // Synchronize marking with all clients
                        photonView.RPC("MarkTarget", RpcTarget.All, markedTarget.photonView.ViewID);
                        
                        // Become intangible
                        photonView.RPC("BecomeIntangible", RpcTarget.All);
                        
                        // Schedule the final attack
                        StartCoroutine(ExecuteFinalAttack());
                    }
                }
            }
        }
        
        [PunRPC]
        private void MarkTarget(int targetViewID)
        {
            // Find the target based on ViewID
            PhotonView targetView = PhotonView.Find(targetViewID);
            if (targetView != null)
            {
                markedTarget = targetView.GetComponent<HeroBase>();
                
                // Create visual mark effect
                if (markEffectPrefab != null && markedTarget != null)
                {
                    markEffect = Instantiate(markEffectPrefab, markedTarget.transform);
                    Destroy(markEffect, markDuration);
                }
            }
        }
        
        [PunRPC]
        private void BecomeIntangible()
        {
            // Make caster intangible
            if (caster != null)
            {
                // Disable collisions
                Collider casterCollider = caster.GetComponent<Collider>();
                if (casterCollider != null)
                {
                    casterCollider.enabled = false;
                }
                
                // Visual intangibility effect
                if (intangibleEffectPrefab != null)
                {
                    intangibleEffect = Instantiate(intangibleEffectPrefab, caster.transform);
                }
                
                // Change shader for ghostly appearance (in a real implementation)
            }
        }
        
        private IEnumerator ExecuteFinalAttack()
        {
            // Wait for mark duration
            yield return new WaitForSeconds(markDuration);
            
            // Verify everything is still valid
            if (markedTarget != null && caster != null && photonView.IsMine)
            {
                // Teleport behind the target
                Vector3 positionBehind = markedTarget.transform.position - markedTarget.transform.forward * 2f;
                caster.transform.position = positionBehind;
                caster.transform.rotation = markedTarget.transform.rotation;
                
                // Execute critical attack
                photonView.RPC("ExecuteCriticalAttack", RpcTarget.All);
            }
            else
            {
                // Cancel if something fails
                DestroyAbility();
            }
        }
        
        [PunRPC]
        private void ExecuteCriticalAttack()
        {
            // Restore tangibility
            if (caster != null)
            {
                Collider casterCollider = caster.GetComponent<Collider>();
                if (casterCollider != null)
                {
                    casterCollider.enabled = true;
                }
                
                // Destroy intangibility effect
                if (intangibleEffect != null)
                {
                    Destroy(intangibleEffect);
                }
            }
            
            // Apply critical damage to target
            if (markedTarget != null && caster != null)
            {
                markedTarget.TakeDamage(criticalDamage, caster, false);
                
                // Visual effect for critical attack
                if (criticalAttackEffectPrefab != null)
                {
                    Instantiate(criticalAttackEffectPrefab, 
                        markedTarget.transform.position, 
                        Quaternion.identity);
                }
            }
            
            // Destroy the ability
            DestroyAbility();
        }
    }
}