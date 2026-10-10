// =====================================================================================
//  03 - ESCUDO DE PLASMA (Fresnel + ruido + interseccion con la escena)   Nivel: MEDIO
// =====================================================================================
//  QUE APRENDES
//   1. FRESNEL: brillo que depende del angulo entre la camara y la superficie.
//   2. Transparencia ADITIVA (Blend One One): la luz se suma a lo que hay detras.
//   3. RUIDO 3D animado para generar "venas" de energia.
//   4. DEPTH TEXTURE: leer la profundidad de la escena para detectar donde el escudo
//      "toca" otros objetos y dibujar un anillo brillante de interseccion.
//
//  REQUISITO: en el URP Asset activa "Depth Texture" (el constructor del museo ya lo hace).
//
//  EN UN JUEGO: domos de proteccion, campos de fuerza, burbujas, portales.
//
//  RETO: cambia _FresnelPower y mira como cambia el borde. Sube _IntersectWidth.
// =====================================================================================
Shader "ShaderLab/Objects/Plasma Shield"
{
    Properties
    {
        [HDR] _Color  ("Color del plasma", Color) = (0.1, 0.8, 2.0, 1)
        _FresnelPower ("Potencia del borde (Fresnel)", Range(0.5, 8)) = 2.5
        _FresnelIntensity ("Intensidad del borde", Range(0, 3)) = 1.2
        _NoiseScale   ("Escala del plasma", Range(0.5, 8)) = 3
        _NoiseSpeed   ("Velocidad del plasma", Range(0, 2)) = 0.35
        _VeinSharpness ("Finura de las venas", Range(1, 20)) = 7
        _VeinIntensity ("Intensidad de las venas", Range(0, 3)) = 1.2
        _IntersectWidth ("Ancho de interseccion (m)", Range(0.01, 1)) = 0.25
        _IntersectIntensity ("Intensidad de interseccion", Range(0, 5)) = 2.5
        _BaseGlow     ("Brillo base", Range(0, 0.5)) = 0.06
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent" }

        Pass
        {
            Name "Shield"
            Tags { "LightMode" = "UniversalForward" }

            Blend One One         // aditivo: color_final = color_pantalla + color_escudo
            ZWrite Off            // los transparentes no escriben profundidad
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "../Includes/ShaderLabCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _FresnelPower;
                float _FresnelIntensity;
                float _NoiseScale;
                float _NoiseSpeed;
                float _VeinSharpness;
                float _VeinIntensity;
                float _IntersectWidth;
                float _IntersectIntensity;
                float _BaseGlow;
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
                float3 normalWS   : TEXCOORD1;
                float3 viewDirWS  : TEXCOORD2;
                float  eyeDepth   : TEXCOORD3;   // profundidad de ESTE vertice vista desde la camara
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS;
                output.positionOS = input.positionOS.xyz;
                output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS  = GetWorldSpaceViewDir(pos.positionWS);
                output.eyeDepth   = -pos.positionVS.z;      // en view space la camara mira hacia -Z
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // ---------- 1) Fresnel: mas brillo en los bordes ----------
                float fresnel = SL_Fresnel(input.normalWS, input.viewDirWS, _FresnelPower);

                // ---------- 2) Plasma: ruido 3D que "sube" con el tiempo ----------
                // Usamos la posicion LOCAL, asi el patron no depende de la escala del objeto.
                float3 p = input.positionOS * _NoiseScale + float3(0, -_Time.y * _NoiseSpeed, 0);
                float n = SL_FBM3D(p);
                // |n*2-1| vale 0 donde el ruido cruza 0.5. Invertido y elevado = lineas finas brillantes.
                float veins = pow(saturate(1.0 - abs(n * 2.0 - 1.0)), _VeinSharpness);

                // ---------- 3) Interseccion con la escena (Scene Depth) ----------
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float diff = sceneDepth - input.eyeDepth;        // distancia entre el escudo y lo que hay detras
                float intersect = saturate(1.0 - diff / _IntersectWidth);
                intersect = intersect * intersect;               // cae mas rapido -> linea mas fina

                // ---------- 4) Combinar ----------
                float energy = _BaseGlow
                             + fresnel * _FresnelIntensity
                             + veins * _VeinIntensity * (0.35 + fresnel)
                             + intersect * _IntersectIntensity;

                return half4(_Color.rgb * energy, 1);
            }
            ENDHLSL
        }
    }
}
