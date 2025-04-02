using UnityEngine;
using UnityEngine.UI;

namespace Photon.Pun.Demo.Asteroids
{
    /// <summary>
    /// Componente que maneja efectos visuales para botones de subida de nivel
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class LevelUpButtonFX : MonoBehaviour
    {
        [Header("Particle Settings")]
        [Tooltip("Color de las partículas")]
        public Color particleColor = new Color(1f, 0.7f, 0.2f, 1f); // Naranja brillante por defecto
        
        [Tooltip("Tamaño de las partículas")]
        [Range(0.5f, 10f)]
        public float particleSize = 3f;
        
        [Tooltip("Velocidad de emisión")]
        [Range(5, 50)]
        public int emissionRate = 20;
        
        [Tooltip("Velocidad de las partículas")]
        [Range(0.5f, 10f)]
        public float particleSpeed = 2f;
        
        [Header("References")]
        [Tooltip("Referencia al sistema de partículas (opcional)")]
        public ParticleSystem particleSystem;
        
        // Referencias privadas
        private Button button;
        private bool initialized = false;
        
        void Awake()
        {
            button = GetComponent<Button>();
            
            // Si no se asignó un sistema de partículas, buscarlo o crearlo
            if (particleSystem == null)
            {
                // Buscar en los hijos
                particleSystem = GetComponentInChildren<ParticleSystem>(true);
                
                // Si no existe, crear uno nuevo
                if (particleSystem == null)
                {
                    CreateParticleSystem();
                }
            }
            
            // Desactivar inicialmente
            if (particleSystem != null)
            {
                particleSystem.gameObject.SetActive(false);
            }
        }
        
        void Start()
        {
            // Configurar las partículas una vez en el inicio
            ConfigureParticleSystem();
            initialized = true;
        }
        
        /// <summary>
        /// Crea un nuevo sistema de partículas como hijo del botón
        /// </summary>
        private void CreateParticleSystem()
        {
            // Crear objeto para el sistema de partículas
            GameObject particleObj = new GameObject("ButtonParticles");
            particleObj.transform.SetParent(transform);
            particleObj.transform.localPosition = Vector3.zero;
            particleObj.transform.localRotation = Quaternion.identity;
            particleObj.transform.localScale = Vector3.one;
            
            // Asegurar que esté delante del botón en la UI
            particleObj.transform.SetAsLastSibling();
            
            // Añadir el sistema de partículas
            particleSystem = particleObj.AddComponent<ParticleSystem>();
            
            // Configurar inicialmente
            ConfigureParticleSystem();
        }
        
        /// <summary>
        /// Configura el sistema de partículas para UI
        /// </summary>
        private void ConfigureParticleSystem()
        {
            if (particleSystem == null) return;
            
            // Detener para poder configurar
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            
            // Main module
            var main = particleSystem.main;
            main.duration = 5.0f;
            main.loop = true;
            main.startLifetime = 1.0f;
            main.startSpeed = particleSpeed;
            main.startSize = particleSize;
            main.startColor = particleColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World; // IMPORTANTE para UI
            main.maxParticles = 100;
            main.playOnAwake = false;
            
            // Emission module
            var emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = emissionRate;
            
            // Shape module
            var shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 30f; // Radio en píxeles para UI
            shape.radiusThickness = 0.01f; // Emisión desde el borde
            shape.arc = 360f;
            
            // Color Over Lifetime
            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(particleColor, 0.0f), 
                    new GradientColorKey(new Color(particleColor.r, particleColor.g * 0.5f, 0f), 1.0f)
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0.0f, 0.0f),
                    new GradientAlphaKey(0.8f, 0.2f),
                    new GradientAlphaKey(0.0f, 1.0f) 
                }
            );
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
            
            // Size Over Lifetime
            var sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0.0f, 0.5f);
            curve.AddKey(0.5f, 1.0f);
            curve.AddKey(1.0f, 0.0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, curve);
            
            // Configurar el renderizador
            var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                // Crear material para partículas UI
                Material particleMat = new Material(Shader.Find("Particles/Standard Unlit"));
                if (particleMat != null)
                {
                    particleMat.SetFloat("_BlendOp", (float)UnityEngine.Rendering.BlendOp.Add);
                    particleMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    particleMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
                }
                
                renderer.material = particleMat;
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.sortMode = ParticleSystemSortMode.YoungestInFront;
                renderer.sortingOrder = 10; // Asegurar que esté por encima de la UI
            }
            
            Debug.Log($"[LevelUpButtonFX] Sistema de partículas configurado para UI: {gameObject.name}");
        }
        
        /// <summary>
        /// Activa las partículas
        /// </summary>
        public void ShowParticles()
        {
            if (particleSystem == null) return;
            
            if (!initialized)
            {
                ConfigureParticleSystem();
                initialized = true;
            }
            
            particleSystem.gameObject.SetActive(true);
            
            if (!particleSystem.isPlaying)
            {
                particleSystem.Clear();
                particleSystem.Play();
            }
            
            Debug.Log($"[LevelUpButtonFX] Partículas activadas para: {gameObject.name}");
        }
        
        /// <summary>
        /// Desactiva las partículas
        /// </summary>
        public void HideParticles()
        {
            if (particleSystem == null) return;
            
            if (particleSystem.isPlaying)
            {
                particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            
            particleSystem.gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Modifica el color de las partículas
        /// </summary>
        public void SetParticleColor(Color color)
        {
            particleColor = color;
            
            if (particleSystem != null)
            {
                var main = particleSystem.main;
                main.startColor = color;
            }
        }
    }
} 