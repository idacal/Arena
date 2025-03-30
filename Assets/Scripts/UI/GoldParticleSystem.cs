using UnityEngine;

namespace Photon.Pun.Demo.Asteroids 
{
    public class GoldParticleSystem : MonoBehaviour 
    {
        [Header("Configuración de Partículas")]
        public Color particleColor = new Color(1f, 0.84f, 0.0f); // Color oro
        public float duration = 1.5f;
        public int particleCount = 10;
        public float particleSize = 0.15f;
        public float velocityMin = 1.0f;
        public float velocityMax = 3.0f;
        public float gravity = 5.0f;
        
        private ParticleSystem particleSystem;
        
        void Awake() 
        {
            // Crear el sistema de partículas si no existe
            if (particleSystem == null) 
            {
                particleSystem = GetComponent<ParticleSystem>();
                if (particleSystem == null)
                {
                    particleSystem = gameObject.AddComponent<ParticleSystem>();
                    ConfigureParticleSystem();
                }
            }
        }
        
        public void Play() 
        {
            if (particleSystem != null) 
            {
                particleSystem.Play();
            }
        }
        
        private void ConfigureParticleSystem() 
        {
            // Configuración principal
            var main = particleSystem.main;
            main.duration = duration;
            main.loop = false;
            main.startLifetime = duration;
            main.startSpeed = Random.Range(velocityMin, velocityMax);
            main.startSize = particleSize;
            main.startColor = particleColor;
            main.maxParticles = particleCount * 2;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            // Emisión
            var emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { 
                new ParticleSystem.Burst(0f, (short)particleCount)
            });
            
            // Forma
            var shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;
            
            // Velocidad
            var velocity = particleSystem.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            
            // Color
            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(particleColor, 0.0f),
                    new GradientColorKey(particleColor, 0.7f),
                    new GradientColorKey(particleColor, 1.0f)
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(1.0f, 0.0f),
                    new GradientAlphaKey(1.0f, 0.7f), 
                    new GradientAlphaKey(0.0f, 1.0f)
                }
            );
            colorOverLifetime.color = gradient;
            
            // Gravedad
            var forces = particleSystem.externalForces;
            forces.enabled = true;
            forces.multiplier = 1f;
            
            // Tamaño a lo largo del tiempo
            var sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeOverLifetimeCurve = new AnimationCurve();
            sizeOverLifetimeCurve.AddKey(0.0f, 1.0f);
            sizeOverLifetimeCurve.AddKey(0.5f, 0.8f);
            sizeOverLifetimeCurve.AddKey(1.0f, 0.0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, sizeOverLifetimeCurve);
            
            // Rotación de las partículas
            var rotationOverLifetime = particleSystem.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.separateAxes = false;
            rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-120f, 120f);
            
            // Física
            var physics = particleSystem.forceOverLifetime;
            physics.enabled = true;
            physics.y = -gravity;
            
            // Renderizador
            var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
                renderer.sortMode = ParticleSystemSortMode.Distance;
                renderer.normalDirection = 1.0f;
                renderer.minParticleSize = 0.01f;
                renderer.maxParticleSize = 0.5f;
            }
        }
        
        // Método para crear un sistema de partículas de oro en una posición específica
        public static GameObject CreateGoldParticles(Vector3 position, Transform parent = null) 
        {
            GameObject particleObj = new GameObject("GoldParticles");
            particleObj.transform.position = position;
            
            if (parent != null) 
            {
                particleObj.transform.SetParent(parent, true);
            }
            
            GoldParticleSystem particleSystem = particleObj.AddComponent<GoldParticleSystem>();
            particleSystem.Play();
            
            // Destruir después de la duración
            Destroy(particleObj, particleSystem.duration + 0.5f);
            
            return particleObj;
        }
    }
} 