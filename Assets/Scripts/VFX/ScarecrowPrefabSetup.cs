using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class ScarecrowPrefabSetup : MonoBehaviour
{
    [Header("Visual Settings")]
    public Color areaColor = new Color(1f, 0.7f, 0f, 1f);  // Naranja por defecto
    public float areaRadius = 5f;
    public Material areaMaterial;
    
    [Header("Scarecrow Settings")]
    public GameObject customScarecrowPrefab;  // Prefab personalizado del espantapájaros
    public float scarecrowScale = 0.7f;       // 70% del tamaño original
    public Color scarecrowColor = new Color(0.7f, 0.5f, 0.3f); // Color marrón para el espantapájaros
    public Material scarecrowMaterial;
    
    private PulsingAOEVisualEffect areaEffect;
    private GameObject scarecrowVisual;
    private bool isSettingUp = false;
    private bool hasInitialized = false;
    
    void OnEnable()
    {
        #if UNITY_EDITOR
        if (!hasInitialized && !isSettingUp && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            hasInitialized = true;
            isSettingUp = true;
            SetupAreaEffect();
            SetupScarecrowVisual();
            isSettingUp = false;
        }
        #endif
    }
    
    public void SetupAreaEffect()
    {
        if (isSettingUp) return;
        
        // Crear o obtener el efecto de área
        Transform existingArea = transform.Find("AreaEffect");
        GameObject areaObj;
        
        if (existingArea != null)
        {
            areaObj = existingArea.gameObject;
            areaEffect = areaObj.GetComponent<PulsingAOEVisualEffect>();
            if (areaEffect == null)
            {
                areaEffect = areaObj.AddComponent<PulsingAOEVisualEffect>();
            }
        }
        else
        {
            areaObj = new GameObject("AreaEffect");
            areaObj.transform.SetParent(transform);
            areaObj.transform.localPosition = Vector3.zero;
            areaEffect = areaObj.AddComponent<PulsingAOEVisualEffect>();
        }
        
        if (areaEffect != null)
        {
            areaEffect.areaColor = areaColor;
            areaEffect.radius = areaRadius;
            areaEffect.pulseSpeed = 2f;
            areaEffect.pulseMinAlpha = 0.2f;
            areaEffect.pulseMaxAlpha = 0.8f;
            areaEffect.heightOffset = 0.05f;
            areaEffect.customMaterial = areaMaterial;
            
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(areaEffect);
            }
            #endif
        }
    }
    
    private Material CreateSafeMaterial(Color color)
    {
        Material material = new Material(Shader.Find("Legacy Shaders/Diffuse"));
        material.color = color;
        return material;
    }
    
    public void SetupScarecrowVisual()
    {
        if (isSettingUp) return;
        
        // Limpiar espantapájaros existente
        Transform existingScarecrow = transform.Find("ScarecrowVisual");
        if (existingScarecrow != null)
        {
            DestroyImmediate(existingScarecrow.gameObject);
        }
        
        // Crear nuevo espantapájaros
        GameObject scarecrow = new GameObject("ScarecrowVisual");
        scarecrow.transform.SetParent(transform);
        scarecrow.transform.localPosition = Vector3.zero;
        
        // Cuerpo
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "Body";
        body.transform.SetParent(scarecrow.transform);
        body.transform.localPosition = new Vector3(0, 1 * scarecrowScale, 0);
        body.transform.localScale = new Vector3(0.5f * scarecrowScale, 2 * scarecrowScale, 0.5f * scarecrowScale);
        
        // Brazos
        GameObject arms = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arms.name = "Arms";
        arms.transform.SetParent(scarecrow.transform);
        arms.transform.localPosition = new Vector3(0, 1.5f * scarecrowScale, 0);
        arms.transform.localScale = new Vector3(2 * scarecrowScale, 0.2f * scarecrowScale, 0.2f * scarecrowScale);
        
        // Cabeza
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(scarecrow.transform);
        head.transform.localPosition = new Vector3(0, 2.5f * scarecrowScale, 0);
        head.transform.localScale = new Vector3(0.7f * scarecrowScale, 0.7f * scarecrowScale, 0.7f * scarecrowScale);
        
        // Aplicar materiales
        if (scarecrowMaterial != null)
        {
            if (body.TryGetComponent<Renderer>(out var bodyRenderer))
                bodyRenderer.sharedMaterial = scarecrowMaterial;
            if (arms.TryGetComponent<Renderer>(out var armsRenderer))
                armsRenderer.sharedMaterial = scarecrowMaterial;
            if (head.TryGetComponent<Renderer>(out var headRenderer))
                headRenderer.sharedMaterial = scarecrowMaterial;
        }
        
        // Eliminar colliders en modo editor
        if (!Application.isPlaying)
        {
            DestroyImmediate(body.GetComponent<Collider>());
            DestroyImmediate(arms.GetComponent<Collider>());
            DestroyImmediate(head.GetComponent<Collider>());
        }
        
        #if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(scarecrow);
            EditorUtility.SetDirty(body);
            EditorUtility.SetDirty(arms);
            EditorUtility.SetDirty(head);
        }
        #endif
    }
    
    void OnValidate()
    {
        #if UNITY_EDITOR
        if (!isSettingUp && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            // Solo actualizar si los valores han cambiado y no estamos en el proceso de crear el prefab
            if (!PrefabUtility.IsPartOfPrefabAsset(gameObject))
            {
                EditorApplication.delayCall += () =>
                {
                    if (this != null && !isSettingUp)
                    {
                        isSettingUp = true;
                        SetupAreaEffect();
                        SetupScarecrowVisual();
                        isSettingUp = false;
                        
                        if (gameObject != null)
                        {
                            EditorUtility.SetDirty(gameObject);
                        }
                    }
                };
            }
        }
        #endif
    }
} 