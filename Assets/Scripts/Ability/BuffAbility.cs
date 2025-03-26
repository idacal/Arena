using UnityEngine;
using System.Collections.Generic;

namespace Photon.Pun.Demo.Asteroids
{
    /// <summary>
    /// Clase que implementa habilidades tipo buff o mejora temporal
    /// </summary>
    public class BuffAbility : AbilityBehaviour
    {
        [Header("Buff Settings")]
        public BuffType buffType = BuffType.AttackDamage;   // Tipo de buff
        public float buffValue = 50f;                       // Valor de la mejora
        public bool isPercentage = false;                   // Si es un valor porcentual
        public float duration = 5f;                         // Duración del buff
        public bool applyToAllies = true;                   // Si se aplica a aliados
        public bool applyToSelf = true;                     // Si se aplica al lanzador
        public float radius = 5f;                           // Radio de efecto (0 = solo al objetivo/lanzador)
        public LayerMask targetLayers;                      // Capas afectadas
        
        [Header("Visual Effects")]
        public GameObject buffVisualPrefab;                 // Efecto visual temporal para mostrar en el objetivo
        public bool attachToBone = false;                   // Si el efecto se adjunta a un hueso específico del personaje
        public string boneName = "Chest";                   // Nombre del hueso si attachToBone es true
        
        // Lista de referencias para limpiar efectos
        private List<BuffInstance> appliedBuffs = new List<BuffInstance>();
        
        // Clase para almacenar información de cada buff aplicado
        protected class BuffInstance
        {
            public HeroBase target;              // Héroe objetivo
            public GameObject visualEffect;      // Efecto visual instanciado
            public float originalValue;          // Valor original de la estadística
            public float endTime;                // Cuando finaliza el buff
            
            public BuffInstance(HeroBase target, GameObject visualEffect, float originalValue, float duration)
            {
                this.target = target;
                this.visualEffect = visualEffect;
                this.originalValue = originalValue;
                this.endTime = Time.time + duration;
            }
        }
        
        // Enumerador para los tipos de buff
        public enum BuffType
        {
            AttackDamage,
            AttackSpeed,
            MoveSpeed,
            Armor,
            MagicResistance,
            HealthRegen,
            ManaRegen
        }
        
        protected override void OnAbilityInitialized()
        {
            // Si tiene radio, funcionará como un buff de área
            if (radius > 0)
            {
                ApplyBuffInArea();
            }
            // Si no, se aplica directamente al objetivo o caster
            else
            {
                if (applyToSelf && caster != null)
                {
                    ApplyBuff(caster);
                }
            }
        }
        
        protected override void AbilityUpdate()
        {
            // Verificar si algún buff ha terminado para removerlo
            for (int i = appliedBuffs.Count - 1; i >= 0; i--)
            {
                BuffInstance buff = appliedBuffs[i];
                
                if (Time.time >= buff.endTime)
                {
                    RemoveBuff(buff);
                    appliedBuffs.RemoveAt(i);
                }
            }
            
            // Si no quedan buffs activos, destruir la habilidad
            if (appliedBuffs.Count == 0 && elapsedTime > 0.5f)
            {
                DestroyAbility();
            }
        }
        
        /// <summary>
        /// Aplica el buff a todos los objetivos válidos en el área
        /// </summary>
        private void ApplyBuffInArea()
        {
            // Solo el dueño de la habilidad aplica los buffs
            if (photonView && !photonView.IsMine)
                return;
                
            // Buscar héroes en el área
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, radius, targetLayers);
            
            foreach (Collider collider in hitColliders)
            {
                // Verificar si es un héroe
                HeroBase hero = collider.GetComponent<HeroBase>();
                
                if (hero != null)
                {
                    // Verificar si aplicamos a aliados o a uno mismo
                    bool isSelf = (hero == caster);
                    bool isAlly = !isSelf && (hero.teamId == caster.teamId);
                    
                    if ((isSelf && applyToSelf) || (isAlly && applyToAllies) || (!isAlly && !applyToAllies))
                    {
                        ApplyBuff(hero);
                    }
                }
            }
        }
        
