using UnityEngine;

namespace Photon.Pun.Demo.Asteroids
{
    public class StarfieldBackground : MonoBehaviour
    {
        [Header("Star Settings")]
        public int starCount = 500;
        public float starSize = 0.1f;
        public Color starColor = Color.white;
        public float starBrightness = 1f;
        
        [Header("Animation Settings")]
        public float scrollSpeed = 0.02f;
        public float twinkleSpeed = 0.3f;
        public float twinkleAmount = 0.2f;
        
        [Header("Render Settings")]
        public int sortingOrder = -1;
        public float cameraDistance = 0f;
        
        [Header("Star Distribution")]
        public float minStarSize = 0.03f;
        public float maxStarSize = 0.15f;
        public float minStarBrightness = 0.5f;
        public float maxStarBrightness = 1f;
        
        [Header("Star Texture")]
        public Texture2D starTexture;
        
        private new ParticleSystem particleSystem;
        private ParticleSystem.Particle[] stars;
        private float[] starTwinkleOffsets;
        private float[] starSizes;
        private float[] starBrightnesses;
        
        void Start()
        {
            // Crear el sistema de partículas
            particleSystem = gameObject.AddComponent<ParticleSystem>();
            
            // Configurar el renderer para que se muestre detrás de la UI
            var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = sortingOrder;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.World;
            renderer.maxParticleSize = 1f;
            renderer.normalDirection = 1f;
            renderer.sortMode = ParticleSystemSortMode.Distance;
            
            // Crear y configurar el material
            Material particleMaterial = new Material(Shader.Find("Sprites/Default"));
            particleMaterial.SetFloat("_Mode", 0);
            particleMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            particleMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            particleMaterial.SetInt("_ZWrite", 0);
            particleMaterial.DisableKeyword("_ALPHATEST_ON");
            particleMaterial.EnableKeyword("_ALPHABLEND_ON");
            particleMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            particleMaterial.renderQueue = 3000;
            
            // Configurar la textura de la estrella
            if (starTexture != null)
            {
                particleMaterial.mainTexture = starTexture;
                particleMaterial.SetTexture("_MainTex", starTexture);
            }
            
            renderer.material = particleMaterial;
            
            // Configurar el módulo principal
            var main = particleSystem.main;
            main.loop = true;
            main.playOnAwake = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = float.MaxValue;
            main.startSize = starSize;
            main.startColor = starColor;
            main.maxParticles = starCount;
            
            // Configurar la emisión
            var emission = particleSystem.emission;
            emission.rateOverTime = 0;
            emission.burstCount = 1;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, starCount));
            
            // Configurar la forma
            var shape = particleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 0f;
            shape.radius = 50f;
            
            // Configurar el color
            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            
            // Inicializar arrays
            stars = new ParticleSystem.Particle[starCount];
            starTwinkleOffsets = new float[starCount];
            starSizes = new float[starCount];
            starBrightnesses = new float[starCount];
            
            // Generar propiedades aleatorias para cada estrella
            for (int i = 0; i < starCount; i++)
            {
                starTwinkleOffsets[i] = Random.Range(0f, 2f * Mathf.PI);
                starSizes[i] = Random.Range(minStarSize, maxStarSize);
                starBrightnesses[i] = Random.Range(minStarBrightness, maxStarBrightness);
            }
            
            // Obtener las partículas iniciales
            particleSystem.GetParticles(stars, starCount);
            
            // Posicionar el objeto a la distancia correcta de la cámara
            transform.position = new Vector3(0, 0, cameraDistance);
            
            // Distribuir las estrellas inicialmente
            for (int i = 0; i < starCount; i++)
            {
                stars[i].position = new Vector3(
                    Random.Range(-15f, 15f),
                    Random.Range(-10f, 10f),
                    Random.Range(-0.5f, 0.5f)
                );
                stars[i].startColor = starColor;
                stars[i].startSize = starSizes[i];
            }
            particleSystem.SetParticles(stars, starCount);
            
            // Asegurarse de que el sistema de partículas esté activo
            particleSystem.Play();
            particleSystem.Simulate(0, true, true);
        }
        
        void Update()
        {
            // Actualizar posición de las estrellas
            for (int i = 0; i < starCount; i++)
            {
                // Mover hacia abajo muy lentamente
                Vector3 pos = stars[i].position;
                pos.y -= scrollSpeed * Time.deltaTime;
                
                // Si la estrella sale de la pantalla, moverla arriba
                if (pos.y < -10f)
                {
                    pos.y = 10f;
                    pos.x = Random.Range(-15f, 15f);
                    pos.z = Random.Range(-0.5f, 0.5f);
                }
                
                // Aplicar parpadeo suave
                float twinkle = Mathf.Sin(Time.time * twinkleSpeed + starTwinkleOffsets[i]) * twinkleAmount;
                Color currentColor = stars[i].GetCurrentColor(particleSystem);
                currentColor.a = starBrightnesses[i] + twinkle;
                
                stars[i].position = pos;
                stars[i].startColor = currentColor;
            }
            
            // Actualizar el sistema de partículas
            particleSystem.SetParticles(stars, starCount);
        }
    }
} 