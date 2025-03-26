using UnityEngine;
using UnityEditor;
using System.IO;

public class ShotgunPrefabGenerator : EditorWindow
{
    private Material projectileMaterial;
    private Material muzzleFlashMaterial;
    private Material impactMaterial;

    [MenuItem("Tools/Generate Shotgun Prefabs")]
    public static void ShowWindow()
    {
        GetWindow<ShotgunPrefabGenerator>("Shotgun Prefab Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Shotgun Prefab Generator", EditorStyles.boldLabel);

        if (GUILayout.Button("Generate All Prefabs"))
        {
            CreateMaterials();
            CreateProjectilePrefab();
            CreateMuzzleFlashPrefab();
            CreateImpactPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private void CreateMaterials()
    {
        // Crear directorio para materiales
        string materialsDirectory = "Assets/Resources/Prefabs/Abilities/Materials";
        if (!Directory.Exists(materialsDirectory))
        {
            string[] folders = materialsDirectory.Split('/');
            string currentPath = folders[0];
            for (int i = 1; i < folders.Length; i++)
            {
                string parentFolder = currentPath;
                currentPath = Path.Combine(currentPath, folders[i]);
                if (!Directory.Exists(currentPath))
                {
                    AssetDatabase.CreateFolder(parentFolder, folders[i]);
                }
            }
        }

        // Buscar shaders seguros
        Shader standardShader = Shader.Find("Universal Render Pipeline/Lit");
        if (standardShader == null)
        {
            standardShader = Shader.Find("Standard");
        }
        if (standardShader == null)
        {
            standardShader = Shader.Find("Mobile/Diffuse");
        }
        
        Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (particleShader == null)
        {
            particleShader = Shader.Find("Mobile/Particles/Additive");
        }
        if (particleShader == null)
        {
            particleShader = Shader.Find("Particles/Standard Unlit");
        }

        // Verificar que tengamos al menos un shader válido
        if (standardShader == null || particleShader == null)
        {
            Debug.LogError("No se encontraron shaders compatibles. Por favor verifica que tienes instalado Universal Render Pipeline o los shaders estándar.");
            return;
        }

        // Material del proyectil
        projectileMaterial = new Material(standardShader);
        projectileMaterial.name = "ShotgunProjectileMaterial";
        if (standardShader.name.Contains("Universal"))
        {
            projectileMaterial.SetColor("_BaseColor", new Color(1f, 0.3f, 0.3f, 1f));
            projectileMaterial.SetColor("_EmissionColor", new Color(1f, 0.3f, 0.3f, 1f) * 2f);
        }
        else
        {
            projectileMaterial.SetColor("_Color", new Color(1f, 0.3f, 0.3f, 1f));
            projectileMaterial.EnableKeyword("_EMISSION");
            projectileMaterial.SetColor("_EmissionColor", new Color(1f, 0.3f, 0.3f, 1f) * 2f);
        }
        AssetDatabase.CreateAsset(projectileMaterial, $"{materialsDirectory}/ShotgunProjectileMaterial.mat");

        // Material del muzzle flash
        muzzleFlashMaterial = new Material(particleShader);
        muzzleFlashMaterial.name = "MuzzleFlashMaterial";
        if (particleShader.name.Contains("Universal"))
        {
            muzzleFlashMaterial.SetColor("_BaseColor", new Color(1f, 0.7f, 0.3f, 0.5f));
        }
        else
        {
            muzzleFlashMaterial.SetColor("_TintColor", new Color(1f, 0.7f, 0.3f, 0.5f));
        }
        AssetDatabase.CreateAsset(muzzleFlashMaterial, $"{materialsDirectory}/MuzzleFlashMaterial.mat");

        // Material del impacto
        impactMaterial = new Material(particleShader);
        impactMaterial.name = "ImpactMaterial";
        if (particleShader.name.Contains("Universal"))
        {
            impactMaterial.SetColor("_BaseColor", new Color(1f, 0.5f, 0.3f, 0.5f));
        }
        else
        {
            impactMaterial.SetColor("_TintColor", new Color(1f, 0.5f, 0.3f, 0.5f));
        }
        AssetDatabase.CreateAsset(impactMaterial, $"{materialsDirectory}/ImpactMaterial.mat");
    }

    private void CreateProjectilePrefab()
    {
        // Crear el objeto base del proyectil
        GameObject projectile = new GameObject("ShotgunProjectile");
        
        // Añadir componentes básicos
        projectile.AddComponent<SphereCollider>().isTrigger = true;
        projectile.AddComponent<Rigidbody>().useGravity = false;
        
        // Crear la esfera visual
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "Visual";
        visual.transform.SetParent(projectile.transform);
        visual.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
        DestroyImmediate(visual.GetComponent<Collider>()); // Eliminar el collider del visual
        
        // Asignar material
        visual.GetComponent<Renderer>().material = projectileMaterial;
        
        // Añadir luz
        Light light = projectile.AddComponent<Light>();
        light.color = new Color(1f, 0.3f, 0.3f);
        light.intensity = 2f;
        light.range = 3f;
        
        // Añadir sistema de partículas para la estela
        var trail = projectile.AddComponent<TrailRenderer>();
        trail.material = projectileMaterial;
        trail.time = 0.2f;
        trail.startWidth = 0.1f;
        trail.endWidth = 0f;
        
        // Añadir sistema de partículas principal
        var particles = projectile.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.startLifetime = 0.5f;
        main.startSpeed = 0.5f;
        main.startSize = 0.1f;
        main.startColor = new Color(1f, 0.3f, 0.3f, 1f);
        
        var emission = particles.emission;
        emission.rateOverTime = 20f;
        
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.1f;
        
        // Asegurar que existe el directorio
        string prefabDirectory = "Assets/Resources/Prefabs/Abilities";
        if (!Directory.Exists(prefabDirectory))
        {
            string[] folders = prefabDirectory.Split('/');
            string currentPath = folders[0];
            for (int i = 1; i < folders.Length; i++)
            {
                string parentFolder = currentPath;
                currentPath = Path.Combine(currentPath, folders[i]);
                if (!Directory.Exists(currentPath))
                {
                    AssetDatabase.CreateFolder(parentFolder, folders[i]);
                }
            }
        }
        
        // Guardar prefab
        string prefabPath = $"{prefabDirectory}/ShotgunProjectile.prefab";
        PrefabUtility.SaveAsPrefabAsset(projectile, prefabPath);
        DestroyImmediate(projectile);
    }

    private void CreateMuzzleFlashPrefab()
    {
        GameObject muzzleFlash = new GameObject("ShotgunMuzzleFlash");
        
        // Sistema de partículas principal
        var particles = muzzleFlash.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.duration = 0.2f;
        main.loop = false;
        main.startLifetime = 0.2f;
        main.startSpeed = 2f;
        main.startSize = 0.3f;
        main.startColor = new Color(1f, 0.7f, 0.3f, 1f);
        
        var emission = particles.emission;
        emission.rateOverTime = 0;
        
        // Configurar burst correctamente
        ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[1];
        bursts[0] = new ParticleSystem.Burst(0.0f, 30);
        emission.SetBursts(bursts);
        
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 15f;
        
        // Configurar el renderer de partículas
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.material = muzzleFlashMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        
        // Añadir luz
        Light light = muzzleFlash.AddComponent<Light>();
        light.color = new Color(1f, 0.7f, 0.3f);
        light.intensity = 3f;
        light.range = 5f;
        
        // Guardar prefab
        string prefabPath = "Assets/Resources/Prefabs/Abilities/ShotgunMuzzleFlash.prefab";
        PrefabUtility.SaveAsPrefabAsset(muzzleFlash, prefabPath);
        DestroyImmediate(muzzleFlash);
    }

    private void CreateImpactPrefab()
    {
        GameObject impact = new GameObject("ShotgunImpact");
        
        // Sistema de partículas principal
        var particles = impact.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = 0.5f;
        main.startSpeed = 3f;
        main.startSize = 0.2f;
        main.startColor = new Color(1f, 0.5f, 0.3f, 1f);
        
        var emission = particles.emission;
        emission.rateOverTime = 0;
        
        // Configurar burst correctamente
        ParticleSystem.Burst[] bursts = new ParticleSystem.Burst[1];
        bursts[0] = new ParticleSystem.Burst(0.0f, 20);
        emission.SetBursts(bursts);
        
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.1f;
        
        // Configurar el renderer de partículas
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.material = impactMaterial;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        
        // Añadir luz
        Light light = impact.AddComponent<Light>();
        light.color = new Color(1f, 0.5f, 0.3f);
        light.intensity = 2f;
        light.range = 3f;
        
        // Guardar prefab
        string prefabPath = "Assets/Resources/Prefabs/Abilities/ShotgunImpact.prefab";
        PrefabUtility.SaveAsPrefabAsset(impact, prefabPath);
        DestroyImmediate(impact);
    }
} 