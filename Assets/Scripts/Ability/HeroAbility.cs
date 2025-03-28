using UnityEngine;

namespace Photon.Pun.Demo.Asteroids
{
    [System.Serializable]
    public class HeroAbility
    {
        public string Name;
        public string Description;
        public Sprite IconSprite;
        public string Hotkey;
        public float Cooldown;
        public int ManaCost;
        public string AbilityType;
        public float DamageAmount;
        public float Duration;
        public float Range;
        public AudioClip AbilitySound;

        // Escalado por nivel
        public float DamageScaling;
        public float DurationScaling;
        public float RangeScaling;
        public float CooldownScaling;
        public float ManaCostScaling;
        public int MaxLevel;
        public int CurrentLevel = 0; // Nivel inicial de la habilidad

        // Requisitos de nivel
        public int[] LevelRequirements;
        public int[] SkillPointCosts;

        // Propiedades calculadas basadas en el nivel actual
        public float CurrentDamage => DamageAmount + (DamageScaling * CurrentLevel);
        public float CurrentDuration => Duration + (DurationScaling * CurrentLevel);
        public float CurrentRange => Range + (RangeScaling * CurrentLevel);
        public float CurrentCooldown => Mathf.Max(0.1f, Cooldown - (CooldownScaling * CurrentLevel));
        public int CurrentManaCost => Mathf.RoundToInt(ManaCost + (ManaCostScaling * CurrentLevel));
        public bool CanBeUpgraded => CurrentLevel < MaxLevel;
        public int RequiredLevel => CurrentLevel < LevelRequirements.Length ? LevelRequirements[CurrentLevel] : int.MaxValue;
        public int UpgradeCost => CurrentLevel < SkillPointCosts.Length ? SkillPointCosts[CurrentLevel] : int.MaxValue;

        // Método para mejorar la habilidad
        public bool TryUpgrade(int heroLevel, int availableSkillPoints)
        {
            if (!CanBeUpgraded)
                return false;

            if (heroLevel < RequiredLevel)
                return false;

            if (availableSkillPoints < UpgradeCost)
                return false;

            CurrentLevel++;
            return true;
        }

        // Método para obtener la descripción actualizada con los valores del nivel actual
        public string GetUpdatedDescription()
        {
            string desc = Description;
            desc = desc.Replace("{damage}", CurrentDamage.ToString("F0"));
            desc = desc.Replace("{duration}", CurrentDuration.ToString("F1"));
            desc = desc.Replace("{range}", CurrentRange.ToString("F0"));
            desc = desc.Replace("{cooldown}", CurrentCooldown.ToString("F1"));
            desc = desc.Replace("{manacost}", CurrentManaCost.ToString());
            return desc;
        }

        // Método para obtener el nivel actual de la habilidad
        public int GetCurrentLevel()
        {
            return CurrentLevel;
        }

        // Método para establecer el nivel de la habilidad
        public void SetLevel(int level)
        {
            CurrentLevel = Mathf.Clamp(level, 0, MaxLevel);
        }
    }
} 