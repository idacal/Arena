using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace Photon.Pun.Demo.Asteroids
{
    public class HeroDetailPanel : MonoBehaviour
    {
        [Header("Panel References")]
        public GameObject DetailPanel;
        public TMP_Text HeroNameText;
        public Image HeroPortraitImage;  // Ahora usará el AvatarSprite (imagen grande)
        public TMP_Text HeroTypeText;
        
        [Header("Base Stats References")]
        public TMP_Text StrengthText;
        public TMP_Text IntelligenceText;
        public TMP_Text AgilityText;
        public TMP_Text StrengthScalingText;
        public TMP_Text IntelligenceScalingText;
        public TMP_Text AgilityScalingText;
        
        [Header("Derived Stats References")]
        public TMP_Text HealthText;
        public TMP_Text ManaText;
        public TMP_Text AttackDamageText;
        public TMP_Text AttackSpeedText;
        public TMP_Text MovementSpeedText;
        public TMP_Text ArmorText;
        public TMP_Text MagicResistanceText;
        public TMP_Text HealthRegenText;
        public TMP_Text ManaRegenText;
        
        [Header("Abilities Container")]
        public GameObject AbilitiesContainer;
        public GameObject AbilityPrefab;
        
        // Lista de GameObjects de habilidades instanciados para poder limpiarlos
        private List<GameObject> instantiatedAbilities = new List<GameObject>();
        
        // Referencia al héroe actualmente mostrado
        private HeroData currentHero;
        
        private void Awake()
        {
            // Ocultar el panel al inicio
            if (DetailPanel != null)
            {
                DetailPanel.SetActive(false);
            }
        }
        
        /// <summary>
        /// Muestra los detalles de un héroe específico
        /// </summary>
        public void ShowHeroDetails(HeroData heroData)
        {
            if (heroData == null)
            {
                Debug.LogError("[HeroDetailPanel] Se intentó mostrar detalles de un héroe nulo");
                HidePanel();
                return;
            }

            if (DetailPanel == null)
            {
                Debug.LogError("[HeroDetailPanel] DetailPanel no está asignado en el Inspector");
                return;
            }

            if (HeroNameText == null)
            {
                Debug.LogError("[HeroDetailPanel] HeroNameText no está asignado en el Inspector");
                return;
            }

            if (HeroPortraitImage == null)
            {
                Debug.LogError("[HeroDetailPanel] HeroPortraitImage no está asignado en el Inspector");
                return;
            }

            if (HeroTypeText == null)
            {
                Debug.LogError("[HeroDetailPanel] HeroTypeText no está asignado en el Inspector");
                return;
            }

            if (StrengthText == null || IntelligenceText == null || AgilityText == null ||
                StrengthScalingText == null || IntelligenceScalingText == null || AgilityScalingText == null ||
                HealthText == null || ManaText == null || AttackDamageText == null || AttackSpeedText == null ||
                MovementSpeedText == null || ArmorText == null || MagicResistanceText == null ||
                HealthRegenText == null || ManaRegenText == null)
            {
                Debug.LogError("[HeroDetailPanel] Una o más referencias de estadísticas no están asignadas en el Inspector");
                return;
            }

            if (AbilitiesContainer == null)
            {
                Debug.LogError("[HeroDetailPanel] AbilitiesContainer no está asignado en el Inspector");
                return;
            }

            if (AbilityPrefab == null)
            {
                Debug.LogError("[HeroDetailPanel] AbilityPrefab no está asignado en el Inspector");
                return;
            }
            
            currentHero = heroData;
            
            // Actualizar información básica
            HeroNameText.text = heroData.Name;
            
            // Priorizar el avatar para el panel de detalles
            if (heroData.AvatarSprite != null)
            {
                HeroPortraitImage.sprite = heroData.AvatarSprite;
                HeroPortraitImage.enabled = true;
            }
            else if (heroData.IconSprite != null) // Usar icono como fallback si no hay avatar
            {
                HeroPortraitImage.sprite = heroData.IconSprite;
                HeroPortraitImage.enabled = true;
                Debug.LogWarning($"El héroe {heroData.Name} no tiene avatar, usando icono como fallback");
            }
            else
            {
                HeroPortraitImage.enabled = false;
            }
            
            // Mostrar atributo principal y tipo
            string attributeColor = heroData.PrimaryAttribute switch
            {
                "Strength" => "#FF4444",    // Red for strength
                "Intelligence" => "#4444FF", // Blue for intelligence
                "Agility" => "#44FF44",     // Green for agility
                _ => "#FFFFFF"              // White by default
            };
            HeroTypeText.text = $"<color={attributeColor}>{heroData.PrimaryAttribute}</color>";
            
            // Update base stats (only numbers)
            StrengthText.text = $"{Mathf.RoundToInt(heroData.CurrentStrength)}";
            IntelligenceText.text = $"{Mathf.RoundToInt(heroData.CurrentIntelligence)}";
            AgilityText.text = $"{Mathf.RoundToInt(heroData.CurrentAgility)}";
            StrengthScalingText.text = $"+{Mathf.RoundToInt(heroData.StrengthScaling)}";
            IntelligenceScalingText.text = $"+{Mathf.RoundToInt(heroData.IntelligenceScaling)}";
            AgilityScalingText.text = $"+{Mathf.RoundToInt(heroData.AgilityScaling)}";
            
            // Update derived stats
            HealthText.text = $"Health: {heroData.MaxHealth:F0}";
            ManaText.text = $"Mana: {heroData.MaxMana:F0}";
            AttackDamageText.text = $"Damage: {heroData.CurrentAttackDamage:F0}";
            AttackSpeedText.text = $"Attack Speed: {heroData.CurrentAttackSpeed:F2}";
            MovementSpeedText.text = $"Movement: {heroData.MovementSpeed:F0}";
            ArmorText.text = $"Armor: {heroData.CurrentArmor:F1}";
            MagicResistanceText.text = $"Magic Resist: {heroData.CurrentMagicResistance:F1}";
            HealthRegenText.text = $"{heroData.CurrentHealthRegen:F1}";
            ManaRegenText.text = $"{heroData.CurrentManaRegen:F1}";
            
            // Limpiar habilidades anteriores
            ClearAbilities();
            
            // Mostrar las habilidades
            foreach (HeroAbility ability in heroData.Abilities)
            {
                GameObject abilityObj = Instantiate(AbilityPrefab, AbilitiesContainer.transform);
                instantiatedAbilities.Add(abilityObj);
                
                AbilityDisplay abilityDisplay = abilityObj.GetComponent<AbilityDisplay>();
                if (abilityDisplay != null)
                {
                    abilityDisplay.Initialize(ability);
                }
            }
            
            // Mostrar el panel
            DetailPanel.SetActive(true);
        }
        
        /// <summary>
        /// Oculta el panel de detalles
        /// </summary>
        public void HidePanel()
        {
            if (DetailPanel != null)
            {
                DetailPanel.SetActive(false);
            }
            
            currentHero = null;
        }
        
        /// <summary>
        /// Limpia las habilidades instanciadas
        /// </summary>
        private void ClearAbilities()
        {
            foreach (GameObject obj in instantiatedAbilities)
            {
                Destroy(obj);
            }
            
            instantiatedAbilities.Clear();
        }
    }
}