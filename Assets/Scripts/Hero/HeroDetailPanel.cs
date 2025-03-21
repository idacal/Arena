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
        
        [Header("Stats References")]
        public TMP_Text HealthText;
        public TMP_Text ManaText;
        public TMP_Text AttackDamageText;
        public TMP_Text AttackSpeedText;
        public TMP_Text MovementSpeedText;
        public TMP_Text ArmorText;
        public TMP_Text MagicResistanceText;
        
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
                HidePanel();
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
            
            HeroTypeText.text = heroData.HeroType;
            
            // Actualizar estadísticas
            HealthText.text = $"Vida: {heroData.Health}";
            ManaText.text = $"Maná: {heroData.Mana}";
            AttackDamageText.text = $"Daño: {heroData.AttackDamage}";
            AttackSpeedText.text = $"Vel. Ataque: {heroData.AttackSpeed}";
            MovementSpeedText.text = $"Velocidad: {heroData.MovementSpeed}";
            ArmorText.text = $"Armadura: {heroData.Armor}";
            MagicResistanceText.text = $"Res. Mágica: {heroData.MagicResistance}";
            
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