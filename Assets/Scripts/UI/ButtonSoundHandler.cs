using UnityEngine;
using UnityEngine.UI;

namespace Photon.Pun.Demo.Asteroids
{
    public class ButtonSoundHandler : MonoBehaviour
    {
        [Header("Sound Settings")]
        public AudioClip customClickSound; // Sonido personalizado para este botón
        public float customVolume = 0.5f; // Volumen personalizado para este botón
        
        private Button button;
        private bool isInitialized = false;
        
        void OnEnable()
        {
            if (!isInitialized)
            {
                InitializeButton();
            }
        }
        
        void Start()
        {
            InitializeButton();
        }
        
        private void InitializeButton()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }
            
            if (button != null && !isInitialized)
            {
                // Remover el listener si ya existe para evitar duplicados
                button.onClick.RemoveListener(PlayClickSound);
                // Agregar el listener
                button.onClick.AddListener(PlayClickSound);
                isInitialized = true;
            }
        }
        
        public void PlayClickSound()
        {
            if (ButtonSoundManager.Instance != null)
            {
                if (customClickSound != null)
                {
                    // Usar el sonido personalizado del botón
                    ButtonSoundManager.Instance.PlayCustomSound(customClickSound, customVolume);
                }
                else
                {
                    // Usar el sonido por defecto del ButtonSoundManager
                    ButtonSoundManager.Instance.PlayClickSound();
                }
            }
        }
    }
} 