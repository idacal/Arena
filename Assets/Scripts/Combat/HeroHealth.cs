using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Pun.Demo.Asteroids;

/// <summary>
/// Clase que maneja la salud y el daño de los héroes
/// </summary>
[RequireComponent(typeof(HeroBase))]
public class HeroHealth : MonoBehaviourPun, IPunObservable
{
    [Header("Configuración de Salud")]
    public float healthRegenRate = 1f; // Regeneración por segundo
    public float respawnTime = 5f;
    
    [Header("UI")]
    public Slider healthSlider;
    public GameObject damageTextPrefab;
    public Transform floatingTextAnchor;
    
    [Header("Efectos")]
    public GameObject deathEffectPrefab;
    public AudioClip damageSound;
    public AudioClip deathSound;
    
    // Eventos
    public event Action<float, int> OnDamageTaken;
    public event Action<int> OnHeroDeath;
    public event Action OnHeroRespawn;
    
    // Referencias internas
    private HeroBase heroBase;
    private AudioSource audioSource;
    private bool isDead = false;
    private float lastDamageTime = 0f;
    private float damageImmunityTime = 0.1f; // Para evitar daño múltiple demasiado rápido
    
    private void Awake()
    {
        heroBase = GetComponent<HeroBase>();
        audioSource = GetComponent<AudioSource>();
        
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1.0f; // 3D sound
            audioSource.minDistance = 5.0f;
            audioSource.maxDistance = 20.0f;
        }
        
        if (floatingTextAnchor == null)
        {
            GameObject anchor = new GameObject("FloatingTextAnchor");
            anchor.transform.SetParent(transform);
            anchor.transform.localPosition = new Vector3(0, 2f, 0);
            floatingTextAnchor = anchor.transform;
        }
        
