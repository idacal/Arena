using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Pun.Demo.Asteroids;

public class TargetingSystem : MonoBehaviourPun
{
    [Header("Configuración")]
    public float targetingRange = 15f;
    public LayerMask targetableLayers;
    public bool autoTargetEnemiesInRange = false;  // Desactivado por defecto
    public float autoTargetInterval = 0.5f;
    
    [Header("Visualización")]
    public GameObject targetIndicatorPrefab;
    public float indicatorHeight = 2.0f;
    public Color allyIndicatorColor = Color.green;
    public Color enemyIndicatorColor = Color.red;
    
    [Header("Cursor")]
    public Texture2D normalCursor;
    public Texture2D enemyCursor;
    public Vector2 cursorHotspot = new Vector2(16, 16);  // Centro del cursor
    
    // Referencias internas
    private Transform cameraTransform;
    private HeroBase heroBase;
    private Transform currentTarget;
    private GameObject targetIndicator;
    private float autoTargetTimer = 0f;
    private PhotonMOBACamera mobaCamera;
    
    // Propiedad para acceder al objetivo actual desde otros scripts
    public Transform CurrentTarget => currentTarget;
    
    private void Awake()
    {
        heroBase = GetComponent<HeroBase>();
        mobaCamera = FindObjectOfType<PhotonMOBACamera>();
        Debug.Log($"[TargetingSystem] Inicializado para {gameObject.name}, heroBase: {(heroBase != null ? "OK" : "NULL")}, mobaCamera: {(mobaCamera != null ? "OK" : "NULL")}");
        
        // Crear indicador de objetivo
        if (targetIndicatorPrefab != null)
        {
            targetIndicator = Instantiate(targetIndicatorPrefab, transform.position, Quaternion.identity);
            targetIndicator.SetActive(false);
            
            // Verificar que tiene el componente TargetIndicatorController
            TargetIndicatorController indicatorController = targetIndicator.GetComponent<TargetIndicatorController>();
            if (indicatorController == null)
            {
                Debug.LogError("[TargetingSystem] El prefab del indicador no tiene el componente TargetIndicatorController");
                return;
            }
            
            // Verificar que tiene el SpriteRenderer
            SpriteRenderer spriteRenderer = targetIndicator.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                Debug.LogError("[TargetingSystem] El prefab del indicador no tiene el componente SpriteRenderer");
                return;
            }
            
            Debug.Log($"[TargetingSystem] Indicador de objetivo creado y configurado correctamente: {targetIndicator.name}");
        }
        else
        {
            Debug.LogError("[TargetingSystem] No se ha asignado un prefab para el indicador de objetivo en el Inspector!");
        }
        
        // Configurar cursor normal por defecto
        if (normalCursor != null)
        {
            Debug.Log($"[TargetingSystem] Configurando cursor normal: {normalCursor.name}, tamaño: {normalCursor.width}x{normalCursor.height}");
            try
            {
                Cursor.SetCursor(normalCursor, cursorHotspot, CursorMode.Auto);
                Debug.Log("[TargetingSystem] Cursor normal configurado correctamente");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[TargetingSystem] Error al configurar cursor normal: {e.Message}");
            }
        }
        else
        {
            Debug.LogError("[TargetingSystem] No se ha asignado la textura normalCursor en el Inspector!");
        }
        
