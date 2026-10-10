// =====================================================================================
//  S06 - PALETA RETRO (GAME BOY)
//  Nivel: BASICO / MEDIO
// =====================================================================================
//  QUE APRENDES
//   1. Cuantizar (posterizar): reducir un valor continuo a pocos niveles con floor().
//   2. Usar el resultado como INDICE para elegir un color de una paleta.
//   3. Ordered dithering opcional con una matriz de Bayer 4x4.
//
//  EN UN JUEGO: estetica Game Boy / 8-bit, minijuegos retro, tutoriales de pixel art.
//
//  COMO SE USA: este es un shader de PANTALLA COMPLETA. Se ejecuta con el "Full Screen Pass
//  Renderer Feature" de URP: Unity le pasa la imagen de la camara en _BlitTexture y el
//  shader devuelve el color final de cada pixel.
// =====================================================================================
Shader "ShaderLab/Screen/Retro Palette"
{
    Properties
    {
        _Color0 ("Color 0 (mas oscuro)", Color) = (0.06, 0.22, 0.06, 1)
        _Color1 ("Color 1", Color) = (0.19, 0.38, 0.19, 1)
        _Color2 ("Color 2", Color) = (0.55, 0.67, 0.06, 1)
        _Color3 ("Color 3 (mas claro)", Color) = (0.61, 0.74, 0.06, 1)
        _Dither ("Dithering", Range(0, 1)) = 0.35
        _Intensity ("Intensidad", Range(0, 1)) = 1
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
            Name "Retro Palette"

            HLSLPROGRAM
            #pragma vertex Vert              // "Vert" viene de Blit.hlsl: dibuja un triangulo que cubre la pantalla
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color0;
                half4 _Color1;
                half4 _Color2;
                half4 _Color3;
                float _Dither;
                float _Intensity;
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
                float lum = dot(color.rgb, float3(0.2126, 0.7152, 0.0722));

                // Matriz de Bayer 4x4 (valores 0..15) para el dithering ordenado.
                const float bayer[16] = { 0, 8, 2, 10,  12, 4, 14, 6,  3, 11, 1, 9,  15, 7, 13, 5 };
                int2 px = int2(uv * _ScaledScreenParams.xy) % 4;
                float threshold = (bayer[px.y * 4 + px.x] / 16.0 - 0.5) * _Dither * 0.5;

                float idx = floor(saturate(lum + threshold) * 3.999);      // 0, 1, 2 o 3
                half3 pal = _Color0.rgb;
                pal = lerp(pal, _Color1.rgb, step(0.5, idx));
                pal = lerp(pal, _Color2.rgb, step(1.5, idx));
                pal = lerp(pal, _Color3.rgb, step(2.5, idx));
                color.rgb = lerp(color.rgb, pal, _Intensity);
                return color;
            }
            ENDHLSL
        }
    }
}
