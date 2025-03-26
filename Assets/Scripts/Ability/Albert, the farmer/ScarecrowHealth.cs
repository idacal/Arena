using UnityEngine;
using Photon.Pun;

namespace Photon.Pun.Demo.Asteroids
{
    public class ScarecrowHealth : MonoBehaviourPunCallbacks, IPunObservable
    {
        [Header("Health Settings")]
        public float maxHealth = 100f;
        
        private float currentHealth;
        private bool isDead = false;
        
        public void Initialize(float health)
        {
            currentHealth = health;
            maxHealth = health;
            isDead = false;
        }
        
        public void TakeDamage(float damage)
        {
            if (isDead) return;
            
            if (photonView.IsMine)
            {
                currentHealth -= damage;
                
                if (currentHealth <= 0)
                {
                    currentHealth = 0;
                    isDead = true;
                    
                    // Notificar al ScarecrowAbility que debe destruirse
                    var ability = GetComponentInParent<ScarecrowAbility>();
                    if (ability != null)
                    {
                        ability.TakeDamage(0); // Esto activará la destrucción
                    }
                }
            }
        }
        
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(currentHealth);
                stream.SendNext(isDead);
            }
            else
            {
                currentHealth = (float)stream.ReceiveNext();
                isDead = (bool)stream.ReceiveNext();
            }
        }
    }
} 