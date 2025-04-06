using UnityEngine;

namespace Photon.Pun.Demo.Asteroids
{
    /// <summary>
    /// ScriptableObject para configurar la habilidad Gaia's Wall
    /// </summary>
    [CreateAssetMenu(fileName = "GaiasWallAbility", menuName = "Game/Abilities/Gaia's Wall", order = 1)]
    public class GaiasWallAbilitySO : AbilitySO
    {
        [Header("Wall Settings")]
        public GameObject wallPrefab;               // Prefab de la muralla
        public float maxPlacementRange = 10f;       // Rango máximo de colocación
        public float wallWidth = 5f;                // Ancho de la muralla
        public float wallHeight = 3f;               // Altura máxima de la muralla
        public float growDuration = 1.5f;           // Duración de la animación de crecimiento
        public LayerMask groundLayer;               // Capa del terreno para detección
        
        [Header("Visual Feedback")]
        public GameObject placementIndicatorPrefab; // Prefab para indicador de posición
        public Material validPlacementMaterial;     // Material cuando es válido colocar
        public Material invalidPlacementMaterial;   // Material cuando no es válido
        
        [Header("Sound Effects")]
        public AudioClip castingSound;              // Sonido al empezar a colocar
        public AudioClip placementSound;            // Sonido al colocar la muralla
        
        /// <summary>
        /// Configura la instancia de la habilidad con los valores del ScriptableObject
        /// </summary>
        /// <param name="abilityInstance">Instancia de la habilidad a configurar</param>
        public void ConfigureWallAbility(AbilityBase abilityInstance)
        {
            // Verificar que es del tipo correcto
            GaiasWallAbility wallAbility = abilityInstance as GaiasWallAbility;
            if (wallAbility != null)
            {
                // Configurar propiedades específicas
                wallAbility.wallPrefab = wallPrefab;
                wallAbility.maxPlacementRange = maxPlacementRange;
                wallAbility.wallWidth = wallWidth;
                wallAbility.wallHeight = wallHeight;
                wallAbility.growDuration = growDuration;
                wallAbility.groundLayer = groundLayer;
                wallAbility.placementIndicatorPrefab = placementIndicatorPrefab;
                wallAbility.validPlacementMaterial = validPlacementMaterial;
                wallAbility.invalidPlacementMaterial = invalidPlacementMaterial;
                
                // Configurar sonidos
                wallAbility.abilitySound = castingSound;
                wallAbility.impactSound = placementSound;
            }
            else
            {
                Debug.LogError("Error al configurar GaiasWallAbility: la instancia no es del tipo correcto");
            }
        }
    }
} 