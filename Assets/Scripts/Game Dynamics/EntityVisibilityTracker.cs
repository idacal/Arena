using UnityEngine;
using Photon.Pun; // Necesario para PhotonView y PunRPC

[RequireComponent(typeof(PhotonView))] // Asegura que haya un PhotonView
public class EntityVisibilityTracker : MonoBehaviourPun // Cambiado a MonoBehaviourPun para acceso fácil a photonView
{
    [Header("Referencias")]
    [Tooltip("Cámara principal usada por este jugador. Busca una en hijos si no se asigna.")]
    [SerializeField] private Camera playerCamera;

    // Campo dummy para el Header
    [Header("Estado")]
    [SerializeField, HideInInspector] private bool _dummyStateHeader; 

    [Tooltip("Indica si la entidad está actualmente dentro de un arbusto.")]
    [field: SerializeField] // Permite ver en Inspector pero mantener setter privado
    public bool IsInBush { get; private set; } = false;

    [Tooltip("Referencia al script del arbusto actual en el que está la entidad.")]
    [field: SerializeField]
    public BushVisibility CurrentBush { get; private set; } = null;

    private int originalCullingMask; // Para guardar la máscara original
    private bool isCameraSetup = false; // Flag para saber si ya configuramos la cámara

    // Añadido para depuración
    public bool showDebugMessages = false;

    // Verificación periódica para asegurar capa correcta
    private float layerCheckInterval = 0.2f; // Reducido de 1.0f a 0.2f para verificar 5 veces por segundo
    private float lastLayerCheckTime = 0f;

    void Start()
    {
        SetupCamera();
    }

