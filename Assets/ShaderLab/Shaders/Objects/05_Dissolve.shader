// =====================================================================================
//  05 - DISOLVER (dissolve con borde incandescente)          Nivel: MEDIO
// =====================================================================================
//  QUE APRENDES
//   1. clip() con una textura/funcion de ruido como "mascara de recorte".
//   2. Un umbral (threshold) que sube de 0 a 1 hace que el objeto desaparezca poco a poco.
//   3. Emision: pintar un borde con un color HDR (mayor que 1) para que "brille".
//   4. Iluminacion simple (Lambert) con SV_IsFrontFace para ver el interior.
//   5. Animar con _Time.y y una onda triangular.
//
//  EN UN JUEGO: muerte de enemigos, aparicion de objetos, teletransporte, revelar mapas.
//
//  RETO: haz que el borde sea de otro color y que el umbral dependa de la altura (posicion Y)
//        para que el objeto se disuelva de arriba hacia abajo.
// =====================================================================================
Shader "ShaderLab/Objects/Dissolve"
{
    Properties
    {
        _BaseColor ("Color base", Color) = (0.75, 0.75, 0.8, 1)
        _Dissolve  ("Disolver (0 = visible, 1 = desaparecido)", Range(0, 1)) = 0.3
        [Toggle] _AutoAnimate ("Animar automaticamente", Float) = 1
        _AutoSpeed ("Velocidad de la animacion", Range(0.05, 1)) = 0.25
        _Phase     ("Desfase de la animacion", Range(0, 1)) = 0
        _NoiseScale ("Escala del ruido", Range(0.3, 10)) = 3
        [Toggle] _WorldSpaceNoise ("Ruido en espacio mundo", Float) = 0
        _EdgeWidth ("Ancho del borde", Range(0.005, 0.3)) = 0.08
        [HDR] _EdgeColor ("Color del borde", Color) = (4, 1.2, 0.15, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "Dissolve"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off      // se ve por dentro cuando el objeto se esta deshaciendo

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "../Includes/ShaderLabCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float _Dissolve;
                float _AutoAnimate;
                float _AutoSpeed;
                float _Phase;
                float _NoiseScale;
                float _WorldSpaceNoise;
                float _EdgeWidth;
                half4 _EdgeColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS;
                output.positionOS = input.positionOS.xyz;
                output.positionWS = pos.positionWS;
                output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input, FRONT_FACE_TYPE isFrontFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                // ---- 1) Cuanto estamos disolviendo (0..1) ----
                // Onda triangular: sube de 0 a 1 y vuelve a bajar. Asi el efecto se repite solo.
                float autoValue = abs(frac(_Time.y * _AutoSpeed + _Phase) * 2.0 - 1.0);
                float dissolve = lerp(_Dissolve, autoValue, step(0.5, _AutoAnimate));

                // ---- 2) Ruido: es nuestra "mascara". Cada punto del objeto tiene un valor 0..1 ----
                float3 noisePos = lerp(input.positionOS, input.positionWS, step(0.5, _WorldSpaceNoise));
                float n = SL_FBM3D(noisePos * _NoiseScale);

                // ---- 3) Umbral: se descartan los puntos cuyo ruido sea menor que "cut" ----
                // cut va de -ancho (todo visible) a 1 (todo invisible)
                float cut = dissolve * (1.0 + _EdgeWidth) - _EdgeWidth;
                clip(n - cut);

                // ---- 4) Borde: la franja que esta justo por encima del umbral ----
                float edge = 1.0 - saturate((n - cut) / _EdgeWidth);

                // ---- 5) Iluminacion simple (Lambert + ambiente) ----
                float3 N = normalize(input.normalWS);
                N = IS_FRONT_VFACE(isFrontFace, N, -N);       // si vemos la cara de adentro, invertimos la normal
                Light mainLight = GetMainLight();
                float ndl = saturate(dot(N, mainLight.direction)) * 0.8 + 0.2;
                half3 color = _BaseColor.rgb * (ndl * mainLight.color + SampleSH(N) * 0.6);

                // ---- 6) Mezclar el borde brillante ----
                color = lerp(color, _EdgeColor.rgb, edge * step(0.0001, dissolve));
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
