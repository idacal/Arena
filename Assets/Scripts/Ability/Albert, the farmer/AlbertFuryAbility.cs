using UnityEngine;
using System.Collections;
using Photon.Pun;
using UnityEngine.AI;

namespace Photon.Pun.Demo.Asteroids
{
    /// <summary>
    /// Habilidad Fury de Albert: Aumenta la velocidad de movimiento y ataque durante un tiempo limitado.
    /// Implementación simplificada que delega la gestión de la velocidad al HeroMovementController.
    /// </summary>
    public class AlbertFuryAbility : AbilityBase
    {
        [Header("Configuración de Buffs")]
        [SerializeField] private float attackSpeedBonus = 100f;   // Aumento de velocidad de ataque en porcentaje
        [SerializeField] private float moveSpeedBonus = 50f;      // Aumento de velocidad de movimiento en porcentaje
        [SerializeField] private float buffDuration = 10f;        // Duración de los buffs en segundos
        
        [Header("Efectos Visuales")]
        [SerializeField] private Color effectColor = Color.red;   // Color del efecto visual
        [SerializeField] private float particleSize = 0.08f;      // Tamaño de las partículas
        [SerializeField] private float particleLifetime = 0.6f;   // Duración de las partículas
        
        // Referencias internas
        private GameObject furyEffect;
        private ParticleSystem furyParticles;
        private float originalAttackSpeed;
        private HeroMovementController movementController;
        private Coroutine buffActiveCoroutine;
        
        // ===============================================================
        // Métodos de la habilidad
        // ===============================================================
        
        protected override void OnAbilityInitialized()
        {
            if (caster == null) return;
            
            Debug.Log($"[AlbertFury] Inicializando en {caster.name}");
            
            // Guardar valor original para restaurarlo después
            originalAttackSpeed = caster.attackSpeed;
            
            // Obtener referencia al controlador de movimiento
            movementController = caster.GetComponent<HeroMovementController>();
            
            // Aplicar los buffs
            if (photonView.IsMine)
            {
                // NUEVO: Aplicar buff de velocidad de ataque usando el sistema de modificadores
                caster.ApplyStatModifier("AttackSpeed", "AlbertFury", true, attackSpeedBonus / 100f, buffDuration);
                
                // NUEVO: Aplicar buff de velocidad de movimiento usando el sistema de modificadores
                caster.ApplyStatModifier("MovementSpeed", "AlbertFury", true, moveSpeedBonus / 100f, buffDuration);
                
                Debug.Log($"[AlbertFury] Buffs aplicados: Ataque +{attackSpeedBonus}%, Movimiento +{moveSpeedBonus}% por {buffDuration} segundos");
                
                // Crear efectos visuales
                photonView.RPC("RPC_CreateFuryEffect", RpcTarget.All);
                
                // Iniciar corrutina para gestionar la duración del buff
                buffActiveCoroutine = StartCoroutine(BuffActiveCoroutine());
            }
        }
        
        /// <summary>
        /// Corrutina para gestionar la duración del buff
        /// </summary>
        private IEnumerator BuffActiveCoroutine()
        {
            Debug.Log($"[AlbertFury] Buff activo por {buffDuration} segundos");
            
            // Esperar la duración del buff
            yield return new WaitForSeconds(buffDuration);
            
            // Los modificadores se eliminan automáticamente por duración en el sistema de HeroData
            // Pero eliminamos los efectos visuales
            if (photonView != null)
            {
                photonView.RPC("RPC_DestroyFuryEffect", RpcTarget.All);
            }
            
            // Destruir la habilidad
            DestroyAbility();
        }
        
        /// <summary>
        /// Sobrescribimos DestroyAbility para asegurar limpieza correcta
        /// </summary>
        protected override void DestroyAbility()
        {
            // Detener corrutinas activas
            if (buffActiveCoroutine != null)
            {
                StopCoroutine(buffActiveCoroutine);
                buffActiveCoroutine = null;
            }
            
            // Asegurar que los valores se restauren
            if (caster != null)
            {
                caster.attackSpeed = originalAttackSpeed;
                
                // Forzar actualización de UI
                HeroUIController uiController = caster.GetComponent<HeroUIController>();
                if (uiController != null)
                {
                    uiController.UpdateHeroStats(caster);
                }
            }
            
            // Limpiar efectos visuales
            if (furyEffect != null)
            {
                Destroy(furyEffect);
                furyEffect = null;
            }
            
            // Destruir la habilidad
            base.DestroyAbility();
        }
        
