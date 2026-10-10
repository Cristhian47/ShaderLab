#!/usr/bin/env python3
"""Genera los grafos de CAMARA (Fullscreen) en Assets/ShaderLab/ShaderGraphs/Screen.
Uso: SG_TEMPLATES=... python3 gen_screen.py <carpeta ShaderGraphs>
"""
import os, sys, json, math
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from sg import Graph

ROOT = sys.argv[1] if len(sys.argv) > 1 else "ShaderGraphs"
PS = "ShaderLab/ShaderGraph/Screen"
REG = []
LUM3 = (0.2126, 0.7152, 0.0722)


def new(name):
    return Graph(name, PS, "fullscreen")


def finish(g, file, title, level, key, concepts, hlsl):
    path = os.path.join(ROOT, "Screen", file + ".shadergraph")
    g.save(path)
    print("OK", path)
    REG.append(dict(file="Screen/" + file, title=title, level=level, kind="camara", key=key, concepts=concepts,
                    shader=g.path + "/" + g.name, hlsl=hlsl))


def v2(g, x, y, **k): return g.combine(x, y, **k)["RG"]
def v3(g, x, y, z, **k): return g.combine(x, y, z, **k)["RGB"]


def read_screen(g, uv=None, title="Color de la pantalla"):
    """Lee el color de la camara. uv=None -> Screen Position (toda la pantalla)."""
    if uv is None:
        uv = g.screenpos("default", name="Screen Position (UV de pantalla)")
    return g.sample_buffer(uv, name="URP Sample Buffer")["Output"]


def uv2(g):
    return g.swizzle(g.screenpos("default", name="Screen Position"), "xy", name="UV (0..1)")


def out(g, rgb):
    g.block("SurfaceDescription.BaseColor", rgb)
    g.block("SurfaceDescription.Alpha", 1.0)


def intensity(g, default=1.0):
    return g.prop_float("Intensidad", "_Intensity", default, (0, 1))


# -------------------------------------------------------------------------------------------
def s01():
    g = new("SG_S01_Grayscale")
    i = intensity(g)
    g.section("1 · Leer la pantalla", "URP Sample Buffer (Blit Source) = la imagen que la camara ya dibujo. Su UV es Screen Position.")
    col = g.rgb(read_screen(g))
    g.section("2 · Luminancia", "Un gris 'correcto' pesa mas el verde (el ojo es mas sensible): dot(color, (0.21, 0.72, 0.07)).")
    w = g.vec3(*LUM3, name="Pesos de luminancia")
    gray = g.dot(col, w, name="Luminancia").show()
    g.section("3 · Mezclar", "Lerp con Intensidad: 0 = color original, 1 = blanco y negro.")
    out(g, g.lerp(col, gray, i, name="Lerp").show())
    finish(g, "SG_S01_Grayscale", "Blanco y negro", 1, "Grayscale", ["Sample Buffer", "Dot Product", "Lerp"], "ShaderLab/Screen/S01_Grayscale")


def s02():
    g = new("SG_S02_Invert")
    i = intensity(g)
    g.section("1 · Leer la pantalla", "La imagen de la camara.")
    col = g.rgb(read_screen(g))
    g.section("2 · Invertir", "One Minus: 1 - color. Negro<->blanco, rojo<->cian.")
    inv = g.oneminus(col, name="One Minus").show()
    g.section("3 · Mezclar", "Intensidad entre normal e invertido.")
    out(g, g.lerp(col, inv, i))
    finish(g, "SG_S02_Invert", "Invertir colores", 1, "Invert", ["One Minus", "Lerp"], "ShaderLab/Screen/S02_Invert")


