// =====================================================================================
//  00 - LIQUID FILL  ("Hello Shader")                        Nivel: MUY BASICO
// =====================================================================================
//  QUE APRENDES
//   1. La estructura de un shader: Properties -> SubShader -> Pass -> vertex/fragment.
//   2. Como los parametros de Properties aparecen en el Inspector del material.
//   3. Usar la posicion del vertice en ESPACIO LOCAL (Object Space) para decidir que se dibuja.
//   4. clip(): descartar pixeles.
//   5. Dibujar las dos caras (Cull Off) y distinguirlas con SV_IsFrontFace.
//
//  EN UN JUEGO: pociones, frascos de vida/mana, tanques, liquido dentro de un recipiente.
//
//  NOTA: este shader asume una esfera de Unity (radio 0.5, y va de -0.5 a 0.5). Si lo usas
//  con otra malla ajusta _MinY y _MaxY.
// =====================================================================================
Shader "ShaderLab/Objects/00 Liquid Fill"
{
    // ---- PROPERTIES: lo que el artista puede editar desde el material ------------------
    Properties
    {
        _ColorBottom  ("Color del fondo",       Color) = (0.45, 0.02, 0.06, 1)
        _ColorTop     ("Color cerca de la superficie", Color) = (1.0, 0.25, 0.25, 1)
        _SurfaceColor ("Color de la tapa (superficie)", Color) = (1.0, 0.65, 0.65, 1)
        _Fill         ("Nivel de llenado",      Range(0, 1))    = 0.6
        _FillPulse    ("Oscilacion del nivel",  Range(0, 0.3))  = 0.0
        _WobbleAmount ("Oleaje (altura)",       Range(0, 0.15)) = 0.04
        _WobbleSpeed  ("Oleaje (velocidad)",    Range(0, 8))    = 2.5
        _RimPower     ("Brillo de borde",       Range(0.5, 8))  = 3
        [HideInInspector] _MinY ("Y minimo de la malla", Float) = -0.5
        [HideInInspector] _MaxY ("Y maximo de la malla", Float) = 0.5
    }

    SubShader
    {
        // Le decimos a Unity que este shader es para URP y que es opaco.
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off          // dibujamos caras de afuera Y de adentro (para ver la "tapa" del liquido)

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Las variables de Properties se declaran dentro de este CBUFFER.
            // (Necesario para que funcione el "SRP Batcher", que hace el render mas rapido.)
            CBUFFER_START(UnityPerMaterial)
                half4 _ColorBottom;
                half4 _ColorTop;
                half4 _SurfaceColor;
                float _Fill;
                float _FillPulse;
                float _WobbleAmount;
                float _WobbleSpeed;
                float _RimPower;
                float _MinY;
                float _MaxY;
            CBUFFER_END

            // Datos que llegan de la malla (por cada vertice)
            struct Attributes
            {
                float4 positionOS : POSITION;   // posicion en espacio local
                float3 normalOS   : NORMAL;
            };

            // Datos que pasamos del vertex shader al fragment shader
            struct Varyings
            {
                float4 positionCS : SV_POSITION;  // posicion en pantalla (clip space)
                float3 positionOS : TEXCOORD0;    // la posicion local, para decidir el nivel del liquido
                float3 normalWS   : TEXCOORD1;
                float3 viewDirWS  : TEXCOORD2;
            };

            // ---------------- VERTEX SHADER: se ejecuta una vez por vertice ----------------
            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = pos.positionCS;
                output.positionOS = input.positionOS.xyz;
                output.normalWS   = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS  = GetWorldSpaceViewDir(pos.positionWS);
                return output;
            }

            // ---------------- FRAGMENT SHADER: se ejecuta una vez por pixel ----------------
            half4 Frag(Varyings input, FRONT_FACE_TYPE isFrontFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                // 1) Altura del liquido en espacio local. _Fill 0..1 -> de _MinY a _MaxY.
                float fill  = saturate(_Fill + sin(_Time.y * 1.3) * _FillPulse);
                float level = lerp(_MinY, _MaxY, fill);

                // 2) Oleaje: movemos la superficie con senos que dependen de X, Z y del tiempo.
                float t = _Time.y * _WobbleSpeed;
                float wobble = (sin(input.positionOS.x * 9.0 + t) + sin(input.positionOS.z * 7.0 + t * 1.3)) * 0.5;
                float surfaceY = level + wobble * _WobbleAmount;

                // 3) clip(x): si x < 0 el pixel se descarta. Queremos conservar lo que esta POR DEBAJO.
                clip(surfaceY - input.positionOS.y);

                // 4) Si vemos la cara de ADENTRO de la esfera, lo que estamos viendo es la "tapa".
                if (!IS_FRONT_VFACE(isFrontFace, true, false))
                    return _SurfaceColor;

                // 5) Degradado vertical: oscuro en el fondo, claro cerca de la superficie.
                float h = saturate((input.positionOS.y - _MinY) / max(surfaceY - _MinY, 0.0001));
                half3 color = lerp(_ColorBottom.rgb, _ColorTop.rgb, h);

                // 6) Borde brillante (rim) usando Fresnel.
                float3 N = normalize(input.normalWS);
                float3 V = normalize(input.viewDirWS);
                float rim = pow(1.0 - saturate(dot(N, V)), _RimPower);
                color += rim * _ColorTop.rgb * 0.8;

                // 7) Linea clara justo debajo de la superficie.
                float edge = 1.0 - smoothstep(0.0, 0.05, surfaceY - input.positionOS.y);
                color = lerp(color, _SurfaceColor.rgb, edge);

                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
