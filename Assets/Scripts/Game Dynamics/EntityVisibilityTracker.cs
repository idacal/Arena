using UnityEngine;

public class EntityVisibilityTracker : MonoBehaviour
{
    public bool IsInBush { get; private set; } = false;
    public BushVisibility CurrentBush { get; private set; } = null;

    public void EnterBush(BushVisibility bush)
    {
        IsInBush = true;
        CurrentBush = bush;
        // Debug.Log(gameObject.name + " entró en el arbusto: " + bush.gameObject.name);
    }

    public void ExitBush(BushVisibility bush)
    {
        // Solo salir si es el arbusto actual (evita problemas si tocas dos a la vez)
        if (CurrentBush == bush)
        {
            IsInBush = false;
            CurrentBush = null;
            // Debug.Log(gameObject.name + " salió del arbusto: " + bush.gameObject.name);
        }
    }
}

