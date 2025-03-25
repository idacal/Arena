using UnityEngine;

[ExecuteInEditMode]
public class AOEVisualEffect : MonoBehaviour
{
    [Header("Visual Settings")]
    public Color areaColor = Color.yellow;
    public float radius = 5f;
    public float heightOffset = 0.05f;
    public Material customMaterial;

    protected MeshRenderer meshRenderer;
    protected MeshFilter meshFilter;
    protected Material materialInstance;
    protected bool initialized = false;

    protected virtual void OnEnable()
    {
        if (!initialized)
        {
            Initialize();
        }
        UpdateVisuals();
    }

    protected virtual void Initialize()
    {
        // Crear o obtener componentes necesarios
        meshRenderer = gameObject.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();

        meshFilter = gameObject.GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();

        // Crear la malla del círculo
        CreateCircleMesh();

        // Configurar el material
        SetupMaterial();

        initialized = true;
    }

    protected virtual void SetupMaterial()
    {
        if (customMaterial != null)
        {
            materialInstance = new Material(customMaterial);
        }
        else
        {
            // Crear un material básico si no hay uno personalizado
            materialInstance = new Material(Shader.Find("Legacy Shaders/Transparent/Diffuse"));
            if (materialInstance != null)
            {
                materialInstance.SetFloat("_Mode", 2); // Fade mode
                materialInstance.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                materialInstance.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                materialInstance.SetInt("_ZWrite", 0);
                materialInstance.DisableKeyword("_ALPHATEST_ON");
                materialInstance.EnableKeyword("_ALPHABLEND_ON");
                materialInstance.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                materialInstance.renderQueue = 3000;
            }
        }

        if (materialInstance != null)
        {
            materialInstance.color = areaColor;
            meshRenderer.material = materialInstance;
        }
    }

    protected virtual void CreateCircleMesh()
    {
        Mesh mesh = new Mesh();
        
        int segments = 60;
        Vector3[] vertices = new Vector3[segments + 2];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.up * heightOffset; // Centro
        
        float angleStep = 360f / segments;
        for (int i = 0; i < segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            vertices[i + 1] = new Vector3(x, heightOffset, z);
            
            if (i < segments - 1)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
            else
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = 1;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;
    }

    public virtual void UpdateVisuals()
    {
        if (!initialized)
            Initialize();

        if (materialInstance != null)
        {
            materialInstance.color = areaColor;
        }

        CreateCircleMesh(); // Actualizar el tamaño si el radio ha cambiado
    }

    protected virtual void OnValidate()
    {
        if (Application.isPlaying)
            UpdateVisuals();
    }

    protected virtual void OnDestroy()
    {
        if (materialInstance != null)
            Destroy(materialInstance);
    }
} 