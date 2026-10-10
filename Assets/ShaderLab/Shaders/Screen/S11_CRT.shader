// =====================================================================================
//  S11 - MONITOR CRT / TV ANTIGUA
//  Nivel: AVANZADO
// =====================================================================================
//  QUE APRENDES
//   1. Distorsion de barril (curvatura de pantalla) deformando las UV.
//   2. Scanlines (lineas horizontales) con seno.
//   3. Mascara RGB: simular los subpixeles rojo/verde/azul de un monitor.
//   4. Componer varios efectos pequenos en un solo shader.
//
//  EN UN JUEGO: arcades dentro del juego, camaras de seguridad, estetica retro/terror analogico.
//
//  COMO SE USA: este es un shader de PANTALLA COMPLETA. Se ejecuta con el "Full Screen Pass
//  Renderer Feature" de URP: Unity le pasa la imagen de la camara en _BlitTexture y el
//  shader devuelve el color final de cada pixel.
// =====================================================================================
Shader "ShaderLab/Screen/CRT"
{
    Properties
    {
        _Curvature ("Curvatura de la pantalla", Range(0, 1)) = 0.35
        _ScanlineCount ("Cantidad de scanlines", Range(100, 1000)) = 360
        _ScanlineStrength ("Fuerza de scanlines", Range(0, 1)) = 0.35
        _MaskStrength ("Mascara RGB", Range(0, 1)) = 0.25
        _Bleed ("Sangrado de color", Range(0, 0.01)) = 0.0025
        _Vignette ("Vineta", Range(0, 1)) = 0.5
        _Flicker ("Parpadeo", Range(0, 0.2)) = 0.04
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
            Name "CRT"

            HLSLPROGRAM
            #pragma vertex Vert              // "Vert" viene de Blit.hlsl: dibuja un triangulo que cubre la pantalla
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Curvature;
                float _ScanlineCount;
                float _ScanlineStrength;
                float _MaskStrength;
                float _Bleed;
                float _Vignette;
                float _Flicker;
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
                // 1) Curvatura: alejamos los puntos del centro segun su distancia al cuadrado.
                float2 cuv = uv * 2.0 - 1.0;
                cuv *= 1.0 + dot(cuv, cuv) * _Curvature * 0.25;
                float2 suv = cuv * 0.5 + 0.5;

                // Fuera de la "pantalla" curva: negro.
                if (suv.x < 0.0 || suv.x > 1.0 || suv.y < 0.0 || suv.y > 1.0)
                    return half4(0, 0, 0, 1);

                // 2) Sangrado cromatico leve (rojo y azul un poco corridos)
                half3 c;
                c.r = SceneColor(suv + float2(_Bleed, 0)).r;
                c.g = SceneColor(suv).g;
                c.b = SceneColor(suv - float2(_Bleed, 0)).b;

                // 3) Scanlines
                float scan = sin(suv.y * _ScanlineCount * 3.14159) * 0.5 + 0.5;
                c *= lerp(1.0, scan, _ScanlineStrength);

                // 4) Mascara RGB: cada columna de pixeles realza un canal distinto
                int col = int(suv.x * _ScaledScreenParams.x) % 3;
                half3 mask = half3(col == 0 ? 1.0 : 0.65, col == 1 ? 1.0 : 0.65, col == 2 ? 1.0 : 0.65);
                c *= lerp(half3(1, 1, 1), mask, _MaskStrength);

                // 5) Vineta + parpadeo
                float2 d = suv - 0.5;
                c *= 1.0 - dot(d, d) * 2.0 * _Vignette;
                c *= 1.0 - _Flicker * (sin(_Time.y * 60.0) * 0.5 + 0.5);

                color.rgb = c * 1.1;
                return color;
            }
            ENDHLSL
        }
    }
}
