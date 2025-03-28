using UnityEngine;

namespace Photon.Pun.Demo.Asteroids
{
    public class ShootingStar : MonoBehaviour
    {
        private TrailRenderer trailRenderer;
        private SpriteRenderer spriteRenderer;
        private Rigidbody2D rb;
        private bool isFading = false;
        private float fadeStartTime;
        private float fadeDuration = 2f; // Duración del movimiento adicional
        private Vector2 lastVelocity; // Para mantener la velocidad cuando la estrella sale de la pantalla
        
        void Start()
        {
            // Obtener o agregar componentes necesarios
            trailRenderer = GetComponent<TrailRenderer>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            rb = GetComponent<Rigidbody2D>();
            
            // Configurar el sprite renderer
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = 1; // Asegurar que esté por encima del fondo
            }
        }
        
        void Update()
        {
            if (isFading)
            {
                float elapsedTime = Time.time - fadeStartTime;
                if (elapsedTime >= fadeDuration)
                {
                    Destroy(gameObject);
                }
            }
        }
        
        void OnBecameInvisible()
        {
            if (!isFading)
            {
                // Guardar la velocidad actual
                if (rb != null)
                {
                    lastVelocity = rb.velocity;
                }
                
                // Ocultar el sprite pero mantener el trail
                if (spriteRenderer != null)
                {
                    spriteRenderer.enabled = false;
                }
                
                // Iniciar el proceso de fade
                isFading = true;
                fadeStartTime = Time.time;
            }
        }
        
        void FixedUpdate()
        {
            if (isFading && rb != null)
            {
                // Mantener el movimiento constante
                rb.velocity = lastVelocity;
            }
        }
    }
} 