using UnityEngine;
using Photon.Pun.Demo.Asteroids;

/// <summary>
/// Script para probar los efectos de oro en el editor o en juego
/// </summary>
public class GoldEffectTester : MonoBehaviour
{
    [Header("Configuración de prueba")]
    public float goldAmount = 50f;
    public string sourceText = "¡Oro ganado!";
    public Vector3 offset = new Vector3(0, 1, 0);
    public KeyCode testKey = KeyCode.G;
    
    [Header("Referencias")]
    public HeroUIController uiController;
    
    void Start()
    {
        // Buscar el UI Controller si no está asignado
        if (uiController == null)
        {
            HeroBase hero = FindObjectOfType<HeroBase>();
            if (hero != null)
            {
                uiController = hero.GetComponent<HeroUIController>();
                if (uiController == null)
                {
                    Debug.LogWarning("No se encontró UI Controller en el héroe");
                }
            }
        }
        
        // Crear CoinSpriteCreator si no existe
        if (FindObjectOfType<CoinSpriteCreator>() == null)
        {
            GameObject creator = new GameObject("CoinSpriteCreator");
            creator.AddComponent<CoinSpriteCreator>();
            Debug.Log("CoinSpriteCreator creado automáticamente");
        }
        
        Debug.Log("GoldEffectTester listo. Presiona " + testKey + " para probar el efecto.");
    }
    
    void Update()
    {
        // Probar efecto al presionar la tecla configurada
        if (Input.GetKeyDown(testKey))
        {
            TestGoldEffect();
        }
    }
    
    /// <summary>
    /// Probar el efecto de oro en la posición actual
    /// </summary>
    public void TestGoldEffect()
    {
        if (uiController != null)
        {
            Vector3 position = transform.position + offset;
            Debug.Log($"Probando efecto de oro: {goldAmount} en posición {position}");
            uiController.ShowGoldRewardText(goldAmount, sourceText, position);
        }
        else
        {
            Debug.LogError("No hay UI Controller asignado para probar");
            
            // Intentar encontrar uno
            HeroBase hero = FindObjectOfType<HeroBase>();
            if (hero != null && hero.GetComponent<HeroUIController>() != null)
            {
                uiController = hero.GetComponent<HeroUIController>();
                Debug.Log("UI Controller encontrado y asignado automáticamente");
                TestGoldEffect(); // Intentar de nuevo
            }
        }
    }
} 