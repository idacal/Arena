using UnityEngine;
using Photon.Pun;
using System.Collections.Generic;
using System.Collections;

[ExecuteInEditMode]
public class FogOfWarSystem : MonoBehaviourPunCallbacks
{
    [Header("Configuración de la Niebla de Guerra")]
    [SerializeField] private Material fogMaterial;
    [SerializeField] private string teamTag = "RedTeam";
    [SerializeField] private Vector2 mapSize = new Vector2(100f, 100f);
    [SerializeField] private float fogHeight = 10f;
    [SerializeField] private Color fogColor = new Color(0, 0, 0, 0.9f);
    [SerializeField] private Color exploredAreaColor = new Color(0.05f, 0.05f, 0.05f, 0.5f);
    [SerializeField] private Color unexploredAreaColor = new Color(0, 0, 0, 1f);
    [SerializeField] private float fogSoftness = 2f;
    [SerializeField] private float fogBlend = 0.5f;
    [SerializeField] private bool dynamicFog = true;
    
    [Header("Referencia a Objetos")]
    [SerializeField] private GameObject fogPlane;
    [SerializeField] private LayerMask revealerLayerMask; // Capa para objetos que revelan la niebla
    [SerializeField] private LayerMask obstacleLayerMask; // Capa para obstáculos que bloquean la visión
    
    [Header("Visibilidad")]
    [SerializeField] private float updateInterval = 0.1f;
    [SerializeField] private bool debugMode = false;
    [SerializeField] private Transform playerToFollow;
    [SerializeField] private bool followPlayer = true;
    
    // Texturas y cámaras
    private Camera fogOfWarCamera;
    private RenderTexture fogOfWarTexture;
    private RenderTexture explorationTexture; // Almacena áreas exploradas permanentemente
    private RenderTexture tempTexture; // Textura temporal para double buffering
    
    // Datos internos
    private bool initialized = false;
    private List<FogRevealer> revealers = new List<FogRevealer>();
    private GameObject cameraObject;
    private Material blendMaterial; // Material para combinar texturas
    private Material revealerMaterial; // Material para revelar la niebla
    
    void OnEnable()
    {
        if (!initialized)
        {
            Initialize();
        }
    }
    
    void OnDisable()
    {
        CleanUp();
    }
    
    void OnDestroy()
    {
        CleanUp();
    }
    
    private void Initialize()
    {
        // Buscar jugador local si no se asignó uno
        if (playerToFollow == null && followPlayer)
        {
            FindLocalPlayer();
        }
        
        // Inicializar el sistema de niebla
        SetupFogSystem();
        
        // Comenzar actualizaciones
        InvokeRepeating("UpdateFogOfWar", 0.1f, updateInterval);
        
        initialized = true;
    }
    
    private void FindLocalPlayer()
    {
        // Buscar todos los objetos con el tag de equipo
        GameObject[] teamObjects = GameObject.FindGameObjectsWithTag(teamTag);
        
        foreach (GameObject obj in teamObjects)
        {
            // Verificar si este objeto tiene un PhotonView que sea mío (jugador local)
            PhotonView view = obj.GetComponent<PhotonView>();
            if (view != null && view.IsMine)
            {
                playerToFollow = obj.transform;
                Debug.Log("FogOfWar: Siguiendo al jugador local: " + obj.name);
                break;
            }
        }
        
        // Si no encontramos un jugador local con PhotonView, buscar cualquier objeto con el tag
        if (playerToFollow == null && teamObjects.Length > 0)
        {
            playerToFollow = teamObjects[0].transform;
            Debug.Log("FogOfWar: No se encontró jugador local. Siguiendo a: " + teamObjects[0].name);
        }
    }
    
    private void SetupFogSystem()
    {
        CleanUp(); // Limpiar recursos anteriores
        
        // Crear las texturas
        int textureSize = 1024;
        fogOfWarTexture = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.R8);
        fogOfWarTexture.wrapMode = TextureWrapMode.Clamp;
        fogOfWarTexture.filterMode = FilterMode.Bilinear;
        fogOfWarTexture.name = "FogOfWarRT";
        fogOfWarTexture.Create();
        
