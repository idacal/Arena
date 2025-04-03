using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Photon.Pun.Demo.Asteroids
{
    /// <summary>
    /// Controla la interfaz de usuario para ajustar el volumen de la música de fondo
    /// </summary>
    public class MusicVolumeUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Slider para controlar el volumen")]
        public Slider volumeSlider;
        
        [Tooltip("Botón para silenciar/activar el sonido")]
        public Button muteButton;
        
        [Tooltip("Icono para mostrar cuando el sonido está activado")]
        public GameObject soundOnIcon;
        
        [Tooltip("Icono para mostrar cuando el sonido está silenciado")]
        public GameObject soundOffIcon;
        
        [Tooltip("(Opcional) Texto que muestra el valor de volumen actual")]
        public TMP_Text volumeValueText;
        
        [Header("Settings")]
        [Tooltip("Mostrar el valor como porcentaje (0-100%) en lugar de decimal (0-1)")]
        public bool showAsPercentage = true;
        
        private BackgroundMusicManager musicManager;
        private bool initialized = false;
        
        void Start()
        {
            InitializeUI();
        }
        
        void OnEnable()
        {
            if (!initialized)
            {
                InitializeUI();
            }
            else
            {
                // Actualizar UI al volver a activarse
                UpdateUI();
            }
        }
        
        /// <summary>
        /// Inicializa la interfaz de usuario y conecta con el BackgroundMusicManager
        /// </summary>
        private void InitializeUI()
        {
            // Buscar el BackgroundMusicManager
            musicManager = BackgroundMusicManager.Instance;
            
            if (musicManager == null)
            {
                Debug.LogError("[MusicVolumeUI] No se encontró BackgroundMusicManager");
                return;
            }
            
            // Configurar el slider
            if (volumeSlider != null)
            {
                // Establecer el valor inicial
                volumeSlider.value = musicManager.GetVolume();
                
                // Limpiar listeners para evitar duplicados
                volumeSlider.onValueChanged.RemoveAllListeners();
                
                // Añadir listener para actualizar el volumen
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
                
                // Registrar el slider en el manager
                musicManager.FindAndSetupVolumeSlider(volumeSlider);
            }
            
            // Configurar el botón de silencio
            if (muteButton != null)
            {
                // Limpiar listeners para evitar duplicados
                muteButton.onClick.RemoveAllListeners();
                
                // Añadir listener para silenciar/activar
                muteButton.onClick.AddListener(OnMuteToggle);
            }
            
            // Actualizar la UI basada en el estado actual
            UpdateUI();
            
            initialized = true;
            Debug.Log("[MusicVolumeUI] Interfaz de control de volumen inicializada");
        }
        
        /// <summary>
        /// Actualiza la interfaz basada en el estado actual del volumen
        /// </summary>
        private void UpdateUI()
        {
            if (musicManager == null) return;
            
            // Actualizar slider si existe
            if (volumeSlider != null)
            {
                volumeSlider.value = musicManager.GetVolume();
            }
            
            // Actualizar texto de valor si existe
            UpdateVolumeText();
            
            // Actualizar iconos de silencio si existen
            UpdateMuteIcons();
        }
        
        /// <summary>
        /// Maneja el cambio de valor del slider
        /// </summary>
        private void OnVolumeChanged(float value)
        {
            if (musicManager == null) return;
            
            // Establecer el nuevo volumen
            musicManager.SetVolume(value);
            
            // Actualizar texto e iconos
            UpdateVolumeText();
            UpdateMuteIcons();
        }
        
        /// <summary>
        /// Maneja el click en el botón de silencio
        /// </summary>
        private void OnMuteToggle()
        {
            if (musicManager == null) return;
            
            // Alternar silencio
            musicManager.ToggleMute();
            
            // Actualizar UI
            UpdateUI();
        }
        
        /// <summary>
        /// Actualiza el texto que muestra el valor del volumen
        /// </summary>
        private void UpdateVolumeText()
        {
            if (volumeValueText == null || musicManager == null) return;
            
            float volume = musicManager.GetVolume();
            
            if (showAsPercentage)
            {
                int percentage = Mathf.RoundToInt(volume * 100);
                volumeValueText.text = $"{percentage}%";
            }
            else
            {
                volumeValueText.text = volume.ToString("F2");
            }
        }
        
        /// <summary>
        /// Actualiza los iconos de silencio/sonido
        /// </summary>
        private void UpdateMuteIcons()
        {
            if (soundOnIcon == null || soundOffIcon == null || musicManager == null) return;
            
            bool isMuted = musicManager.GetVolume() <= 0.01f;
            
            soundOnIcon.SetActive(!isMuted);
            soundOffIcon.SetActive(isMuted);
        }
    }
} 