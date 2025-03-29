using UnityEngine;
using Photon.Pun;

public class UnitVisibility : MonoBehaviourPunCallbacks
{
    private FogOfWarController fogController;
    private int currentTeam = -1;
    
    public void Initialize(int teamID)
    {
        currentTeam = teamID;
        SetupTeamLayer();
    }
    
    private void SetupTeamLayer()
    {
        if (currentTeam == -1) return;
        
        // Asignar la capa correspondiente según el equipo
        string layerName = currentTeam == 0 ? "RedTeam" : "BlueTeam";
        int layerIndex = LayerMask.NameToLayer(layerName);
        
        if (layerIndex == -1)
        {
            Debug.LogError($"La capa {layerName} no existe! Asegúrate de crear las capas RedTeam y BlueTeam en Unity.");
            return;
        }
        
        gameObject.layer = layerIndex;
    }
    
    private void Start()
    {
        if (!PhotonNetwork.IsConnected) return;
        
        fogController = FindObjectOfType<FogOfWarController>();
        if (fogController == null)
        {
            Debug.LogError("No se encontró el FogOfWarController en la escena!");
            return;
        }
        
        // Si el objeto tiene un PhotonView, obtener el equipo del propietario
        if (photonView != null && photonView.Owner != null)
        {
            if (photonView.Owner.CustomProperties.TryGetValue("Team", out object teamObj))
            {
                Initialize((int)teamObj);
            }
        }
    }
} 