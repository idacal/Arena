using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

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
        public float AttackRange;
        public float RespawnTime;
        
        // NEW: Sistema de modificadores para stats
        private Dictionary<string, List<StatModifier>> statModifiers = new Dictionary<string, List<StatModifier>>();
        
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
        
        // Nueva propiedad para obtener la velocidad con modificadores aplicados
        public float CurrentMovementSpeed => GetModifiedStat("MovementSpeed", MovementSpeed);
        
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
        
        // Propiedades de rango de daño (95% a 100% del daño base)
        public float MinAttackDamage => CurrentAttackDamage * 0.95f;
        public float MaxAttackDamage => CurrentAttackDamage;
        public string AttackDamageRange => $"{Mathf.RoundToInt(MinAttackDamage)} - {Mathf.RoundToInt(MaxAttackDamage)}";
        
        // Modificado para usar el sistema de modificadores
        public float CurrentAttackSpeed => GetModifiedStat("AttackSpeed", 1f + (CurrentAgility * AttackSpeedPerAgility));
        public float CurrentHealthRegen => CurrentStrength * HealthRegenPerStrength;
        public float CurrentManaRegen => CurrentIntelligence * ManaRegenPerIntelligence;
        
        // Evento para notificar cuando un stat cambia
        public event Action<string> StatChanged;
        
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
                   $"Damage: {AttackDamageRange}\n" +
                   $"Attack Speed: {CurrentAttackSpeed:F2}\n" +
                   $"Armor: {CurrentArmor:F1}\n" +
                   $"Magic Resistance: {CurrentMagicResistance:F1}\n" +
                   $"Health Regen: {CurrentHealthRegen:F1}/s\n" +
                   $"Mana Regen: {CurrentManaRegen:F1}/s";
        }
        
        // Clase para representar modificadores de estadísticas
        [Serializable]
        public class StatModifier
        {
            public enum ModifierType { Flat, Percent }
            
            public string SourceName;      // Nombre de la fuente del modificador (habilidad, buff, etc.)
            public ModifierType Type;      // Tipo de modificador
            public float Value;            // Valor del modificador
            public float Duration;         // Duración en segundos (0 = permanente)
            public float EndTime;          // Tiempo cuando finaliza el modificador
            
            public StatModifier(string sourceName, ModifierType type, float value, float duration = 0)
            {
                SourceName = sourceName;
                Type = type;
                Value = value;
                Duration = duration;
                EndTime = duration > 0 ? Time.time + duration : 0;
            }
            
            public bool IsExpired => Duration > 0 && Time.time > EndTime;
        }
        
        // Método para agregar un modificador a una estadística
        public void AddStatModifier(string statName, StatModifier modifier)
        {
            if (!statModifiers.ContainsKey(statName))
            {
                statModifiers[statName] = new List<StatModifier>();
            }
            
            statModifiers[statName].Add(modifier);
            
            // Notificar a los componentes que el stat ha cambiado
            OnStatChanged(statName);
        }
        
        // Método para remover modificadores por nombre de fuente
        public void RemoveStatModifiersBySource(string statName, string sourceName)
        {
            if (statModifiers.ContainsKey(statName))
            {
                statModifiers[statName].RemoveAll(mod => mod.SourceName == sourceName);
                
                // Notificar a los componentes que el stat ha cambiado
                OnStatChanged(statName);
            }
        }
        
        // Método para limpiar los modificadores expirados
        public bool CleanupExpiredModifiers()
        {
            bool anyRemoved = false;
            
            foreach (var statName in statModifiers.Keys.ToList())
            {
                int countBefore = statModifiers[statName].Count;
                statModifiers[statName].RemoveAll(mod => mod.IsExpired);
                
                if (countBefore != statModifiers[statName].Count)
                {
                    // Notificar a los componentes que el stat ha cambiado
                    OnStatChanged(statName);
                    anyRemoved = true;
                }
            }
            
            return anyRemoved;
        }
        
        // Método para calcular una estadística con modificadores
        public float GetModifiedStat(string statName, float baseValue)
        {
            if (!statModifiers.ContainsKey(statName) || statModifiers[statName].Count == 0)
            {
                return baseValue;
            }
            
            float finalValue = baseValue;
            float sumPercentAdd = 0;
            
            // Primero aplicar modificadores flat
            foreach (var mod in statModifiers[statName].Where(m => m.Type == StatModifier.ModifierType.Flat))
            {
                finalValue += mod.Value;
            }
            
            // Luego aplicar modificadores porcentuales
            foreach (var mod in statModifiers[statName].Where(m => m.Type == StatModifier.ModifierType.Percent))
            {
                sumPercentAdd += mod.Value;
            }
            
            // Aplicar el total porcentual (sumamos los porcentajes para evitar aplicaciones multiplicativas)
            finalValue *= (1 + sumPercentAdd);
            
            return finalValue;
        }
        
        // Método para notificar a los componentes que un stat ha cambiado
        private void OnStatChanged(string statName)
        {
            // Invocar el evento StatChanged para que HeroBase pueda reaccionar
            StatChanged?.Invoke(statName);
        }
    }
}