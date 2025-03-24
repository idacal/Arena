using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

/// <summary>
/// Gestiona la configuración global del juego y su inicialización
/// </summary>
public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance { get; private set; }
    
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Inicializar sistemas
        InitializeGame();
    }
    
    private void InitializeGame()
    {
        // Validar que los layers estén configurados correctamente
        if (!LayerManager.ValidateLayersAndTags())
        {
            Debug.LogError("Los layers y tags necesarios no están configurados correctamente. " +
                "Por favor, añade los layers 'Player' y 'Projectile' y los tags 'RedTeam' y 'BlueTeam' en " +
                "Edit > Project Settings > Tags & Layers");
        }
        
        // Configurar la matriz de colisiones para los projectiles
        ConfigureCollisionMatrix();
        
        Debug.Log("GameManager inicializado correctamente");
    }
    
    private void ConfigureCollisionMatrix()
    {
        // Nota: La matriz de colisiones debe configurarse manualmente en Unity
        // ya que no se puede modificar por código, pero podemos validar que esté correcta
        
        Debug.Log("Recuerda configurar la matriz de colisiones en Edit > Project Settings > Physics");
        Debug.Log("Asegúrate que Player pueda colisionar consigo mismo");
        Debug.Log("Asegúrate que Projectile pueda colisionar con Player");
    }
    
    // Método público para reiniciar un juego
    public void RestartGame()
    {
        // Reiniciar la escena actual
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
} 