// =====================================================================================
//  S01 - ESCALA DE GRISES
//  Nivel: MUY BASICO
// =====================================================================================
//  QUE APRENDES
//   1. Leer el color de la pantalla y devolver otro color.
//   2. LUMINANCIA: el ojo ve el verde mas brillante que el azul, por eso se pondera
//      (0.2126, 0.7152, 0.0722) en vez de promediar R, G y B.
//   3. lerp(a, b, t): mezclar el original con el efecto.
//
//  EN UN JUEGO: recuerdos/flashbacks, vida baja, pausa, "mundo sin color".
//
//  COMO SE USA: este es un shader de PANTALLA COMPLETA. Se ejecuta con el "Full Screen Pass
//  Renderer Feature" de URP: Unity le pasa la imagen de la camara en _BlitTexture y el
//  shader devuelve el color final de cada pixel.
// =====================================================================================
Shader "ShaderLab/Screen/Grayscale"
{
    Properties
    {
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
            Name "Grayscale"

            HLSLPROGRAM
            #pragma vertex Vert              // "Vert" viene de Blit.hlsl: dibuja un triangulo que cubre la pantalla
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
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
                float gray = dot(color.rgb, float3(0.2126, 0.7152, 0.0722));   // luminancia
                color.rgb = lerp(color.rgb, gray.xxx, _Intensity);
                return color;
            }
            ENDHLSL
        }
    }
}
