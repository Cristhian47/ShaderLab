// =====================================================================================
//  S04 - VINETA (BORDES OSCUROS / DANO)
//  Nivel: BASICO
// =====================================================================================
//  QUE APRENDES
//   1. Usar las coordenadas de pantalla (uv) para saber DONDE esta cada pixel.
//   2. Distancia al centro con length(), corregida por aspect ratio (para que sea redonda).
//   3. smoothstep() para hacer transiciones suaves.
//
//  EN UN JUEGO: enfoque de camara, terror, recibir dano (poniendo el color en rojo), apuntar.
//
//  COMO SE USA: este es un shader de PANTALLA COMPLETA. Se ejecuta con el "Full Screen Pass
//  Renderer Feature" de URP: Unity le pasa la imagen de la camara en _BlitTexture y el
//  shader devuelve el color final de cada pixel.
// =====================================================================================
Shader "ShaderLab/Screen/Vignette"
{
    Properties
    {
        _Color ("Color de la vineta", Color) = (0, 0, 0, 1)
        _Intensity ("Intensidad", Range(0, 1)) = 0.85
        _Radius ("Radio (donde empieza)", Range(0.1, 1.2)) = 0.8
        _Softness ("Suavidad", Range(0.01, 1)) = 0.45
        _Roundness ("Redondez (1 = circulo)", Range(0, 1)) = 1
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
            Name "Vignette"

            HLSLPROGRAM
            #pragma vertex Vert              // "Vert" viene de Blit.hlsl: dibuja un triangulo que cubre la pantalla
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Intensity;
                float _Radius;
                float _Softness;
                float _Roundness;
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
                float2 d = uv - 0.5;
                float aspect = _ScaledScreenParams.x / _ScaledScreenParams.y;
                d.x *= lerp(1.0, aspect, _Roundness);                      // corregir para que sea un circulo y no un ovalo
                float dist = length(d) * 1.6;
                float vig = smoothstep(_Radius - _Softness, _Radius, dist); // 0 en el centro, 1 en los bordes
                color.rgb = lerp(color.rgb, _Color.rgb, vig * _Intensity);
                return color;
            }
            ENDHLSL
        }
    }
}
