using UnityEngine;

namespace Photon.Pun.Demo.Asteroids
{
    /// <summary>
    /// Script para el indicador visual de colocación de habilidades
    /// </summary>
    public class PlacementIndicator : MonoBehaviour
    {
        [Header("Visual Settings")]
        public float pulseSpeed = 2f;             // Velocidad de pulso
        public float minScale = 0.9f;             // Escala mínima del pulso
        public float maxScale = 1.1f;             // Escala máxima del pulso
        public float rotationSpeed = 15f;         // Velocidad de rotación
        public bool rotateIndicator = true;       // Si debe rotar
        public bool pulseIndicator = true;        // Si debe pulsar
        
        // Variables privadas
        private Vector3 baseScale;                // Escala base del indicador
        private float pulseTime = 0f;             // Contador para el pulso
        
        private void Start()
        {
            // Guardar la escala inicial
            baseScale = transform.localScale;
        }
        
        private void Update()
        {
            // Efecto de pulso
            if (pulseIndicator)
            {
                pulseTime += Time.deltaTime * pulseSpeed;
                
                // Calcular factor de escala para el pulso
                float pulseFactor = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(pulseTime) + 1f) * 0.5f);
                
                // Aplicar escala
                transform.localScale = baseScale * pulseFactor;
            }
            
            // Efecto de rotación
            if (rotateIndicator)
            {
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            }
        }
        
        /// <summary>
        /// Configura el color del indicador
        /// </summary>
        /// <param name="color">Color a aplicar</param>
        public void SetColor(Color color)
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }
        }
        
        /// <summary>
        /// Configura la escala del indicador
        /// </summary>
        /// <param name="scale">Nueva escala</param>
        public void SetScale(Vector3 scale)
        {
            transform.localScale = scale;
            baseScale = scale;
        }
    }
} 