def s03():
    g = new("SG_S03_Sepia")
    i = intensity(g)
    vg = g.prop_float("Oscurecer bordes", "_Vignette", 0.35, (0, 1))
    g.section("1 · Leer la pantalla", "")
    col = g.rgb(read_screen(g))
    g.section("2 · Matriz sepia", "Cada canal nuevo = combinacion de R,G,B originales. 3 Dot Product con 3 vectores = una matriz de color.")
    r = g.dot(col, g.vec3(0.393, 0.769, 0.189, name="Fila R"))
    gg = g.dot(col, g.vec3(0.349, 0.686, 0.168, name="Fila G"))
    b = g.dot(col, g.vec3(0.272, 0.534, 0.131, name="Fila B"))
    sepia = v3(g, r, gg, b, name="Sepia").show()
    g.section("3 · Bordes oscuros", "Distancia al centro al cuadrado: dot(d,d). Crece hacia las esquinas.")
    d = g.sub(uv2(g), 0.5, name="UV - 0.5")
    vig = g.oneminus(g.mul(g.sat(g.mul(g.dot(d, d), 2.0)), vg), name="Factor de vineta")
    g.section("4 · Mezclar", "")
    out(g, g.lerp(col, g.mul(sepia, vig), i))
    finish(g, "SG_S03_Sepia", "Sepia (foto antigua)", 2, "Sepia", ["Dot Product", "Combine", "UV del centro"], "ShaderLab/Screen/S03_Sepia")


def s04():
    g = new("SG_S04_Vignette")
    c = g.prop_color("Color de la vineta", "_Color", (0, 0, 0, 1))
    it = g.prop_float("Intensidad", "_Intensity", 0.85, (0, 1))
    rad = g.prop_float("Radio (donde empieza)", "_Radius", 0.8, (0.1, 1.2))
    sf = g.prop_float("Suavidad", "_Softness", 0.45, (0.01, 1))
    rd = g.prop_float("Redondez (1 = circulo)", "_Roundness", 1.0, (0, 1))
    g.section("1 · Leer la pantalla", "")
    col = g.rgb(read_screen(g))
    g.section("2 · Distancia al centro", "UV - 0.5 centra el origen. Multiplicamos X por la proporcion de pantalla (Screen: ancho/alto) para que sea circulo y no ovalo.")
    d = g.split(g.sub(uv2(g), 0.5, name="UV - 0.5"))
    sc = g.screen_size()
    aspect = g.div(sc["Width"], sc["Height"], name="Aspecto")
    dx = g.mul(d["R"], g.lerp(1.0, aspect, rd), name="X corregida")
    dist = g.mul(g.length(v2(g, dx, d["G"])), 1.6, name="Distancia").show()
    g.section("3 · Borde suave", "Smoothstep(radio - suavidad, radio, distancia): 0 al centro, 1 en el borde.")
    vig = g.smooth(g.sub(rad, sf), rad, dist, name="Mascara de vineta").show()
    g.section("4 · Mezclar", "")
    out(g, g.lerp(col, g.rgb(c), g.mul(vig, it)))
    finish(g, "SG_S04_Vignette", "Vineta", 2, "Vignette", ["Screen node", "Length", "Smoothstep"], "ShaderLab/Screen/S04_Vignette")


def s05():
    g = new("SG_S05_Pixelate")
    ps = g.prop_float("Tamano del pixel", "_PixelSize", 8, (1, 40))
    g.section("1 · Cuantas celdas", "Pantalla (ancho,alto) / tamano del pixel = cuantos 'pixeles grandes' caben.")
    sc = g.screen_size()
    cells = g.div(v2(g, sc["Width"], sc["Height"]), g.max(ps, 1.0), name="Celdas").show()
    g.section("2 · Centro de cada celda", "floor(uv*celdas) elige la celda; +0.5 va al centro; / celdas vuelve a 0..1.")
    snapped = g.div(g.add(g.floor(g.mul(uv2(g), cells)), 0.5), cells, name="UV ajustada a la celda")
    g.section("3 · Leer ese punto", "Todos los pixeles de una celda leen el MISMO punto -> bloques.")
    out(g, g.rgb(read_screen(g, snapped)))
    finish(g, "SG_S05_Pixelate", "Pixelar", 2, "Pixelate", ["Screen node", "Floor", "UV personalizada en Sample Buffer"], "ShaderLab/Screen/S05_Pixelate")


