using UnityEngine;
using DG.Tweening;

public class DOTweenInitializer : MonoBehaviour
{
    private static DOTweenInitializer instance;
    
    public static DOTweenInitializer Instance
    {
        get { return instance; }
    }
    
    private void Awake()
    {
        // Implementar Singleton
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
        
        // Configurar DOTween
        DOTween.SetTweensCapacity(200, 125); // Ajusta estos números según tus necesidades
        
        // Configurar el modo de actualización
        DOTween.defaultTimeScaleIndependent = true;
        
        // Configurar el modo de reciclaje
        DOTween.defaultRecyclable = true;
        
        // Configurar el modo de auto-kill
        DOTween.defaultAutoKill = true;
        
        // Configurar el modo de auto-play
        DOTween.defaultAutoPlay = AutoPlay.None;
    }
    
    private void OnDestroy()
    {
        // Limpiar todas las tweens al destruir el objeto
        DOTween.Clear();
    }
} 