        if (enemyCursor == null)
        {
            Debug.LogError("[TargetingSystem] No se ha asignado la textura enemyCursor en el Inspector!");
        }
    }
    
    private void Start()
    {
        // Intentar encontrar la cámara si no se encontró en Awake
        if (mobaCamera == null)
        {
            mobaCamera = FindObjectOfType<PhotonMOBACamera>();
            if (mobaCamera != null)
            {
                Debug.Log("[TargetingSystem] Cámara encontrada en Start");
            }
            else
            {
                Debug.LogError("[TargetingSystem] No se pudo encontrar la cámara en Start");
            }
        }
    }
    
    private void OnDestroy()
    {
        if (targetIndicator != null)
        {
            Destroy(targetIndicator);
        }
    }
    
    private void Update()
    {
        if (!photonView.IsMine) return;
        
        // Auto-targeting (solo si está activado)
        if (autoTargetEnemiesInRange && currentTarget == null)
        {
            autoTargetTimer -= Time.deltaTime;
            if (autoTargetTimer <= 0)
            {
                autoTargetTimer = autoTargetInterval;
                FindNearestEnemyTarget();
            }
        }
        
        // Actualizar posición del indicador de objetivo
        UpdateTargetIndicator();
        
        // Verificar si el objetivo sigue siendo válido
        ValidateCurrentTarget();
        
        // Actualizar cursor basado en lo que está bajo el mouse
        UpdateCursor();
    }
    
    private void UpdateCursor()
    {
        if (mobaCamera == null)
        {
            // Intentar encontrar la cámara una vez más
            mobaCamera = FindObjectOfType<PhotonMOBACamera>();
            if (mobaCamera == null)
            {
                Debug.LogWarning("[TargetingSystem] mobaCamera es null en UpdateCursor");
                return;
            }
        }
        
        if (normalCursor == null || enemyCursor == null)
        {
            Debug.LogWarning("[TargetingSystem] Texturas de cursor no asignadas en el Inspector");
            return;
        }
        
        Camera camera = mobaCamera.GetComponent<Camera>();
        if (camera == null)
        {
            Debug.LogWarning("[TargetingSystem] No se encontró el componente Camera en mobaCamera");
            return;
        }
        
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, targetableLayers))
        {
            GameObject hitObject = hit.collider.gameObject;
            Debug.Log($"[TargetingSystem] Mouse sobre objeto: {hitObject.name}, tag: {hitObject.tag}");
            
            // Verificar si el objeto es un enemigo
            if (LayerManager.IsEnemy(gameObject, hitObject))
            {
                Debug.Log("[TargetingSystem] Cambiando a cursor de enemigo");
                try
                {
                    Cursor.SetCursor(enemyCursor, cursorHotspot, CursorMode.Auto);
                    Cursor.visible = true;
                    Debug.Log("[TargetingSystem] Cursor de enemigo configurado correctamente");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[TargetingSystem] Error al configurar cursor de enemigo: {e.Message}");
                }
            }
            else
            {
                Debug.Log("[TargetingSystem] Cambiando a cursor normal");
                try
                {
                    Cursor.SetCursor(normalCursor, cursorHotspot, CursorMode.Auto);
                    Cursor.visible = true;
                    Debug.Log("[TargetingSystem] Cursor normal configurado correctamente");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[TargetingSystem] Error al configurar cursor normal: {e.Message}");
                }
            }
        }
        else
        {
            Debug.Log("[TargetingSystem] Mouse no sobre ningún objeto, usando cursor normal");
            try
            {
                Cursor.SetCursor(normalCursor, cursorHotspot, CursorMode.Auto);
                Cursor.visible = true;
                Debug.Log("[TargetingSystem] Cursor normal configurado correctamente");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[TargetingSystem] Error al configurar cursor normal: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// Selecciona manualmente un objetivo
    /// </summary>
    /// <param name="target">El transform del objetivo a seleccionar</param>
    /// <returns>True si el objetivo es válido y se ha seleccionado</returns>
    public bool SetTarget(Transform target)
    {
        if (!photonView.IsMine) return false;
        
        Debug.Log($"[TargetingSystem] Intentando establecer objetivo: {(target != null ? target.name : "NULL")}");
        
        // Verificar si el objetivo es válido
        if (target == null) 
        {
            Debug.Log("[TargetingSystem] Objetivo inválido: NULL");
            ClearTarget();
            return false;
        }
        
        // Verificar distancia
        float distance = Vector3.Distance(transform.position, target.position);
        if (distance > targetingRange)
        {
            Debug.Log($"[TargetingSystem] Objetivo fuera de rango: {distance} > {targetingRange}");
            return false;
        }
        
        // Verificar que tiene los componentes necesarios
        HeroBase targetHero = target.GetComponent<HeroBase>();
        HeroHealth targetHealth = target.GetComponent<HeroHealth>();
        
        if (targetHero == null || targetHealth == null)
        {
            Debug.Log($"[TargetingSystem] Objetivo sin componentes necesarios: HeroBase={targetHero != null}, HeroHealth={targetHealth != null}");
            return false;
        }
        
        // Verificar si está muerto
        if (targetHealth.IsDead())
        {
            Debug.Log("[TargetingSystem] Objetivo está muerto");
            return false;
        }
        
        // Establecer como objetivo actual
        currentTarget = target;
        Debug.Log($"[TargetingSystem] Objetivo establecido correctamente: {target.name}");
        
        // Notificar al servidor
        if (PhotonNetwork.IsConnected)
        {
            photonView.RPC("RPC_UpdateTarget", RpcTarget.Others, 
                currentTarget != null ? currentTarget.GetComponent<PhotonView>().ViewID : -1);
        }
        
        return true;
    }
    
    /// <summary>
    /// Limpia el objetivo actual
    /// </summary>
    public void ClearTarget()
    {
        currentTarget = null;
        
        if (targetIndicator != null)
        {
            targetIndicator.SetActive(false);
        }
        
        // Notificar al servidor
        if (PhotonNetwork.IsConnected && photonView.IsMine)
        {
            photonView.RPC("RPC_UpdateTarget", RpcTarget.Others, -1);
        }
    }
    
    /// <summary>
    /// Busca automáticamente el enemigo más cercano dentro del rango
    /// </summary>
    /// <returns>True si se encontró un objetivo válido</returns>
    public bool FindNearestEnemyTarget()
    {
        if (!photonView.IsMine) return false;
        
        // Buscar todos los jugadores usando el layer Player
        int playerLayerMask = LayerManager.GetPlayerLayerMask();
        Debug.Log($"[TargetingSystem] Buscando enemigos con layer Player, mi tag: {gameObject.tag}");
        
        // Buscar todos los colliders en rango con el layer correcto
        Collider[] colliders = Physics.OverlapSphere(transform.position, targetingRange, playerLayerMask);
        Debug.Log($"[TargetingSystem] Encontrados {colliders.Length} objetos en rango");
        
        Transform nearestTarget = null;
        float nearestDistance = float.MaxValue;
        
        foreach (Collider col in colliders)
        {
            // Ignorar a uno mismo
            if (col.gameObject == gameObject)
                continue;
                
            // Verificar que es un héroe
            HeroBase otherHero = col.GetComponent<HeroBase>();
            HeroHealth otherHealth = col.GetComponent<HeroHealth>();
            
            if (otherHero == null || otherHealth == null)
                continue;
            
            // Verificar si es enemigo basado en tags
            if (!LayerManager.IsEnemy(gameObject, col.gameObject))
            {
                Debug.Log($"[TargetingSystem] Ignorando a {col.gameObject.name} porque no es enemigo (tags: {gameObject.tag} vs {col.gameObject.tag})");
                continue;
            }
                
            // Verificar que no está muerto
            if (otherHealth.IsDead())
                continue;
                
            // Calcular distancia
            float distance = Vector3.Distance(transform.position, col.transform.position);
            
            // Actualizar si es el más cercano
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestTarget = col.transform;
                Debug.Log($"[TargetingSystem] Nuevo objetivo más cercano: {col.gameObject.name} a {distance} metros");
            }
        }
        
        // Establecer como objetivo si se encontró uno
        if (nearestTarget != null)
        {
            return SetTarget(nearestTarget);
        }
        
        Debug.Log("[TargetingSystem] No se encontró ningún enemigo en rango");
        return false;
    }
    
    /// <summary>
    /// Verifica si el objetivo actual sigue siendo válido
    /// </summary>
    private void ValidateCurrentTarget()
    {
        if (!photonView.IsMine) return;
        
        if (currentTarget == null)
            return;
            
        // Verificar que el objetivo sigue vivo
        HeroHealth targetHealth = currentTarget.GetComponent<HeroHealth>();
        if (targetHealth == null || targetHealth.IsDead())
        {
            ClearTarget();
            return;
        }
        
        // Verificar distancia
        float distance = Vector3.Distance(transform.position, currentTarget.position);
        if (distance > targetingRange * 1.5f) // Damos un poco más de margen para no perder objetivos al moverse
        {
            ClearTarget();
            return;
        }
    }
    
    /// <summary>
    /// Actualiza la posición y apariencia del indicador de objetivo
    /// </summary>
    private void UpdateTargetIndicator()
    {
        if (targetIndicator == null || currentTarget == null)
        {
            if (targetIndicator == null)
            {
                Debug.LogWarning("[TargetingSystem] targetIndicator es null en UpdateTargetIndicator");
            }
            if (currentTarget == null)
            {
                Debug.LogWarning("[TargetingSystem] currentTarget es null en UpdateTargetIndicator");
            }
            return;
        }
            
        // Actualizar posición (ligeramente elevado del suelo)
        Vector3 targetPos = currentTarget.position;
        targetPos.y = currentTarget.position.y + indicatorHeight;
        targetIndicator.transform.position = targetPos;
        
        // Mostrar el indicador
        targetIndicator.SetActive(true);
        
        // Determinar si el objetivo es aliado o enemigo basado en tags
        bool isAlly = LayerManager.IsAlly(gameObject, currentTarget.gameObject);
        
        // Actualizar color basado en si es aliado o enemigo
        TargetIndicatorController indicatorController = targetIndicator.GetComponent<TargetIndicatorController>();
        if (indicatorController != null)
        {
            // Usar los colores configurados en el inspector
            Color indicatorColor = isAlly ? allyIndicatorColor : enemyIndicatorColor;
            indicatorController.SetColor(indicatorColor);
            
            Debug.Log($"[TargetingSystem] Indicador de objetivo actualizado: {currentTarget.name}, es aliado: {isAlly}, color: {indicatorColor}");
        }
        else
        {
            Debug.LogError("[TargetingSystem] No se encontró el componente TargetIndicatorController en el indicador");
        }
    }
    
    /// <summary>
    /// Devuelve el objetivo actual
    /// </summary>
    public Transform GetCurrentTarget()
    {
        return currentTarget;
    }
    
    /// <summary>
    /// Comprueba si un transform es el objetivo actual
    /// </summary>
    public bool IsCurrentTarget(Transform target)
    {
        return currentTarget == target;
    }
    
    [PunRPC]
    private void RPC_UpdateTarget(int targetViewID)
    {
        if (photonView.IsMine) return; // El dueño ya tiene la info actualizada
        
        if (targetViewID < 0)
        {
            // Limpiar objetivo
            currentTarget = null;
            if (targetIndicator != null)
            {
                targetIndicator.SetActive(false);
            }
        }
        else
        {
            // Buscar el objeto con ese ViewID
            PhotonView targetView = PhotonView.Find(targetViewID);
            if (targetView != null)
            {
                currentTarget = targetView.transform;
            }
        }
    }
} 