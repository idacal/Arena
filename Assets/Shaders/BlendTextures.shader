Shader "Hidden/BlendTextures" {
    Properties {
        _MainTex ("Current Visibility", 2D) = "white" {}
        _BlendTex ("Exploration History", 2D) = "white" {}
        _BlendFactor ("Blend Factor", Range(0, 1)) = 0.9
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Pass {
            ZTest Always Cull Off ZWrite Off
            Blend One Zero
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
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
                
                // Combinar usando max para mostrar áreas ya exploradas
                fixed4 combined = max(current, history * _BlendFactor);
                
                return combined;
            }
            ENDCG
        }
    }
    Fallback Off
} 