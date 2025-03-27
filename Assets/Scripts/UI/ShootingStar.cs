using UnityEngine;

namespace Photon.Pun.Demo.Asteroids
{
    public class ShootingStar : MonoBehaviour
    {
        private TrailRenderer trailRenderer;
        private SpriteRenderer spriteRenderer;
        
        void Start()
        {
            // Obtener o agregar componentes necesarios
            trailRenderer = GetComponent<TrailRenderer>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            
            // Configurar el sprite renderer
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = 1; // Asegurar que esté por encima del fondo
            }
        }
        
        void OnBecameInvisible()
        {
            // Destruir la estrella cuando salga de la pantalla
            Destroy(gameObject);
        }
    }
} 