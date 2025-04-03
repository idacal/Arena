using UnityEngine;

public class PulsingAOEVisualEffect : AOEVisualEffect
{
    [Header("Pulse Settings")]
    public float pulseSpeed = 2f;
    public float pulseMinAlpha = 0.2f;
    public float pulseMaxAlpha = 0.8f;

    private float pulseTime;
    private Color originalColor;  // Para guardar el color original y conservar RGB
    private bool colorInitialized = false;

    protected override void OnEnable()
    {
        base.OnEnable();
        pulseTime = 0f;
        
        // Capturar el color original solo una vez para preservar RGB
        if (!colorInitialized && materialInstance != null)
        {
            originalColor = materialInstance.color;
            colorInitialized = true;
        }
    }

    private void Update()
    {
        if (!initialized || materialInstance == null) return;

        // Actualizar el tiempo del pulso
        pulseTime += Time.deltaTime * pulseSpeed;

        // Calcular el alpha usando una función sinusoidal
        float alpha = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, (Mathf.Sin(pulseTime) + 1f) * 0.5f);

        // Actualizar SOLO el alfa, manteniendo el color RGB original
        Color newColor = colorInitialized ? originalColor : areaColor;
        newColor.a = alpha;
        materialInstance.color = newColor;
    }

    public override void UpdateVisuals()
    {
        base.updateColorOnRefresh = false;  // Desactivar actualización de color en la clase base
        base.UpdateVisuals();
        
        // Capturar el color original después de la primera inicialización
        if (!colorInitialized && materialInstance != null)
        {
            originalColor = materialInstance.color;
            colorInitialized = true;
        }
        
        // Asegurarse de que el material tenga la configuración correcta para transparencia
        if (materialInstance != null)
        {
            // Mantener el color RGB, actualizar solo alfa
            Color currentColor = materialInstance.color;
            currentColor.a = pulseMaxAlpha;
            materialInstance.color = currentColor;
        }
    }
} 