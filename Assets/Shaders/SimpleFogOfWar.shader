Shader "Custom/SimpleFogOfWar"
{
    Properties
    {
        _FogTexture ("Fog Texture", 2D) = "white" {}
        _UnexploredColor ("Unexplored Color", Color) = (0, 0, 0, 1)
        _ExploredColor ("Explored Color", Color) = (0, 0, 0, 0.5)
        _VisibleColor ("Visible Color", Color) = (0, 0, 0, 0)
        _VisibleThreshold ("Visible Threshold", Range(0.1, 0.9)) = 0.7
        _ExploredThreshold ("Explored Threshold", Range(0.01, 0.5)) = 0.1
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue" = "Transparent+100"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }
        
        ZWrite Off
        ZTest Always
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha
        
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
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            sampler2D _FogTexture;
            float4 _FogTexture_ST;
            float4 _UnexploredColor;
            float4 _ExploredColor;
            float4 _VisibleColor;
            float _VisibleThreshold;
            float _ExploredThreshold;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _FogTexture);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Leer el valor de la textura de niebla (R contiene la visibilidad)
                float fogValue = tex2D(_FogTexture, i.uv).r;
                
                // Decidir el color y opacidad según el valor de visibilidad
                fixed4 finalColor;
                
                if (fogValue >= _VisibleThreshold) {
                    // Área completamente visible
                    finalColor = _VisibleColor;
                }
                else if (fogValue >= _ExploredThreshold) {
                    // Área explorada pero no visible actualmente
                    // Hacer una transición suave entre explorada y visible
                    float t = (fogValue - _ExploredThreshold) / (_VisibleThreshold - _ExploredThreshold);
                    finalColor = lerp(_ExploredColor, _VisibleColor, t);
                }
                else {
                    // Área nunca explorada (completamente negra)
                    finalColor = _UnexploredColor;
                }
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    Fallback "Unlit/Transparent"
} 