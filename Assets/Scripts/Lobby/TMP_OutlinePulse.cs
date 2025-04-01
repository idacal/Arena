using UnityEngine;
using UnityEngine.UI; // Necesario para Text y Outline

[RequireComponent(typeof(Text))]     // Ahora requiere el componente Text estándar
[RequireComponent(typeof(Outline))] // Y también requiere el componente Outline que añadiste
public class TextOutlinePulse : MonoBehaviour // Nombre de clase cambiado
{
    [Tooltip("El valor mínimo del alfa (transparencia) del outline (0 = invisible, 1 = totalmente opaco)")]
    [Range(0f, 1f)]
    public float minAlpha = 0.3f;

    [Tooltip("El valor máximo del alfa (transparencia) del outline (0 = invisible, 1 = totalmente opaco)")]
    [Range(0f, 1f)]
    public float maxAlpha = 1.0f;

    [Tooltip("La velocidad a la que pulsa el brillo (valores más altos = más rápido)")]
    public float pulseSpeed = 1.5f;

    // No necesitamos la referencia al Text directamente, pero sí al Outline
    private Outline outlineComponent;
    private Color initialEffectColor;

    void Awake()
    {
        outlineComponent = GetComponent<Outline>();
        // Guardar el color original configurado en el Inspector para el Outline
        initialEffectColor = outlineComponent.effectColor;
    }

    void Update()
    {
        // Calcular el nuevo alfa usando una onda sinusoidal para un pulso suave
        float sinWave = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f; // Resultado entre 0 y 1

        // Interpolar entre minAlpha y maxAlpha basado en la onda sinusoidal
        float currentAlpha = Mathf.Lerp(minAlpha, maxAlpha, sinWave);

        // Crear el nuevo color con el alfa calculado, manteniendo los colores RGB originales del Effect Color
        Color newEffectColor = new Color(initialEffectColor.r, initialEffectColor.g, initialEffectColor.b, currentAlpha);

        // Aplicar el nuevo color al Effect Color del componente Outline
        outlineComponent.effectColor = newEffectColor;
    }

    // Opcional: Restablecer el color original si el objeto se deshabilita
    void OnDisable()
    {
        if (outlineComponent != null)
        {
             outlineComponent.effectColor = initialEffectColor;
        }
    }

     // Opcional: Asegurarse de volver a aplicar el pulso si se reactiva
    void OnEnable()
    {
         if (outlineComponent != null)
         {
            Update(); // Forzar actualización
         }
    }
}