        /// <summary>
        /// Limpieza final cuando se destruye el objeto
        /// </summary>
        private void OnDestroy()
        {
            // Restaurar velocidad de ataque por seguridad
            if (caster != null)
            {
                caster.attackSpeed = originalAttackSpeed;
            }
            
            // Limpiar efectos visuales
            if (furyEffect != null)
            {
                Destroy(furyEffect);
                furyEffect = null;
            }
        }
        
        // ===============================================================
        // Efectos visuales
        // ===============================================================
        
        [PunRPC]
        private void RPC_CreateFuryEffect()
        {
            if (caster == null) return;
            
            // Destruir efecto anterior si existe
            if (furyEffect != null)
            {
                Destroy(furyEffect);
            }
            
            // Crear contenedor para el efecto
            furyEffect = new GameObject("FuryEffect");
            furyEffect.transform.SetParent(caster.transform);
            furyEffect.transform.localPosition = new Vector3(0, 1.9f, 0); // Posicionar sobre la cabeza
            
            // Configurar sistema de partículas
            furyParticles = furyEffect.AddComponent<ParticleSystem>();
            
            // Configuración principal
            var main = furyParticles.main;
            main.startColor = effectColor;
            main.startLifetime = particleLifetime;
            main.startSize = particleSize;
            main.loop = true;
            main.startSpeed = 1.0f;
            main.duration = 5f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            
            // Emisión
            var emission = furyParticles.emission;
            emission.enabled = true;
            emission.rateOverTime = 50f;
            
            // Forma
            var shape = furyParticles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.05f;
            
            // Tamaño a lo largo del tiempo
            var size = furyParticles.sizeOverLifetime;
            size.enabled = true;
            AnimationCurve sizeOverLifetimeCurve = new AnimationCurve();
            sizeOverLifetimeCurve.AddKey(0f, 0.5f);
            sizeOverLifetimeCurve.AddKey(0.5f, 1.0f);
            sizeOverLifetimeCurve.AddKey(1f, 0.1f);
            size.size = new ParticleSystem.MinMaxCurve(1f, sizeOverLifetimeCurve);
            
            // Movimiento
            var velocity = furyParticles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
            
            // Color
            var colorOverLifetime = furyParticles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient colorGradient = new Gradient();
            colorGradient.SetKeys(
                new GradientColorKey[] { 
                    new GradientColorKey(Color.red, 0.0f), 
                    new GradientColorKey(new Color(1f, 0.5f, 0f), 0.5f),
                    new GradientColorKey(Color.yellow, 1.0f) 
                },
                new GradientAlphaKey[] { 
                    new GradientAlphaKey(0.7f, 0.0f), 
                    new GradientAlphaKey(0.5f, 0.7f), 
                    new GradientAlphaKey(0f, 1.0f) 
                }
            );
            colorOverLifetime.color = colorGradient;
            
            // Luz
            Light light = furyEffect.AddComponent<Light>();
            light.color = effectColor;
            light.intensity = 0.8f;
            light.range = 1.5f;
            
            // Iniciar sistema de partículas
            furyParticles.Play();
            
            Debug.Log($"[AlbertFury] Efecto visual creado para {caster.name}");
        }
        
        [PunRPC]
        private void RPC_DestroyFuryEffect()
        {
            if (furyEffect != null)
            {
                // Detener emisión
                if (furyParticles != null)
                {
                    var emission = furyParticles.emission;
                    emission.enabled = false;
                    
                    var main = furyParticles.main;
                    main.loop = false;
                    
                    // Destruir con retraso para que se vean desaparecer las partículas
                    Destroy(furyEffect, particleLifetime);
                }
                else
                {
                    Destroy(furyEffect);
                }
                
                furyEffect = null;
            }
        }
    }
}