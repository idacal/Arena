using UnityEngine;
using TMPro;

namespace Photon.Pun.Demo.Asteroids
{
    /// <summary>
    /// Configura elementos del texto flotante, utilizar en prefabs de texto flotante
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public class FloatingTextSetup : MonoBehaviour
    {
        [Tooltip("La duración del texto flotante en segundos")]
        public float lifetime = 2.0f;
        
        // Para asegurarse de que el texto esté vacío al inicio
        private void Awake()
        {
            // Limpiar cualquier texto por defecto en el prefab
            TMP_Text textComponent = GetComponent<TMP_Text>();
            if (textComponent != null)
            {
                textComponent.text = "";
                
                // Agregar billboard automáticamente si no existe
                if (GetComponent<Billboard>() == null)
                {
                    gameObject.AddComponent<Billboard>();
                }
            }
            
            // Destruir después de la duración para garantizar limpieza
            Destroy(gameObject, 5f);
        }
        
        // Esto es solo para depuración y visualización en el editor
        private void OnValidate()
        {
            TMP_Text textComponent = GetComponent<TMP_Text>();
            if (textComponent != null)
            {
                // Establecer texto para visualización en el editor
                if (Application.isEditor && !Application.isPlaying)
                {
                    textComponent.text = "TextoEjemplo";
                }
            }
        }
    }
} 