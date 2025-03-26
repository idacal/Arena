using UnityEngine;
using Photon.Pun;

namespace Photon.Pun.Demo.Asteroids
{
    public class AlbertFuryAbility : BuffAbility
    {
        [Header("Fury Settings")]
        public float furyDuration = 6f;         // Duración del estado de furia
        public float attackSpeedBonus = 100f;   // Bonus de velocidad de ataque
        public float movementSpeedBonus = 30f;  // Bonus de velocidad de movimiento
        
        [Header("Visual Settings")]
        public Color particleColor = new Color(1f, 0.5f, 0f, 0.7f); // Color naranja para las partículas
        public float particleSize = 0.2f;       // Tamaño pequeño para las partículas
        public int particleCount = 15;          // Cantidad reducida de partículas
        
        private GameObject furyEffect;
        private BuffInstance attackSpeedBuff;
        private BuffInstance moveSpeedBuff;
        
        protected override void OnAbilityInitialized()
        {
            // Configurar el buff para velocidad de ataque
            buffType = BuffType.AttackSpeed;
            buffValue = attackSpeedBonus;
            isPercentage = true;
            duration = furyDuration;
            applyToAllies = false;
            applyToSelf = true;
            radius = 0f;  // Solo aplica al lanzador
            
            // Llamar a la inicialización base que aplicará el buff de velocidad de ataque
            base.OnAbilityInitialized();
            
            // Aplicar bonus de velocidad de movimiento usando el sistema de buffs
            if (caster != null)
            {
                // Guardar el valor original de velocidad de movimiento
                float originalMoveSpeed = caster.moveSpeed;
                
                // Calcular el nuevo valor
                float newMoveSpeed = originalMoveSpeed * (1 + movementSpeedBonus / 100f);
                
                // Actualizar la velocidad de movimiento
                caster.moveSpeed = newMoveSpeed;
                
                // Actualizar el NavMeshAgent si existe
                HeroMovementController moveController = caster.GetComponent<HeroMovementController>();
                if (moveController != null && moveController.navAgent != null)
                {
                    moveController.navAgent.speed = newMoveSpeed;
                }
                
                // Registrar el buff de velocidad de movimiento
                moveSpeedBuff = new BuffInstance(caster, null, originalMoveSpeed, furyDuration);
            }
            
            // Usar el PhotonView para sincronizar la creación del efecto en todos los clientes
            if (photonView.IsMine)
            {
                photonView.RPC("RPC_CreateFuryEffect", RpcTarget.AllBuffered);
            }
        }
        
        protected override void AbilityUpdate()
        {
            base.AbilityUpdate();
            
            // Verificar si el buff de velocidad de movimiento ha terminado
            if (moveSpeedBuff != null && Time.time >= moveSpeedBuff.endTime)
            {
                // Restaurar la velocidad de movimiento original
                caster.moveSpeed = moveSpeedBuff.originalValue;
                
                // Actualizar el NavMeshAgent si existe
                HeroMovementController moveController = caster.GetComponent<HeroMovementController>();
                if (moveController != null && moveController.navAgent != null)
                {
                    moveController.navAgent.speed = moveSpeedBuff.originalValue;
                }
                
                moveSpeedBuff = null;
            }
        }
        
        [PunRPC]
        private void RPC_CreateFuryEffect()
        {
            // Verificar que tenemos el caster y que no hay un efecto ya creado
            if (caster == null)
            {
                Debug.LogWarning("AlbertFuryAbility: No se encontró caster para crear el efecto");
                return;
            }
            
            // Limpiar efecto existente si hay alguno
            if (furyEffect != null)
            {
                Destroy(furyEffect);
                furyEffect = null;
            }
            
            // Crear el efecto visual
            CreateSimpleParticleEffect();
        }
        
        private void CreateSimpleParticleEffect()
        {
            // Crear un objeto para el efecto
            GameObject effectObj = new GameObject("SimpleFuryEffect");
            effectObj.transform.position = caster.transform.position + Vector3.up; // Posicionar a la altura del personaje
            effectObj.transform.SetParent(caster.transform);
            effectObj.transform.localPosition = new Vector3(0, 1.0f, 0);
            
            // Crear sistema de partículas
            ParticleSystem particles = effectObj.AddComponent<ParticleSystem>();
            
            // Configurar todas las propiedades con el sistema detenido
            var main = particles.main;
            main.startColor = particleColor;
            main.startSize = particleSize;
            main.startSpeed = 0.5f;
            main.startLifetime = 1.0f;
            main.maxParticles = particleCount * 2;
            
            // Emisión
            var emission = particles.emission;
            emission.rateOverTime = particleCount;
            
            // Forma
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;
            shape.radiusThickness = 1.0f;
            
            // Color over lifetime
            var colorModule = particles.colorOverLifetime;
            colorModule.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(particleColor, 0.0f), new GradientColorKey(particleColor, 0.7f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.7f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            colorModule.color = gradient;
            
            // Iniciar el sistema
            particles.Play();
            
            // Guardar referencia
            furyEffect = effectObj;
        }
        
        protected override void DestroyAbility()
        {
            // Sincronizar destrucción de efectos visuales
            if (photonView.IsMine)
            {
                photonView.RPC("RPC_DestroyFuryEffect", RpcTarget.AllBuffered);
            }
            
            // Restaurar la velocidad de movimiento si el buff aún está activo
            if (moveSpeedBuff != null)
            {
                caster.moveSpeed = moveSpeedBuff.originalValue;
                
                // Actualizar el NavMeshAgent si existe
                HeroMovementController moveController = caster.GetComponent<HeroMovementController>();
                if (moveController != null && moveController.navAgent != null)
                {
                    moveController.navAgent.speed = moveSpeedBuff.originalValue;
                }
            }
            
            // Luego llamar a la destrucción base que quitará el buff
            base.DestroyAbility();
        }
        
        [PunRPC]
        private void RPC_DestroyFuryEffect()
        {
            // Detener y destruir el efecto visual
            if (furyEffect != null)
            {
                ParticleSystem ps = furyEffect.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Stop(true);
                    
                    // Desacoplar para una destrucción limpia
                    furyEffect.transform.SetParent(null);
                    
                    // Destruir después de que terminen las partículas
                    Destroy(furyEffect, 2f);
                }
                else
                {
                    Destroy(furyEffect);
                }
                
                furyEffect = null;
            }
        }
        
        // Limpieza en caso de destrucción inesperada
        void OnDestroy()
        {
            if (furyEffect != null)
            {
                Destroy(furyEffect);
                furyEffect = null;
            }
        }
    }
}