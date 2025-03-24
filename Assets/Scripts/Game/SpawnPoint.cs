using UnityEngine;

/// <summary>
/// Define un punto de aparición para los héroes, con un ID de equipo
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    public int teamID;  // 0 = Rojo, 1 = Azul
    
    private void OnDrawGizmos()
    {
        // Dibujar un gizmo para visualizar el punto de aparición en el editor
        Gizmos.color = (teamID == 0) ? Color.red : Color.blue;
        Gizmos.DrawSphere(transform.position, 1f);
        Gizmos.DrawRay(transform.position, transform.forward * 2f);
    }
} 