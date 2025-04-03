using UnityEngine;
using UnityEditor;
using Photon.Pun;
using System.Collections.Generic;
using Photon.Pun.Demo.Asteroids;  // Namespace correcto para las habilidades
using System.IO;

public class AbilityPrefabCreator : EditorWindow
{
    private const string MATERIALS_PATH = "Assets/Resources/Materials/Abilities";

    [MenuItem("Tools/Ability Prefabs/Create Prefabs Window")]
    public static void ShowWindow()
    {
        GetWindow<AbilityPrefabCreator>("Ability Prefab Creator");
    }
    
    void OnGUI()
    {
        GUILayout.Label("Ability Creator", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Create BombZone in Scene"))
        {
            CreateBombZoneInScene();
        }
        
        if (GUILayout.Button("Create Scarecrow in Scene"))
        {
    
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("1. Crea el objeto en la escena\n2. Ajusta los colores y materiales\n3. Cuando esté listo, arrastra el objeto a la carpeta Prefabs", MessageType.Info);
    }
    
    private Material CreateAndSaveMaterial(string name, Color color, bool isTransparent = false)
    {
        // Asegurar que existe el directorio
        if (!Directory.Exists(MATERIALS_PATH))
        {
            Directory.CreateDirectory(MATERIALS_PATH);
        }

        string materialPath = $"{MATERIALS_PATH}/{name}.mat";
        
        // Buscar material existente
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        
        if (material == null)
        {
            // Intentar encontrar un shader válido
            Shader shader = null;
            string[] shaderNames = new string[] 
            {
                "Legacy Shaders/Transparent/Diffuse",
                "Legacy Shaders/Diffuse",
                "Sprites/Default",
                "Mobile/Diffuse",
                "Standard"
            };
            
            foreach (string shaderName in shaderNames)
            {
                shader = Shader.Find(shaderName);
                if (shader != null)
                {
                    Debug.Log($"Usando shader: {shaderName}");
                    break;
                }
            }
            
            if (shader == null)
            {
                Debug.LogError("No se pudo encontrar ningún shader válido. Usando Shader por defecto.");
                return new Material(Shader.Find("Default-Diffuse"));
            }
            
            // Crear nuevo material
            material = new Material(shader);
            
            if (isTransparent)
            {
                material.SetFloat("_Mode", 2); // Fade mode
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0);
                material.DisableKeyword("_ALPHATEST_ON");
                material.EnableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = 3000;
            }
            
            material.color = color;
            
            // Guardar el material como asset
            AssetDatabase.CreateAsset(material, materialPath);
            AssetDatabase.SaveAssets();
        }
        else
        {
            // Actualizar el material existente
            material.color = color;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
        }
        
        return material;
    }
    
    void CreateBombZoneInScene()
    {
        // Crear materiales
        Material areaMaterial = CreateAndSaveMaterial("BombZone_Area", new Color(1f, 1f, 0f, 0.5f), true);
        Material bombMaterial = CreateAndSaveMaterial("BombZone_Bomb", new Color(1f, 0.92f, 0.016f, 1f));
        
        // Crear el objeto principal
        GameObject bombZone = new GameObject("BombZone");
        Undo.RegisterCreatedObjectUndo(bombZone, "Create BombZone");
        
        // Añadir componentes necesarios
        var setup = Undo.AddComponent<BombZonePrefabSetup>(bombZone);
        var ability = Undo.AddComponent<BombZoneAbility>(bombZone);
        var photonView = Undo.AddComponent<PhotonView>(bombZone);
        
        // Configurar PhotonView
        photonView.ObservedComponents = new List<Component> { ability };
        photonView.ViewID = 0;
        
        // Configurar BombZoneAbility
        ability.radius = 5f;
        ability.bombCount = 8;
        ability.bombDamage = 50f;
        ability.bombLifetime = 5f;
        ability.bombDetonationRadius = 1f;
        ability.areaColor = areaMaterial.color;
        
        // Configurar BombZonePrefabSetup
        setup.areaRadius = ability.radius;
        setup.bombCount = ability.bombCount;
        setup.areaColor = ability.areaColor;
        setup.bombScale = 0.3f;
        setup.bombColor = bombMaterial.color;
        setup.areaMaterial = areaMaterial;
        setup.bombMaterial = bombMaterial;
        
        // Posicionar en el centro de la escena
        bombZone.transform.position = Vector3.zero;
        
        // Seleccionar el objeto creado
        Selection.activeGameObject = bombZone;
        
        // Forzar la configuración inicial
        EditorApplication.delayCall += () =>
        {
            if (setup != null)
            {
                Undo.RecordObject(setup, "Setup BombZone");
                setup.SetupAreaEffect();
                setup.SetupBombVisuals();
            }
        };
    }
    
    
} 