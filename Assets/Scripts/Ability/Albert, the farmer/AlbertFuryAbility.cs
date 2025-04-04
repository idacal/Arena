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
    public class AlbertFuryAbility : AbilityBase, IPunObservable
    {
        [Header("Configuración de Buffs")]
        [SerializeField] private float attackSpeedBonus = 100f;   // Aumento de velocidad de ataque en porcentaje
        [SerializeField] private float moveSpeedBonus = 50f;      // Aumento de velocidad de movimiento en porcentaje
        [SerializeField] private float buffDuration = 10f;        // Duración de los buffs en segundos
        
        [Header("Efectos Visuales")]
        [SerializeField] private Color effectColor = Color.red;   // Color del efecto visual
        [SerializeField] private float particleSize = 0.04f;      // Tamaño de las partículas (reducido a la mitad)
        [SerializeField] private float particleLifetime = 0.4f;   // Duración de las partículas (reducida para que no lleguen tan lejos)
        
        // Referencias internas
        private GameObject furyEffect;
        private ParticleSystem furyParticles;
        private Material particleMaterial;
        private float originalAttackSpeed;
        private HeroMovementController movementController;
        private Coroutine buffActiveCoroutine;
        
        // Variables para sincronización
        private bool _effectActive = false;
        
        // ===============================================================
        // Métodos de la habilidad
        // ===============================================================
        
        private void OnEnable()
        {
            Debug.Log($"[AlbertFury] OnEnable: ViewID={photonView?.ViewID}, IsMine={photonView?.IsMine}");
            
            // Si ya estamos inicializados pero no tenemos efecto visual y deberíamos tenerlo
            if (isInitialized && _effectActive && furyEffect == null)
            {
                Debug.Log("[AlbertFury] OnEnable: Recreando efecto visual");
                RPC_CreateFuryEffect();
            }
        }
        
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
                
                // Crear efectos visuales para todos
                _effectActive = true;
                photonView.RPC("RPC_CreateFuryEffect", RpcTarget.All);
                
                // Asegurar que nosotros también lo tengamos (por si el RPC falla)
                if (furyEffect == null) {
                    CreateEffectLocally();
                }
                
                // Iniciar corrutina para gestionar la duración del buff
                buffActiveCoroutine = StartCoroutine(BuffActiveCoroutine());
            }
        }
        
        /// <summary>
        /// Crea el efecto localmente sin usar RPC (para casos donde el RPC falle)
        /// </summary>
        private void CreateEffectLocally()
        {
            Debug.Log("[AlbertFury] Creando efecto localmente (sin RPC)");
            RPC_CreateFuryEffect();
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
            
            // Destruir material de partículas
            if (particleMaterial != null)
            {
                Destroy(particleMaterial);
                particleMaterial = null;
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
            
            // Destruir material
            if (particleMaterial != null)
            {
                Destroy(particleMaterial);
                particleMaterial = null;
            }
        }
        
        // ===============================================================
        // Efectos visuales
        // ===============================================================
        
        [PunRPC]
        private void RPC_CreateFuryEffect()
        {
            if (caster == null)
            {
                Debug.LogError("[AlbertFury] No se puede crear efecto visual: caster es null");
                return;
            }
            
            Debug.Log($"[AlbertFury] Creando efecto visual para {caster.name}, ViewID: {photonView.ViewID}, IsMine: {photonView.IsMine}");
            
            // Destruir efecto anterior si existe
            if (furyEffect != null)
            {
                if (furyParticles != null)
                {
                    furyParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
                Destroy(furyEffect);
                furyEffect = null;
                furyParticles = null;
            }
            
            try
            {
                // Crear contenedor para el efecto
                furyEffect = new GameObject("FuryEffect");
                furyEffect.transform.SetParent(caster.transform);
                furyEffect.transform.localPosition = new Vector3(0, 1.7f, 0); // Posicionar sobre la cabeza (ligeramente más bajo)
                
                // Configurar sistema de partículas
                furyParticles = furyEffect.AddComponent<ParticleSystem>();
                
                // IMPORTANTE: Detener el sistema antes de configurarlo
                furyParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                
                // Crear material para las partículas
                particleMaterial = new Material(Shader.Find("Particles/Standard Unlit"));
                if (particleMaterial == null || particleMaterial.shader == null)
                {
                    Debug.LogWarning("[AlbertFury] Shader 'Particles/Standard Unlit' no encontrado, intentando shader alternativo");
                    
                    // Intentar con Mobile Particles primero
                    particleMaterial = new Material(Shader.Find("Mobile/Particles/Additive"));
                    
                    // Si todavía no funciona, intentar con Sprites/Default
                    if (particleMaterial == null || particleMaterial.shader == null)
                    {
                        particleMaterial = new Material(Shader.Find("Sprites/Default"));
                        Debug.LogWarning("[AlbertFury] Usando shader 'Sprites/Default' como último recurso");
                    }
                }
                
                if (particleMaterial != null)
                {
                    particleMaterial.SetColor("_Color", effectColor);
                    particleMaterial.SetColor("_TintColor", effectColor); // Para shaders que usan _TintColor
                }
                else
                {
                    Debug.LogError("[AlbertFury] No se pudo crear un material para las partículas");
                }
                
                // Obtener renderer y asignar material
                var renderer = furyParticles.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                {
                    renderer.material = particleMaterial;
                    renderer.renderMode = ParticleSystemRenderMode.Billboard;
                    renderer.enableGPUInstancing = true;
                }
                
                // Configuración principal - ¡ANTES de iniciar el sistema!
                var main = furyParticles.main;
                main.startColor = effectColor;
                main.startLifetime = particleLifetime;
                main.startSize = particleSize;
                main.loop = true;
                main.duration = 5f;
                main.startSpeed = 0.7f; // Velocidad reducida para que no vayan tan lejos
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.maxParticles = 100; // Limitar número de partículas
                
                // Emisión
                var emission = furyParticles.emission;
                emission.enabled = true;
                emission.rateOverTime = 30f; // Reducir tasa de emisión
                
                // Forma
                var shape = furyParticles.shape;
                shape.enabled = true;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.03f; // Radio más pequeño
                
                // Limitar distancia
                var limitVelocity = furyParticles.limitVelocityOverLifetime;
                limitVelocity.enabled = true;
                limitVelocity.limit = 1.0f; // Limitar velocidad máxima
                
                // Tamaño a lo largo del tiempo
                var size = furyParticles.sizeOverLifetime;
                size.enabled = true;
                AnimationCurve sizeOverLifetimeCurve = new AnimationCurve();
                sizeOverLifetimeCurve.AddKey(0f, 0.5f);
                sizeOverLifetimeCurve.AddKey(0.5f, 0.8f);
                sizeOverLifetimeCurve.AddKey(1f, 0.1f);
                size.size = new ParticleSystem.MinMaxCurve(1f, sizeOverLifetimeCurve);
                
                // Movimiento
                var velocity = furyParticles.velocityOverLifetime;
                velocity.enabled = true;
                velocity.x = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f); // Reducir rango
                velocity.y = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);  // Menos altura
                velocity.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f); // Reducir rango
                
                // Color
                var colorOverLifetime = furyParticles.colorOverLifetime;
                colorOverLifetime.enabled = true;
                Gradient colorGradient = new Gradient();
                colorGradient.SetKeys(
                    new GradientColorKey[] { 
                        new GradientColorKey(new Color(1f, 0.3f, 0.1f), 0.0f),  // Naranja rojizo 
                        new GradientColorKey(new Color(1f, 0.7f, 0.2f), 0.5f),  // Naranja
                        new GradientColorKey(new Color(1f, 0.9f, 0.3f), 1.0f)   // Amarillo claro
                    },
                    new GradientAlphaKey[] { 
                        new GradientAlphaKey(0.8f, 0.0f), 
                        new GradientAlphaKey(0.6f, 0.7f), 
                        new GradientAlphaKey(0f, 1.0f) 
                    }
                );
                colorOverLifetime.color = colorGradient;
                
                // Luz
                Light light = furyEffect.AddComponent<Light>();
                light.color = new Color(1f, 0.5f, 0.1f); // Naranja para la luz
                light.intensity = 0.6f;
                light.range = 1.2f;
                light.type = LightType.Point;
                light.shadows = LightShadows.None;
                
                // DESPUÉS de configurar todo, iniciar el sistema de partículas
                furyParticles.Play();
                _effectActive = true;
                
                Debug.Log($"[AlbertFury] Efecto visual creado exitosamente para {caster.name}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AlbertFury] Error al crear efecto: {e.Message}\n{e.StackTrace}");
                
                // Si falla, crear un efecto de luz simple como fallback
                if (furyEffect != null)
                {
                    Light fallbackLight = furyEffect.AddComponent<Light>();
                    fallbackLight.color = new Color(1f, 0.5f, 0.1f); // Naranja para la luz
                    fallbackLight.intensity = 1.0f;
                    fallbackLight.range = 2.0f;
                    _effectActive = true;
                }
            }
        }
        
        [PunRPC]
        private void RPC_DestroyFuryEffect()
        {
            if (furyEffect != null)
            {
                // Detener emisión
                if (furyParticles != null)
                {
                    // Reducir tasa de emisión gradualmente
                    var emission = furyParticles.emission;
                    emission.enabled = false;
                    
                    // Configurar para que las partículas actuales terminen su ciclo
                    var main = furyParticles.main;
                    main.loop = false;
                    
                    // Desactivar luz gradualmente durante 0.5 segundos
                    Light light = furyEffect.GetComponent<Light>();
                    if (light != null)
                    {
                        StartCoroutine(FadeOutLight(light, 0.5f));
                    }
                    
                    // Destruir con retraso para que se vean desaparecer las partículas
                    Destroy(furyEffect, particleLifetime + 0.1f);
                }
                else
                {
                    Destroy(furyEffect);
                }
                
                furyEffect = null;
                _effectActive = false;
            }
        }
        
        /// <summary>
        /// Corrutina para desvanecer gradualmente la luz
        /// </summary>
        private IEnumerator FadeOutLight(Light light, float duration)
        {
            float startIntensity = light.intensity;
            float elapsed = 0;
            
            while (elapsed < duration)
            {
                light.intensity = Mathf.Lerp(startIntensity, 0, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            light.intensity = 0;
        }

        /// <summary>
        /// Implementación de IPunObservable para sincronizar el estado del buff
        /// </summary>
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            try {
                if (stream.IsWriting)
                {
                    // Datos a enviar a otros clientes
                    stream.SendNext(_effectActive);
                    
                    // Enviar si tenemos furyEffect activo (para mejor diagnóstico)
                    stream.SendNext(furyEffect != null);
                }
                else
                {
                    // Datos a recibir de otros clientes
                    bool wasActive = _effectActive;
                    _effectActive = (bool)stream.ReceiveNext();
                    
                    // Recibir estado de furyEffect
                    bool hasEffect = (bool)stream.ReceiveNext();
                    
                    // Diagnóstico avanzado
                    Debug.Log($"[AlbertFury] Recibiendo estado: _effectActive={_effectActive}, hasEffect={hasEffect}, nuestroEffect={furyEffect != null}");
                    
                    // Si el efecto debe estar activo pero no tenemos el objeto visual, crearlo
                    if (_effectActive && furyEffect == null)
                    {
                        Debug.Log($"[AlbertFury] Recreando efecto perdido en cliente");
                        RPC_CreateFuryEffect();
                    }
                    // Si el efecto no debe estar activo pero tenemos el objeto visual, destruirlo
                    else if (!_effectActive && furyEffect != null)
                    {
                        Debug.Log($"[AlbertFury] Limpiando efecto innecesario en cliente");
                        RPC_DestroyFuryEffect();
                    }
                }
            }
            catch (System.Exception e) {
                Debug.LogError($"[AlbertFury] Error en OnPhotonSerializeView: {e.Message}");
            }
        }
    }
}