def s06():
    g = new("SG_S06_RetroPalette")
    c0 = g.prop_color("Color 0 (mas oscuro)", "_Color0", (0.06, 0.22, 0.06, 1))
    c1 = g.prop_color("Color 1", "_Color1", (0.19, 0.38, 0.19, 1))
    c2 = g.prop_color("Color 2", "_Color2", (0.55, 0.67, 0.06, 1))
    c3 = g.prop_color("Color 3 (mas claro)", "_Color3", (0.61, 0.74, 0.06, 1))
    dth = g.prop_float("Dithering", "_Dither", 0.35, (0, 1))
    i = intensity(g)
    g.section("1 · Brillo", "Pasamos la imagen a un solo valor de brillo (0..1).")
    col = g.rgb(read_screen(g))
    lum = g.dot(col, g.vec3(*LUM3, name="Pesos de luminancia"), name="Luminancia")
    g.section("2 · Dithering", "El nodo Dither suma un patron fijo (Bayer) segun el pixel: los degradados se vuelven tramas.")
    d = g.dither(lum, g.screenpos("default", name="Screen Position"), name="Dither")
    lumd = g.lerp(lum, d, dth, name="Dither * cantidad").show()
    g.section("3 · Elegir 1 de 4 colores", "idx = floor(brillo*3.999) -> 0,1,2,3. Los Step + Lerp en cadena escogen la paleta.")
    idx = g.floor(g.mul(g.sat(lumd), 3.999), name="Indice 0..3").show()
    pal = g.rgb(c0)
    pal = g.lerp(pal, g.rgb(c1), g.step(0.5, idx))
    pal = g.lerp(pal, g.rgb(c2), g.step(1.5, idx))
    pal = g.lerp(pal, g.rgb(c3), g.step(2.5, idx), name="Color de la paleta").show()
    g.section("4 · Mezclar", "")
    out(g, g.lerp(col, pal, i))
    finish(g, "SG_S06_RetroPalette", "Paleta retro (Game Boy)", 3, "RetroPalette", ["Dither", "Floor", "Step", "Paleta"], "ShaderLab/Screen/S06_RetroPalette")


def s07():
    g = new("SG_S07_ChromaticAberration")
    am = g.prop_float("Cantidad", "_Amount", 0.012, (0, 0.05))
    pu = g.prop_float("Pulso animado", "_Pulse", 0.5, (0, 1))
    ps = g.prop_float("Velocidad del pulso", "_PulseSpeed", 3, (0, 10))
    g.section("1 · Direccion desde el centro", "uv - 0.5 apunta hacia afuera. Mas lejos del centro, mayor separacion.")
    uv = uv2(g)
    dr = g.sub(uv, 0.5, name="Direccion")
    g.section("2 · Cantidad con pulso", "amount * (1 + pulso*sin(tiempo)).")
    t = g.time()["Time"]
    amt = g.mul(am, g.add(1.0, g.mul(pu, g.sin(g.mul(t, ps)))), name="Cantidad animada")
    g.section("3 · Tres lecturas", "Rojo se lee un poco hacia afuera, verde en su sitio, azul hacia adentro.")
    off = g.mul(dr, amt, name="Desplazamiento")
    r = g.split(read_screen(g, g.add(uv, off, name="UV + offset")))["R"]
    gr = g.split(read_screen(g, uv))["G"]
    b = g.split(read_screen(g, g.sub(uv, off, name="UV - offset")))["B"]
    g.section("4 · Juntar", "Combine arma el color con los tres canales leidos en lugares distintos.")
    out(g, v3(g, r, gr, b, name="RGB separado").show())
    finish(g, "SG_S07_ChromaticAberration", "Aberracion cromatica", 3, "ChromaticAberration", ["Split", "Combine", "3 lecturas de UV"], "ShaderLab/Screen/S07_ChromaticAberration")