        // Textura para el historial de exploración
        explorationTexture = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.R8);
        explorationTexture.wrapMode = TextureWrapMode.Clamp;
        explorationTexture.filterMode = FilterMode.Bilinear;
        explorationTexture.name = "ExplorationRT";
        explorationTexture.Create();
        
        // Textura temporal para double buffering
        tempTexture = new RenderTexture(textureSize, textureSize, 0, RenderTextureFormat.R8);
        tempTexture.wrapMode = TextureWrapMode.Clamp;
        tempTexture.filterMode = FilterMode.Bilinear;
        tempTexture.name = "TempRT";
        tempTexture.Create();
        
        // Limpiar textura de exploración (negro = no explorado)
        RenderTexture.active = explorationTexture;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = null;
        
        // Crear la cámara ortográfica
        cameraObject = new GameObject("FogOfWarCamera");
        fogOfWarCamera = cameraObject.AddComponent<Camera>();
        fogOfWarCamera.orthographic = true;
        fogOfWarCamera.orthographicSize = mapSize.y / 2f;
        fogOfWarCamera.clearFlags = CameraClearFlags.SolidColor;
        fogOfWarCamera.backgroundColor = Color.black;
        fogOfWarCamera.cullingMask = revealerLayerMask;
        fogOfWarCamera.targetTexture = fogOfWarTexture;
        fogOfWarCamera.enabled = true;
        fogOfWarCamera.transform.position = new Vector3(0, fogHeight, 0);
        fogOfWarCamera.transform.rotation = Quaternion.Euler(90, 0, 0);
        
        // Crear material para los revealers
        if (revealerMaterial == null)
        {
            Shader revealerShader = Shader.Find("Unlit/Color");
            if (revealerShader == null)
            {
                Debug.LogError("No se pudo encontrar el shader 'Unlit/Color'. Asegúrate de que esté incluido en tu proyecto.");
                return;
            }
            revealerMaterial = new Material(revealerShader);
            revealerMaterial.color = Color.white;
        }
        
        // Configurar material para combinación
        if (blendMaterial == null)
        {
            Shader blendShader = Shader.Find("Hidden/BlendTextures");
            if (blendShader == null)
            {
                // Crear shader básico si no existe
                CreateBlendShader();
                blendShader = Shader.Find("Hidden/BlendTextures");
            }
            
            if (blendShader != null)
            {
                blendMaterial = new Material(blendShader);
                blendMaterial.SetFloat("_BlendFactor", 0.95f); // Factor de persistencia
            }
            else
            {
                Debug.LogError("No se pudo crear el shader de combinación para FogOfWar");
            }
        }
        
        // Crear o configurar el plano de niebla
        if (fogPlane == null)
        {
            fogPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            fogPlane.name = "FogOfWarPlane";
            DestroyImmediate(fogPlane.GetComponent<Collider>());
            fogPlane.transform.localScale = new Vector3(mapSize.x / 10f, 1, mapSize.y / 10f);
            fogPlane.transform.position = new Vector3(0, 0.1f, 0);
        }
        
        // Configurar material del plano
        if (fogMaterial == null)
        {
            Shader fogShader = Shader.Find("Custom/FogOfWar");
            if (fogShader != null)
            {
                fogMaterial = new Material(fogShader);
            }
            else
            {
                Debug.LogError("No se encontró el shader 'Custom/FogOfWar'. Asegúrate de que está incluido en tu proyecto.");
                return;
            }
        }
        
        // Aplicar material al plano de niebla
        MeshRenderer planeRenderer = fogPlane.GetComponent<MeshRenderer>();
        if (planeRenderer != null)
        {
            Material fogMaterialInstance = new Material(fogMaterial);
            fogMaterialInstance.SetTexture("_FogTexture", explorationTexture);
            fogMaterialInstance.SetColor("_FogColor", fogColor);
            fogMaterialInstance.SetFloat("_FogSoftness", fogSoftness);
            fogMaterialInstance.SetFloat("_FogBlend", fogBlend);
            
            // Asegurar que los colores no tengan componentes no deseados
            // Ajustar el color de área explorada para evitar tintes morados
            Color exploredColor = new Color(0.05f, 0.05f, 0.05f, exploredAreaColor.a);
            Color unexploredColor = new Color(0, 0, 0, unexploredAreaColor.a);
            
            fogMaterialInstance.SetColor("_ExploredAreaColor", exploredColor);
            fogMaterialInstance.SetColor("_UnexploredAreaColor", unexploredColor);
            fogMaterialInstance.SetFloat("_VisibilityThreshold", 0.7f); // Umbral para visibilidad
            
            planeRenderer.material = fogMaterialInstance;
            fogMaterial = fogMaterialInstance;
        }
        
        // Buscar revealers existentes
        FindAllRevealers();
        
        Debug.Log("Sistema de niebla de guerra inicializado correctamente");
    }
    
    private void CreateBlendShader()
    {
        // Crear shader temporal para combinar texturas
        string shaderCode = @"
Shader ""Hidden/BlendTextures"" {
    Properties {
        _MainTex (""Current Visibility"", 2D) = ""white"" {}
        _BlendTex (""Exploration History"", 2D) = ""white"" {}
        _BlendFactor (""Blend Factor"", Range(0, 1)) = 0.9
    }
    SubShader {
        Tags { ""Queue""=""Transparent"" ""RenderType""=""Transparent"" }
        Pass {
            ZTest Always Cull Off ZWrite Off
            Blend One Zero
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include ""UnityCG.cginc""
            
            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _MainTex;
            sampler2D _BlendTex;
            float _BlendFactor;
            
            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target {
                // Leer visibilidad actual y exploración histórica
                fixed4 current = tex2D(_MainTex, i.uv);
                fixed4 history = tex2D(_BlendTex, i.uv);
                
                // Combinar usando max para recordar áreas ya exploradas
                // El factor de mezcla permite un desvanecimiento gradual de áreas antiguas
                return max(current, history * _BlendFactor);
            }
            ENDCG
        }
    }
}";
        // Guardar shader a un archivo temporal
        string shaderPath = "Assets/Shaders/BlendTextures.shader";
        System.IO.File.WriteAllText(shaderPath, shaderCode);
        
        // Recargar asset database
        #if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
        #endif
        
        Debug.Log("Shader de combinación creado en: " + shaderPath);
    }
    
    private void UpdateFogOfWar()
    {
        if (!initialized || fogOfWarCamera == null || fogMaterial == null)
            return;
        
        // Actualizar posición de la cámara para seguir al jugador
        if (followPlayer && playerToFollow != null)
        {
            Vector3 playerPos = playerToFollow.position;
            fogOfWarCamera.transform.position = new Vector3(playerPos.x, fogHeight, playerPos.z);
            
            // Mover el plano de niebla
            if (fogPlane != null)
            {
                fogPlane.transform.position = new Vector3(playerPos.x, 0.1f, playerPos.z);
            }
            
            // Actualizar offset en el shader
            if (fogMaterial != null)
            {
                fogMaterial.SetVector("_WorldPosition", new Vector4(playerPos.x, playerPos.z, 0, 0));
                fogMaterial.SetVector("_WorldSize", new Vector4(mapSize.x, mapSize.y, 0, 0));
            }
        }
        
        // Renderizar visibilidad actual
        fogOfWarCamera.Render();
        
        // Combinar la visibilidad actual con la exploración histórica
        if (dynamicFog && blendMaterial != null)
        {
            // Guardar actual renderTexture
            RenderTexture currentRT = RenderTexture.active;
            
            try
            {
                // Implementación de Double Buffering
                // 1. Copiar exploration -> temp
                Graphics.Blit(explorationTexture, tempTexture);
                
                // 2. Configurar material de combinación
                blendMaterial.SetTexture("_MainTex", fogOfWarTexture);    // Visibilidad actual
                blendMaterial.SetTexture("_BlendTex", tempTexture);       // Historial de exploración (copia)
                
                // 3. Combinar fogOfWarTexture con tempTexture y guardar en explorationTexture
                Graphics.Blit(fogOfWarTexture, explorationTexture, blendMaterial);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error al combinar texturas: " + e.Message);
            }
            finally
            {
                // Restaurar renderTexture
                RenderTexture.active = currentRT;
            }
        }
    }
    
    // Encuentra todos los objetos que revelan la niebla
    private void FindAllRevealers()
    {
        revealers.Clear();
        
        // Buscar todos los objetos con componente FogRevealer
        FogRevealer[] allRevealers = FindObjectsOfType<FogRevealer>();
        
        foreach (FogRevealer revealer in allRevealers)
        {
            // Solo agregar los del mismo equipo
            if (revealer.TeamTag == teamTag)
            {
                revealers.Add(revealer);
                // Configurar el material para el revealer
                revealer.SetRevealerMaterial(revealerMaterial);
            }
        }
        
        Debug.Log($"FogOfWar: Se encontraron {revealers.Count} reveladores para el equipo {teamTag}");
    }
    
    // Registra un nuevo objeto que revela la niebla
    public void RegisterRevealer(FogRevealer revealer)
    {
        if (!revealers.Contains(revealer) && revealer.TeamTag == teamTag)
        {
            revealers.Add(revealer);
            revealer.SetRevealerMaterial(revealerMaterial);
        }
    }
    
    // Elimina un revelador de la lista
    public void UnregisterRevealer(FogRevealer revealer)
    {
        if (revealers.Contains(revealer))
        {
            revealers.Remove(revealer);
        }
    }
    
    private void CleanUp()
    {
        // Detener actualizaciones
        CancelInvoke("UpdateFogOfWar");
        
        // Primero, eliminar referencias de la cámara a las texturas
        if (fogOfWarCamera != null)
        {
            fogOfWarCamera.targetTexture = null;
        }
        
        // Destruir cámara y su objeto
        if (cameraObject != null)
        {
            DestroyImmediate(cameraObject);
            cameraObject = null;
            fogOfWarCamera = null;
        }
        
        // Luego liberar las texturas
        if (fogOfWarTexture != null)
        {
            fogOfWarTexture.Release();
            DestroyImmediate(fogOfWarTexture);
            fogOfWarTexture = null;
        }
        
        if (explorationTexture != null)
        {
            explorationTexture.Release();
            DestroyImmediate(explorationTexture);
            explorationTexture = null;
        }
        
        if (tempTexture != null)
        {
            tempTexture.Release();
            DestroyImmediate(tempTexture);
            tempTexture = null;
        }
        
        // Limpiar materiales
        if (revealerMaterial != null)
        {
            DestroyImmediate(revealerMaterial);
            revealerMaterial = null;
        }
        
        if (blendMaterial != null)
        {
            DestroyImmediate(blendMaterial);
            blendMaterial = null;
        }
        
        initialized = false;
    }
    
    // Métodos para pruebas y depuración
    public void RevealAll()
    {
        if (explorationTexture != null)
        {
            RenderTexture.active = explorationTexture;
            GL.Clear(true, true, Color.white);
            RenderTexture.active = null;
        }
    }
    
    public void ResetFog()
    {
        if (explorationTexture != null)
        {
            RenderTexture.active = explorationTexture;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = null;
        }
    }
    
    // Gizmos para visualización en el editor
    private void OnDrawGizmosSelected()
    {
        if (debugMode)
        {
            // Dibujar el área del mapa
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, new Vector3(mapSize.x, 0.1f, mapSize.y));
            
            // Dibujar la posición de la cámara si existe
            if (fogOfWarCamera != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(fogOfWarCamera.transform.position, 1f);
                Gizmos.DrawLine(fogOfWarCamera.transform.position, 
                                fogOfWarCamera.transform.position + fogOfWarCamera.transform.forward * 5f);
            }
        }
    }
} 