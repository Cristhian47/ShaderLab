// =====================================================================================
//  S10 - DETECCION DE BORDES (SOBEL)
//  Nivel: MEDIO / AVANZADO
// =====================================================================================
//  QUE APRENDES
//   1. Operador SOBEL: dos kernels (horizontal y vertical) que miden cuanto cambia la
//      luminosidad entre pixeles vecinos. Mucho cambio = borde.
//   2. Leer 8 vecinos en un bucle manual.
//   3. Componer: dibujar los bordes sobre la imagen original (o solos).
//
//  EN UN JUEGO: estilo comic/boceto, vision de "detective", resaltar entornos, post-proceso toon.
//
//  RETO: en vez de luminosidad, usa la PROFUNDIDAD (SampleSceneDepth) para bordes mas limpios.
//
//  COMO SE USA: este es un shader de PANTALLA COMPLETA. Se ejecuta con el "Full Screen Pass
//  Renderer Feature" de URP: Unity le pasa la imagen de la camara en _BlitTexture y el
//  shader devuelve el color final de cada pixel.
// =====================================================================================
Shader "ShaderLab/Screen/Edge Detect"
{
    Properties
    {
        _EdgeColor ("Color del borde", Color) = (0, 0, 0, 1)
        _Threshold ("Umbral de deteccion", Range(0, 1)) = 0.12
        _Softness ("Suavidad", Range(0.001, 0.5)) = 0.08
        _Thickness ("Grosor (pixeles)", Range(0.5, 4)) = 1.2
        [Toggle] _OnlyEdges ("Mostrar solo bordes", Float) = 0
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
            Name "Edge Detect"

            HLSLPROGRAM
            #pragma vertex Vert              // "Vert" viene de Blit.hlsl: dibuja un triangulo que cubre la pantalla
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _EdgeColor;
                float _Threshold;
                float _Softness;
                float _Thickness;
                float _OnlyEdges;
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
                float2 texel = _Thickness / _ScaledScreenParams.xy;
                float3 lumW = float3(0.2126, 0.7152, 0.0722);

                // Luminosidad de los 8 vecinos
                float tl = dot(SceneColor(uv + texel * float2(-1,  1)).rgb, lumW);
                float  t = dot(SceneColor(uv + texel * float2( 0,  1)).rgb, lumW);
                float tr = dot(SceneColor(uv + texel * float2( 1,  1)).rgb, lumW);
                float  l = dot(SceneColor(uv + texel * float2(-1,  0)).rgb, lumW);
                float  r = dot(SceneColor(uv + texel * float2( 1,  0)).rgb, lumW);
                float bl = dot(SceneColor(uv + texel * float2(-1, -1)).rgb, lumW);
                float  b = dot(SceneColor(uv + texel * float2( 0, -1)).rgb, lumW);
                float br = dot(SceneColor(uv + texel * float2( 1, -1)).rgb, lumW);

                // Sobel: gradiente horizontal (gx) y vertical (gy)
                float gx = -tl - 2.0 * l - bl + tr + 2.0 * r + br;
                float gy = -tl - 2.0 * t - tr + bl + 2.0 * b + br;
                float edge = sqrt(gx * gx + gy * gy);

                edge = smoothstep(_Threshold, _Threshold + _Softness, edge);

                half3 baseColor = lerp(color.rgb, half3(1, 1, 1), step(0.5, _OnlyEdges));
                color.rgb = lerp(baseColor, _EdgeColor.rgb, edge);
                return color;
            }
            ENDHLSL
        }
    }
}