    void SetupCamera()
    {
        if (photonView.IsMine) // Solo configurar la cámara para el jugador local
        {
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }

            if (playerCamera != null)
            {
                originalCullingMask = playerCamera.cullingMask;
                isCameraSetup = true;
                LogMessage($"Cámara encontrada y máscara de culling original guardada: {LayerMaskToString(originalCullingMask)}");

                 // IMPORTANTE: Asegurarse que la cámara local SIEMPRE vea su propia capa (Player)
                // y las capas de aliados y neutrales por defecto.
                // La configuración inicial de qué capas ve (aliados/enemigos/neutros)
                // debe hacerse en el prefab de la cámara o en otro script de inicialización.
                // Este script SOLO gestiona la capa HiddenInBush dinámicamente.

                // Asegurarnos que al inicio no vea HiddenInBush (a menos que spawnee dentro?)
                // Por seguridad, lo quitamos al inicio si está presente.
                 if ((playerCamera.cullingMask & (1 << LayerManager.HiddenInBushLayerID)) != 0)
                 {
                     playerCamera.cullingMask &= ~(1 << LayerManager.HiddenInBushLayerID);
                     LogMessage($"Se quitó HiddenInBush de la máscara inicial (estaba presente). Nueva máscara: {LayerMaskToString(playerCamera.cullingMask)}");
                     originalCullingMask = playerCamera.cullingMask; // Actualizar la original guardada
                 }

            }
            else
            {
                Debug.LogError($"EntityVisibilityTracker en {gameObject.name} no pudo encontrar la cámara del jugador.", this);
            }
        }
    }


    public void EnterBush(BushVisibility bush)
    {
        bool wasAlreadyInBush = IsInBush; // Guardar estado anterior
        IsInBush = true;
        CurrentBush = bush;

        // Modificar culling mask SOLO para el jugador local
        if (photonView.IsMine && isCameraSetup && !wasAlreadyInBush) // Solo añadir si no estaba ya en un arbusto
        {
            playerCamera.cullingMask |= (1 << LayerManager.HiddenInBushLayerID);
            LogMessage($"Entró al arbusto. Cámara local ahora ve HiddenInBush. Máscara: {LayerMaskToString(playerCamera.cullingMask)}");
        }
        // LogMessage($"{gameObject.name} entró en el arbusto: {bush.gameObject.name}"); // Log original
    }

    public void ExitBush(BushVisibility bush)
    {
        // Solo procesar si estamos saliendo del arbusto actual
        if (CurrentBush == bush)
        {
            IsInBush = false;
            CurrentBush = null;

            // Modificar culling mask SOLO para el jugador local
            if (photonView.IsMine && isCameraSetup)
            {
                // Restaurar la máscara original (que no tenía HiddenInBush)
                playerCamera.cullingMask = originalCullingMask;
                LogMessage($"Salió del arbusto. Cámara local restaurada a máscara original. Máscara: {LayerMaskToString(playerCamera.cullingMask)}");
                
                // Verificar INMEDIATAMENTE que la capa sea la correcta al salir
                // En lugar de esperar al siguiente intervalo
                VerifyCorrectLayer();
                
                // También verificar nuevamente después de un pequeño intervalo 
                // para evitar posibles condiciones de carrera
                Invoke("VerifyCorrectLayer", 0.05f);
            }
        }
        else if (IsInBush)
        {
             LogMessage($"Intentó salir del arbusto {bush.gameObject.name} pero no era el actual ({CurrentBush?.gameObject.name}). No se cambia estado ni cámara.");
        }
    }

    void Update()
    {
        // Verificar periódicamente que la capa sea correcta (para todos los clientes)
        if (Time.time > lastLayerCheckTime + layerCheckInterval)
        {
            lastLayerCheckTime = Time.time;
            VerifyCorrectLayer();
        }
    }

    /// <summary>
    /// Verifica que la capa del objeto sea consistente con su estado IsInBush
    /// </summary>
    private void VerifyCorrectLayer()
    {
        // Solo verificamos si ya estamos completamente inicializados
        if (!gameObject || !enabled || !photonView || photonView.ViewID <= 0)
            return;

        int currentLayer = gameObject.layer;
        bool isPlayerLayer = currentLayer == LayerManager.PlayerLayerID;
        bool isHiddenLayer = currentLayer == LayerManager.HiddenInBushLayerID;

        // Si está en un arbusto, debería estar en HiddenInBush
        if (IsInBush && !isHiddenLayer)
        {
            if (showDebugMessages)
            {
                Debug.LogWarning($"[EntityVisibilityTracker ({gameObject.name})] ⚠️ La entidad está en un arbusto pero tiene capa {LayerMask.LayerToName(currentLayer)}. Corrigiendo a HiddenInBush.");
            }
            
            // Si estamos en un arbusto pero no en la capa correcta, corregir
            if (photonView.IsMine)
            {
                // Enviar RPC para corregir en todos los clientes
                photonView.RPC("RPC_ForceLayer", RpcTarget.AllBuffered, LayerManager.HiddenInBushLayerID, true);
            }
        }
        // Si NO está en un arbusto, debería estar en Player
        else if (!IsInBush && !isPlayerLayer)
        {
            if (showDebugMessages)
            {
                Debug.LogWarning($"[EntityVisibilityTracker ({gameObject.name})] ⚠️ La entidad NO está en arbusto pero tiene capa {LayerMask.LayerToName(currentLayer)}. Corrigiendo a Player.");
            }
            
            // Si no estamos en un arbusto pero no en la capa Player, corregir
            if (photonView.IsMine)
            {
                // Enviar RPC para corregir en todos los clientes
                photonView.RPC("RPC_ForceLayer", RpcTarget.AllBuffered, LayerManager.PlayerLayerID, false);
            }
        }
    }

    /// <summary>
    /// RPC para forzar una capa, ignorando verificaciones. Más directo que RPC_SetVisibilityLayer.
    /// </summary>
    [PunRPC]
    private void RPC_ForceLayer(int layerId, bool isEnteringBush)
    {
        if (showDebugMessages)
        {
            Debug.LogWarning($"[EntityVisibilityTracker ({gameObject.name})] 🔄 Forzando capa a {LayerMask.LayerToName(layerId)} ({layerId}). IsEnteringBush={isEnteringBush}");
        }
        
        // Aplicar la capa directamente al objeto (sin recursión para mayor velocidad y simplicidad)
        gameObject.layer = layerId;
        
        // Aplicar también a los hijos principales que necesitan la misma visibilidad
        foreach (Transform child in transform)
        {
            if (child != null)
            {
                child.gameObject.layer = layerId;
                // Renderer, MeshFilter, Collider, etc.
                foreach (Transform subChild in child)
                {
                    if (subChild != null)
                        subChild.gameObject.layer = layerId;
                }
            }
        }
    }

    /// <summary>
    /// RPC llamado por BushVisibility para cambiar la capa de esta entidad en todas las instancias.
    /// </summary>
    [PunRPC]
    public void RPC_SetVisibilityLayer(int layerId)
    {
        int currentLayer = gameObject.layer;
        string currentLayerName = LayerMask.LayerToName(currentLayer);
        string newLayerName = LayerMask.LayerToName(layerId);
        
        // Comprobar si la capa es válida
        if (string.IsNullOrEmpty(newLayerName))
        {
            // Este error siempre debe ser visible
            Debug.LogError($"[EntityVisibilityTracker ({gameObject.name})] ❌ RPC_SetVisibilityLayer recibió un ID de capa inválido: {layerId}", this);
            return;
        }

        // Prioridad especial: Si intentamos restaurar a Player desde HiddenInBush, forzar el cambio
        // aunque la capa actual no sea HiddenInBush (por si otro proceso la cambió)
        bool isRestoringFromBush = (layerId == LayerManager.PlayerLayerID && 
                                    (currentLayer == LayerManager.HiddenInBushLayerID || !IsInBush));
        
        // No hacer nada si ya tenemos la capa correcta (y no es el caso especial)
        if (currentLayer == layerId && !isRestoringFromBush)
        {
            if (showDebugMessages)
            {
                Debug.Log($"[EntityVisibilityTracker ({gameObject.name} - {(photonView.IsMine ? "Local" : "Remote")})] ⏩ RPC ignorado: La entidad ya tiene la capa {newLayerName} ({layerId})", this);
            }
            return;
        }

        // Log del cambio de capa (siempre visible si debug está activado)
        if (showDebugMessages)
        {
            if (isRestoringFromBush)
            {
                Debug.Log($"[EntityVisibilityTracker ({gameObject.name} - {(photonView.IsMine ? "Local" : "Remote")})] 🚨 RESTAURANDO PLAYER: Cambiando capa de {currentLayerName} ({currentLayer}) a {newLayerName} ({layerId})", this);
            }
            else
            {
                Debug.Log($"[EntityVisibilityTracker ({gameObject.name} - {(photonView.IsMine ? "Local" : "Remote")})] 📥 RPC recibido: Cambiando capa de {currentLayerName} ({currentLayer}) a {newLayerName} ({layerId})", this);
            }
        }
        
        // Cambiar la capa recursivamente en este GameObject y todos sus hijos
        try 
        {
            // Usar método más directo para caso especial de restauración a Player
            if (isRestoringFromBush)
            {
                // Aplicar la capa directamente al objeto y sus hijos inmediatos 
                // para máxima velocidad y fiabilidad
                gameObject.layer = layerId;
                
                foreach (Transform child in transform)
                {
                    if (child != null)
                    {
                        child.gameObject.layer = layerId;
                        // También a nietos (Renderer, MeshFilter, etc.)
                        foreach (Transform subChild in child)
                        {
                            if (subChild != null)
                                subChild.gameObject.layer = layerId;
                        }
                    }
                }
                
                // También actualizar el estado explícitamente
                IsInBush = false;
                CurrentBush = null;
            }
            else
            {
                // Para otros casos, usar el método recursivo normal
                LayerManager.SetLayerRecursively(transform, layerId);
            }
            
            // Verificación adicional después del cambio
            if (gameObject.layer != layerId && showDebugMessages)
            {
                Debug.LogWarning($"[EntityVisibilityTracker ({gameObject.name})] ⚠️ Verificación fallida: Después de cambiar capa, sigue siendo {LayerMask.LayerToName(gameObject.layer)} ({gameObject.layer}) en lugar de {newLayerName} ({layerId})", this);
            }
            else if (showDebugMessages)
            {
                Debug.Log($"[EntityVisibilityTracker ({gameObject.name})] ✅ Capa cambiada correctamente a {newLayerName}", this);
            }
        }
        catch (System.Exception e)
        {
            // Este error siempre debe ser visible
            Debug.LogError($"[EntityVisibilityTracker ({gameObject.name})] ❌ Error al cambiar capa: {e.Message}", this);
        }
    }


    private void LogMessage(string message)
    {
        // Este LogMessage sigue siendo solo para la instancia local (IsMine)
        // Lo usamos para logs de cámara, EnterBush, ExitBush, etc.
        if (showDebugMessages && photonView.IsMine)
        {
            Debug.Log($"[EntityVisibilityTracker ({gameObject.name})] {message}", this);
        }
    }

     // Helper para debugging: Convierte una máscara de bits a nombres de capas
    private string LayerMaskToString(int mask)
    {
        string layers = "";
        for (int i = 0; i < 32; i++)
        {
            if ((mask & (1 << i)) != 0)
            {
                string layerName = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(layerName))
                {
                    if (!string.IsNullOrEmpty(layers)) layers += ", ";
                    layers += layerName;
                }
            }
        }
        return $"[{layers}]";
    }
}