def s08():
    g = new("SG_S08_Underwater")
    st = g.prop_float("Fuerza de la distorsion", "_Strength", 0.006, (0, 0.03))
    fr = g.prop_float("Frecuencia de las ondas", "_Frequency", 28, (1, 80))
    sp = g.prop_float("Velocidad", "_Speed", 1.8, (0, 6))
    tint = g.prop_color("Color del agua", "_Tint", (0.35, 0.75, 0.95, 1))
    ta = g.prop_float("Cantidad de tinte", "_TintAmount", 0.55, (0, 1))
    g.section("1 · Ondas en las UV", "offset = (sin(y*frec + t), cos(x*frec*1.3 + t*1.2)) * fuerza. Movemos DONDE leemos, no el color.")
    uv = uv2(g)
    u = g.split(uv)
    t = g.mul(g.time()["Time"], sp, name="Tiempo")
    ox = g.sin(g.add(g.mul(u["G"], fr), t))
    oy = g.cos(g.add(g.mul(g.mul(u["R"], fr), 1.3), g.mul(t, 1.2)))
    off = g.mul(v2(g, ox, oy), st, name="Offset ondulante").show()
    g.section("2 · Leer distorsionado", "")
    col = g.rgb(read_screen(g, g.add(uv, off, name="UV + offset")))
    g.section("3 · Tinte azul", "Multiplicar por el color del agua. Los bordes se oscurecen mas.")
    tinted = g.mul(col, g.lerp(g.vec3(1, 1, 1), g.rgb(tint), ta), name="Tinte")
    d = g.sub(uv, 0.5)
    edge = g.sat(g.mul(g.dot(d, d), 2.0), name="Borde")
    out(g, g.lerp(tinted, g.mul(g.mul(tinted, g.rgb(tint)), 0.6), edge))
    finish(g, "SG_S08_Underwater", "Bajo el agua", 3, "Underwater", ["UV distorsionadas", "Sin/Cos", "Tint"], "ShaderLab/Screen/S08_Underwater")


def s09():
    g = new("SG_S09_Blur")
    rad = g.prop_float("Radio (en pixeles)", "_Radius", 2.5, (0, 8))
    g.section("1 · Tamano de un pixel", "texel = 1 / tamano de pantalla. Es la distancia entre dos pixeles vecinos en UV.")
    sc = g.screen_size()
    texel = g.div(1.0, v2(g, sc["Width"], sc["Height"]), name="Texel").show()
    uv = uv2(g)
    consts = {}
    for y in (-1, 0, 1):
        for x in (-1, 0, 1):
            if (x, y) != (0, 0):
                consts[(x, y)] = g.vec2(x, y, name="Vecino (%d,%d)" % (x, y))
    acc = [None]

    def tap(o, x, y, w):
        off = o if (x, y) == (0, 0) else g.mul(o, consts[(x, y)])
        s = read_screen(g, g.add(uv, off) if (x, y) != (0, 0) else uv)
        term = g.mul(s, float(w))
        acc[0] = term if acc[0] is None else g.add(acc[0], term)

    g.section("2 · Anillo interior (3x3)", "Promediamos 9 lecturas alrededor del pixel. El centro pesa mas (4), los lados 2, las esquinas 1.")
    o1 = g.mul(texel, g.mul(rad, 0.5), name="Offset anillo 1")
    for y in (-1, 0, 1):
        for x in (-1, 0, 1):
            tap(o1, x, y, (2 - abs(x)) * (2 - abs(y)) * (2 if (x, y) == (0, 0) else 1))
    g.section("3 · Anillo exterior", "Mismo patron al doble de distancia: blur mas ancho con pocas lecturas.")
    o2 = g.mul(texel, rad, name="Offset anillo 2")
    for y in (-1, 0, 1):
        for x in (-1, 0, 1):
            if (x, y) != (0, 0):
                tap(o2, x, y, (2 - abs(x)) * (2 - abs(y)))
    g.section("4 · Promedio", "Dividir por la suma de pesos (32).")
    out(g, g.rgb(g.div(acc[0], 32.0, name="Suma / 32").show()))
    finish(g, "SG_S09_Blur", "Desenfoque (Blur)", 4, "Blur", ["Muchas lecturas", "Pesos", "Texel"], "ShaderLab/Screen/S09_Blur")


