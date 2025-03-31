using UnityEngine;
using Photon.Pun;

public class KillFeedSoundManager : MonoBehaviourPunCallbacks
{
    [Header("Sonidos de Multi-Kills")]
    [SerializeField] private AudioClip killSound;
    [SerializeField] private AudioClip firstBloodSound;
    [SerializeField] private AudioClip doubleKillSound;
    [SerializeField] private AudioClip tripleKillSound;
    [SerializeField] private AudioClip quadraKillSound;
    [SerializeField] private AudioClip pentaKillSound;
    
    [Header("Sonidos de Kill Streaks")]
    [SerializeField] private AudioClip killStreakSound;
    [SerializeField] private AudioClip dominatingSound;
    [SerializeField] private AudioClip unstoppableSound;
    [SerializeField] private AudioClip godlikeSound;
    [SerializeField] private AudioClip legendarySound;
    
    [Header("Configuración de Audio")]
    [SerializeField] private float killVolume = 0.7f;
    [SerializeField] private float multiKillVolume = 0.8f;
    [SerializeField] private float firstBloodVolume = 1f;
    [SerializeField] private float streakVolume = 0.9f;
    
    private AudioSource audioSource;
    private static KillFeedSoundManager instance;
    
    public static KillFeedSoundManager Instance
    {
        get { return instance; }
    }
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Obtener o crear el AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }
    
    public void PlayKillSound()
    {
        if (killSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(killSound, killVolume);
        }
    }
    
    public void PlayFirstBloodSound()
    {
        if (firstBloodSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(firstBloodSound, firstBloodVolume);
        }
    }
    
    public void PlayMultiKillSound(int killCount)
    {
        if (audioSource == null) return;
        
        AudioClip soundToPlay = null;
        float volume = multiKillVolume;
        
        switch (killCount)
        {
            case 2:
                soundToPlay = doubleKillSound;
                break;
            case 3:
                soundToPlay = tripleKillSound;
                break;
            case 4:
                soundToPlay = quadraKillSound;
                break;
            case 5:
                soundToPlay = pentaKillSound;
                break;
        }
        
        if (soundToPlay != null)
        {
            audioSource.PlayOneShot(soundToPlay, volume);
        }
    }
    
    public void PlayKillStreakSound(int streakCount)
    {
        if (audioSource == null) return;
        
        AudioClip soundToPlay = null;
        float volume = streakVolume;
        
        if (streakCount >= 5)
        {
            soundToPlay = killStreakSound;
        }
        if (streakCount >= 10)
        {
            soundToPlay = dominatingSound;
        }
        if (streakCount >= 15)
        {
            soundToPlay = unstoppableSound;
        }
        if (streakCount >= 20)
        {
            soundToPlay = godlikeSound;
        }
        if (streakCount >= 25)
        {
            soundToPlay = legendarySound;
        }
        
        if (soundToPlay != null)
        {
            audioSource.PlayOneShot(soundToPlay, volume);
        }
    }
} 