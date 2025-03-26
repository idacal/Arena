using UnityEngine;
using UnityEngine.UI;

namespace Photon.Pun.Demo.Asteroids
{
    public class ButtonSoundHandler : MonoBehaviour
    {
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
                ButtonSoundManager.Instance.PlayClickSound();
            }
        }
    }
} 