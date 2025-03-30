using UnityEngine;
using TMPro;

namespace Photon.Pun.Demo.Asteroids
{
    /// <summary>
    /// Crea un texto flotante en tiempo de ejecución sin necesidad de prefab
    /// </summary>
    public class FloatingTextCreator : MonoBehaviour
    {
        private static FloatingTextCreator instance;
        
        /// <summary>
        /// Singleton para acceder al creador de textos flotantes
        /// </summary>
        public static FloatingTextCreator Instance
        {
            get
            {
                if (instance == null)
                {
                    // Crear un objeto para el singleton
                    GameObject go = new GameObject("FloatingTextCreator");
                    instance = go.AddComponent<FloatingTextCreator>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// Crea un texto flotante en la posición especificada
        /// </summary>
        /// <param name="position">Posición del texto</param>
        /// <param name="text">Texto a mostrar</param>
        /// <param name="color">Color del texto</param>
        /// <param name="fontSize">Tamaño de la fuente</param>
        /// <param name="duration">Duración de la animación</param>
        /// <returns>El GameObject creado</returns>
        public GameObject CreateFloatingText(Vector3 position, string text, Color color, float fontSize = 3f, float duration = 2f)
        {
            // Crear un GameObject para el texto flotante
            GameObject textObj = new GameObject("FloatingText");
            textObj.transform.position = position;
            
            // Añadir un componente TextMeshPro
            TextMeshPro textComponent = textObj.AddComponent<TextMeshPro>();
            textComponent.text = text;
            textComponent.fontSize = fontSize;
            textComponent.fontStyle = FontStyles.Bold;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.color = color;
            
            // Hacer que el texto sea visible desde ambos lados
            textComponent.enableCulling = false;
            
            // Añadir efectos visuales para que destaque
            textComponent.outlineWidth = 0.25f;
            textComponent.outlineColor = new Color(0.2f, 0.2f, 0.0f, 1f);
            
            // Añadir sombra para mejor legibilidad
            if (textComponent.fontSharedMaterial != null)
            {
                // Intenta añadir efecto de brillo si el material lo soporta
                try
                {
                    textComponent.fontSharedMaterial.EnableKeyword("GLOW_ON");
                    textComponent.fontSharedMaterial.SetFloat("_GlowPower", 0.5f);
                    textComponent.fontSharedMaterial.SetColor("_GlowColor", new Color(1f, 0.9f, 0f, 0.8f));
                }
                catch (System.Exception)
                {
                    // Ignorar si el material no soporta este efecto
                }
            }
            
            // Agregar componente de billboard para que siempre mire a la cámara
            Billboard billboard = textObj.AddComponent<Billboard>();
            
            // Destruir después de la duración especificada
            Destroy(textObj, duration + 0.5f);
            
            return textObj;
        }
    }
} 