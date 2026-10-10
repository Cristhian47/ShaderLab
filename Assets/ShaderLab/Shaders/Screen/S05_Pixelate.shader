// =====================================================================================
//  S05 - PIXELADO (RETRO)
//  Nivel: BASICO
// =====================================================================================
//  QUE APRENDES
//   1. Manipular las UV: floor() convierte coordenadas continuas en una cuadricula.
//   2. Muestrear con filtro de punto (sin suavizar) para ver pixeles grandes.
//   3. Relacion entre pixeles de pantalla y coordenadas uv.
//
//  EN UN JUEGO: estetica retro de baja resolucion, censura, transiciones, glitch.
//
//  COMO SE USA: este es un shader de PANTALLA COMPLETA. Se ejecuta con el "Full Screen Pass
//  Renderer Feature" de URP: Unity le pasa la imagen de la camara en _BlitTexture y el
//  shader devuelve el color final de cada pixel.
// =====================================================================================
Shader "ShaderLab/Screen/Pixelate"
{
    Properties
    {
        _PixelSize ("Tamano del pixel (en pixeles reales)", Range(1, 40)) = 8
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
            Name "Pixelate"

            HLSLPROGRAM
            #pragma vertex Vert              // "Vert" viene de Blit.hlsl: dibuja un triangulo que cubre la pantalla
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _PixelSize;
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
                float2 cells = _ScaledScreenParams.xy / max(_PixelSize, 1.0);   // cuantos "pixeles grandes" caben
                float2 snapped = (floor(uv * cells) + 0.5) / cells;              // centro de cada celda
                color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, snapped);
                return color;
            }
            ENDHLSL
        }
    }
}
