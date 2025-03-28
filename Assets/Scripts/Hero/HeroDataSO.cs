using UnityEngine;
using System.Collections.Generic;

namespace Photon.Pun.Demo.Asteroids
{
    [CreateAssetMenu(fileName = "New Hero", menuName = "MOBA/Hero Data")]
    public class HeroDataSO : ScriptableObject
    {
        [Header("Información Básica")]
        public int Id;
        public string Name;
        [TextArea(3, 5)]
        public string Description;
        
        [Header("Imágenes")]
        [Tooltip("Icono cuadrado y pequeño (ej: 128x128) para mostrar en la cuadrícula de selección")]
        public Sprite IconSprite;
        [Tooltip("Imagen más grande y detallada (ej: 512x512) para el panel de detalles")]
        public Sprite AvatarSprite;
        
        public string PrefabName;

        [Header("Estadísticas Base")]
        public float BaseStrength = 20f;      // Fuerza base
        public float BaseIntelligence = 20f;  // Inteligencia base
        public float BaseAgility = 20f;       // Agilidad base
        public string PrimaryAttribute = "Strength"; // Atributo principal (Strength, Intelligence, Agility)
        
        [Header("Estadísticas Derivadas")]
        public float HealthPerStrength = 20f;     // Vida por punto de fuerza
        public float ManaPerIntelligence = 10f;   // Maná por punto de inteligencia
        public float ArmorPerAgility = 0.5f;      // Armadura por punto de agilidad
        public float AttackDamagePerStrength = 1f;    // Daño de ataque por punto de fuerza
        public float AttackDamagePerIntelligence = 1f; // Daño de ataque por punto de inteligencia
        public float AttackDamagePerAgility = 1f;      // Daño de ataque por punto de agilidad
        public float AttackSpeedPerAgility = 0.01f;    // Velocidad de ataque por punto de agilidad
        public float MagicResistancePerIntelligence = 0.5f; // Resistencia mágica por punto de inteligencia
        public float HealthRegenPerStrength = 0.1f;    // Regeneración de vida por punto de fuerza
        public float ManaRegenPerIntelligence = 0.05f; // Regeneración de maná por punto de inteligencia
        public float MovementSpeed = 350f;             // Velocidad de movimiento base
        public float RespawnTime = 5.0f;               // Tiempo de respawn

        [Header("Sistema de Niveles")]
        public int MaxLevel = 18;
        public float BaseExperience = 100f;
        public float ExperienceScaling = 1.5f;
        public int SkillPointsPerLevel = 1;
        
        [Header("Escalado de Atributos por Nivel")]
        public float StrengthScaling = 2f;        // Fuerza por nivel
        public float IntelligenceScaling = 2f;    // Inteligencia por nivel
        public float AgilityScaling = 2f;         // Agilidad por nivel

        [Header("Habilidades")]
        public List<AbilitySO> Abilities = new List<AbilitySO>();

        /// <summary>
        /// Convierte el ScriptableObject a una estructura HeroData
        /// </summary>
        public HeroData ToHeroData()
        {
            HeroData heroData = new HeroData
            {
                Id = this.Id,
                Name = this.Name,
                Description = this.Description,
                IconSprite = this.IconSprite,
                AvatarSprite = this.AvatarSprite,
                PrefabName = this.PrefabName,
                
                // Atributos base
                BaseStrength = this.BaseStrength,
                BaseIntelligence = this.BaseIntelligence,
                BaseAgility = this.BaseAgility,
                PrimaryAttribute = this.PrimaryAttribute,
                
                // Escalados de atributos
                StrengthScaling = this.StrengthScaling,
                IntelligenceScaling = this.IntelligenceScaling,
                AgilityScaling = this.AgilityScaling,
                
                // Estadísticas derivadas
                HealthPerStrength = this.HealthPerStrength,
                ManaPerIntelligence = this.ManaPerIntelligence,
                ArmorPerAgility = this.ArmorPerAgility,
                AttackDamagePerStrength = this.AttackDamagePerStrength,
                AttackDamagePerIntelligence = this.AttackDamagePerIntelligence,
                AttackDamagePerAgility = this.AttackDamagePerAgility,
                AttackSpeedPerAgility = this.AttackSpeedPerAgility,
                MagicResistancePerIntelligence = this.MagicResistancePerIntelligence,
                HealthRegenPerStrength = this.HealthRegenPerStrength,
                ManaRegenPerIntelligence = this.ManaRegenPerIntelligence,
                MovementSpeed = this.MovementSpeed,
                RespawnTime = this.RespawnTime,
                
                // Sistema de niveles
                MaxLevel = this.MaxLevel,
                BaseExperience = this.BaseExperience,
                ExperienceScaling = this.ExperienceScaling,
                SkillPointsPerLevel = this.SkillPointsPerLevel,
                CurrentLevel = 1,
                CurrentExperience = 0,
                AvailableSkillPoints = 0
            };

            // Convertir habilidades
            foreach (var abilitySO in Abilities)
            {
                if (abilitySO != null)
                {
                    heroData.Abilities.Add(abilitySO.ToHeroAbility());
                }
            }

            return heroData;
        }
    }
}