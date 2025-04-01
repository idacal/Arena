using UnityEngine;
using UnityEditor; // Necesario para funciones del editor como MenuItem y AssetDatabase
using System.IO;   // Necesario para manejar rutas de directorios

public class CosmicFogCreator
{
    [MenuItem("Tools/Create Cosmic Fog")] // Añade la opción al menú Tools
    public static void CreateCosmicFogEffect()
    {
        // --- Crear el GameObject y el Sistema de Partículas ---
        GameObject fogGO = new GameObject("CosmicFogEffect");
        ParticleSystem ps = fogGO.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psRenderer = fogGO.GetComponent<ParticleSystemRenderer>();

        // --- Configurar Módulos Principales ---
        var main = ps.main; // Necesitamos obtener los módulos para modificarlos
        main.duration = 5f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f); // Vida un poco más larga para el cruce
        main.startSpeed = 0f; // Sin velocidad inicial aleatoria, movimiento más uniforme
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.5f); // Tamaño inicial diminuto
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.15f, 0.15f, 0.25f, 0.05f), new Color(0.25f, 0.25f, 0.4f, 0.15f)); // Alfa inicial MUY bajo
        main.maxParticles = 3000; // Límite muchísimo mayor para efecto nube (Rate * LifetimeMax)
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // --- Configurar Emisión ---
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 300; // Tasa de emisión masiva para densidad de nube

        // --- Configurar Forma del Emisor ---
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(30, 5, 10); // Caja más plana y estrecha en Y, como una banda

        // --- Configurar Color sobre la Vida ---
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient grad = new Gradient();
        // Clave Alfa: Fade in suave, mantiene, fade out
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0.0f), new GradientAlphaKey(1.0f, 0.15f), new GradientAlphaKey(1.0f, 0.85f), new GradientAlphaKey(0.0f, 1.0f) } // Alpha: 0 -> 1 (rápido) -> 1 -> 0
        );
        colorOverLifetime.color = grad;

        // --- Configurar Tamaño sobre la Vida ---
        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f); // Mantener tamaño relativo constante (más simple)

        // --- Configurar Velocidad sobre la Vida para movimiento lateral ---
        var velocityOverLifetime = ps.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
        velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(0.6f); // Velocidad un poco más lenta para efecto nube
        velocityOverLifetime.y = 0f;
        velocityOverLifetime.z = 0f;

        // --- (Opcional) Configurar Ruido para movimiento sutil ---
        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.02f; // Ruido casi imperceptible
        noise.frequency = 0.1f; // Frecuencia más baja para ondulaciones suaves
        noise.scrollSpeed = 0.05f;
        noise.quality = ParticleSystemNoiseQuality.High;

        // --- Crear y Configurar el Material ---
        // Definir ruta y asegurarse de que el directorio exista
        string baseMaterialFolder = "Assets/Materials";
        string specificMaterialFolder = "CosmicFog";
        string materialFolderPath = Path.Combine(baseMaterialFolder, specificMaterialFolder); // Path.Combine es más robusto
        string materialPath = Path.Combine(materialFolderPath, "CosmicFogMaterial.mat");

        // Asegurarse de que las carpetas base y específica existan usando AssetDatabase
        if (!AssetDatabase.IsValidFolder(baseMaterialFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
            AssetDatabase.Refresh(); // Refrescar para asegurar que Unity lo vea
            Debug.Log($"Directorio creado: {baseMaterialFolder}");
        }
        if (!AssetDatabase.IsValidFolder(materialFolderPath))
        {
            AssetDatabase.CreateFolder(baseMaterialFolder, specificMaterialFolder);
            AssetDatabase.Refresh(); // Refrescar de nuevo
            Debug.Log($"Directorio creado: {materialFolderPath}");
        }

        // Buscar un shader adecuado, priorizando uno simple y común
        Shader particleShader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (particleShader == null)
        {
             Debug.LogError("Shader 'Legacy Shaders/Particles/Alpha Blended' no encontrado. ¡El efecto podría no verse correctamente!");
             // Intentar otro fallback si es necesario, o dejarlo null y manejar el error
             particleShader = Shader.Find("Particles/Standard Unlit"); // Último recurso
             if (particleShader == null) {
                 Debug.LogError("Tampoco se encontró 'Particles/Standard Unlit'. Se usará el shader por defecto de partículas.");
                 // Unity asignará uno por defecto si psRenderer.material se establece a un nuevo material sin shader válido
             }
        }

        // Crear instancia del material
        Material fogMaterial = new Material(particleShader); // Asignar el shader encontrado

        // Configurar propiedades del material - Para Alpha Blended, principalmente el tinte
        fogMaterial.SetColor("_TintColor", Color.white); // Usar _TintColor en lugar de _Color para este shader

        // Ya no es necesario configurar _Mode, _SrcBlend, etc., ya que Alpha Blended lo maneja internamente.
        /*
        if (fogMaterial.HasProperty("_Mode"))
        {
           // ... código eliminado ...
        }
        else
        {
             // ... código eliminado ...
        }
        */

        // Guardar el material como un asset
        AssetDatabase.CreateAsset(fogMaterial, materialPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(); // Refrescar para que Unity lo vea

        // Asignar el material al Renderer del Sistema de Partículas
        psRenderer.material = fogMaterial;

        // Asignar la textura por defecto de partículas de Unity para suavizar los bordes
        Texture defaultParticleTex = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");
        if (defaultParticleTex != null)
        {
            psRenderer.material.mainTexture = defaultParticleTex;
             Debug.Log("Textura 'Default-Particle' asignada al material.");
        } else {
             Debug.LogWarning("No se pudo encontrar la textura 'Default-Particle'. Las partículas podrían verse como cuadrados.");
        }

        // Seleccionar el objeto creado en la jerarquía para que el usuario lo vea
        Selection.activeGameObject = fogGO;
        // Centrar la vista de escena en el objeto creado
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.FrameSelected();
        }

        Debug.Log("Efecto Cosmic Fog creado exitosamente en la escena y material guardado en " + materialPath);
    }
}