def s10():
    g = new("SG_S10_EdgeDetect")
    ec = g.prop_color("Color del borde", "_EdgeColor", (0, 0, 0, 1))
    th = g.prop_float("Umbral de deteccion", "_Threshold", 0.12, (0, 1))
    sf = g.prop_float("Suavidad", "_Softness", 0.08, (0.001, 0.5))
    tk = g.prop_float("Grosor (pixeles)", "_Thickness", 1.2, (0.5, 4))
    oe = g.prop_bool("Mostrar solo bordes", "_OnlyEdges", False)
    g.section("1 · Pixel vecino", "texel = grosor / tamano de pantalla.")
    sc = g.screen_size()
    texel = g.div(tk, v2(g, sc["Width"], sc["Height"]), name="Texel * grosor")
    uv = uv2(g)
    g.section("2 · Brillo de los 8 vecinos", "Leemos los 8 pixeles que rodean al actual y convertimos cada uno a brillo.")
    w = g.vec4(LUM3[0], LUM3[1], LUM3[2], 0.0, name="Pesos de luminancia")
    L = {}
    for (x, y), nm in {(-1, 1): "tl", (0, 1): "t", (1, 1): "tr", (-1, 0): "l", (1, 0): "r", (-1, -1): "bl", (0, -1): "b", (1, -1): "br"}.items():
        off = g.mul(texel, g.vec2(x, y, name="Vecino %s" % nm))
        L[nm] = g.dot(read_screen(g, g.add(uv, off)), w, name="Brillo " + nm)
    g.section("3 · Sobel", "Dos filtros (horizontal y vertical) restan un lado del otro. Donde hay un cambio brusco de brillo, el resultado es grande.")
    gx = g.sub(g.add(g.add(L["tr"], g.mul(L["r"], 2.0)), L["br"]), g.add(g.add(L["tl"], g.mul(L["l"], 2.0)), L["bl"]), name="Gradiente X")
    gy = g.sub(g.add(g.add(L["bl"], g.mul(L["b"], 2.0)), L["br"]), g.add(g.add(L["tl"], g.mul(L["t"], 2.0)), L["tr"]), name="Gradiente Y")
    edge = g.length(v2(g, gx, gy), name="Fuerza del borde").show()
    edge = g.smooth(th, g.add(th, sf), edge, name="Borde con umbral").show()
    g.section("4 · Pintar", "El borde se dibuja con su color, sobre la imagen (o sobre blanco).")
    col = g.rgb(read_screen(g))
    base = g.branch(oe, g.vec3(1, 1, 1), col, name="Solo bordes?")
    out(g, g.lerp(base, g.rgb(ec), edge))
    finish(g, "SG_S10_EdgeDetect", "Deteccion de bordes (Sobel)", 4, "EdgeDetect", ["Sobel", "Branch", "Boolean"], "ShaderLab/Screen/S10_EdgeDetect")


