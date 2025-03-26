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

        [Header("Estadísticas")]
        public int Health = 1000;
        public int Mana = 500;
        public string HeroType = "Luchador";
        public float AttackDamage = 60;
        public float AttackSpeed = 1.0f;
        public float MovementSpeed = 350;
        public float Armor = 20;
        public float MagicResistance = 20;
        public float HealthRegenRate = 1.0f;
        public float ManaRegenRate = 0.01f;  // 1% por segundo por defecto
        public float RespawnTime = 5.0f;

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
                Health = this.Health,
                Mana = this.Mana,
                HeroType = this.HeroType,
                AttackDamage = this.AttackDamage,
                AttackSpeed = this.AttackSpeed,
                MovementSpeed = this.MovementSpeed,
                Armor = this.Armor,
                MagicResistance = this.MagicResistance,
                HealthRegenRate = this.HealthRegenRate,
                ManaRegenRate = this.ManaRegenRate,
                RespawnTime = this.RespawnTime
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