using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;

namespace Photon.Pun.Demo.Asteroids
{
    /// <summary>
    /// Clase base para todos los comportamientos de habilidades
    /// </summary>
    public abstract class AbilityBehaviour : MonoBehaviourPun, IPunObservable
    {
        [Header("Ability Settings")]
        public float lifetime = 5f;          // Duración de la habilidad en segundos
        public bool destroyOnImpact = true;  // Si se destruye al impactar
        
        [Header("Damage/Effect Settings")]
        public float baseDamage = 100f;      // Daño base de la habilidad
        public bool isMagicDamage = true;    // Si es daño mágico o físico
        public float effectDuration = 0f;    // Duración de efectos adicionales
        
        [Header("References")]
        public GameObject impactEffectPrefab; // Prefab para efecto de impacto
        
        // Referencias protegidas
        protected HeroBase caster;           // Héroe que lanzó la habilidad
        protected HeroAbility abilityData;   // Datos de la habilidad
        protected int casterId;              // ID del jugador que la lanzó
        protected float elapsedTime = 0f;    // Tiempo transcurrido desde la creación
        
        // Lista de objetivos ya impactados para evitar múltiples impactos
        protected List<int> hitTargets = new List<int>();
        
        // Flag para controlar si la destrucción está en curso
        private bool isBeingDestroyed = false;
        
        /// <summary>
        /// Inicializa la habilidad con datos específicos
        /// </summary>
        public virtual void Initialize(HeroBase caster, HeroAbility abilityData, int casterId)
        {
            this.caster = caster;
            this.abilityData = abilityData;
            this.casterId = casterId;
            
            // Si hay datos de habilidad, usar para configurar
            if (abilityData != null)
            {
                baseDamage = abilityData.DamageAmount > 0 ? abilityData.DamageAmount : baseDamage;
                effectDuration = abilityData.Duration > 0 ? abilityData.Duration : effectDuration;
            }
            
            // Inicialización específica según tipo de habilidad
            OnAbilityInitialized();
            
            // Auto-destrucción después de lifetime
            if (lifetime > 0)
            {
                Invoke("DestroyAbility", lifetime);
            }
        }
        
        /// <summary>
        /// Método virtual para inicialización específica en clases derivadas
        /// </summary>
        protected virtual void OnAbilityInitialized()
        {
            // Implementar en clases derivadas
        }
        
        void Update()
        {
            // Incrementar tiempo transcurrido
            elapsedTime += Time.deltaTime;
            
            // Lógica específica de la habilidad
            AbilityUpdate();
        }
        
        /// <summary>
        /// Método para la lógica específica de cada habilidad
        /// </summary>
        protected virtual void AbilityUpdate()
        {
            // Implementar en clases derivadas
        }
        
        /// <summary>
        /// Procesa el impacto con un objetivo
        /// </summary>
        protected virtual void ProcessImpact(HeroBase target)
        {
            // Si ya impactamos a este objetivo y no permitimos múltiples impactos, ignorar
            if (hitTargets.Contains(target.photonView.ViewID))
                return;
                
            // Agregar a la lista de objetivos impactados
            hitTargets.Add(target.photonView.ViewID);
                
            // Aplicar daño si corresponde
            if (baseDamage > 0 && caster != null)
            {
                target.TakeDamage(baseDamage, caster, isMagicDamage);
            }
            
            // Aplicar efectos adicionales específicos
            ApplyEffects(target);
            
            // Crear efecto de impacto si existe
            if (impactEffectPrefab != null)
            {
                // Usar PhotonNetwork.Instantiate si estamos en red
                if (photonView.IsMine)
                {
                    // Intentar obtener ruta relativa a Resources
                    string path = impactEffectPrefab.name;
                    GameObject impactObj = Resources.Load<GameObject>("Effects/" + path);
                    
                    if (impactObj != null)
                    {
                        PhotonNetwork.Instantiate("Effects/" + path, 
                            target.transform.position + Vector3.up, 
                            Quaternion.identity);
                    }
                    else
                    {
                        // Fallback a Instantiate local
                        Instantiate(impactEffectPrefab, 
                            target.transform.position + Vector3.up, 
                            Quaternion.identity);
                    }
                }
            }
            
            // Si debe destruirse al impactar
            if (destroyOnImpact)
            {
                DestroyAbility();
            }
        }
        
        /// <summary>
        /// Aplica efectos adicionales al objetivo
        /// </summary>
        protected virtual void ApplyEffects(HeroBase target)
        {
            // Implementar en clases derivadas (stun, slow, etc.)
        }
        
        /// <summary>
        /// Destruye la habilidad de forma segura
        /// </summary>
        protected virtual void DestroyAbility()
        {
            // Evitar múltiples destrucciones
            if (isBeingDestroyed)
                return;
                
            isBeingDestroyed = true;
            
            // Cancelar cualquier Invoke pendiente
            CancelInvoke();
            
            // Si somos el dueño del objeto, destruirlo en red
            if (photonView.IsMine)
            {
                // Informar sobre la destrucción para debug
                Debug.Log($"[AbilityBehaviour] Destruyendo {gameObject.name} en red. IsMine: {photonView.IsMine}, ViewID: {photonView.ViewID}");
                
                // Usar PhotonNetwork.Destroy para objetos creados con PhotonNetwork.Instantiate
                PhotonNetwork.Destroy(gameObject);
            }
            else
            {
                // Si no somos el dueño, solo liberamos recursos locales
                Debug.Log($"[AbilityBehaviour] Destrucción local de {gameObject.name}. No somos dueños, ViewID: {photonView.ViewID}");
                
                // Podemos desactivar el GameObject para ocultarlo mientras esperamos que el dueño lo destruya
                gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Implementación para sincronización en red (Photon)
        /// </summary>
        public virtual void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // Datos a sincronizar
                stream.SendNext(elapsedTime);
                stream.SendNext(hitTargets.Count);
                
                // Enviar IDs de objetivos impactados
                foreach (int targetId in hitTargets)
                {
                    stream.SendNext(targetId);
                }
                
                // Enviar estado de destrucción
                stream.SendNext(isBeingDestroyed);
            }
            else
            {
                // Leer datos sincronizados
                elapsedTime = (float)stream.ReceiveNext();
                int hitCount = (int)stream.ReceiveNext();
                
                // Limpiar y recrear lista de objetivos impactados
                hitTargets.Clear();
                for (int i = 0; i < hitCount; i++)
                {
                    hitTargets.Add((int)stream.ReceiveNext());
                }
                
                // Leer estado de destrucción
                bool remoteIsBeingDestroyed = (bool)stream.ReceiveNext();
                
                // Si el dueño marcó el objeto para destrucción, ocultarlo localmente
                if (remoteIsBeingDestroyed && !isBeingDestroyed)
                {
                    isBeingDestroyed = true;
                    gameObject.SetActive(false);
                }
            }
        }
    }
}