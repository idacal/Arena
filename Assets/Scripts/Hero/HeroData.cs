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
        
        // Separación de imágenes para diferentes usos
        public Sprite IconSprite;     // Icono pequeño para la cuadrícula de selección
        public Sprite AvatarSprite;   // Imagen más grande para el panel de detalles
        
        public string PrefabName; // Nombre del prefab a instanciar para este héroe
        
        // Estadísticas básicas
        public int Health;
        public int Mana;
        public string HeroType; // Ejemplo: Tanque, Mago, Soporte, Asesino, etc.
        public float AttackDamage;
        public float AttackSpeed;
        public float MovementSpeed;
        public float Armor;
        public float MagicResistance;
        public float HealthRegenRate;
        public float ManaRegenRate;  // Tasa de regeneración de maná por segundo
        public float RespawnTime;
        
        // Lista de habilidades (implementación dinámica)
        public List<HeroAbility> Abilities = new List<HeroAbility>();
    }
    
    [Serializable]
    public class HeroAbility
    {
        public string Name;
        public string Description;
        public Sprite IconSprite;
        public string Hotkey; // Por ejemplo: "Q", "W", "E", "R", o cualquier otra tecla
        public float Cooldown;
        public int ManaCost;
        public string AbilityType; // Ejemplo: "Pasiva", "Activa", "Ultimate", etc.
        
        // Valores adicionales opcionales según tu juego
        public float DamageAmount;
        public float Duration;
        public float Range;
        
        // Añadir referencia para el sonido
        public AudioClip AbilitySound;
    }
}