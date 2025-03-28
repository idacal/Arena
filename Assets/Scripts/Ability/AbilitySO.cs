using UnityEngine;

namespace Photon.Pun.Demo.Asteroids
{
    [CreateAssetMenu(fileName = "New Ability", menuName = "MOBA/Ability Data")]
    public class AbilitySO : ScriptableObject
    {
        [Header("Información Básica")]
        public string Name;
        [TextArea(3, 5)]
        public string Description;
        public Sprite IconSprite;
        public string Hotkey = "Q"; // Por ejemplo: Q, W, E, R
        
        [Header("Costos y Tiempos")]
        public float Cooldown = 10f;
        public int ManaCost = 50;
        public string AbilityType = "Activa"; // Activa, Pasiva, Ultimate
        
        [Header("Efectos")]
        public float DamageAmount;
        public float Duration;
        public float Range;
        
        [Header("Escalado por Nivel")]
        public float DamageScaling = 0f; // Cuánto aumenta el daño por nivel
        public float DurationScaling = 0f; // Cuánto aumenta la duración por nivel
        public float RangeScaling = 0f; // Cuánto aumenta el rango por nivel
        public float CooldownScaling = 0f; // Cuánto reduce el cooldown por nivel
        public float ManaCostScaling = 0f; // Cuánto aumenta el coste de maná por nivel
        public int MaxLevel = 5; // Nivel máximo de la habilidad
        
        [Header("Requisitos de Nivel")]
        public int[] LevelRequirements = new int[] { 1, 3, 5, 7, 9 }; // Nivel requerido para cada mejora
        public int[] SkillPointCosts = new int[] { 1, 1, 1, 1, 1 }; // Coste en puntos de habilidad por nivel
        public int StartingLevel = 1; // Nivel inicial de la habilidad
        
        [Header("Prefabs y Efectos Visuales")]
        public GameObject AbilityPrefab; // Para efectos visuales o proyectiles
        public AudioClip AbilitySound;
        
        /// <summary>
        /// Convierte el ScriptableObject a una estructura HeroAbility
        /// </summary>
        public HeroAbility ToHeroAbility()
        {
            return new HeroAbility
            {
                Name = this.Name,
                Description = this.Description,
                IconSprite = this.IconSprite,
                Hotkey = this.Hotkey,
                Cooldown = this.Cooldown,
                ManaCost = this.ManaCost,
                AbilityType = this.AbilityType,
                DamageAmount = this.DamageAmount,
                Duration = this.Duration,
                Range = this.Range,
                AbilitySound = this.AbilitySound,
                DamageScaling = this.DamageScaling,
                DurationScaling = this.DurationScaling,
                RangeScaling = this.RangeScaling,
                CooldownScaling = this.CooldownScaling,
                ManaCostScaling = this.ManaCostScaling,
                MaxLevel = this.MaxLevel,
                CurrentLevel = this.StartingLevel,
                LevelRequirements = this.LevelRequirements,
                SkillPointCosts = this.SkillPointCosts
            };
        }
    }
}