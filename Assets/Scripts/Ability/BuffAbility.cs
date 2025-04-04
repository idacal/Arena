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
        public float buffAmount = 50f;                       // Valor de la mejora
        public bool isPercentage = false;                   // Si es un valor porcentual
        public float buffDuration = 5f;                         // Duración del buff
        public bool applyToAllies = true;                   // Si se aplica a aliados
        public bool applyToSelf = true;                     // Si se aplica al lanzador
        public float radius = 5f;                           // Radio de efecto (0 = solo al objetivo/lanzador)
        public LayerMask targetLayers;                      // Capas afectadas
        
        [Header("Visual Effects")]
        public GameObject buffEffectPrefab;                 // Efecto visual temporal para mostrar en el objetivo
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
        /// Aplica los efectos del buff a un héroe específico
        /// </summary>
        protected virtual void ApplyBuff(HeroBase hero)
        {
            if (hero == null) return;
            
            // Si ya tiene un buff activo, primero quitarlo
            RemoveExistingBuff(hero);
            
            // Obtener el valor original de la estadística
            float originalValue = GetStatValue(hero, buffType);
            
            // Calcular el nuevo valor con el porcentaje o valor plano
            float newValue;
            if (isPercentage)
            {
                newValue = originalValue * (1 + (buffAmount / 100f));
            }
            else
            {
                newValue = originalValue + buffAmount;
            }
            
            // Crear efecto visual si hay prefab
            GameObject vfx = null;
            if (buffEffectPrefab != null)
            {
                vfx = Instantiate(buffEffectPrefab, hero.transform.position, Quaternion.identity);
                vfx.transform.SetParent(hero.transform);
                
                // Configurar duración del efecto
                Destroy(vfx, buffDuration);
            }
            
            // Registrar buff aplicado
            BuffInstance newBuff = new BuffInstance(hero, vfx, originalValue, buffDuration);
            appliedBuffs.Add(newBuff);
            
            // NUEVO: Usar el sistema de modificadores en lugar de establecer el valor directamente
            // Esto maneja la propagación a NavMeshAgent y UI automáticamente
            string statName = GetStatName(buffType);
            if (!string.IsNullOrEmpty(statName))
            {
                hero.ApplyStatModifier(statName, this.GetType().Name, isPercentage, 
                                      isPercentage ? buffAmount / 100f : buffAmount, 
                                      buffDuration);
            }
            else
            {
                // Fallback al comportamiento anterior
                SetStatValue(hero, buffType, newValue);
                
                // Actualizar componentes específicos según el tipo de buff
                UpdateSpecificComponents(hero, buffType, newValue);
            }
            
            // Debug
            Debug.Log($"[BuffAbility] Buff aplicado a {hero.heroName}: {buffType} {(isPercentage ? "+" + buffAmount + "%" : "+" + buffAmount)} durante {buffDuration}s");
        }
        
        /// <summary>
        /// Obtiene el nombre de la estadística para el sistema de modificadores
        /// </summary>
        private string GetStatName(BuffType type)
        {
            switch (type)
            {
                case BuffType.AttackDamage: return "AttackDamage";
                case BuffType.AttackSpeed: return "AttackSpeed";
                case BuffType.MoveSpeed: return "MovementSpeed";
                case BuffType.Armor: return "Armor";
                case BuffType.MagicResistance: return "MagicResistance";
                case BuffType.HealthRegen: return "HealthRegen";
                case BuffType.ManaRegen: return "ManaRegen";
                default: return "";
            }
        }
        
        /// <summary>
        /// Remueve un buff aplicado previamente
        /// </summary>
        private void RemoveBuff(BuffInstance buff)
        {
            // NUEVO: Usar el sistema de modificadores para eliminar
            string statName = GetStatName(buffType);
            if (!string.IsNullOrEmpty(statName) && buff.target != null)
            {
                buff.target.RemoveStatModifiersBySource(statName, this.GetType().Name);
            }
            else if (buff.target != null)
            {
                // Fallback al comportamiento anterior
                SetStatValue(buff.target, buffType, buff.originalValue);
                
                // Actualizar componentes específicos según el tipo de buff
                UpdateSpecificComponents(buff.target, buffType, buff.originalValue);
            }
            
            // Destruir efecto visual si existe
            if (buff.visualEffect != null)
            {
                Destroy(buff.visualEffect);
            }
            
            // Debug
            if (buff.target != null)
            {
                Debug.Log($"[BuffAbility] Buff eliminado de {buff.target.heroName}: {buffType}");
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
        
        /// <summary>
        /// Actualiza componentes específicos según el tipo de buff
        /// Esto es un método de fallback para casos que no utilicen el sistema de modificadores
        /// </summary>
        private void UpdateSpecificComponents(HeroBase hero, BuffType buffType, float newValue)
        {
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
                    
                case BuffType.AttackSpeed:
                    // Podríamos necesitar actualizar animadores u otros componentes
                    break;
                    
                // Otros casos específicos se pueden agregar aquí
            }
        }
        
        /// <summary>
        /// Elimina un buff existente en el héroe si ya hay uno aplicado
        /// </summary>
        private void RemoveExistingBuff(HeroBase hero)
        {
            // Buscar si el héroe ya tiene un buff de este tipo
            for (int i = appliedBuffs.Count - 1; i >= 0; i--)
            {
                if (appliedBuffs[i].target == hero)
                {
                    // Remover el buff anterior
                    RemoveBuff(appliedBuffs[i]);
                    appliedBuffs.RemoveAt(i);
                    break;
                }
            }
        }
    }
}