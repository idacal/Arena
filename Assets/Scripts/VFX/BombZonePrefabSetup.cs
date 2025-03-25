using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class BombZonePrefabSetup : MonoBehaviour
{
    [Header("Visual Settings")]
    public Color areaColor = new Color(1f, 0.9f, 0.2f, 1f);  // Amarillo por defecto
    public float areaRadius = 5f;
    public Material areaMaterial;
    
    [Header("Bomb Settings")]
    public GameObject singleBombPrefab;  // Prefab para una bomba individual
    public int bombCount = 8;
    public float bombScale = 0.3f;
    public Color bombColor = Color.yellow;
    public Material bombMaterial;
    
    private PulsingAOEVisualEffect areaEffect;
    private GameObject[] bombVisuals;
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
            SetupBombVisuals();
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
    
    public void SetupBombVisuals()
    {
        if (isSettingUp) return;
        
        // Limpiar bombas existentes
        Transform bombsContainer = transform.Find("BombsContainer");
        if (bombsContainer != null)
        {
            DestroyImmediate(bombsContainer.gameObject);
        }
        
        // Crear nuevo contenedor
        GameObject containerObj = new GameObject("BombsContainer");
        bombsContainer = containerObj.transform;
        bombsContainer.SetParent(transform);
        bombsContainer.localPosition = Vector3.zero;
        
        bombVisuals = new GameObject[bombCount];
        float bombAreaRadius = areaRadius * 0.9f;
        
        #if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Random.InitState((int)System.DateTime.Now.Ticks);
        }
        #endif
        
        for (int i = 0; i < bombCount; i++)
        {
            GameObject bomb;
            if (singleBombPrefab != null)
            {
                bomb = Instantiate(singleBombPrefab);
            }
            else
            {
                bomb = CreateBasicBomb();
            }
            
            // Posicionar la bomba aleatoriamente dentro del área
            Vector2 randomPoint = Random.insideUnitCircle * bombAreaRadius;
            bomb.transform.SetParent(bombsContainer);
            bomb.transform.localPosition = new Vector3(randomPoint.x, 0.1f, randomPoint.y);
            bomb.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            bomb.transform.localScale = Vector3.one * bombScale;
            
            // Aplicar material
            var renderer = bomb.GetComponent<Renderer>();
            if (renderer != null && bombMaterial != null)
            {
                renderer.sharedMaterial = bombMaterial;
            }
            
            bombVisuals[i] = bomb;
            
            #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(bomb);
            }
            #endif
        }
        
        #if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorUtility.SetDirty(containerObj);
        }
        #endif
    }
    
    GameObject CreateBasicBomb()
    {
        GameObject bomb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bomb.name = "BombVisual";
        
        // Eliminar collider en modo editor
        #if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            DestroyImmediate(bomb.GetComponent<Collider>());
        }
        #endif
        
        return bomb;
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
                        SetupBombVisuals();
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