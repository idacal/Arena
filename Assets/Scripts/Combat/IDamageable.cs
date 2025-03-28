using UnityEngine;

namespace Photon.Pun.Demo.Asteroids
{
    public interface IDamageable
    {
        void TakeDamage(float damage, object attacker);
    }
} 