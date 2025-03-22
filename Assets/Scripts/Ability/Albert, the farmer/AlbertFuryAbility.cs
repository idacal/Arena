using UnityEngine;

namespace Photon.Pun.Demo.Asteroids
{
    public class AlbertFuryAbility : BuffAbility
    {
        [Header("Fury Settings")]
        public GameObject furyEffectPrefab;    // Efecto visual de "poder"
        public float furyDuration = 6f;         // Duración del estado de furia
        public float attackSpeedBonus = 100f;   // Bonus de velocidad de ataque
        
        private GameObject furyEffect;
        
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
            
            // Llamar a la inicialización base que aplicará el buff
            base.OnAbilityInitialized();
            
            // Crear efecto visual de furia
            if (furyEffectPrefab != null && caster != null)
            {
                furyEffect = Instantiate(furyEffectPrefab, caster.transform.position, Quaternion.identity);
                furyEffect.transform.SetParent(caster.transform);
            }
            else
            {
                // Si no hay prefab, crear un efecto básico
                CreateBasicFuryEffect();
            }
        }
        
        private void CreateBasicFuryEffect()
        {
            if (caster == null) return;
            
            // Crear un objeto para el efecto
            GameObject effectObj = new GameObject("FuryEffect");
            effectObj.transform.SetParent(caster.transform);
            effectObj.transform.localPosition = Vector3.zero;
            
            // Crear sistema de partículas básico
            ParticleSystem particles = effectObj.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startColor = new Color(1f, 0.5f, 0f, 0.7f);  // Naranja
            main.startSize = 0.5f;
            main.startSpeed = 2f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.duration = duration;
            main.loop = true;
            
            var emission = particles.emission;
            emission.rateOverTime = 20f;
            
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1.5f;
            shape.radiusThickness = 0f;  // Emitir desde la superficie
            
            // Añadir un Light para simular el "aura" de energía
            GameObject lightObj = new GameObject("FuryLight");
            lightObj.transform.SetParent(effectObj.transform);
            lightObj.transform.localPosition = Vector3.zero;
            
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.5f, 0f);  // Naranja
            light.intensity = 1.5f;
            light.range = 3f;
            
            // Guardar referencia
            furyEffect = effectObj;
            
            // Crear efecto simple de líneas de energía
            CreateEnergyLines(furyEffect.transform);
        }
        
        private void CreateEnergyLines(Transform parent)
        {
            // Crear varias líneas de energía que suben desde el personaje
            int lineCount = 8;
            for (int i = 0; i < lineCount; i++)
            {
                GameObject lineObj = new GameObject("EnergyLine_" + i);
                lineObj.transform.SetParent(parent);
                lineObj.transform.localPosition = Vector3.zero;
                
                // Crear LineRenderer
                LineRenderer line = lineObj.AddComponent<LineRenderer>();
                line.material = new Material(Shader.Find("Sprites/Default"));
                line.startColor = new Color(1f, 0.5f, 0f, 0.8f);  // Naranja
                line.endColor = new Color(1f, 0.8f, 0f, 0f);      // Amarillo transparente
                line.startWidth = 0.05f;
                line.endWidth = 0.02f;
                line.positionCount = 2;
                
                // Posición inicial y final
                float angle = (i / (float)lineCount) * 360f * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                line.SetPosition(0, Vector3.up * 1f); // Desde la mitad del personaje
                line.SetPosition(1, Vector3.up * 3f + direction * 0.5f); // Hacia arriba y afuera
                
                // Añadir componente para animar la línea
                EnergyLineAnimation animator = lineObj.AddComponent<EnergyLineAnimation>();
                animator.line = line;
                animator.speed = Random.Range(0.5f, 1.5f);
                animator.height = Random.Range(2.5f, 4f);
                animator.radius = Random.Range(0.3f, 0.7f);
            }
        }
        
        protected override void DestroyAbility()
        {
            // Destruir efecto de furia
            if (furyEffect != null)
            {
                // Desacoplar del padre para que termine correctamente
                furyEffect.transform.SetParent(null);
                
                // Configurar para autodestrucción
                ParticleSystem particles = furyEffect.GetComponent<ParticleSystem>();
                if (particles != null)
                {
                    var main = particles.main;
                    main.loop = false;
                    Destroy(furyEffect, main.duration + main.startLifetime.constantMax);
                }
                else
                {
                    Destroy(furyEffect);
                }
            }
            
            // Continuar con la destrucción normal
            base.DestroyAbility();
        }
    }
    
    // Clase auxiliar para animar las líneas de energía
    public class EnergyLineAnimation : MonoBehaviour
    {
        public LineRenderer line;
        public float speed = 1f;
        public float height = 3f;
        public float radius = 0.5f;
        
        private float time = 0f;
        
        void Update()
        {
            if (line == null) return;
            
            time += Time.deltaTime * speed;
            
            // Animar la línea
            float yOffset = Mathf.Sin(time) * 0.5f;
            float xOffset = Mathf.Cos(time * 0.7f) * 0.3f;
            float zOffset = Mathf.Sin(time * 0.5f) * 0.3f;
            
            // Actualizar posición final
            Vector3 endPos = new Vector3(xOffset, height + yOffset, zOffset);
            line.SetPosition(1, endPos);
        }
    }
}