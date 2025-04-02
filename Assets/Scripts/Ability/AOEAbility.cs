using UnityEngine;
using System.Collections.Generic;

namespace Photon.Pun.Demo.Asteroids
{
    /// <summary>
    /// Implementación de una habilidad de efecto en área (AOE)
    /// </summary>
    public class AOEAbility : AbilityBase
    {
        [Header("AOE Settings")]
        public float radius = 5f;                  // Radio del efecto de área
        public bool useGrowingRadius = false;      // Si el radio crece con el tiempo
        public float growthSpeed = 1f;             // Velocidad de crecimiento del radio
        public float maxRadius = 10f;              // Radio máximo si está creciendo
        public float damageInterval = 0.5f;        // Intervalo de daño para efectos continuos (0 = solo aplica una vez)
        public bool affectsAllies = false;         // Si afecta a aliados o solo a enemigos
        public LayerMask targetLayers;             // Capas afectadas por la AOE
        
        [Header("Visual Effects")]
        public GameObject aoeVisualEffect;         // Efecto visual del área
        public bool scaleVisualWithRadius = true;  // Si el visual se escala con el radio
        public ParticleSystem particleEffect;      // Sistema de partículas (opcional)
        
        // Variables privadas
        private float currentRadius;                // Radio actual
        private float lastDamageTime = 0f;          // Último momento en que se aplicó daño
        private Dictionary<int, float> lastHitTimes = new Dictionary<int, float>(); // Para rastrear intervalos por objetivo
        private bool soundPlayed = false;           // Flag para controlar si ya se reprodujo el sonido
        
        protected override void OnAbilityInitialized()
        {
            // Inicializar radio
            currentRadius = radius;
            
            // Escalar efecto visual inicial
            if (aoeVisualEffect != null && scaleVisualWithRadius)
            {
                aoeVisualEffect.transform.localScale = new Vector3(
                    currentRadius * 2f,
                    aoeVisualEffect.transform.localScale.y,
                    currentRadius * 2f
                );
            }
            
            // Iniciar efectos de partículas
            if (particleEffect != null)
            {
                // Ajustar área de emisión si es un efecto de área
                var shapeModule = particleEffect.shape;
                if (shapeModule.enabled && shapeModule.shapeType == ParticleSystemShapeType.Circle)
                {
                    shapeModule.radius = currentRadius;
                }
                
                particleEffect.Play();
            }
            
            // Reproducir sonido al inicializar
            if (photonView.IsMine && !soundPlayed && abilitySound != null)
            {
                PlayAbilitySound();
                soundPlayed = true;
            }
        }
        
        // Método para actualización, llamarlo desde Update en las clases derivadas
        protected virtual void Update()
        {
            // Asegurar que reproducimos el sonido si no se hizo en OnAbilityInitialized
            if (photonView.IsMine && !soundPlayed && abilitySound != null)
            {
                PlayAbilitySound();
                soundPlayed = true;
            }
            
            // Actualizar radio si está creciendo
            if (useGrowingRadius)
            {
                UpdateRadius();
            }
            
            // Verificar objetivos en área
            CheckTargetsInArea();
            
            // Actualizar efectos visuales
            UpdateVisuals();
        }
        
        /// <summary>
        /// Actualiza el radio del efecto si está configurado para crecer
        /// </summary>
        private void UpdateRadius()
        {
            if (currentRadius < maxRadius)
            {
                // Incrementar radio
                currentRadius += growthSpeed * Time.deltaTime;
                
                // Limitar al máximo
                if (currentRadius > maxRadius)
                {
                    currentRadius = maxRadius;
                }
            }
        }
        
        /// <summary>
        /// Actualiza los efectos visuales del AOE
        /// </summary>
        private void UpdateVisuals()
        {
            // Escalar el efecto visual si está configurado para hacerlo
            if (aoeVisualEffect != null && scaleVisualWithRadius)
            {
                aoeVisualEffect.transform.localScale = new Vector3(
                    currentRadius * 2f,
                    aoeVisualEffect.transform.localScale.y,
                    currentRadius * 2f
                );
            }
            
            // Actualizar área de emisión de partículas
            if (particleEffect != null)
            {
                var shapeModule = particleEffect.shape;
                if (shapeModule.enabled && shapeModule.shapeType == ParticleSystemShapeType.Circle)
                {
                    shapeModule.radius = currentRadius;
                }
            }
        }
        
        /// <summary>
        /// Comprueba qué objetivos están en el área de efecto y aplica el efecto correspondiente
        /// </summary>
        protected virtual void CheckTargetsInArea()
        {
            if (!photonView.IsMine) return;
            
            // Si ha pasado suficiente tiempo desde el último daño
            if (Time.time >= lastDamageTime + damageInterval || damageInterval <= 0)
            {
                // Buscar todos los colliders en el área
                Collider[] hitColliders = Physics.OverlapSphere(transform.position, currentRadius, targetLayers);
                
                // Procesar cada collider
                foreach (var hitCollider in hitColliders)
                {
                    // Verificar si es un héroe
                    HeroBase targetHero = hitCollider.GetComponent<HeroBase>();
                    if (targetHero != null && (!targetHero.photonView.IsMine || targetHero != caster))
                    {
                        // Verificar si debemos afectar a este objetivo (aliado/enemigo)
                        if ((affectsAllies || IsEnemy(targetHero)))
                        {
                            // Verificar intervalo por objetivo
                            int targetId = targetHero.photonView.ViewID;
                            float lastHitTime = 0f;
                            
                            if (lastHitTimes.TryGetValue(targetId, out lastHitTime))
                            {
                                if (Time.time < lastHitTime + damageInterval)
                                    continue; // Saltarse este objetivo, aún no toca dañarlo
                            }
                            
                            // Actualizar tiempo del último impacto para este objetivo
                            lastHitTimes[targetId] = Time.time;
                            
                            // Procesar impacto
                            ProcessImpact(targetHero);
                        }
                    }
                }
                
                // Actualizar tiempo del último daño general
                lastDamageTime = Time.time;
            }
        }
        
        /// <summary>
        /// Comprueba si un héroe es enemigo del lanzador
        /// </summary>
        protected bool IsEnemy(HeroBase targetHero)
        {
            // Si no tenemos referencia al lanzador, asumir que es enemigo
            if (caster == null)
                return true;
                
            // Comparar equipos
            return caster.teamId != targetHero.teamId;
        }
        
        // Podemos sobrescribir ProcessImpact si necesitamos efectos especiales para AOE
        protected override void ProcessImpact(HeroBase target)
        {
            // Aplicar daño
            if (baseDamage > 0 && caster != null)
            {
                target.TakeDamage(baseDamage, caster, isMagicDamage);
            }
            
            // Aplicar efectos adicionales específicos
            ApplyEffects(target);
            
            // Creamos un efecto de impacto por cada objetivo afectado
            if (impactEffectPrefab != null)
            {
                Instantiate(impactEffectPrefab, target.transform.position + Vector3.up, Quaternion.identity);
            }
            
            // Importante: NO destruimos la habilidad al impactar, ya que un AOE afecta a múltiples objetivos
            // La destrucción se maneja por tiempo de vida
        }
        
        // Método para aplicar efectos adicionales específicos
        protected virtual void ApplyEffects(HeroBase target)
        {
            // Implementar en clases derivadas
        }
        
        // Visualización en el editor
        void OnDrawGizmos()
        {
            // Dibujar esfera para visualizar el área en el editor
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, radius);
            
            if (useGrowingRadius)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
                Gizmos.DrawWireSphere(transform.position, maxRadius);
            }
        }
    }
}