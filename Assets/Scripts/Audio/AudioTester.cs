using UnityEngine;
using Photon.Pun.Demo.Asteroids;

public class AudioTester : MonoBehaviour
{
    [Header("Sonidos de prueba")]
    public AudioClip testSound;
    public float volume = 1.0f;
    
    // Referencia opcional al héroe
    private HeroBase playerHero;
    
    void Start()
    {
        // Intentar encontrar automáticamente al héroe local
        FindLocalHero();
        
        // Registrar este objeto en el DontDestroyOnLoad
        DontDestroyOnLoad(gameObject);
        
        Debug.Log("[AudioTester] Listo para probar sonidos. Usa las teclas para probar.");
    }
    
    void Update()
    {
        // Tecla T - Probar sonido directo
        if (Input.GetKeyDown(KeyCode.T))
        {
            PlayTestSound();
        }
        
        // Tecla G - Probar sonido de oro del héroe
        if (Input.GetKeyDown(KeyCode.G))
        {
            TestHeroGoldSound();
        }
        
        // Tecla H - Buscar héroe
        if (Input.GetKeyDown(KeyCode.H))
        {
            FindLocalHero();
        }
        
        // Tecla 1 - Usar método 1 para reproducir
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            PlayWithMethod1();
        }
        
        // Tecla 2 - Usar método 2 para reproducir
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            PlayWithMethod2();
        }
        
        // Tecla 3 - Usar método 3 para reproducir
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            PlayWithMethod3();
        }
    }
    
    void FindLocalHero()
    {
        HeroBase[] heroes = FindObjectsOfType<HeroBase>();
        foreach (var hero in heroes)
        {
            if (hero.photonView.IsMine)
            {
                playerHero = hero;
                Debug.Log($"[AudioTester] Encontrado héroe local: {hero.heroName}");
                break;
            }
        }
        
        if (playerHero == null)
        {
            Debug.LogWarning("[AudioTester] No se encontró un héroe local");
        }
    }
    
    void PlayTestSound()
    {
        if (testSound == null)
        {
            Debug.LogError("[AudioTester] No hay sonido de prueba asignado");
            return;
        }
        
        Debug.Log("[AudioTester] Reproduciendo sonido de prueba con varios métodos:");
        
        // Método 1: AudioSource directo
        AudioSource.PlayClipAtPoint(testSound, Camera.main.transform.position, volume);
        Debug.Log("[AudioTester] Método 1: AudioSource.PlayClipAtPoint");
        
        // Método 2: Crear AudioSource temporal
        GameObject tempObj = new GameObject("TempAudio");
        tempObj.transform.position = Camera.main.transform.position;
        AudioSource tempSource = tempObj.AddComponent<AudioSource>();
        tempSource.clip = testSound;
        tempSource.spatialBlend = 0f; // 2D
        tempSource.volume = volume;
        tempSource.Play();
        Destroy(tempObj, testSound.length + 0.5f);
        Debug.Log("[AudioTester] Método 2: GameObject temporal con AudioSource");
        
        // Método 3: Usar el AudioSource del objeto
        AudioSource source = GetComponent<AudioSource>();
        if (source == null)
        {
            source = gameObject.AddComponent<AudioSource>();
        }
        source.spatialBlend = 0f; // 2D
        source.PlayOneShot(testSound, volume);
        Debug.Log("[AudioTester] Método 3: PlayOneShot en AudioSource local");
    }
    
    void TestHeroGoldSound()
    {
        if (playerHero == null)
        {
            Debug.LogError("[AudioTester] No hay héroe asignado para probar sonido de oro");
            FindLocalHero();
            if (playerHero == null) return;
        }
        
        Debug.Log($"[AudioTester] Probando sonidos de oro del héroe {playerHero.heroName}:");
        
        // Verificar si tiene sonidos asignados
        if (playerHero.goldSound == null && 
            playerHero.smallGoldSound == null && 
            playerHero.bigGoldSound == null)
        {
            Debug.LogError("[AudioTester] El héroe no tiene ningún sonido de oro asignado");
            return;
        }
        
        // Usar el método de prueba del héroe
        playerHero.TestGoldSounds();
        
        Debug.Log("[AudioTester] Prueba de sonidos de oro completada");
    }
    
    void PlayWithMethod1()
    {
        if (testSound == null) return;
        AudioSource.PlayClipAtPoint(testSound, Camera.main.transform.position, volume * 2);
        Debug.Log("[AudioTester] Reproduciendo con método 1 (volumen x2)");
    }
    
    void PlayWithMethod2()
    {
        if (testSound == null) return;
        GameObject tempObj = new GameObject("TempAudio_LOUD");
        AudioSource src = tempObj.AddComponent<AudioSource>();
        src.clip = testSound;
        src.volume = volume * 2;
        src.spatialBlend = 0f;
        src.priority = 0;
        src.Play();
        Destroy(tempObj, testSound.length + 0.5f);
        Debug.Log("[AudioTester] Reproduciendo con método 2 (volumen x2)");
    }
    
    void PlayWithMethod3()
    {
        if (playerHero != null && playerHero.goldSound != null)
        {
            playerHero.PlaySound(playerHero.goldSound, 2.0f, true);
            Debug.Log("[AudioTester] Reproduciendo sonido de oro con método 3 (volumen x2)");
        }
        else
        {
            Debug.LogError("[AudioTester] No se puede usar método 3 - héroe o sonido no disponible");
        }
    }
} 