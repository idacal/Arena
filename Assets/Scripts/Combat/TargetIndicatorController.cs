using UnityEngine;

/// <summary>
/// Controla la visualización y animación del indicador de objetivo
/// </summary>
public class TargetIndicatorController : MonoBehaviour
{
    [Header("Animación")]
    public float rotationSpeed = 50f;
    public float pulseSpeed = 2f;
    public float minScale = 0.9f;
    public float maxScale = 1.1f;
    
    [Header("Color")]
    public Color defaultColor = Color.white;
    
    private SpriteRenderer spriteRenderer;
    private float initialScale;
    private Vector3 baseScale;
    
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("TargetIndicatorController requiere un SpriteRenderer");
            enabled = false;
            return;
        }
        
        // Configurar el material para que multiplique el color
        Material material = new Material(spriteRenderer.material);
        material.SetFloat("_Mode", 0); // Opaque
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 1);
        material.DisableKeyword("_ALPHATEST_ON");
        material.DisableKeyword("_ALPHABLEND_ON");
        material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        spriteRenderer.material = material;
        
        baseScale = transform.localScale;
        initialScale = baseScale.x;
        
        // Establecer color inicial
        spriteRenderer.color = defaultColor;
    }
    
    void Update()
    {
        // Rotar el indicador
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        
        // Efecto de pulso
        float pulse = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(Time.time * pulseSpeed) + 1) * 0.5f);
        transform.localScale = new Vector3(
            baseScale.x * pulse,
            baseScale.y * pulse, 
            baseScale.z
        );
    }
    
    /// <summary>
    /// Establece el color del indicador basado en si el objetivo es aliado o enemigo
    /// </summary>
    /// <param name="isAlly">True si el objetivo es aliado, False si es enemigo</param>
    public void SetTargetType(bool isAlly)
    {
        if (spriteRenderer != null)
        {
            // Verde para aliados, rojo para enemigos
            spriteRenderer.color = isAlly ? Color.green : Color.red;
        }
    }
    
    /// <summary>
    /// Establece un color personalizado para el indicador
    /// </summary>
    /// <param name="color">Color a aplicar</param>
    public void SetColor(Color color)
    {
        if (spriteRenderer != null)
        {
            // Asegurar que el color mantiene su opacidad
            Color newColor = color;
            newColor.a = spriteRenderer.color.a;
            spriteRenderer.color = newColor;
        }
    }
} 