// =====================================================================================
//  04 - HOLOGRAMA (scanlines + glitch + parpadeo)            Nivel: MEDIO
// =====================================================================================
//  QUE APRENDES
//   1. Usar el TIEMPO (_Time.y) para animar un shader sin ningun script.
//   2. Patrones en ESPACIO MUNDO: las lineas se quedan fijas en el mundo mientras el
//      objeto se mueve o gira -> sensacion de "proyeccion".
//   3. Glitch: desplazar vertices por "bandas" con un numero pseudo-aleatorio.
//   4. Transparencia aditiva y Cull Off.
//
//  EN UN JUEGO: interfaces holograficas, mapas 3D, NPCs de ciencia ficcion, teletransporte.
//
//  RETO: haz que el glitch solo ocurra cada 2 segundos (pista: usa frac(_Time.y * 0.5)).
// =====================================================================================
Shader "ShaderLab/Objects/Hologram"
{
    Properties
    {
        [HDR] _Color ("Color del holograma", Color) = (0.2, 0.9, 1.4, 1)
        _FresnelPower ("Potencia del borde", Range(0.5, 6)) = 2
        _BaseAlpha    ("Brillo base", Range(0, 1)) = 0.25
        _ScanDensity  ("Densidad de lineas", Range(1, 120)) = 40
        _ScanSpeed    ("Velocidad de lineas", Range(-10, 10)) = 2
        _ScanIntensity ("Intensidad de lineas", Range(0, 2)) = 0.9
        _GlitchAmount ("Cantidad de glitch", Range(0, 1)) = 0.12
        _GlitchStrength ("Fuerza del glitch (m)", Range(0, 0.5)) = 0.12
        _FlickerSpeed ("Velocidad del parpadeo", Range(0, 60)) = 25
        _FlickerAmount ("Intensidad del parpadeo", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Pass
        {
            Name "Hologram"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One    // aditivo pero controlado por alpha
            ZWrite Off
            Cull Off              // vemos tambien la parte de atras -> mas "volumen"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../Includes/ShaderLabCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _FresnelPower;
                float _BaseAlpha;
                float _ScanDensity;
                float _ScanSpeed;
                float _ScanIntensity;
                float _GlitchAmount;
                float _GlitchStrength;
                float _FlickerSpeed;
                float _FlickerAmount;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 viewDirWS  : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

                // ---- GLITCH ----
                // Dividimos el mundo en franjas horizontales de 0.15 m. Cada franja, 8 veces por
                // segundo, "tira un dado" (hash). Si el dado cae en el rango, la franja se corre de lado.
                float band = floor(positionWS.y / 0.15);
                float step8 = floor(_Time.y * 8.0);
                float dice = SL_Hash21(float2(band, step8));
                float glitch = step(1.0 - _GlitchAmount, dice);
                positionWS.x += (SL_Hash21(float2(band + 7.0, step8)) * 2.0 - 1.0) * glitch * _GlitchStrength;

                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS  = GetWorldSpaceViewDir(positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float fresnel = SL_Fresnel(input.normalWS, input.viewDirWS, _FresnelPower);

                // ---- SCANLINES ---- seno en la altura del MUNDO, que se desplaza con el tiempo.
                float scan = sin(input.positionWS.y * _ScanDensity - _Time.y * _ScanSpeed) * 0.5 + 0.5;
                scan = smoothstep(0.55, 0.95, scan);

                // ---- PARPADEO ---- variacion rapida y pseudo-aleatoria del brillo.
                float flicker = 1.0 - _FlickerAmount * SL_Hash21(float2(floor(_Time.y * _FlickerSpeed), 3.0));

                float intensity = (_BaseAlpha + fresnel + scan * _ScanIntensity) * flicker;
                return half4(_Color.rgb * intensity, saturate(intensity));
            }
            ENDHLSL
        }
    }
}
