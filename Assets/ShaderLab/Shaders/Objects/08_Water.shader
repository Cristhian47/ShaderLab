// =====================================================================================
//  08 - AGUA (olas + profundidad + espuma)                   Nivel: AVANZADO
// =====================================================================================
//  QUE APRENDES
//   1. Olas en el VERTEX shader: suma de varias ondas seno en distintas direcciones.
//   2. Recalcular la NORMAL a partir de la derivada de las olas (para la luz).
//   3. DEPTH TEXTURE: la diferencia entre la profundidad del agua y la del fondo da la
//      "profundidad del agua" -> color poco profundo / profundo y ESPUMA en la orilla.
//   4. Transparencia, Fresnel (reflejo del cielo) y brillo especular.
//
//  REQUISITO: "Depth Texture" activado en el URP Asset (el constructor del museo lo hace).
//
//  EN UN JUEGO: lagos, oceanos, rios, piscinas, charcos.
//
//  RETO: anade una textura de normales que se desplace para dar detalle fino a las olas.
// =====================================================================================
Shader "ShaderLab/Objects/Water"
{
    Properties
    {
        _ShallowColor ("Color poco profundo", Color) = (0.20, 0.85, 0.80, 0.35)
        _DeepColor    ("Color profundo",      Color) = (0.02, 0.18, 0.40, 0.95)
        _SkyColor     ("Color del reflejo (cielo)", Color) = (0.55, 0.75, 1.0, 1)
        _DepthMax     ("Profundidad maxima de color (m)", Range(0.1, 5)) = 1.2
        _FoamWidth    ("Ancho de la espuma (m)", Range(0.01, 1)) = 0.25
        _FoamNoiseScale ("Escala del ruido de espuma", Range(1, 30)) = 10
        _WaveHeight   ("Altura de las olas (m)", Range(0, 0.5)) = 0.07
        _WaveFreq     ("Frecuencia de las olas", Range(0.2, 6)) = 1.6
        _WaveSpeed    ("Velocidad de las olas", Range(0, 4)) = 1.2
        _NormalStrength ("Fuerza de la normal", Range(0, 4)) = 1.5
        _Glossiness   ("Dureza del brillo", Range(8, 256)) = 90
        _SpecIntensity ("Intensidad del brillo", Range(0, 3)) = 1.2
        _ReflectionStrength ("Fuerza del reflejo", Range(0, 1)) = 0.6
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Pass
        {
            Name "Water"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "../Includes/ShaderLabCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                half4 _SkyColor;
                float _DepthMax;
                float _FoamWidth;
                float _FoamNoiseScale;
                float _WaveHeight;
                float _WaveFreq;
                float _WaveSpeed;
                float _NormalStrength;
                float _Glossiness;
                float _SpecIntensity;
                float _ReflectionStrength;
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
                float  eyeDepth   : TEXCOORD2;
                float  wave       : TEXCOORD3;     // altura de la ola (-1..1) para espuma en las crestas
            };

            // Suma de 3 ondas. Devuelve la altura (x) y la pendiente en X/Z (yz).
            // Cada onda: h = sin(dot(dir, pos) * frecuencia + tiempo * velocidad)
            // La derivada de sin es cos -> nos da la inclinacion de la superficie.
            float3 Waves(float2 p)
            {
                float t = _Time.y * _WaveSpeed;

                float2 d1 = normalize(float2( 1.0,  0.3));
                float2 d2 = normalize(float2(-0.6,  1.0));
                float2 d3 = normalize(float2( 0.2, -1.0));

                float f1 = _WaveFreq;
                float f2 = _WaveFreq * 1.7;
                float f3 = _WaveFreq * 2.9;

                float p1 = dot(d1, p) * f1 + t;
                float p2 = dot(d2, p) * f2 + t * 1.3;
                float p3 = dot(d3, p) * f3 + t * 1.9;

                float h = sin(p1) * 0.5 + sin(p2) * 0.3 + sin(p3) * 0.2;
                float2 slope = d1 * cos(p1) * 0.5 * f1
                             + d2 * cos(p2) * 0.3 * f2
                             + d3 * cos(p3) * 0.2 * f3;
                return float3(h, slope);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

                // Las olas se calculan con la posicion en el MUNDO: si pones dos planos juntos,
                // las olas coinciden perfectamente.
                float3 w = Waves(positionWS.xz);
                positionWS += normalWS * w.x * _WaveHeight;

                // Normal inclinada segun la pendiente de la ola.
                float3 bent = normalize(normalWS + float3(-w.y, 0, -w.z) * _WaveHeight * _NormalStrength);

                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS   = bent;
                output.eyeDepth   = -TransformWorldToView(positionWS).z;
                output.wave       = w.x;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 N = normalize(input.normalWS);
                float3 V = normalize(GetWorldSpaceViewDir(input.positionWS));
                Light mainLight = GetMainLight();

                // ---- 1) Profundidad del agua = (profundidad del fondo) - (profundidad de la superficie) ----
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float diff = max(sceneDepth - input.eyeDepth, 0.0);
                float depth01 = saturate(diff / _DepthMax);

                // ---- 2) Color segun profundidad ----
                half4 water = lerp(_ShallowColor, _DeepColor, sqrt(depth01));
                half3 color = water.rgb;

                // Algo de luz del sol sobre el agua
                color *= (saturate(dot(float3(0, 1, 0), mainLight.direction)) * 0.5 + 0.6) * mainLight.color;

                // ---- 3) Reflejo del cielo (Fresnel) ----
                float fresnel = SL_Fresnel(N, V, 4.0);
                color = lerp(color, _SkyColor.rgb, fresnel * _ReflectionStrength);

                // ---- 4) Brillo especular del sol ----
                float3 H = normalize(mainLight.direction + V);
                float spec = pow(saturate(dot(N, H)), _Glossiness) * _SpecIntensity;
                color += spec * mainLight.color;

                // ---- 5) Espuma: en la orilla (poca profundidad) y en las crestas de las olas ----
                float foamNoise = SL_ValueNoise(input.positionWS.xz * _FoamNoiseScale + _Time.y * 0.3);
                float shore = 1.0 - saturate(diff / _FoamWidth);
                float crest = smoothstep(0.75, 1.0, input.wave * 0.5 + 0.5 + foamNoise * 0.15);
                float foam = saturate(shore * (0.55 + foamNoise) + crest * 0.35);
                foam = smoothstep(0.35, 0.6, foam);
                color = lerp(color, half3(1, 1, 1), foam);

                float alpha = saturate(water.a + foam + fresnel * 0.3);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
