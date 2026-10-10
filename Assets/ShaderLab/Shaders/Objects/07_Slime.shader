// =====================================================================================
//  07 - SLIME / GELATINA (animacion de vertices + translucidez)   Nivel: MEDIO
// =====================================================================================
//  QUE APRENDES
//   1. VERTEX ANIMATION: mover los vertices desde el shader (gratis, sin tocar la CPU).
//   2. Squash & stretch: principio clasico de animacion aplicado en un shader.
//   3. Transparencia con Alpha Blend (SrcAlpha, OneMinusSrcAlpha).
//   4. Un "falso subsurface scattering": la luz que atraviesa el cuerpo.
//   5. Brillo especular suave (Blinn-Phong).
//
//  EN UN JUEGO: slimes, gelatinas, globos, criaturas blandas, burbujas.
//
//  RETO: haz que el slime se aplaste mas cuando _SquashAmount es alto y ponle un
//        segundo color en la parte de abajo.
// =====================================================================================
Shader "ShaderLab/Objects/Slime"
{
    Properties
    {
        _Color      ("Color del cuerpo", Color) = (0.25, 0.95, 0.35, 1)
        _CoreColor  ("Color del interior", Color) = (0.02, 0.35, 0.12, 1)
        [HDR] _RimColor ("Color de borde", Color) = (0.7, 1.4, 0.8, 1)
        _AlphaCenter ("Opacidad al centro", Range(0, 1)) = 0.75
        _AlphaRim    ("Opacidad en el borde", Range(0, 1)) = 0.95
        _WobbleAmount ("Fuerza del temblor", Range(0, 0.15)) = 0.05
        _WobbleFreq   ("Frecuencia del temblor", Range(1, 20)) = 7
        _WobbleSpeed  ("Velocidad del temblor", Range(0, 10)) = 3
        _SquashAmount ("Aplastamiento", Range(0, 0.4)) = 0.12
        _SquashSpeed  ("Velocidad de respiracion", Range(0, 8)) = 3
        _SpecPower    ("Dureza del brillo", Range(4, 128)) = 48
        _SSS          ("Luz que atraviesa", Range(0, 2)) = 0.8
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Pass
        {
            Name "Slime"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha     // transparencia "normal"
            ZWrite On                           // al ser convexo, escribir profundidad evita problemas de orden
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "../Includes/ShaderLabCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _CoreColor;
                half4 _RimColor;
                float _AlphaCenter;
                float _AlphaRim;
                float _WobbleAmount;
                float _WobbleFreq;
                float _WobbleSpeed;
                float _SquashAmount;
                float _SquashSpeed;
                float _SpecPower;
                float _SSS;
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
                float  height01   : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 pos = input.positionOS.xyz;

                // ---- TEMBLOR: empujamos cada vertice a lo largo de su normal con un seno ----
                float t = _Time.y * _WobbleSpeed;
                float wob = sin(pos.y * _WobbleFreq + t) * sin(pos.x * _WobbleFreq * 0.7 + t * 1.3);
                pos += input.normalOS * wob * _WobbleAmount;

                // ---- SQUASH & STRETCH: cuando se aplasta en Y, se ensancha en X y Z ----
                float squash = sin(_Time.y * _SquashSpeed) * _SquashAmount;
                pos.y  *= 1.0 + squash;
                pos.xz *= 1.0 - squash * 0.5;

                VertexPositionInputs v = GetVertexPositionInputs(pos);
                output.positionCS = v.positionCS;
                output.positionWS = v.positionWS;
                output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                output.height01   = saturate(input.positionOS.y + 0.5);   // 0 abajo, 1 arriba (esfera unitaria)
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 N = normalize(input.normalWS);
                float3 V = normalize(GetWorldSpaceViewDir(input.positionWS));
                Light mainLight = GetMainLight();
                float3 L = mainLight.direction;

                float fresnel = SL_Fresnel(N, V, 2.5);

                // Color: mas oscuro y profundo abajo, mas claro arriba. El centro (mirando de frente) es el "interior".
                half3 body = lerp(_CoreColor.rgb, _Color.rgb, input.height01 * 0.7 + 0.3);
                body = lerp(body, _Color.rgb, fresnel);

                // Luz difusa suave + ambiente
                float ndl = saturate(dot(N, L)) * 0.6 + 0.4;
                half3 color = body * (ndl * mainLight.color + SampleSH(N) * 0.5);

                // Falso subsurface: si la luz viene "desde atras" del slime, el borde brilla.
                float back = pow(saturate(dot(V, -L)), 2.0);
                color += _Color.rgb * back * _SSS * fresnel;

                // Brillo especular (la "humedad" del slime)
                float3 H = normalize(L + V);
                float spec = pow(saturate(dot(N, H)), _SpecPower);
                color += spec * mainLight.color;

                // Luz de borde
                color += _RimColor.rgb * fresnel * 0.6;

                float alpha = lerp(_AlphaCenter, _AlphaRim, fresnel);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
