using UnityEngine;
using Photon.Pun;

namespace Photon.Pun.Demo.Asteroids
{
    /// <summary>
    /// Clase base para todas las habilidades del juego
    /// </summary>
    public abstract class AbilityBase : MonoBehaviourPun
    {
        [Header("Base Settings")]
        public float lifetime = 5f;          // Tiempo de vida de la habilidad
        public bool destroyOnImpact = true;  // Si se debe destruir al impactar
        
        [Header("Damage Settings")]
        public float baseDamage = 0f;        // Daño base de la habilidad
        public bool isMagicDamage = false;   // Si es daño mágico en lugar de físico
        public float effectDuration = 0f;    // Duración de efectos adicionales

        [Header("Effects")]
        public GameObject impactEffectPrefab; // Prefab para efectos de impacto

        [Header("Audio")]
        public AudioClip abilitySound;       // Sonido principal de la habilidad
        public AudioClip impactSound;        // Sonido de impacto
        [Range(0f, 1f)]
        public float volumeMultiplier = 1f;  // Multiplicador de volumen
        [Range(0f, 1f)]
        public float spatialBlend = 1f;      // Mezcla espacial (0 = 2D, 1 = 3D)
        
        // Referencias de componentes
        protected HeroBase caster;           // Referencia al héroe que lanza la habilidad
        protected bool isInitialized = false; // Flag para controlar inicialización
        
        // Cache del AudioSource
        protected AudioSource _audioSource;
        protected AudioSource audioSource 
        {
            get 
            {
                if (_audioSource == null)
                {
                    _audioSource = GetComponent<AudioSource>();
                    if (_audioSource == null)
                    {
                        _audioSource = gameObject.AddComponent<AudioSource>();
                        _audioSource.spatialBlend = spatialBlend;
                        _audioSource.minDistance = 2.0f;
                        _audioSource.maxDistance = 20.0f;
                        _audioSource.volume = volumeMultiplier;
                    }
                }
                return _audioSource;
            }
        }
        
        /// <summary>
        /// Inicialización común para todas las habilidades
        /// </summary>
        /// <param name="caster">Héroe que lanza la habilidad</param>
        public virtual void Initialize(HeroBase caster)
        {
            if (isInitialized) return;
            
            this.caster = caster;
            isInitialized = true;
            
            // Programar destrucción automática después del tiempo de vida
            Invoke("DestroyAbility", lifetime);
            
            // Llamar a la inicialización específica
            OnAbilityInitialized();

            // Reproducir sonido inicial de la habilidad
            if (photonView.IsMine)
            {
                PlayAbilitySound();
            }
        }
        
        /// <summary>
        /// Método para ser sobrescrito por las clases derivadas con inicialización específica
        /// </summary>
        protected virtual void OnAbilityInitialized() { }
        
        /// <summary>
        /// Destruye la habilidad
        /// </summary>
        protected virtual void DestroyAbility()
        {
            if (photonView.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// Método para procesar impacto con un héroe
        /// </summary>
        /// <param name="target">Héroe impactado</param>
        protected virtual void ProcessImpact(HeroBase target)
        {
            // Aplicar daño si hay y el caster existe
            if (baseDamage > 0 && caster != null)
            {
                target.TakeDamage(baseDamage, caster, isMagicDamage);
            }
            
            // Reproducir sonido de impacto
            PlayImpactSound();
            
            // Destruir si está configurado para ello
            if (destroyOnImpact)
            {
                DestroyAbility();
            }
        }

        /// <summary>
        /// Reproduce el sonido de la habilidad usando RPC para sincronización
        /// </summary>
        protected virtual void PlayAbilitySound()
        {
            if (abilitySound != null)
            {
                // Reproducir localmente
                PlaySoundLocally(abilitySound);
                
                // Sincronizar con otros clientes
                photonView.RPC("RPC_PlaySound", RpcTarget.Others, 0);
            }
        }
        
        /// <summary>
        /// Reproduce el sonido de impacto de la habilidad usando RPC para sincronización
        /// </summary>
        protected virtual void PlayImpactSound()
        {
            if (impactSound != null)
            {
                // Reproducir localmente
                PlaySoundLocally(impactSound);
                
                // Sincronizar con otros clientes
                photonView.RPC("RPC_PlaySound", RpcTarget.Others, 1);
            }
        }
        
        /// <summary>
        /// Reproduce el sonido de manera local
        /// </summary>
        protected void PlaySoundLocally(AudioClip sound)
        {
            if (sound != null)
            {
                audioSource.PlayOneShot(sound, volumeMultiplier);
                
                Debug.Log($"[AbilityBase] Reproduciendo sonido local: {sound.name} en {gameObject.name}");
            }
        }
        
        [PunRPC]
        protected void RPC_PlaySound(int soundType)
        {
            AudioClip sound = null;
            
            // Determinar qué sonido reproducir
            switch (soundType)
            {
                case 0: // Sonido principal
                    sound = abilitySound;
                    break;
                case 1: // Sonido de impacto
                    sound = impactSound;
                    break;
            }
            
            // Reproducir el sonido localmente
            if (sound != null)
            {
                PlaySoundLocally(sound);
            }
        }
    }
} 