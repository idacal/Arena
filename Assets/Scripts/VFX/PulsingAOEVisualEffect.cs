using UnityEngine;

public class PulsingAOEVisualEffect : AOEVisualEffect
{
    [Header("Pulse Settings")]
    public float pulseSpeed = 2f;
    public float pulseMinAlpha = 0.2f;
    public float pulseMaxAlpha = 0.8f;

    private float pulseTime;

    protected override void OnEnable()
    {
        base.OnEnable();
        pulseTime = 0f;
    }

    private void Update()
    {
        if (!initialized || materialInstance == null) return;

        // Actualizar el tiempo del pulso
        pulseTime += Time.deltaTime * pulseSpeed;

        // Calcular el alpha usando una función sinusoidal
        float alpha = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, (Mathf.Sin(pulseTime) + 1f) * 0.5f);

        // Actualizar el color del material con el nuevo alpha
        Color newColor = areaColor;
        newColor.a = alpha;
        materialInstance.color = newColor;
    }

    public override void UpdateVisuals()
    {
        base.UpdateVisuals();
        
        // Asegurarse de que el material tenga la configuración correcta para transparencia
        if (materialInstance != null)
        {
            Color currentColor = materialInstance.color;
            currentColor.a = pulseMaxAlpha;
            materialInstance.color = currentColor;
        }
    }
} 