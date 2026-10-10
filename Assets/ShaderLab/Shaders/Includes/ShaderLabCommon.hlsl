#ifndef SHADERLAB_COMMON_INCLUDED
#define SHADERLAB_COMMON_INCLUDED

// =====================================================================================
//  SHADER LAB - Funciones de utilidad compartidas
// =====================================================================================
//  Un archivo .hlsl "include" sirve para no copiar y pegar las mismas funciones en cada
//  shader. Se usa con:  #include "../Includes/ShaderLabCommon.hlsl"
//
//  Aqui viven las funciones de RUIDO (noise) que usan varios shaders. Un ruido es una
//  funcion que, dado un punto, devuelve un numero "pseudo-aleatorio pero suave" entre 0 y 1.
//  Con ruido se hacen nubes, lava, agua, disolver, plasma, fuego, etc.
// =====================================================================================

// ---- Hash: convierte una coordenada en un numero "aleatorio" estable (0..1) ----------
// "Estable" significa que la misma entrada SIEMPRE da la misma salida (no es random real).
float SL_Hash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float SL_Hash31(float3 p)
{
    p = frac(p * 0.1031);
    p += dot(p, p.zyx + 31.32);
    return frac((p.x + p.y) * p.z);
}

// ---- Value Noise 2D -------------------------------------------------------------------
// Idea: dividimos el plano en celdas, cada esquina tiene un valor aleatorio (hash) y
// interpolamos suavemente entre las 4 esquinas.
float SL_ValueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);          // curva "smoothstep" para suavizar

    float a = SL_Hash21(i);
    float b = SL_Hash21(i + float2(1, 0));
    float c = SL_Hash21(i + float2(0, 1));
    float d = SL_Hash21(i + float2(1, 1));

    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

// ---- Value Noise 2D que se REPITE sin costuras (tileable) -----------------------------
// "period" = cada cuantas celdas se repite el patron. Sirve para texturas que dan la vuelta
// (por ejemplo una esfera, donde u=0 y u=1 son el mismo borde).
float2 SL_Wrap(float2 cell, float2 period)
{
    return cell - period * floor(cell / period);
}

float SL_ValueNoiseTile(float2 p, float2 period)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);

    float a = SL_Hash21(SL_Wrap(i,                period));
    float b = SL_Hash21(SL_Wrap(i + float2(1, 0), period));
    float c = SL_Hash21(SL_Wrap(i + float2(0, 1), period));
    float d = SL_Hash21(SL_Wrap(i + float2(1, 1), period));

    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

// ---- Value Noise 3D -------------------------------------------------------------------
float SL_ValueNoise3D(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);

    float n000 = SL_Hash31(i);
    float n100 = SL_Hash31(i + float3(1, 0, 0));
    float n010 = SL_Hash31(i + float3(0, 1, 0));
    float n110 = SL_Hash31(i + float3(1, 1, 0));
    float n001 = SL_Hash31(i + float3(0, 0, 1));
    float n101 = SL_Hash31(i + float3(1, 0, 1));
    float n011 = SL_Hash31(i + float3(0, 1, 1));
    float n111 = SL_Hash31(i + float3(1, 1, 1));

    float x00 = lerp(n000, n100, f.x);
    float x10 = lerp(n010, n110, f.x);
    float x01 = lerp(n001, n101, f.x);
    float x11 = lerp(n011, n111, f.x);

    return lerp(lerp(x00, x10, f.y), lerp(x01, x11, f.y), f.z);
}

// ---- FBM (Fractal Brownian Motion) ----------------------------------------------------
// Sumar varias "capas" (octavas) de ruido: cada capa tiene el doble de frecuencia y la
// mitad de amplitud. Da un resultado con detalle grande Y detalle fino (nubes, roca, lava).
float SL_FBM3D(float3 p)
{
    float v = 0.0;
    float a = 0.5;
    for (int k = 0; k < 4; k++)
    {
        v += a * SL_ValueNoise3D(p);
        p *= 2.0;
        a *= 0.5;
    }
    return v / 0.9375;      // 0.5 + 0.25 + 0.125 + 0.0625 = 0.9375 -> normaliza a 0..1
}

float SL_FBMTile(float2 p, float2 period)
{
    float v = 0.0;
    float a = 0.5;
    for (int k = 0; k < 4; k++)
    {
        v += a * SL_ValueNoiseTile(p, period);
        p *= 2.0;
        period *= 2.0;
        a *= 0.5;
    }
    return v / 0.9375;
}

// ---- Fresnel ---------------------------------------------------------------------------
// Vale 0 cuando miras la superficie de frente y 1 cuando la ves de lado (en el borde).
// Es la base de escudos, hologramas, rim light, cristal, agua...
float SL_Fresnel(float3 normalWS, float3 viewDirWS, float power)
{
    return pow(1.0 - saturate(dot(normalize(normalWS), normalize(viewDirWS))), power);
}

#endif // SHADERLAB_COMMON_INCLUDED
