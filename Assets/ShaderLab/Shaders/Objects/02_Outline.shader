// =====================================================================================
//  02 - OUTLINE (contorno con "casco invertido")             Nivel: BASICO / MEDIO
// =====================================================================================
//  QUE APRENDES
//   1. Un shader puede tener VARIOS PASES: cada pase vuelve a dibujar el objeto.
//   2. Mover vertices en el vertex shader (empujarlos a lo largo de la normal).
//   3. Cull Front: dibujar solo las caras traseras.
//   4. LightMode "SRPDefaultUnlit" para pases extra en URP.
//
//  COMO FUNCIONA (el truco del "inverted hull"):
//    Pase 1: dibujamos una copia un poquito MAS GRANDE del modelo, pintada de un color
//            plano, y solo sus caras traseras (Cull Front).
//    Pase 2: dibujamos el modelo normal encima.
//    Lo unico que sobresale de atras es el borde -> parece un contorno.
//
//  EN UN JUEGO: resaltar objetos interactuables, seleccion de unidades, estilo comic.
//
//  PRUEBALO: en la sala, mira un objeto del diorama (cofre, barril, cristal): se resalta.
// =====================================================================================
Shader "ShaderLab/Objects/Outline"
{
    Properties
    {
        _BaseColor    ("Color del objeto",   Color) = (0.9, 0.55, 0.2, 1)
        _OutlineColor ("Color del contorno", Color) = (0.03, 0.03, 0.05, 1)
        _OutlineWidth ("Grosor del contorno (metros)", Range(0, 0.15)) = 0.03
        _SmoothNormals ("Usar normales suavizadas", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        // ------------------------------- PASE 1: CONTORNO -------------------------------
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front       // <- solo caras traseras: lo que queda visible es el borde

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _OutlineColor;
                float _OutlineWidth;
                float _SmoothNormals;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;

                // Un cubo tiene normales "duras" (cada cara apunta a un lado) y al empujar
                // los vertices se abren huecos en las esquinas. Truco: para mallas convexas y
                // centradas, la direccion "desde el centro hacia el vertice" es una normal suave.
                float3 smoothDir = normalize(input.positionOS.xyz);
                float3 dirOS = normalize(lerp(input.normalOS, smoothDir, _SmoothNormals));

                // Pasamos a mundo y empujamos el vertice hacia afuera.
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 dirWS = normalize(TransformObjectToWorldDir(dirOS));
                positionWS += dirWS * _OutlineWidth;

                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }

        // --------------------------- PASE 2: OBJETO NORMAL ------------------------------
        Pass
        {
            Name "Base"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _OutlineColor;
                float _OutlineWidth;
                float _SmoothNormals;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // "Half Lambert": suaviza la luz para que el lado oscuro no quede negro.
                Light mainLight = GetMainLight();
                float ndl = dot(normalize(input.normalWS), mainLight.direction) * 0.5 + 0.5;
                half3 ambient = SampleSH(normalize(input.normalWS)) * 0.5;
                half3 color = _BaseColor.rgb * (ndl * mainLight.color + ambient);
                return half4(color, 1);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}
