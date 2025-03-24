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
        }
        
        baseScale = transform.localScale;
        initialScale = baseScale.x;
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
            spriteRenderer.color = color;
        }
    }
} 