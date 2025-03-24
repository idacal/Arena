using UnityEngine;
using System.Collections.Generic;
using Photon.Pun;

/// <summary>
/// Gestor personalizado de prefabs para Photon
/// </summary>
public class PhotonPrefabManager : MonoBehaviourPunCallbacks, IPunPrefabPool
{
    [System.Serializable]
    public class PrefabMapping
    {
        public string prefabId;
        public GameObject prefab;
    }

    public static PhotonPrefabManager Instance { get; private set; }

    [Header("Prefabs")]
    [Tooltip("Lista de prefabs que se pueden instanciar en red")]
    public List<PrefabMapping> networkPrefabs = new List<PrefabMapping>();

    private Dictionary<string, GameObject> prefabDictionary = new Dictionary<string, GameObject>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializePrefabPool();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializePrefabPool()
    {
        // Limpiar diccionario
        prefabDictionary.Clear();

        // Registrar todos los prefabs
        foreach (var mapping in networkPrefabs)
        {
            if (mapping.prefab != null && !string.IsNullOrEmpty(mapping.prefabId))
            {
                prefabDictionary[mapping.prefabId] = mapping.prefab;
                Debug.Log($"[PhotonPrefabManager] Registrado prefab: {mapping.prefabId}");
            }
            else
            {
                Debug.LogError($"[PhotonPrefabManager] Prefab inválido o ID vacío en la configuración");
            }
        }

        // Asignar este pool a PhotonNetwork
        PhotonNetwork.PrefabPool = this;
        Debug.Log("[PhotonPrefabManager] Pool de prefabs inicializado");
    }

    // Implementación de IPunPrefabPool
    public GameObject Instantiate(string prefabId, Vector3 position, Quaternion rotation)
    {
        GameObject prefab;
        if (prefabDictionary.TryGetValue(prefabId, out prefab))
        {
            if (prefab == null)
            {
                Debug.LogError($"[PhotonPrefabManager] Prefab '{prefabId}' es null en el diccionario");
                return null;
            }

            GameObject obj = Instantiate(prefab, position, rotation);
            Debug.Log($"[PhotonPrefabManager] Instanciado prefab: {prefabId}");
            return obj;
        }

        Debug.LogError($"[PhotonPrefabManager] Prefab no encontrado: {prefabId}");
        return null;
    }

    public void Destroy(GameObject gameObject)
    {
        Destroy(gameObject);
    }

    // Método de utilidad para obtener el ID de un prefab
    public static string GetPrefabId(GameObject prefab)
    {
        if (Instance == null || prefab == null) return null;

        foreach (var mapping in Instance.networkPrefabs)
        {
            if (mapping.prefab == prefab)
            {
                return mapping.prefabId;
            }
        }

        return null;
    }
} 