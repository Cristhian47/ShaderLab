// =====================================================================================
//  06 - LAVA (UV scrolling + distorsion + emision)           Nivel: MEDIO
// =====================================================================================
//  QUE APRENDES
//   1. Coordenadas UV: como "pegar" un patron sobre una malla.
//   2. UV SCROLLING: desplazar las UV con el tiempo para que el patron fluya.
//   3. DOMAIN WARPING: deformar las UV con otro ruido para que el flujo se vea organico.
//   4. Emision: colores HDR (> 1) que "brillan" mas que el resto de la escena.
//   5. Ruido que se repite sin costuras (tileable), necesario para una esfera.
//
//  EN UN JUEGO: rios de lava, magma, energia, liquidos radiactivos, pantanos magicos.
//
//  RETO: cambia _FlowSpeed a un numero negativo. Anade un segundo color de brasa.
// =====================================================================================
Shader "ShaderLab/Objects/Lava"
{
    Properties
    {
        _CrustColor ("Color de la costra", Color) = (0.06, 0.02, 0.015, 1)
        [HDR] _HotColor  ("Color caliente", Color) = (2.2, 0.55, 0.05, 1)
        [HDR] _CoreColor ("Color del nucleo", Color) = (4.0, 2.4, 0.5, 1)
        [IntRange] _Tiling ("Repeticiones del patron", Range(1, 12)) = 4
        _FlowSpeed ("Velocidad del flujo", Range(-1, 1)) = 0.12
        _DistortStrength ("Fuerza de la distorsion", Range(0, 1.5)) = 0.55
        _Threshold ("Cantidad de lava", Range(0.2, 0.8)) = 0.48
        _PulseSpeed ("Velocidad de pulso", Range(0, 5)) = 1.2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "Lava"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../Includes/ShaderLabCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _CrustColor;
                half4 _HotColor;
                half4 _CoreColor;
                float _Tiling;
                float _FlowSpeed;
                float _DistortStrength;
                float _Threshold;
                float _PulseSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;      // coordenadas UV de la malla
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float tiling = round(_Tiling);
                float2 period = float2(tiling, tiling);
                float2 uv = input.uv * tiling;              // repetir el patron N veces
                float t = _Time.y * _FlowSpeed;

                // ---- 1) DOMAIN WARPING: dos ruidos lentos que "empujan" las UV ----
                float2 warp = float2(
                    SL_FBMTile(uv + float2(t, 0.0), period),
                    SL_FBMTile(uv + float2(3.7, 1.3 - t), period));
                float2 warpedUV = uv + (warp - 0.5) * 2.0 * _DistortStrength;

                // ---- 2) UV SCROLLING: el patron principal avanza con el tiempo ----
                float n = SL_FBMTile(warpedUV + float2(0.0, t * 2.0), period);

                // ---- 3) Convertir el ruido en "lava" ----
                // Valores BAJOS del ruido = lava caliente (grietas). Valores altos = costra oscura.
                float hot  = 1.0 - smoothstep(_Threshold - 0.06, _Threshold + 0.06, n);
                float core = 1.0 - smoothstep(_Threshold - 0.20, _Threshold - 0.04, n);

                // Pulso suave: la lava "respira".
                float pulse = 0.85 + 0.15 * sin(_Time.y * _PulseSpeed + n * 6.0);

                half3 color = _CrustColor.rgb;
                color = lerp(color, _HotColor.rgb * pulse, hot);
                color = lerp(color, _CoreColor.rgb * pulse, core);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
