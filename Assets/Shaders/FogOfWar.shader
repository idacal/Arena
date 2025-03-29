Shader "Custom/FogOfWar"
{
    Properties
    {
        _FogTexture ("Fog Texture", 2D) = "black" {}
        _FogColor ("Fog Color", Color) = (0, 0, 0, 0.9)
        _FogSoftness ("Fog Softness", Range(0, 5)) = 2
        _FogBlend ("Fog Blend", Range(0, 1)) = 0.5
        _WorldPosition ("World Position (XZ)", Vector) = (0, 0, 0, 0)
        _WorldSize ("World Size (XY)", Vector) = (100, 100, 0, 0)
        _ExploredAreaColor ("Explored Area Color", Color) = (0, 0, 0, 0.5)
        _UnexploredAreaColor ("Unexplored Area Color", Color) = (0, 0, 0, 1)
        _VisibilityThreshold ("Visibility Threshold", Range(0, 1)) = 0.5
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue" = "Transparent+100"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "IgnoreProjector" = "True"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 worldPos : TEXCOORD1;
                float4 color : COLOR;
            };
            
            sampler2D _FogTexture;
            float4 _FogTexture_ST;
            float4 _FogColor;
            float _FogSoftness;
            float _FogBlend;
            float4 _WorldPosition; // XZ position of player
            float4 _WorldSize; // XY size of map
            float4 _ExploredAreaColor;
            float4 _UnexploredAreaColor;
            float _VisibilityThreshold;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _FogTexture);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.color = v.color;
                return o;
            }
            
            float2 WorldToFogUV(float3 worldPos)
            {
                // Convertir coordenadas del mundo a coordenadas UV para la textura de niebla
                // Centrar alrededor de _WorldPosition (posición del jugador)
                float2 centered = float2(worldPos.x - _WorldPosition.x, worldPos.z - _WorldPosition.y);
                
                // Normalizar al tamaño del mapa
                float2 normalized = centered / _WorldSize.xy + 0.5;
                
                return normalized;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Convertir la posición del mundo a UV para la textura de niebla
                float2 fogUV = WorldToFogUV(i.worldPos);
                
                // Verificar si el UV está dentro del rango válido (0-1)
                if (fogUV.x < 0 || fogUV.x > 1 || fogUV.y < 0 || fogUV.y > 1) {
                    return _UnexploredAreaColor; // Fuera del mapa, usar negro completo
                }
                
                // Sample the fog texture (valor de visibilidad)
                float visibility = tex2D(_FogTexture, fogUV).r;
                
                // Apply edge blur for smoother transitions
                if (_FogSoftness > 0)
                {
                    float2 texelSize = float2(1.0 / 1024.0, 1.0 / 1024.0); // Tamaño del texel
                    
                    // Filtro de suavizado simple
                    float blur = 0;
                    int samples = 0;
                    
                    // Radio del kernel basado en la suavidad
                    int kernelSize = max(1, min(3, floor(_FogSoftness)));
                    
                    for (int y = -kernelSize; y <= kernelSize; y++)
                    {
                        for (int x = -kernelSize; x <= kernelSize; x++)
                        {
                            float2 offset = float2(x, y) * texelSize;
                            float2 sampleUV = fogUV + offset;
                            
                            // Solo considerar UVs válidos
                            if (sampleUV.x >= 0 && sampleUV.x <= 1 && sampleUV.y >= 0 && sampleUV.y <= 1) {
                                blur += tex2D(_FogTexture, sampleUV).r;
                                samples++;
                            }
                        }
                    }
                    
                    // Normalizar solo si hay muestras
                    if (samples > 0) {
                        blur /= samples;
                        // Usar un valor intermedio para evitar cambios bruscos
                        visibility = max(visibility, blur * 0.8);
                    }
                }
                
                // Lógica de visibilidad simplificada para evitar artefactos de color
                
                // Definir umbrales de visibilidad
                float notExploredThreshold = 0.1;  // < 0.1 = no explorado
                float fullyVisibleThreshold = _VisibilityThreshold; // > threshold = visible
                
                // Calcular opacidad de la niebla
                float fogOpacity;
                
                if (visibility < notExploredThreshold) {
                    // No explorado - completamente opaco (negro)
                    return _UnexploredAreaColor;
                } 
                else if (visibility > fullyVisibleThreshold) {
                    // Visible actualmente - completamente transparente
                    return float4(0, 0, 0, 0);
                }
                else {
                    // Zona de transición (área explorada)
                    // Cálculo de transparencia suave
                    float normalizedVisibility = (visibility - notExploredThreshold) / (fullyVisibleThreshold - notExploredThreshold);
                    float opacity = lerp(_ExploredAreaColor.a, 0, normalizedVisibility);
                    
                    // Usar solo gris oscuro para áreas exploradas, sin tintes de color
                    return float4(_ExploredAreaColor.rgb, opacity);
                }
            }
            ENDCG
        }
    }
} 