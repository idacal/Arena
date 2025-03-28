using UnityEngine;
using System;
using System.Collections.Generic;

namespace Photon.Pun.Demo.Asteroids
{
    [Serializable]
    public class HeroData
    {
        public int Id;
        public string Name;
        public string Description;
        
        // Image separation for different uses
        public Sprite IconSprite;     // Small icon for selection grid
        public Sprite AvatarSprite;   // Larger image for details panel
        
        public string PrefabName; // Name of the prefab to instantiate for this hero
        
        // Base Attributes
        public float BaseStrength;
        public float BaseIntelligence;
        public float BaseAgility;
        public string PrimaryAttribute;
        
        // Attribute Scaling
        public float StrengthScaling;
        public float IntelligenceScaling;
        public float AgilityScaling;
        
        // Derived Stats
        public float HealthPerStrength;
        public float ManaPerIntelligence;
        public float ArmorPerAgility;
        public float AttackDamagePerStrength;
        public float AttackDamagePerIntelligence;
        public float AttackDamagePerAgility;
        public float AttackSpeedPerAgility;
        public float MagicResistancePerIntelligence;
        public float HealthRegenPerStrength;
        public float ManaRegenPerIntelligence;
        public float MovementSpeed;
        public float RespawnTime;
        
        // Level System
        public int MaxLevel;
        public float BaseExperience;
        public float ExperienceScaling;
        public int SkillPointsPerLevel;
        public int CurrentLevel;
        public float CurrentExperience;
        public int AvailableSkillPoints;
        
        // Abilities list
        public List<HeroAbility> Abilities = new List<HeroAbility>();
        
        // Properties calculated for compatibility with existing code
        public int Health => Mathf.RoundToInt(MaxHealth);
        public int Mana => Mathf.RoundToInt(MaxMana);
        public string HeroType => PrimaryAttribute; // Usamos el atributo principal como tipo de héroe
        public float AttackDamage => CurrentAttackDamage;
        public float AttackSpeed => CurrentAttackSpeed;
        public float Armor => CurrentArmor;
        public float MagicResistance => CurrentMagicResistance;
        public float HealthRegenRate => CurrentHealthRegen;
        public float ManaRegenRate => CurrentManaRegen;
        
        // Properties calculated for the new system
        public float CurrentStrength => BaseStrength + (StrengthScaling * (CurrentLevel - 1));
        public float CurrentIntelligence => BaseIntelligence + (IntelligenceScaling * (CurrentLevel - 1));
        public float CurrentAgility => BaseAgility + (AgilityScaling * (CurrentLevel - 1));
        
        public float MaxHealth => CurrentStrength * HealthPerStrength;
        public float MaxMana => CurrentIntelligence * ManaPerIntelligence;
        public float CurrentArmor => CurrentAgility * ArmorPerAgility;
        public float CurrentMagicResistance => CurrentIntelligence * MagicResistancePerIntelligence;
        public float CurrentAttackDamage
        {
            get
            {
                switch (PrimaryAttribute)
                {
                    case "Strength":
                        return CurrentStrength * AttackDamagePerStrength;
                    case "Intelligence":
                        return CurrentIntelligence * AttackDamagePerIntelligence;
                    case "Agility":
                        return CurrentAgility * AttackDamagePerAgility;
                    default:
                        return 0;
                }
            }
        }
        public float CurrentAttackSpeed => 1f + (CurrentAgility * AttackSpeedPerAgility);
        public float CurrentHealthRegen => CurrentStrength * HealthRegenPerStrength;
        public float CurrentManaRegen => CurrentIntelligence * ManaRegenPerIntelligence;
        
        // Method to add experience
        public bool AddExperience(float amount)
        {
            if (CurrentLevel >= MaxLevel)
                return false;
            
            CurrentExperience += amount;
            
            // Calculate experience needed for the next level
            float experienceNeeded = GetExperienceForNextLevel();
            
            // Level up if there's enough experience
            while (CurrentExperience >= experienceNeeded && CurrentLevel < MaxLevel)
            {
                CurrentExperience -= experienceNeeded;
                LevelUp();
                experienceNeeded = GetExperienceForNextLevel();
            }
            
            return true;
        }
        
        // Method to level up
        private void LevelUp()
        {
            CurrentLevel++;
            AvailableSkillPoints += SkillPointsPerLevel;
        }
        
        // Method to calculate experience needed for the next level
        public float GetExperienceForNextLevel()
        {
            return BaseExperience * Mathf.Pow(ExperienceScaling, CurrentLevel - 1);
        }
        
        // Method to level up an ability
        public bool LevelUpAbility(int abilityIndex)
        {
            if (abilityIndex < 0 || abilityIndex >= Abilities.Count)
                return false;
            
            return Abilities[abilityIndex].TryUpgrade(CurrentLevel, AvailableSkillPoints);
        }
        
        // Method to get current experience progress (0-1)
        public float GetExperienceProgress()
        {
            if (CurrentLevel >= MaxLevel)
                return 1f;
            
            float currentLevelExp = GetExperienceForNextLevel();
            return CurrentExperience / currentLevelExp;
        }
        
        // Method to get a detailed description of the stats
        public string GetStatsDescription()
        {
            return $"Level {CurrentLevel}\n" +
                   $"Strength: {CurrentStrength:F1} (+{StrengthScaling:F1})\n" +
                   $"Intelligence: {CurrentIntelligence:F1} (+{IntelligenceScaling:F1})\n" +
                   $"Agility: {CurrentAgility:F1} (+{AgilityScaling:F1})\n" +
                   $"Health: {MaxHealth:F0}\n" +
                   $"Mana: {MaxMana:F0}\n" +
                   $"Damage: {CurrentAttackDamage:F0}\n" +
                   $"Attack Speed: {CurrentAttackSpeed:F2}\n" +
                   $"Armor: {CurrentArmor:F1}\n" +
                   $"Magic Resistance: {CurrentMagicResistance:F1}\n" +
                   $"Health Regen: {CurrentHealthRegen:F1}/s\n" +
                   $"Mana Regen: {CurrentManaRegen:F1}/s";
        }
    }
}