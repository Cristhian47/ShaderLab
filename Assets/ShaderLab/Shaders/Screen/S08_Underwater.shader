// =====================================================================================
//  S08 - BAJO EL AGUA (DISTORSION DE UV)
//  Nivel: MEDIO
// =====================================================================================
//  QUE APRENDES
//   1. DISTORSION DE UV: leer la imagen "desde otro lugar" para deformarla.
//   2. Ondas con seno que dependen de la posicion y del tiempo.
//   3. Teñir la imagen (tint) con un color y oscurecer con la distancia a los bordes.
//
//  EN UN JUEGO: nadar, drogas/mareo, calor (espejismo), transiciones de recuerdo.
//
//  COMO SE USA: este es un shader de PANTALLA COMPLETA. Se ejecuta con el "Full Screen Pass
//  Renderer Feature" de URP: Unity le pasa la imagen de la camara en _BlitTexture y el
//  shader devuelve el color final de cada pixel.
// =====================================================================================
Shader "ShaderLab/Screen/Underwater"
{
    Properties
    {
        _Strength ("Fuerza de la distorsion", Range(0, 0.03)) = 0.006
        _Frequency ("Frecuencia de las ondas", Range(1, 80)) = 28
        _Speed ("Velocidad", Range(0, 6)) = 1.8
        _Tint ("Color del agua", Color) = (0.35, 0.75, 0.95, 1)
        _TintAmount ("Cantidad de tinte", Range(0, 1)) = 0.55
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
            Name "Underwater"

            HLSLPROGRAM
            #pragma vertex Vert              // "Vert" viene de Blit.hlsl: dibuja un triangulo que cubre la pantalla
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Strength;
                float _Frequency;
                float _Speed;
                half4 _Tint;
                float _TintAmount;
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
                float t = _Time.y * _Speed;
                float2 offset = float2(
                    sin(uv.y * _Frequency + t),
                    cos(uv.x * _Frequency * 1.3 + t * 1.2)) * _Strength;
                color = SceneColor(uv + offset);

                // Tinte azul que se hace mas fuerte hacia los bordes (como si fuera mas profundo).
                float2 d = uv - 0.5;
                float edge = saturate(dot(d, d) * 2.0);
                half3 tinted = color.rgb * lerp(half3(1, 1, 1), _Tint.rgb, _TintAmount);
                color.rgb = lerp(tinted, tinted * _Tint.rgb * 0.6, edge);
                return color;
            }
            ENDHLSL
        }
    }
}
