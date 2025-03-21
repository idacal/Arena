using UnityEngine;
using System.Collections.Generic;

namespace Photon.Pun.Demo.Asteroids
{
    public class SpectralSlideAbility : BuffAbility
    {
        [Header("Spectral Slide Settings")]
        public float damageOnPass = 60f;
        public LayerMask enemyLayers;
        
        private Collider casterCollider;
        private List<int> damagedTargets = new List<int>();
        
        protected override void OnAbilityInitialized()
        {
            base.OnAbilityInitialized();
            
            // Get caster's collider
            if (caster != null)
            {
                casterCollider = caster.GetComponent<Collider>();
                
                // Disable collisions with enemies
                if (casterCollider != null)
                {
                    Physics.IgnoreLayerCollision(caster.gameObject.layer, LayerMask.NameToLayer("Enemy"), true);
                }
            }
            
            // Clear list of damaged targets
            damagedTargets.Clear();
        }
        
        protected override void AbilityUpdate()
        {
            base.AbilityUpdate();
            
            // If we are the owner, check for collisions with enemies
            if (photonView.IsMine && caster != null)
            {
                CheckEnemyPassing();
            }
        }
        
        private void CheckEnemyPassing()
        {
            // Create a box to detect enemies in the path
            Vector3 boxSize = casterCollider != null ? casterCollider.bounds.size : new Vector3(1, 2, 1);
            Collider[] hitColliders = Physics.OverlapBox(
                caster.transform.position, 
                boxSize / 2, 
                caster.transform.rotation, 
                enemyLayers);
            
            foreach (Collider col in hitColliders)
            {
                HeroBase enemy = col.GetComponent<HeroBase>();
                if (enemy != null && enemy != caster)
                {
                    int enemyId = enemy.photonView.ViewID;
                    
                    // If we haven't damaged this enemy yet
                    if (!damagedTargets.Contains(enemyId))
                    {
                        damagedTargets.Add(enemyId);
                        enemy.TakeDamage(damageOnPass, caster, false);
                        
                        // Visual effect when passing through
                        if (impactEffectPrefab != null)
                        {
                            Instantiate(impactEffectPrefab, enemy.transform.position, Quaternion.identity);
                        }
                    }
                }
            }
        }
        
        protected override void DestroyAbility()
        {
            // Restore collisions with enemies
            if (caster != null && casterCollider != null)
            {
                Physics.IgnoreLayerCollision(caster.gameObject.layer, LayerMask.NameToLayer("Enemy"), false);
            }
            
            base.DestroyAbility();
        }
    }
}