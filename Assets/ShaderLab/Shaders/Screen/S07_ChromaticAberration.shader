// =====================================================================================
//  S07 - ABERRACION CROMATICA
//  Nivel: MEDIO
// =====================================================================================
//  QUE APRENDES
//   1. Leer la textura VARIAS VECES con uv distintas (una por canal de color).
//   2. Desplazamiento radial: la direccion desde el centro de la pantalla.
//   3. Animar un parametro con _Time.
//
//  EN UN JUEGO: impactos/dano, velocidad, sustos, estados de mareo, estetica de lente.
//
//  COMO SE USA: este es un shader de PANTALLA COMPLETA. Se ejecuta con el "Full Screen Pass
//  Renderer Feature" de URP: Unity le pasa la imagen de la camara en _BlitTexture y el
//  shader devuelve el color final de cada pixel.
// =====================================================================================
Shader "ShaderLab/Screen/Chromatic Aberration"
{
    Properties
    {
        _Amount ("Cantidad", Range(0, 0.05)) = 0.012
        _Pulse ("Pulso animado", Range(0, 1)) = 0.5
        _PulseSpeed ("Velocidad del pulso", Range(0, 10)) = 3
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
            Name "Chromatic Aberration"

            HLSLPROGRAM
            #pragma vertex Vert              // "Vert" viene de Blit.hlsl: dibuja un triangulo que cubre la pantalla
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Amount;
                float _Pulse;
                float _PulseSpeed;
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
                float2 dir = uv - 0.5;                                              // direccion desde el centro
                float amt = _Amount * (1.0 + _Pulse * sin(_Time.y * _PulseSpeed));  // cantidad con pulso
                // Cada canal de color se lee de un lugar ligeramente distinto.
                float r = SceneColor(uv + dir * amt).r;
                float g = SceneColor(uv).g;
                float b = SceneColor(uv - dir * amt).b;
                color.rgb = half3(r, g, b);
                return color;
            }
            ENDHLSL
        }
    }
}
