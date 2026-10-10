// =====================================================================================
//  S12 - NIEBLA POR PROFUNDIDAD (DEPTH BUFFER)
//  Nivel: AVANZADO
// =====================================================================================
//  QUE APRENDES
//   1. La DEPTH TEXTURE: Unity guarda la distancia de cada pixel a la camara.
//   2. LinearEyeDepth: convertir la profundidad "cruda" (no lineal) a metros.
//   3. Reconstruir un efecto 3D (niebla) usando solo informacion 2D de pantalla.
//   4. Visualizar el depth buffer en escala de grises (muy util para depurar).
//
//  REQUISITO: el Full Screen Pass Renderer Feature debe pedir "Depth" en Requirements
//  (el constructor del museo ya lo configura).
//
//  EN UN JUEGO: niebla atmosferica, terror, escenas de aire libre, profundidad de campo.
//
//  COMO SE USA: este es un shader de PANTALLA COMPLETA. Se ejecuta con el "Full Screen Pass
//  Renderer Feature" de URP: Unity le pasa la imagen de la camara en _BlitTexture y el
//  shader devuelve el color final de cada pixel.
// =====================================================================================
Shader "ShaderLab/Screen/Depth Fog"
{
    Properties
    {
        _FogColor ("Color de la niebla", Color) = (0.55, 0.62, 0.75, 1)
        _FogStart ("Distancia donde empieza (m)", Range(0, 60)) = 4
        _FogEnd ("Distancia de niebla total (m)", Range(1, 120)) = 40
        _Density ("Densidad", Range(0, 1)) = 1
        [Toggle] _ShowDepth ("Mostrar solo la profundidad", Float) = 0
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
            Name "Depth Fog"

            HLSLPROGRAM
            #pragma vertex Vert              // "Vert" viene de Blit.hlsl: dibuja un triangulo que cubre la pantalla
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _FogColor;
                float _FogStart;
                float _FogEnd;
                float _Density;
                float _ShowDepth;
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
                float rawDepth = SampleSceneDepth(uv);
                float eyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);      // distancia en metros

                float fog = saturate((eyeDepth - _FogStart) / max(_FogEnd - _FogStart, 0.001)) * _Density;
                half3 fogged = lerp(color.rgb, _FogColor.rgb, fog);

                // Modo depuracion: el depth buffer como escala de grises (cerca = negro, lejos = blanco).
                half3 depthView = saturate(eyeDepth / max(_FogEnd, 0.001)).xxx;
                color.rgb = lerp(fogged, depthView, step(0.5, _ShowDepth));
                return color;
            }
            ENDHLSL
        }
    }
}
