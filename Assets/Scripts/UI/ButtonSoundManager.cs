using UnityEngine;

namespace Photon.Pun.Demo.Asteroids
{
    public class ButtonSoundManager : MonoBehaviour
    {
        private static ButtonSoundManager instance;
        public static ButtonSoundManager Instance => instance;

        [Header("Sound Settings")]
        public AudioClip clickSound;
        public float volume = 0.5f;
        
        private AudioSource audioSource;
        
        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void Start()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.volume = volume;
        }
        
        public void PlayClickSound()
        {
            if (clickSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(clickSound);
            }
        }
    }
} 