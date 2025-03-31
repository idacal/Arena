using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Pun.Demo.Asteroids;

/// <summary>
/// Administra el spawn de criaturas neutrales y garantiza que estén correctamente configuradas para la sincronización en red
/// </summary>
public class CreepSpawnManager : MonoBehaviourPunCallbacks
{
    public List<Transform> spawnPoints = new List<Transform>();
    public GameObject mushroomPrefab; // Referencia al prefab de la criatura
    public float respawnTime = 60f;
    
    private List<GameObject> spawnedCreeps = new List<GameObject>();
    
    private void Start()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // Solo el Master Client se encarga de spawnear las criaturas
            SpawnAllCreeps();
        }
    }
    
    public override void OnMasterClientSwitched(Photon.Realtime.Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            // Si el Master Client cambia, el nuevo Master Client se encarga de spawnear las criaturas
            SpawnAllCreeps();
        }
    }
    
    private void SpawnAllCreeps()
    {
        Debug.Log("[CreepSpawnManager] Spawning all creeps");
        
        // Limpiar criaturas anteriores
        foreach (GameObject creep in spawnedCreeps)
        {
            if (creep != null)
            {
                PhotonNetwork.Destroy(creep);
            }
        }
        spawnedCreeps.Clear();
        
        // Spawnear nuevas criaturas
        foreach (Transform spawnPoint in spawnPoints)
        {
            GameObject creep = SpawnCreep(spawnPoint.position);
            if (creep != null)
            {
                spawnedCreeps.Add(creep);
            }
        }
    }
    
    public GameObject SpawnCreep(Vector3 position)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return null;
        }
        
        Debug.Log($"[CreepSpawnManager] Spawning creep at position {position}");
        
        // Spawnear la criatura en la red
        GameObject creep = PhotonNetwork.Instantiate(mushroomPrefab.name, position, Quaternion.identity);
        
        // Configurar los componentes de red
        ConfigureNetworking(creep);
        
        return creep;
    }
    
    private void ConfigureNetworking(GameObject creep)
    {
        PhotonView photonView = creep.GetComponent<PhotonView>();
        if (photonView == null)
        {
            Debug.LogError("[CreepSpawnManager] Creep does not have PhotonView component!");
            return;
        }
        
        // Asegurarse de que el componente PhotonTransformView esté presente
        PhotonTransformView photonTransformView = creep.GetComponent<PhotonTransformView>();
        if (photonTransformView == null)
        {
            photonTransformView = creep.AddComponent<PhotonTransformView>();
            Debug.Log("[CreepSpawnManager] Added PhotonTransformView to creep");
        }
        
        // Configurar el PhotonTransformView
        photonTransformView.m_SynchronizePosition = true;
        photonTransformView.m_SynchronizeRotation = true;
        photonTransformView.m_SynchronizeScale = false;
        
        // Configurar el PhotonView para observar el PhotonTransformView
        if (photonView.ObservedComponents == null || photonView.ObservedComponents.Count == 0)
        {
            photonView.ObservedComponents = new List<Component> { photonTransformView };
            photonView.Synchronization = ViewSynchronization.UnreliableOnChange;
            Debug.Log("[CreepSpawnManager] Configured PhotonView to observe PhotonTransformView");
        }
        
        // Asegurarse de que el NeutralCreep está presente
        NeutralCreep neutralCreep = creep.GetComponent<NeutralCreep>();
        if (neutralCreep == null)
        {
            neutralCreep = creep.AddComponent<NeutralCreep>();
            Debug.Log("[CreepSpawnManager] Added NeutralCreep component to creep");
        }
        
        Debug.Log($"[CreepSpawnManager] Creep network configuration complete. PhotonView ViewID: {photonView.ViewID}");
    }
    
    public void OnCreepDeath(NeutralCreep creep)
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            return;
        }
        
        // Cuando una criatura muere, programar su respawn
        StartCoroutine(RespawnCreep(creep.transform.position));
    }
    
    private System.Collections.IEnumerator RespawnCreep(Vector3 position)
    {
        yield return new WaitForSeconds(respawnTime);
        
        if (PhotonNetwork.IsMasterClient)
        {
            GameObject creep = SpawnCreep(position);
            if (creep != null)
            {
                spawnedCreeps.Add(creep);
            }
        }
    }
} 