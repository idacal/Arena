using UnityEngine;
using UnityEditor;
using TMPro;

namespace Photon.Pun.Demo.Asteroids
{
#if UNITY_EDITOR
    /// <summary>
    /// Clase de editor para crear prefabs de texto flotante
    /// </summary>
    public class FloatingTextPrefabCreator
    {
        [MenuItem("Tools/Create/Floating Text Prefab")]
        public static void CreateFloatingTextPrefab()
        {
            // Crear un GameObject para el texto flotante
            GameObject floatingTextObj = new GameObject("FloatingText");
            
            // Añadir un componente TextMeshPro
            TextMeshPro textComponent = floatingTextObj.AddComponent<TextMeshPro>();
            
            // Configurar el texto
            textComponent.text = "";
            textComponent.fontSize = 3;
            textComponent.fontStyle = FontStyles.Bold;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.color = Color.yellow;
            
            // Hacer que el texto sea visible desde ambos lados
            textComponent.enableCulling = false;
            
            // Agregar componente de billboard
            floatingTextObj.AddComponent<Billboard>();
            
            // Agregar componente de setup
            FloatingTextSetup setup = floatingTextObj.AddComponent<FloatingTextSetup>();
            setup.lifetime = 2.0f;
            
            // Crear un directorio para el prefab si no existe
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/UI"))
            {
                AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
            }
            
            // Guardar el prefab
            string prefabPath = "Assets/Prefabs/UI/FloatingText.prefab";
            
            // Crear el prefab
            PrefabUtility.SaveAsPrefabAsset(floatingTextObj, prefabPath);
            
            // Mostrar mensaje de éxito
            Debug.Log($"Prefab de texto flotante creado en: {prefabPath}");
            
            // Seleccionar el prefab en el proyecto
            UnityEngine.Object prefabAsset = AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject));
            Selection.activeObject = prefabAsset;
            
            // Destruir el objeto temporal
            Object.DestroyImmediate(floatingTextObj);
        }
    }
#endif
} 