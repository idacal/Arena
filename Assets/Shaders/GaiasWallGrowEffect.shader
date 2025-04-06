Shader "Custom/GaiasWallGrowEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _GlowColor ("Glow Color", Color) = (0,1,0,1)
        _GlowIntensity ("Glow Intensity", Range(0, 2)) = 1.0
        _GlowHeight ("Glow Height", Range(0, 1)) = 0.2
        _GrowProgress ("Grow Progress", Range(0, 1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

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
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float3 normal : NORMAL;
                float height : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _GlowColor;
            float _GlowIntensity;
            float _GlowHeight;
            float _GrowProgress;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                
                // Calcular la altura normalizada (0 en la base, 1 en la parte superior)
                float localHeight = mul(unity_ObjectToWorld, v.vertex).y;
                float objHeight = mul(unity_ObjectToWorld, float4(0, 1, 0, 0)).y;
                o.height = localHeight / objHeight;
                
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Textura base
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                
                // Efecto de brillo en la base
                float glowFactor = saturate(1.0 - i.height / _GlowHeight);
                
                // Añadir efecto de crecimiento
                float growEffect = step(i.height, _GrowProgress);
                
                // Aplicar brillo solo a la parte que está creciendo
                float growGlow = saturate((1.0 - abs(i.height - _GrowProgress) * 10) * growEffect);
                
                // Combinar efectos
                col = lerp(col, _GlowColor * _GlowIntensity, glowFactor * growEffect);
                col += _GlowColor * growGlow * _GlowIntensity * 0.5;
                
                return col * growEffect; // Solo mostrar la parte que ha crecido
            }
            ENDCG
        }
    }
} 