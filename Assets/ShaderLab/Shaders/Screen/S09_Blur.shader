// =====================================================================================
//  S09 - DESENFOQUE (BLUR)
//  Nivel: MEDIO
// =====================================================================================
//  QUE APRENDES
//   1. KERNEL de convolucion: promediar un pixel con sus vecinos.
//   2. Pesos gaussianos (el centro pesa mas que las esquinas).
//   3. Tamano de un texel: 1 / resolucion de pantalla.
//   4. Repetir el kernel con distintos radios para un blur mas grande por el mismo precio.
//
//  EN UN JUEGO: menus de pausa, vision borrosa por dano, profundidad de campo, vidrio esmerilado.
//
//  COMO SE USA: este es un shader de PANTALLA COMPLETA. Se ejecuta con el "Full Screen Pass
//  Renderer Feature" de URP: Unity le pasa la imagen de la camara en _BlitTexture y el
//  shader devuelve el color final de cada pixel.
// =====================================================================================
Shader "ShaderLab/Screen/Blur"
{
    Properties
    {
        _Radius ("Radio (en pixeles)", Range(0, 8)) = 2.5
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
            Name "Blur"

            HLSLPROGRAM
            #pragma vertex Vert              // "Vert" viene de Blit.hlsl: dibuja un triangulo que cubre la pantalla
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Radius;
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
                float2 texel = 1.0 / _ScaledScreenParams.xy;

                // Kernel 3x3 gaussiano:  1 2 1 / 2 4 2 / 1 2 1  (suma 16)
                half3 sum = 0;
                float total = 0;
                for (int ring = 1; ring <= 2; ring++)        // dos anillos: radio y 2*radio
                {
                    float2 o = texel * _Radius * ring * 0.5;
                    for (int y = -1; y <= 1; y++)
                    {
                        for (int x = -1; x <= 1; x++)
                        {
                            float w = (2 - abs(x)) * (2 - abs(y));
                            sum += SceneColor(uv + float2(x, y) * o).rgb * w;
                            total += w;
                        }
                    }
                }
                color.rgb = sum / total;
                return color;
            }
            ENDHLSL
        }
    }
}
