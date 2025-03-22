using UnityEngine;

namespace Photon.Pun.Demo.Asteroids
{
    /// <summary>
    /// Clase auxiliar para manejar la creación segura de materiales en runtime
    /// Añade esta clase a tu proyecto para evitar errores de shader
    /// </summary>
    public static class ShaderSafetyUtility
    {
        // Define shaders alternativos en orden de preferencia
        private static readonly string[] FALLBACK_SHADERS = new string[]
        {
            "Mobile/Diffuse",
            "Sprites/Default",
            "Standard",
            "Mobile/Particles/Additive",
            "Mobile/Particles/Alpha Blended",
            "Unlit/Color"
        };
        
        /// <summary>
        /// Crea un material de forma segura, con fallbacks si el shader principal no está disponible
        /// </summary>
        /// <param name="primaryShaderName">Nombre del shader preferido</param>
        /// <param name="color">Color a aplicar al material</param>
        /// <returns>Material seguro o null si no se pudo crear</returns>
        public static Material CreateSafeMaterial(string primaryShaderName, Color color)
        {
            Material material = null;
            
            // Intentar con el shader primario
            try
            {
                Shader primaryShader = Shader.Find(primaryShaderName);
                if (primaryShader != null)
                {
                    material = new Material(primaryShader);
                    material.color = color;
                    return material;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Error al crear material con shader {primaryShaderName}: {e.Message}");
            }
            
            // Intentar con shaders alternativos
            foreach (string shaderName in FALLBACK_SHADERS)
            {
                try
                {
                    Shader fallbackShader = Shader.Find(shaderName);
                    if (fallbackShader != null)
                    {
                        material = new Material(fallbackShader);
                        material.color = color;
                        Debug.Log($"Usando shader alternativo: {shaderName}");
                        return material;
                    }
                }
                catch (System.Exception) 
                {
                    // Intentar con el siguiente
                    continue;
                }
            }
            
            // Último recurso - material por defecto
            Debug.LogError("No se pudo crear un material seguro con ningún shader!");
            return null;
        }
        
        /// <summary>
        /// Crea un particle system seguro con configuraciones predeterminadas
        /// </summary>
        public static ParticleSystem CreateSafeParticleSystem(GameObject parent, Color color, float duration, float size)
        {
            GameObject particleObj = new GameObject("SafeParticleSystem");
            particleObj.transform.SetParent(parent.transform);
            particleObj.transform.localPosition = Vector3.zero;
            
            ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();
            
            // Configuración principal
            var main = ps.main;
            main.startColor = color;
            main.startSize = size;
            main.duration = duration;
            main.loop = false;
            
            // Emisión básica
            var emission = ps.emission;
            emission.rateOverTime = 10f;
            
            // Forma básica
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;
            
            // Renderer seguro
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateSafeMaterial("Particles/Standard Unlit", color);
            
            return ps;
        }
        
        /// <summary>
        /// Crea un LineRenderer seguro con configuraciones predeterminadas
        /// </summary>
        public static LineRenderer CreateSafeLineRenderer(GameObject parent, Color color, float width, int points)
        {
            GameObject lineObj = new GameObject("SafeLineRenderer");
            lineObj.transform.SetParent(parent.transform);
            lineObj.transform.localPosition = Vector3.zero;
            
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.startWidth = width;
            lr.endWidth = width;
            lr.positionCount = points;
            
            // Material seguro
            lr.material = CreateSafeMaterial("Sprites/Default", color);
            lr.startColor = color;
            lr.endColor = color;
            
            return lr;
        }
    }
}