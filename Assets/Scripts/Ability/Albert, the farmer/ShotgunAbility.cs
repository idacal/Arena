using UnityEngine;
using System.Collections.Generic;

namespace Photon.Pun.Demo.Asteroids
{
    public class ShotgunAbility : ProjectileAbility
    {
        [Header("Shotgun Settings")]
        public float knockbackForce = 5f;      // Fuerza del empuje
        public float knockbackDuration = 0.5f;  // Duración del empuje
        
        protected override void ProcessImpact(HeroBase target)
        {
            // Aplicar daño normalmente
            base.ProcessImpact(target);
            
            // Aplicar knockback
            Vector3 knockbackDirection = (target.transform.position - transform.position).normalized;
            knockbackDirection.y = 0; // Para evitar empujar hacia arriba o abajo
            
            // Si el objetivo tiene un Rigidbody, aplicar fuerza
            Rigidbody targetRb = target.GetComponent<Rigidbody>();
            if (targetRb != null && !targetRb.isKinematic)
            {
                targetRb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
            }
            
            // Alternativamente, mover al personaje directamente
            // Esto es útil si el NavMeshAgent está controlando el movimiento
            target.transform.position += knockbackDirection * knockbackForce * 0.2f;
            
            // Pausar el NavMeshAgent brevemente para el efecto de aturdimiento
            HeroMovementController targetMovement = target.GetComponent<HeroMovementController>();
            if (targetMovement != null)
            {
                targetMovement.ApplyStun(knockbackDuration);
            }
        }
    }
}