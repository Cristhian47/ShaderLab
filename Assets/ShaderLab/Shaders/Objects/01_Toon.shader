// =====================================================================================
//  01 - TOON / CEL SHADING                                   Nivel: BASICO
// =====================================================================================
//  QUE APRENDES
//   1. Como se calcula la iluminacion: producto punto N·L (normal · direccion de la luz).
//   2. Cuantizar (escalonar) la luz para obtener bandas de color estilo caricatura.
//   3. Luz especular estilizada (un brillo "duro") y Rim Light (luz de borde).
//   4. Leer la luz principal de URP (GetMainLight) y recibir sombras.
//
//  EN UN JUEGO: juegos estilo anime/comic (Zelda BotW, Genshin, Borderlands...).
//
//  RETO: cambia _Steps a 2 y luego a 6. Que pasa? Prueba cambiar el color de sombra.
// =====================================================================================
Shader "ShaderLab/Objects/Toon"
{
    Properties
    {
        _BaseColor   ("Color base",           Color) = (0.25, 0.55, 0.95, 1)
        _ShadowColor ("Color de sombra",      Color) = (0.12, 0.10, 0.35, 1)
        _Steps       ("Bandas de luz",        Range(1, 6)) = 3
        _SpecSize    ("Tamano del brillo",    Range(0, 0.5)) = 0.08
        _SpecColor2  ("Color del brillo",     Color) = (1, 1, 1, 1)
        _RimAmount   ("Luz de borde",         Range(0, 1)) = 0.6
        _RimColor    ("Color de borde",       Color) = (0.7, 0.85, 1, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "ToonForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            // Variantes (keywords) para que el shader pueda RECIBIR sombras de URP.
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _ShadowColor;
                float _Steps;
                float _SpecSize;
                half4 _SpecColor2;
                float _RimAmount;
                half4 _RimColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 N = normalize(input.normalWS);
                float3 V = normalize(GetWorldSpaceViewDir(input.positionWS));

                // Luz principal (la Directional Light de la escena) + sombras.
                float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                // --- 1) Luz difusa (Lambert): N·L va de -1 a 1. Nos quedamos con 0..1.
                float ndl = saturate(dot(N, mainLight.direction)) * mainLight.shadowAttenuation;

                // --- 2) Cuantizar: en vez de un degradado suave, "escalones".
                //        ceil(x * pasos) / pasos  convierte 0..1 en 0, 1/n, 2/n ... 1
                float toon = ceil(ndl * _Steps) / _Steps;

                // --- 3) Mezclar color de sombra y color iluminado segun la banda.
                half3 lit    = _BaseColor.rgb * mainLight.color;
                half3 shadow = _BaseColor.rgb * _ShadowColor.rgb;
                half3 color  = lerp(shadow, lit, toon);

                // --- 4) Brillo especular duro: Blinn-Phong con corte (step).
                float3 H = normalize(mainLight.direction + V);
                float spec = pow(saturate(dot(N, H)), 64.0) * mainLight.shadowAttenuation;
                float specMask = step(1.0 - _SpecSize, spec);
                color = lerp(color, _SpecColor2.rgb, specMask);

                // --- 5) Rim light: aparece en los bordes (Fresnel) del lado iluminado.
                float rim = (1.0 - saturate(dot(N, V))) * ndl;
                float rimMask = smoothstep(0.35, 0.40, rim) * _RimAmount;
                color = lerp(color, _RimColor.rgb, rimMask);

                return half4(color, 1);
            }
            ENDHLSL
        }

        // Para que el objeto PROYECTE sombras reutilizamos el pase de sombras del shader Lit de URP.
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}