def s11():
    g = new("SG_S11_CRT")
    cv = g.prop_float("Curvatura de la pantalla", "_Curvature", 0.35, (0, 1))
    sn = g.prop_float("Cantidad de scanlines", "_ScanlineCount", 360, (100, 1000))
    ss = g.prop_float("Fuerza de scanlines", "_ScanlineStrength", 0.35, (0, 1))
    ms = g.prop_float("Mascara RGB", "_MaskStrength", 0.25, (0, 1))
    bl = g.prop_float("Sangrado de color", "_Bleed", 0.0025, (0, 0.01))
    vg = g.prop_float("Vineta", "_Vignette", 0.5, (0, 1))
    fl = g.prop_float("Parpadeo", "_Flicker", 0.04, (0, 0.2))
    g.section("1 · Curvar la pantalla", "UV -1..1; las empujamos hacia afuera mas cuanto mas lejos del centro (dot(c,c)).")
    cuv = g.sub(g.mul(uv2(g), 2.0), 1.0, name="UV -1..1")
    curved = g.mul(cuv, g.add(1.0, g.mul(g.mul(g.dot(cuv, cuv), cv), 0.25)), name="Curvada")
    suv = g.add(g.mul(curved, 0.5), 0.5, name="UV curvada").show()
    g.section("2 · Fuera de la pantalla = negro", "Cuatro Step multiplicados valen 1 solo si la UV esta dentro de 0..1.")
    s = g.split(suv)
    inside = g.mul(g.mul(g.step(0.0, s["R"]), g.step(s["R"], 1.0)), g.mul(g.step(0.0, s["G"]), g.step(s["G"], 1.0)), name="Dentro?")
    g.section("3 · Sangrado RGB", "Rojo y azul se leen un poco desplazados en X: franjas de color como en un CRT.")
    bo = v2(g, bl, 0.0, name="Offset de sangrado")
    r = g.split(read_screen(g, g.add(suv, bo)))["R"]
    gg = g.split(read_screen(g, suv))["G"]
    b = g.split(read_screen(g, g.sub(suv, bo)))["B"]
    c = v3(g, r, gg, b, name="Color")
    g.section("4 · Scanlines", "Un seno en Y oscurece lineas horizontales.")
    scan = g.add(g.mul(g.sin(g.mul(g.mul(s["G"], sn), math.pi)), 0.5), 0.5, name="Scanlines").show()
    c = g.mul(c, g.lerp(1.0, scan, ss))
    g.section("5 · Mascara RGB", "Cada columna de pixeles realza un canal (R, G o B) como los fosforos del CRT.")
    sc = g.screen_size()
    col = g.modulo(g.floor(g.mul(s["R"], sc["Width"])), 3.0, name="Columna 0,1,2")
    chans = []
    for i in range(3):
        is_i = g.oneminus(g.step(0.5, g.abs(g.sub(col, float(i)))), name="Columna == %d" % i)
        chans.append(g.lerp(0.65, 1.0, is_i))
    mask = v3(g, chans[0], chans[1], chans[2], name="Mascara")
    c = g.mul(c, g.lerp(g.vec3(1, 1, 1), mask, ms))
    g.section("6 · Vineta y parpadeo", "Bordes mas oscuros y un parpadeo rapido de brillo.")
    d = g.sub(suv, 0.5)
    c = g.mul(c, g.oneminus(g.mul(g.mul(g.dot(d, d), 2.0), vg)))
    t = g.time()["Time"]
    c = g.mul(c, g.oneminus(g.mul(fl, g.add(g.mul(g.sin(g.mul(t, 60.0)), 0.5), 0.5))))
    out(g, g.mul(g.mul(c, 1.1), inside, name="Resultado"))
    finish(g, "SG_S11_CRT", "Monitor CRT", 5, "CRT", ["UV curvada", "Scanlines", "Modulo", "Combinar efectos"], "ShaderLab/Screen/S11_CRT")


def s12():
    g = new("SG_S12_DepthFog")
    fc = g.prop_color("Color de la niebla", "_FogColor", (0.55, 0.62, 0.75, 1))
    fs = g.prop_float("Distancia donde empieza (m)", "_FogStart", 4, (0, 60))
    fe = g.prop_float("Distancia de niebla total (m)", "_FogEnd", 40, (1, 120))
    dn = g.prop_float("Densidad", "_Density", 1, (0, 1))
    sd = g.prop_bool("Mostrar solo la profundidad", "_ShowDepth", False)
    g.section("1 · Distancia por pixel", "Scene Depth (Eye) = distancia en METROS entre la camara y lo que hay en cada pixel.")
    eye = g.scene_depth("eye", name="Scene Depth (Eye)").show()
    g.section("2 · Cantidad de niebla", "(distancia - inicio) / (fin - inicio), limitado a 0..1, por la densidad.")
    fog = g.mul(g.sat(g.div(g.sub(eye, fs), g.max(g.sub(fe, fs), 0.001))), dn, name="Niebla 0..1").show()
    g.section("3 · Mezclar con la imagen", "")
    col = g.rgb(read_screen(g))
    fogged = g.lerp(col, g.rgb(fc), fog, name="Imagen con niebla")
    g.section("4 · Modo depuracion", "Un Boolean muestra la profundidad como gris para entender la distancia.")
    dv = g.sat(g.div(eye, g.max(fe, 0.001)))
    out(g, g.branch(sd, v3(g, dv, dv, dv), fogged, name="Mostrar profundidad?"))
    finish(g, "SG_S12_DepthFog", "Niebla por profundidad", 4, "DepthFog", ["Scene Depth", "Distancia en metros", "Boolean"], "ShaderLab/Screen/S12_DepthFog")


if __name__ == "__main__":
    for f in (s01, s02, s03, s04, s05, s06, s07, s08, s09, s10, s11, s12):
        f()
    json.dump(REG, open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "_screen_manifest.json"), "w"), indent=1, ensure_ascii=False)
