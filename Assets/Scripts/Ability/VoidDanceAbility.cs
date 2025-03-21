using UnityEngine;
using System.Collections.Generic;

namespace Photon.Pun.Demo.Asteroids
{
    public class VoidDanceAbility : AOEAbility
    {
        [Header("Void Dance Settings")]
        public float damageIncreaseFactor = 1.25f;
        public int maxHitStacks = 4;
        
        // Dictionary to track hits per target
        private Dictionary<int, int> hitsByTarget = new Dictionary<int, int>();
        
        protected override void OnAbilityInitialized()
        {
            base.OnAbilityInitialized();
            hitsByTarget.Clear();
        }
        
        protected override void ProcessImpact(HeroBase target)
        {
            // Get unique target ID
            int targetId = target.photonView.ViewID;
            
            // Increment hit counter
            if (!hitsByTarget.ContainsKey(targetId))
            {
                hitsByTarget[targetId] = 0;
            }
            
            hitsByTarget[targetId]++;
            
            // Limit to maximum stacks
            if (hitsByTarget[targetId] > maxHitStacks)
            {
                hitsByTarget[targetId] = maxHitStacks;
            }
            
            // Calculate damage based on accumulated hits
            float stackMultiplier = Mathf.Pow(damageIncreaseFactor, hitsByTarget[targetId] - 1);
            float actualDamage = baseDamage * stackMultiplier;
            
            // Apply damage
            if (actualDamage > 0 && caster != null)
            {
                target.TakeDamage(actualDamage, caster, isMagicDamage);
            }
            
            // Apply additional effects
            ApplyEffects(target);
            
            // Visual impact effect
            if (impactEffectPrefab != null)
            {
                Instantiate(impactEffectPrefab, target.transform.position + Vector3.up, Quaternion.identity);
            }
        }
    }
}