        // Inicializar salud usando los valores de HeroBase
        UpdateHealthUI();
    }
    
    private void Update()
    {
        if (!photonView.IsMine) return;
        if (isDead) return;
        
        // Regeneración de salud
        if (heroBase.CurrentHealth < heroBase.MaxHealth)
        {
            // Aplicamos la regeneración de salud a través de HeroBase
            heroBase.Heal(healthRegenRate * Time.deltaTime);
            UpdateHealthUI();
        }
    }
    
    public void TakeDamage(float amount, int attackerActorNumber)
    {
        // Evitar daño si está muerto
        if (isDead) return;
        
        // Evitar daño demasiado frecuente
        if (Time.time - lastDamageTime < damageImmunityTime) return;
        lastDamageTime = Time.time;
        
        if (photonView.IsMine)
        {
            ApplyDamage(amount, attackerActorNumber);
        }
        else if (PhotonNetwork.IsConnected)
        {
            // Solo el dueño del personaje puede aplicar el daño real
            photonView.RPC("RPC_TakeDamage", RpcTarget.MasterClient, amount, attackerActorNumber);
        }
    }
    
    private void ApplyDamage(float amount, int attackerActorNumber)
    {
        // Buscar el atacante si está disponible
        HeroBase attacker = null;
        PhotonView[] photonViews = FindObjectsOfType<PhotonView>();
        foreach (PhotonView view in photonViews)
        {
            if (view.Owner != null && view.Owner.ActorNumber == attackerActorNumber)
            {
                attacker = view.GetComponent<HeroBase>();
                if (attacker != null) break;
            }
        }
        
        // Aplicar daño usando HeroBase
        if (attacker != null)
        {
            heroBase.TakeDamage(amount, attacker);
        }
        else
        {
            // Si no encontramos al atacante, aplicamos daño directamente
            heroBase.ApplyDamageLocally(amount);
        }
        
        // Mostrar efecto de daño
        if (damageTextPrefab != null && floatingTextAnchor != null)
        {
            // Instanciar texto flotante
            GameObject damageText = Instantiate(damageTextPrefab, floatingTextAnchor.position, Quaternion.identity, floatingTextAnchor);
            damageText.GetComponent<TextMesh>().text = Mathf.RoundToInt(amount).ToString();
            Destroy(damageText, 1.5f);
        }
        
        // Reproducir sonido de daño
        if (damageSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(damageSound);
        }
        
        // Actualizar UI
        UpdateHealthUI();
        
        // Notificar daño
        OnDamageTaken?.Invoke(amount, attackerActorNumber);
        
        // Comprobar si ha muerto
        if (heroBase.CurrentHealth <= 0 && !isDead)
        {
            Die(attackerActorNumber);
        }
        
        // Sincronizar con otros clientes
        if (PhotonNetwork.IsConnected)
        {
            photonView.RPC("RPC_SyncHealth", RpcTarget.Others, heroBase.CurrentHealth, isDead);
        }
    }
    
    private void Die(int killerActorNumber)
    {
        if (isDead) return;
        
        isDead = true;
        
        // Notificar muerte
        OnHeroDeath?.Invoke(killerActorNumber);
        
        // Reproducir sonido de muerte
        if (deathSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(deathSound);
        }
        
        // Mostrar efecto de muerte
        if (deathEffectPrefab != null)
        {
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Instantiate(deathEffectPrefab.name, transform.position, transform.rotation);
            }
            else
            {
                Instantiate(deathEffectPrefab, transform.position, transform.rotation);
            }
        }
        
        // Desactivar temporalmente el héroe
        if (photonView.IsMine)
        {
            // Desactivar movimiento y habilidades
            HeroMovementController movement = GetComponent<HeroMovementController>();
            if (movement) movement.enabled = false;
            
            NetworkAbilityHelper abilityHelper = GetComponent<NetworkAbilityHelper>();
            if (abilityHelper) abilityHelper.enabled = false;
            
            // Desactivar colisión
            Collider heroCollider = GetComponent<Collider>();
            if (heroCollider) heroCollider.enabled = false;
            
            // Hacer transparente el modelo
            MakeTransparent(true);
            
            // Iniciar respawn
            StartCoroutine(RespawnCoroutine());
        }
    }
    
    private IEnumerator RespawnCoroutine()
    {
        // Esperar tiempo de respawn
        yield return new WaitForSeconds(respawnTime);
        
        // Respawn
        Respawn();
    }
    
    private void Respawn()
    {
        if (!isDead) return;
        
        // Restaurar salud utilizando HeroBase
        isDead = false;
        
        // Invocar método Respawn de HeroBase
        heroBase.Heal(heroBase.MaxHealth);
        
        // Reubicar en el punto de respawn del equipo
        if (heroBase != null)
        {
            // Buscar punto de respawn basado en el equipo
            GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
            foreach (GameObject spawnPoint in spawnPoints)
            {
                SpawnPoint sp = spawnPoint.GetComponent<SpawnPoint>();
                if (sp != null && sp.teamID == heroBase.teamId)
                {
                    transform.position = spawnPoint.transform.position;
                    transform.rotation = spawnPoint.transform.rotation;
                    break;
                }
            }
        }
        
        // Restaurar componentes
        HeroMovementController movement = GetComponent<HeroMovementController>();
        if (movement) movement.enabled = true;
        
        NetworkAbilityHelper abilityHelper = GetComponent<NetworkAbilityHelper>();
        if (abilityHelper) abilityHelper.enabled = true;
        
        Collider heroCollider = GetComponent<Collider>();
        if (heroCollider) heroCollider.enabled = true;
        
        // Restaurar visibilidad
        MakeTransparent(false);
        
        // Actualizar UI
        UpdateHealthUI();
        
        // Notificar respawn
        OnHeroRespawn?.Invoke();
        
        // Sincronizar con otros clientes
        if (PhotonNetwork.IsConnected)
        {
            photonView.RPC("RPC_SyncHealth", RpcTarget.Others, heroBase.CurrentHealth, isDead);
            photonView.RPC("RPC_OnRespawn", RpcTarget.Others);
        }
    }
    
    private void MakeTransparent(bool transparent)
    {
        // Cambiar la transparencia del modelo
        SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer renderer in renderers)
        {
            Color color = renderer.material.color;
            color.a = transparent ? 0.3f : 1.0f;
            renderer.material.color = color;
            
            // Ajustar modo de renderizado
            if (transparent)
            {
                renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                renderer.material.SetInt("_ZWrite", 0);
                renderer.material.DisableKeyword("_ALPHATEST_ON");
                renderer.material.EnableKeyword("_ALPHABLEND_ON");
                renderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                renderer.material.renderQueue = 3000;
            }
            else
            {
                renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                renderer.material.SetInt("_ZWrite", 1);
                renderer.material.DisableKeyword("_ALPHATEST_ON");
                renderer.material.DisableKeyword("_ALPHABLEND_ON");
                renderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                renderer.material.renderQueue = -1;
            }
        }
    }
    
    private void UpdateHealthUI()
    {
        if (healthSlider != null && heroBase != null)
        {
            healthSlider.maxValue = heroBase.MaxHealth;
            healthSlider.value = heroBase.CurrentHealth;
        }
    }
    
    // RPC para daño
    [PunRPC]
    private void RPC_TakeDamage(float amount, int attackerActorNumber)
    {
        // Solo el master procesa el daño real para evitar trampas
        if (PhotonNetwork.IsMasterClient)
        {
            ApplyDamage(amount, attackerActorNumber);
        }
    }
    
    // RPC para sincronizar salud
    [PunRPC]
    private void RPC_SyncHealth(float health, bool dead)
    {
        if (!photonView.IsMine) // Solo actualizar remotos
        {
            isDead = dead;
            UpdateHealthUI();
            
            // Si estaba muerto y ahora no, o viceversa, actualizar
            if (isDead)
            {
                // Desactivar elementos visuales
                Collider heroCollider = GetComponent<Collider>();
                if (heroCollider) heroCollider.enabled = false;
                
                MakeTransparent(true);
            }
        }
    }
    
    // RPC para respawn
    [PunRPC]
    private void RPC_OnRespawn()
    {
        if (!photonView.IsMine) // Solo actualizar remotos
        {
            Collider heroCollider = GetComponent<Collider>();
            if (heroCollider) heroCollider.enabled = true;
            
            MakeTransparent(false);
        }
    }
    
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Enviando datos
            stream.SendNext(isDead);
        }
        else
        {
            // Recibiendo datos
            isDead = (bool)stream.ReceiveNext();
            
            UpdateHealthUI();
        }
    }
    
    // Métodos públicos para otros scripts
    
    public bool IsDead()
    {
        return isDead || heroBase.IsDead;
    }
    
    public float GetHealthPercentage()
    {
        return heroBase.CurrentHealth / heroBase.MaxHealth;
    }
    
    public void Heal(float amount)
    {
        if (isDead) return;
        
        if (photonView.IsMine)
        {
            heroBase.Heal(amount);
            UpdateHealthUI();
            
            if (PhotonNetwork.IsConnected)
            {
                photonView.RPC("RPC_SyncHealth", RpcTarget.Others, heroBase.CurrentHealth, isDead);
            }
        }
    }
}