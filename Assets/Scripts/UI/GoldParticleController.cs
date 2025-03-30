using UnityEngine;
using System.Collections;

namespace Photon.Pun.Demo.Asteroids
{
    /// <summary>
    /// Controla el ciclo de vida de las partículas de oro, asegurando que se reproduzcan solo una vez
    /// </summary>
    public class GoldParticleController : MonoBehaviour
    {
        private ParticleSystem particleSystem;
        private float lifetime;
        private bool hasPlayed = false;
        
        /// <summary>
        /// Inicializa el controlador con un sistema de partículas y una duración
        /// </summary>
        public void Initialize(ParticleSystem ps, float duration)
        {
            particleSystem = ps;
            lifetime = duration;
            
            // Iniciar corrutina para destruir después de que termine
            StartCoroutine(DestroyAfterLifetime());
        }
        
        void Start()
        {
            if (particleSystem == null)
            {
                particleSystem = GetComponent<ParticleSystem>();
            }
            
            // Asegurarse de que el sistema esté configurado correctamente
            if (particleSystem != null)
            {
                // Forzar que el sistema no tenga loop
                var main = particleSystem.main;
                main.loop = false;
                
                if (!hasPlayed)
                {
                    particleSystem.Play(true);
                    hasPlayed = true;
                    Debug.Log($"GoldParticleController: Reproduciendo partículas {gameObject.name}");
                }
            }
        }
        
        /// <summary>
        /// Corrutina para destruir el sistema después de su tiempo de vida
        /// </summary>
        private IEnumerator DestroyAfterLifetime()
        {
            // Esperar a que termine la reproducción
            yield return new WaitForSeconds(lifetime);
            
            // Verificar si ya terminó de emitir y todas las partículas han muerto
            if (particleSystem != null)
            {
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                
                // Esperar a que todas las partículas mueran
                while (particleSystem.particleCount > 0)
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }
            
            // Destruir el objeto
            Debug.Log($"GoldParticleController: Destruyendo sistema {gameObject.name}");
            Destroy(gameObject);
        }
        
        void OnDestroy()
        {
            Debug.Log($"GoldParticleController: Sistema destruido {gameObject.name}");
        }
    }
} 