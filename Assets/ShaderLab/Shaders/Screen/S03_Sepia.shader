// =====================================================================================
//  S03 - SEPIA (FOTO ANTIGUA)
//  Nivel: BASICO
// =====================================================================================
//  QUE APRENDES
//   1. Transformar un color con una MATRIZ 3x3 (multiplicar por pesos distintos cada canal).
//   2. Combinar efectos: sepia + vineta suave para dar sensacion de foto vieja.
//
//  EN UN JUEGO: juegos del oeste, recuerdos, cinematicas "de archivo", noir.
//
//  COMO SE USA: este es un shader de PANTALLA COMPLETA. Se ejecuta con el "Full Screen Pass
//  Renderer Feature" de URP: Unity le pasa la imagen de la camara en _BlitTexture y el
//  shader devuelve el color final de cada pixel.
// =====================================================================================
Shader "ShaderLab/Screen/Sepia"
{
    Properties
    {
        _Intensity ("Intensidad", Range(0, 1)) = 1
        _Vignette ("Oscurecer bordes", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        // Un efecto de pantalla no usa profundidad ni mezcla: simplemente reemplaza cada pixel.
        ZWrite Off
        ZTest Always
        Cull Off
        Blend Off

        Pass
        {
            Name "Sepia"

            HLSLPROGRAM
            #pragma vertex Vert              // "Vert" viene de Blit.hlsl: dibuja un triangulo que cubre la pantalla
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Intensity;
                float _Vignette;
            CBUFFER_END

            // Atajo: leer la imagen de la camara en la coordenada uv.
            half4 SceneColor(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;           // 0..1 sobre toda la pantalla
                half4 color = SceneColor(uv);
                float3 c = color.rgb;
                float3 sepia = float3(
                    dot(c, float3(0.393, 0.769, 0.189)),
                    dot(c, float3(0.349, 0.686, 0.168)),
                    dot(c, float3(0.272, 0.534, 0.131)));
                float2 d = uv - 0.5;
                float vig = 1.0 - saturate(dot(d, d) * 2.0) * _Vignette;   // oscurece hacia los bordes
                color.rgb = lerp(c, sepia * vig, _Intensity);
                return color;
            }
            ENDHLSL
        }
    }
}
