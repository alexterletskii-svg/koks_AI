Shader "Custom/TeleportBeam"
{
    Properties
    {
        _Color ("Основной цвет", Color) = (0, 1, 1, 0.8)
        _EmissionColor ("Цвет эмиссии", Color) = (0, 1, 1, 1)
        _EmissionIntensity ("Интенсивность эмиссии", Range(0, 5)) = 2.0
        _FadeStart ("Начало затухания (0-1)", Range(0, 1)) = 0.3
        _FadeEnd ("Конец затухания (0-1)", Range(0, 1)) = 0.85
        _ScrollSpeed ("Скорость скролла", Float) = 1.0
        _PulseSpeed ("Скорость пульсации", Float) = 3.0
    }
    
    SubShader
    {
        Tags { 
            "Queue" = "Transparent" 
            "RenderType" = "Transparent" 
            "IgnoreProjector" = "True" 
        }
        
        LOD 200
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            // Включаем поддержку Single Pass Instanced (VR)
            #pragma multi_compile_instancing
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                UNITY_FOG_COORDS(3)
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            fixed4 _Color;
            fixed4 _EmissionColor;
            float _EmissionIntensity;
            float _FadeStart;
            float _FadeEnd;
            float _ScrollSpeed;
            float _PulseSpeed;
            
            v2f vert(appdata v)
            {
                v2f o;
                
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                // Определяем, является ли это крышкой цилиндра
                float isCap = abs(dot(normalize(i.normal), float3(0, 1, 0)));
                
                // Если это крышка — делаем полностью прозрачной
                float capAlpha = 1.0 - smoothstep(0.9, 1.0, isCap);
                
                // Для боковой поверхности — градиент по UV.y
                float sideFade = 1.0 - smoothstep(_FadeStart, _FadeEnd, i.uv.y);
                
                // Итоговый фактор прозрачности
                float alpha = sideFade * capAlpha;
                
                // Скролл
                float2 scrolledUV = i.uv;
                scrolledUV.y -= _Time.y * _ScrollSpeed;
                
                // Пульсация
                float pulse = 0.7 + 0.3 * sin(_Time.y * _PulseSpeed);
                
                // Базовый цвет
                fixed4 col = _Color;
                col.a *= alpha;
                
                // Эмиссия
                fixed3 emission = _EmissionColor.rgb * _EmissionIntensity * pulse;
                col.rgb += emission * alpha;
                
                // Лёгкое свечение по краям (rim-like эффект)
                float rim = 1.0 - abs(dot(normalize(i.normal), float3(0, 0, 1)));
                col.rgb += _EmissionColor.rgb * rim * 0.3 * alpha;
                
                UNITY_APPLY_FOG(i.fogCoord, col);
                
                return col;
            }
            ENDCG
        }
    }
    
    FallBack "Transparent/VertexLit"
}