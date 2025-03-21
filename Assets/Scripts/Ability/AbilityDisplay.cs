using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Photon.Pun.Demo.Asteroids
{
    public class AbilityDisplay : MonoBehaviour
    {
        [Header("UI References")]
        public TMP_Text AbilityNameText;
        public TMP_Text HotkeyText;
        public Image AbilityIconImage;
        public TMP_Text AbilityDescriptionText;
        public TMP_Text CooldownText;
        public TMP_Text ManaCostText;
        public TMP_Text AbilityTypeText;
        
        // Elementos opcionales según tu diseño
        public GameObject AdditionalStatsContainer;
        public TMP_Text DamageText;
        public TMP_Text DurationText;
        public TMP_Text RangeText;
        
        /// <summary>
        /// Inicializa el display con los datos de una habilidad
        /// </summary>
        public void Initialize(HeroAbility ability)
        {
            // Información básica
            AbilityNameText.text = ability.Name;
            HotkeyText.text = ability.Hotkey;
            
            if (ability.IconSprite != null)
            {
                AbilityIconImage.sprite = ability.IconSprite;
                AbilityIconImage.enabled = true;
            }
            else
            {
                AbilityIconImage.enabled = false;
            }
            
            AbilityDescriptionText.text = ability.Description;
            CooldownText.text = $"{ability.Cooldown}s";
            ManaCostText.text = $"{ability.ManaCost}";
            AbilityTypeText.text = ability.AbilityType;
            
            // Estadísticas adicionales si están disponibles
            if (AdditionalStatsContainer != null)
            {
                bool hasAdditionalStats = false;
                
                if (DamageText != null && ability.DamageAmount > 0)
                {
                    DamageText.text = $"Damage: {ability.DamageAmount}";
                    DamageText.gameObject.SetActive(true);
                    hasAdditionalStats = true;
                }
                else if (DamageText != null)
                {
                    DamageText.gameObject.SetActive(false);
                }
                
                if (DurationText != null && ability.Duration > 0)
                {
                    DurationText.text = $"Duration: {ability.Duration}s";
                    DurationText.gameObject.SetActive(true);
                    hasAdditionalStats = true;
                }
                else if (DurationText != null)
                {
                    DurationText.gameObject.SetActive(false);
                }
                
                if (RangeText != null && ability.Range > 0)
                {
                    RangeText.text = $"Range: {ability.Range}";
                    RangeText.gameObject.SetActive(true);
                    hasAdditionalStats = true;
                }
                else if (RangeText != null)
                {
                    RangeText.gameObject.SetActive(false);
                }
                
                // Mostrar u ocultar el contenedor según si hay estadísticas para mostrar
                AdditionalStatsContainer.SetActive(hasAdditionalStats);
            }
        }
    }
}