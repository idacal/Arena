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
        
        [Header("Prefabs y Efectos Visuales")]
        public GameObject AbilityPrefab; // Para efectos visuales o proyectiles
        public AudioClip AbilitySound;
        
        /// <summary>
        /// Convierte el ScriptableObject a una estructura HeroAbility
        /// </summary>
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
            AbilitySound = this.AbilitySound // Añadir sonido
        };
    }
    }
}