        /// <summary>
        /// Aplica el buff a un héroe específico
        /// </summary>
        private void ApplyBuff(HeroBase hero)
        {
            // Verificar que no tenga ya un buff de este tipo aplicado
            if (HasActiveBuff(hero))
                return;
                
            // Valor original de la estadística que vamos a modificar
            float originalValue = GetStatValue(hero, buffType);
            
            // Aplicar el buff
            float newValue;
            if (isPercentage)
            {
                // Si es porcentaje, calculamos el incremento
                newValue = originalValue * (1 + buffValue / 100f);
            }
            else
            {
                // Si es valor absoluto, sumamos directamente
                newValue = originalValue + buffValue;
            }
            
            // Establecer el nuevo valor
            SetStatValue(hero, buffType, newValue);
            
            // Crear efecto visual si existe
            GameObject visualEffect = null;
            if (buffVisualPrefab != null)
            {
                // Posición para el efecto
                Transform attachPoint = hero.transform;
                
                // Si queremos adjuntar a un hueso específico
                if (attachToBone && hero.animator != null)
                {
                    Transform boneTransform = FindBoneRecursive(hero.animator.transform, boneName);
                    if (boneTransform != null)
                    {
                        attachPoint = boneTransform;
                    }
                }
                
                // Instanciar efecto visual
                visualEffect = Instantiate(buffVisualPrefab, attachPoint.position, Quaternion.identity);
                
                // Adjuntar al punto específico
                visualEffect.transform.SetParent(attachPoint);
            }
            
            // Registrar la instancia del buff
            appliedBuffs.Add(new BuffInstance(hero, visualEffect, originalValue, duration));
            
            // Llamar a funciones específicas según el tipo de buff
            switch (buffType)
            {
                case BuffType.MoveSpeed:
                    // Actualizar NavMeshAgent si tiene
                    HeroMovementController movement = hero.GetComponent<HeroMovementController>();
                    if (movement != null && movement.navAgent != null)
                    {
                        movement.navAgent.speed = newValue;
                    }
                    break;
                // Otros casos específicos se pueden agregar aquí
            }
        }
        
        /// <summary>
        /// Remueve un buff aplicado previamente
        /// </summary>
        private void RemoveBuff(BuffInstance buff)
        {
            // Restaurar el valor original
            SetStatValue(buff.target, buffType, buff.originalValue);
            
            // Destruir efecto visual si existe
            if (buff.visualEffect != null)
            {
                Destroy(buff.visualEffect);
            }
            
            // Actualizar componentes específicos según el tipo de buff
            switch (buffType)
            {
                case BuffType.MoveSpeed:
                    // Actualizar NavMeshAgent si tiene
                    HeroMovementController movement = buff.target.GetComponent<HeroMovementController>();
                    if (movement != null && movement.navAgent != null)
                    {
                        movement.navAgent.speed = buff.originalValue;
                    }
                    break;
                // Otros casos específicos se pueden agregar aquí
            }
        }
        
        /// <summary>
        /// Verifica si un héroe ya tiene un buff activo de este tipo
        /// </summary>
        private bool HasActiveBuff(HeroBase hero)
        {
            foreach (BuffInstance buff in appliedBuffs)
            {
                if (buff.target == hero)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Obtiene el valor actual de una estadística
        /// </summary>
        private float GetStatValue(HeroBase hero, BuffType statType)
        {
            switch (statType)
            {
                case BuffType.AttackDamage:
                    return hero.attackDamage;
                case BuffType.AttackSpeed:
                    return hero.attackSpeed;
                case BuffType.MoveSpeed:
                    return hero.moveSpeed;
                case BuffType.Armor:
                    return hero.armor;
                case BuffType.MagicResistance:
                    return hero.magicResistance;
                // Para Health/Mana Regen habría que añadir esos campos a HeroBase
                default:
                    return 0f;
            }
        }
        
        /// <summary>
        /// Establece un nuevo valor para una estadística
        /// </summary>
        private void SetStatValue(HeroBase hero, BuffType statType, float value)
        {
            switch (statType)
            {
                case BuffType.AttackDamage:
                    hero.attackDamage = value;
                    break;
                case BuffType.AttackSpeed:
                    hero.attackSpeed = value;
                    break;
                case BuffType.MoveSpeed:
                    hero.moveSpeed = value;
                    break;
                case BuffType.Armor:
                    hero.armor = value;
                    break;
                case BuffType.MagicResistance:
                    hero.magicResistance = value;
                    break;
                // Para Health/Mana Regen habría que añadir esos campos a HeroBase
            }
        }
        
        /// <summary>
        /// Busca un hueso por nombre de forma recursiva
        /// </summary>
        private Transform FindBoneRecursive(Transform parent, string boneName)
        {
            // Verificar si este es el hueso que buscamos
            if (parent.name.ToLower().Contains(boneName.ToLower()))
            {
                return parent;
            }
            
            // Buscar en todos los hijos
            foreach (Transform child in parent)
            {
                Transform found = FindBoneRecursive(child, boneName);
                if (found != null)
                {
                    return found;
                }
            }
            
            return null;
        }
        
        protected override void DestroyAbility()
        {
            // Eliminar todos los buffs activos antes de destruir
            for (int i = appliedBuffs.Count - 1; i >= 0; i--)
            {
                RemoveBuff(appliedBuffs[i]);
            }
            
            appliedBuffs.Clear();
            
            // Continuar con la destrucción normal
            base.DestroyAbility();
        }
        
        // Visualización en el editor
        void OnDrawGizmos()
        {
            if (radius > 0)
            {
                // Visualizar el radio de efecto
                Gizmos.color = applyToAllies ? Color.green : Color.red;
                Gizmos.DrawWireSphere(transform.position, radius);
            }
        }
    }
}