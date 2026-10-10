#!/usr/bin/env python3
"""Genera los grafos de OBJETOS (y los de Fundamentos) en Assets/ShaderLab/ShaderGraphs/.
Uso: SG_TEMPLATES=.../templates.json python3 gen_objects.py <carpeta Assets/ShaderLab/ShaderGraphs>
"""
import os, sys, json
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from sg import Graph

REG = []   # manifiesto


def reg(g, file, title, level, kind, key, concepts, hlsl=None, extra=None):
    REG.append(dict(file=file, title=title, level=level, kind=kind, key=key, concepts=concepts,
                    shader=g.path + "/" + g.name, hlsl=hlsl or "", **(extra or {})))


def v2(g, x, y, **k):
    return g.combine(x, y, **k)["RG"]


def v3(g, x, y, z, **k):
    return g.combine(x, y, z, **k)["RGB"]


def noise3(g, p, scale=1.0):
    """Ruido 3D aproximado: promedio de dos Gradient Noise 2D (planos XY y ZY)."""
    xy = g.swizzle(p, "xy", name="XY")
    zy = g.swizzle(p, "zy", name="ZY")
    a = g.gnoise(xy, scale)
    b = g.gnoise(zy, scale)
    return g.mul(g.add(a, b, name="Sumar los dos ruidos"), 0.5, name="Promedio")


MAIN_LIGHT = """#if defined(SHADERGRAPH_PREVIEW)
    Direction = float3(0.5, 0.5, 0.0);
    LightColor = float3(1.0, 1.0, 1.0);
#else
    Direction = normalize(_MainLightPosition.xyz);
    LightColor = _MainLightColor.rgb;
#endif"""


def main_light(g):
    return g.custom("ShaderLab_MainLight", [], [("Direction", 3), ("LightColor", 3)], MAIN_LIGHT,
                    name="Luz principal (Custom Function)")


def out_dir_arg():
    return sys.argv[1] if len(sys.argv) > 1 else "ShaderGraphs"


ROOT = out_dir_arg()
PO = "ShaderLab/ShaderGraph/Objects"
PF = "ShaderLab/ShaderGraph/Fundamentos"


def save(g, sub, file):
    path = os.path.join(ROOT, sub, file + ".shadergraph")
    g.save(path)
    print("OK", path)


# ===========================================================================================
#  FUNDAMENTOS
# ===========================================================================================
def f1_color():
    g = Graph("F1_Color", PF, "unlit")
    g.section("Entrada: un color", "Una PROPIEDAD aparece en el Material (Inspector). Se arrastra desde el Blackboard.")
    c = g.prop_color("Color", "_BaseColor", (0.2, 0.6, 1.0, 1))
    g.section("Salida", "El Master Stack (a la derecha) decide que pinta el objeto. Base Color = el color de la superficie.")
    g.block("SurfaceDescription.BaseColor", c)
    save(g, "Fundamentos", "F1_Color")
    reg(g, "Fundamentos/F1_Color", "Mi primer color", 0, "fundamento", "F1",
        ["Master Stack", "Base Color", "Propiedad (Property)"])


def f2_gradient():
    g = Graph("F2_Gradiente", PF, "unlit")
    g.section("Posicion", "El nodo Position entrega donde esta cada punto de la malla. Y = altura.")
    p = g.pos("object")
    y = g.split(p)["G"]
    g.section("Remapear", "Remap convierte el rango -0.5..0.5 (esfera) a 0..1 para poder usarlo como mezcla.")
    t = g.remap(y, (-0.5, 0.5), (0, 1), name="Remap -0.5..0.5 a 0..1").show()
    g.section("Mezclar dos colores", "Lerp(A, B, T): con T=0 devuelve A, con T=1 devuelve B.")
    a = g.prop_color("Color abajo", "_ColorBottom", (0.1, 0.1, 0.6, 1))
    b = g.prop_color("Color arriba", "_ColorTop", (1.0, 0.4, 0.2, 1))
    mix = g.lerp(a, b, t, name="Lerp").show()
    g.block("SurfaceDescription.BaseColor", mix)
    save(g, "Fundamentos", "F2_Gradiente")
    reg(g, "Fundamentos/F2_Gradiente", "Gradiente por altura", 0, "fundamento", "F2",
        ["Position", "Split", "Remap", "Lerp"])


def f3_time():
    g = Graph("F3_Tiempo", PF, "unlit")
    g.section("Tiempo", "El nodo Time entrega segundos desde que arranco el juego. Sine Time ya viene como seno.")
    t = g.time()
    g.section("Onda 0..1", "Un seno va de -1 a 1; lo llevamos a 0..1 con (seno*0.5 + 0.5) para usarlo como mezcla.")
    s = g.sin(g.mul(t["Time"], g.prop_float("Velocidad", "_Speed", 2, (0, 10))), name="Seno")
    w = g.add(g.mul(s, 0.5), 0.5, name="Llevar a 0..1").show()
    g.section("Color animado", "El color late entre dos colores.")
    a = g.prop_color("Color A", "_ColorA", (0.05, 0.05, 0.3, 1))
    b = g.prop_color("Color B", "_ColorB", (1.0, 0.8, 0.1, 1))
    g.block("SurfaceDescription.BaseColor", g.lerp(a, b, w).show())
    save(g, "Fundamentos", "F3_Tiempo")
    reg(g, "Fundamentos/F3_Tiempo", "Animar con el tiempo", 0, "fundamento", "F3",
        ["Time", "Sine", "Multiply / Add", "Lerp"])


def f4_fresnel():
    g = Graph("F4_Fresnel", PF, "unlit")
    g.section("Fresnel", "Fresnel Effect = 1 en los bordes (donde la superficie se aleja de la camara) y 0 al centro.")
    f = g.fresnel(g.prop_float("Potencia", "_FresnelPower", 3, (0.5, 8))).show()
    g.section("Color", "Multiplicamos el borde por un color HDR (valores > 1 brillan con Bloom).")
    c = g.prop_color("Color", "_Color", (0.1, 0.8, 2.0, 1), hdr=True)
    g.block("SurfaceDescription.BaseColor", g.mul(c, f, name="Color * Fresnel"))
    save(g, "Fundamentos", "F4_Fresnel")
    reg(g, "Fundamentos/F4_Fresnel", "Brillo de borde (Fresnel)", 0, "fundamento", "F4",
        ["Fresnel Effect", "Color HDR", "Multiply"])


# ===========================================================================================
#  1. LIQUID FILL
# ===========================================================================================
def liquid():
    g = Graph("SG_00_LiquidFill", PO, "unlit", render_face="both", alpha_clip=True)
    cb = g.prop_color("Color del fondo", "_ColorBottom", (0.45, 0.02, 0.06, 1))
    ct = g.prop_color("Color cerca de la superficie", "_ColorTop", (1.0, 0.25, 0.25, 1))
    cs = g.prop_color("Color de la tapa", "_SurfaceColor", (1.0, 0.65, 0.65, 1))
    fill = g.prop_float("Nivel de llenado", "_Fill", 0.6, (0, 1))
    pulse = g.prop_float("Oscilacion del nivel", "_FillPulse", 0.0, (0, 0.3))
    wamp = g.prop_float("Oleaje (altura)", "_WobbleAmount", 0.04, (0, 0.15))
    wspd = g.prop_float("Oleaje (velocidad)", "_WobbleSpeed", 2.5, (0, 8))
    rimp = g.prop_float("Brillo de borde", "_RimPower", 3.0, (0.5, 8))

    g.section("1 · Nivel del liquido", "El nivel va de abajo (-0.5) a arriba (+0.5) de la esfera. Fill 0..1 se remapea a ese rango.")
    t = g.time()["Time"]
    f = g.sat(g.add(fill, g.mul(g.sin(g.mul(t, 1.3)), pulse)), name="Fill + pulso")
    level = g.remap(f, (0, 1), (-0.5, 0.5), name="Nivel (Y local)").show()

    g.section("2 · Oleaje de la superficie", "Dos senos (uno segun X, otro segun Z) desplazan la altura de la superficie.")
    p = g.pos("object")
    sp = g.split(p)
    wt = g.mul(t, wspd, name="Tiempo * velocidad")
    s1 = g.sin(g.add(g.mul(sp["R"], 9.0), wt))
    s2 = g.sin(g.add(g.mul(sp["B"], 7.0), g.mul(wt, 1.3)))
    wob = g.mul(g.add(s1, s2), 0.5, name="Oleaje -1..1")
    surf = g.add(level, g.mul(wob, wamp), name="Altura de la superficie").show()

    g.section("3 · Recortar (Alpha Clip)", "Si el punto esta POR ENCIMA de la superficie, Alpha=0 y se descarta. Threshold 0.5.")
    keep = g.step(sp["G"], surf, name="Step: 1 si Y <= superficie").show()

    g.section("4 · Color del liquido", "Gradiente fondo->superficie + brillo de borde (Fresnel) + tapa brillante en el corte.")
    h = g.sat(g.div(g.add(sp["G"], 0.5), g.max(g.add(surf, 0.5), 0.0001)), name="Altura normalizada")
    body = g.lerp(cb, ct, h)
    rim = g.mul(g.mul(g.fresnel(rimp), ct), 0.8, name="Rim")
    col = g.add(body, rim)
    edge = g.oneminus(g.smooth(0.0, 0.05, g.sub(surf, sp["G"])), name="Cerca de la superficie")
    col = g.lerp(col, cs, edge, name="Tapa")

    g.section("5 · Cara trasera", "Is Front Face: por dentro de la esfera se ve el color de la tapa (el liquido 'lleno').")
    final = g.branch(g.front_face(), col, cs, name="Branch cara frontal/trasera")
    g.block("SurfaceDescription.BaseColor", final)
    g.block("SurfaceDescription.Alpha", keep)
    g.block("SurfaceDescription.AlphaClipThreshold", 0.5)
    save(g, "Objects", "SG_00_LiquidFill")
    reg(g, "Objects/SG_00_LiquidFill", "Liquido en una esfera", 1, "objeto", "LiquidFill",
        ["Position (Object)", "Alpha Clip", "Is Front Face", "Fresnel", "Remap"], "ShaderLab/Objects/LiquidFill")


# ===========================================================================================
#  2. TOON
# ===========================================================================================
def toon():
    g = Graph("SG_01_Toon", PO, "unlit")
    base = g.prop_color("Color base", "_BaseColor", (0.25, 0.55, 0.95, 1))
    shadow = g.prop_color("Color de sombra", "_ShadowColor", (0.12, 0.10, 0.35, 1))
    steps = g.prop_float("Bandas de luz", "_Steps", 3, (1, 6))
    ssize = g.prop_float("Tamano del brillo", "_SpecSize", 0.08, (0, 0.5))
    scol = g.prop_color("Color del brillo", "_SpecColor2", (1, 1, 1, 1))
    ramt = g.prop_float("Luz de borde", "_RimAmount", 0.6, (0, 1))
    rcol = g.prop_color("Color de borde", "_RimColor", (0.7, 0.85, 1, 1))

    g.section("1 · Datos de luz", "Custom Function: lee la luz principal de URP. Es el UNICO codigo del grafo.")
    ml = main_light(g)
    N = g.normal("world")
    V = g.viewdir("world")

    g.section("2 · Luz en bandas", "N.L (cuanto mira la superficie a la luz) se 'escalona': ceil(x*pasos)/pasos.")
    ndl = g.sat(g.dot(N, ml["Direction"]), name="N . L").show()
    toonv = g.div(g.ceil(g.mul(ndl, steps)), steps, name="Escalonar en bandas").show()
    lit = g.lerp(g.mul(g.rgb(base), g.rgb(shadow)), g.mul(g.rgb(base), ml["LightColor"]), toonv, name="Sombra -> luz")

    g.section("3 · Brillo especular", "Blinn-Phong: N.H elevado a 64, luego Step lo convierte en un circulo duro.")
    H = g.normalize(g.add(ml["Direction"], V), name="Half vector")
    spec = g.pow(g.sat(g.dot(N, H)), 64.0)
    smask = g.step(g.oneminus(ssize), spec, name="Step: brillo duro")
    col = g.lerp(lit, g.rgb(scol), smask)

    g.section("4 · Luz de borde", "(1 - N.V) detecta el borde; se multiplica por la luz para que solo salga del lado iluminado.")
    rim = g.mul(g.oneminus(g.sat(g.dot(N, V))), ndl)
    rmask = g.mul(g.smooth(0.35, 0.40, rim), ramt, name="Mascara de borde")
    col = g.lerp(col, g.rgb(rcol), rmask)
    g.block("SurfaceDescription.BaseColor", col)
    save(g, "Objects", "SG_01_Toon")
    reg(g, "Objects/SG_01_Toon", "Toon / Cel shading", 2, "objeto", "Toon",
        ["Custom Function (luz)", "Normal Vector", "Ceiling (bandas)", "Step", "Smoothstep"], "ShaderLab/Objects/Toon")


# ===========================================================================================
#  3. OUTLINE (2 materiales)
# ===========================================================================================
def outline():
    g = Graph("SG_02_Outline_Objeto", PO, "lit")
    base = g.prop_color("Color del objeto", "_BaseColor", (0.9, 0.55, 0.2, 1))
    g.prop_color("Color del contorno", "_OutlineColor", (0.03, 0.03, 0.05, 1))
    g.prop_float("Grosor del contorno (m)", "_OutlineWidth", 0.03, (0, 0.15))
    g.prop_float("Usar normales suavizadas", "_SmoothNormals", 1, (0, 1))
    g.section("Objeto normal", "Material 1: un Lit sencillo. El contorno lo dibuja OTRO material (SG_02_Outline_Contorno). Las 3 propiedades del contorno estan aqui SOLO para editarlas desde los sliders del museo: este grafo no las usa, se las pasa al material del contorno.")
    g.block("SurfaceDescription.BaseColor", base)
    g.block("SurfaceDescription.Smoothness", 0.25)
    save(g, "Objects", "SG_02_Outline_Objeto")
    reg(g, "Objects/SG_02_Outline_Objeto", "Contorno (1/2): el objeto", 2, "objeto", "Outline",
        ["Lit Target", "Multi-material"], "ShaderLab/Objects/Outline",
        extra=dict(pairedWith="Objects/SG_02_Outline_Contorno"))

    h = Graph("SG_02_Outline_Contorno", PO, "unlit", render_face="back", cast_shadows=False)
    oc = h.prop_color("Color del contorno", "_OutlineColor", (0.03, 0.03, 0.05, 1))
    ow = h.prop_float("Grosor del contorno (m)", "_OutlineWidth", 0.03, (0, 0.15))
    sn = h.prop_float("Usar normales suavizadas", "_SmoothNormals", 1, (0, 1))
    h.section("1 · Direccion de empuje", "Empujamos cada vertice hacia afuera. Con esfera/organico: mezclamos la normal con la posicion normalizada para no abrir huecos.")
    pos = h.pos("object")
    nor = h.normal("object")
    dirOS = h.normalize(h.lerp(nor, h.normalize(pos), sn), name="Direccion de empuje")
    h.section("2 · Empujar en mundo", "Pasamos a World Space para que el grosor sea en METROS aunque el objeto este escalado.")
    dirWS = h.transform(dirOS, "object", "world", "direction", True, name="Direccion a Mundo")
    posWS = h.transform(pos, "object", "world", "position", False, name="Posicion a Mundo")
    moved = h.add(posWS, h.mul(dirWS, ow), name="Posicion + dir * grosor")
    h.section("3 · Volver a Object", "El bloque Vertex Position espera Object Space.")
    back = h.transform(moved, "world", "object", "position", False, name="Posicion a Objeto")
    h.block("VertexDescription.Position", back)
    h.section("4 · Color", "Render Face = Back: solo se ven las caras traseras (Cull Front). Asi queda detras del objeto como un contorno.")
    h.block("SurfaceDescription.BaseColor", h.rgb(oc))
    save(h, "Objects", "SG_02_Outline_Contorno")
    reg(h, "Objects/SG_02_Outline_Contorno", "Contorno (2/2): el casco invertido", 2, "objeto", "OutlineHull",
        ["Vertex Position", "Transform", "Render Face = Back"], "ShaderLab/Objects/Outline",
        extra=dict(pairedWith="Objects/SG_02_Outline_Objeto", hidden=True))


# ===========================================================================================
#  4. PLASMA SHIELD
# ===========================================================================================
def plasma():
    g = Graph("SG_03_PlasmaShield", PO, "unlit", surface="transparent", blend="additive", render_face="front")
    col = g.prop_color("Color del plasma", "_Color", (0.1, 0.8, 2.0, 1), hdr=True)
    fp = g.prop_float("Potencia del borde", "_FresnelPower", 2.5, (0.5, 8))
    fi = g.prop_float("Intensidad del borde", "_FresnelIntensity", 1.2, (0, 3))
    ns = g.prop_float("Escala del plasma", "_NoiseScale", 3, (0.5, 8))
    nsp = g.prop_float("Velocidad del plasma", "_NoiseSpeed", 0.35, (0, 2))
    vs = g.prop_float("Finura de las venas", "_VeinSharpness", 7, (1, 20))
    vi = g.prop_float("Intensidad de las venas", "_VeinIntensity", 1.2, (0, 3))
    iw = g.prop_float("Ancho de interseccion (m)", "_IntersectWidth", 0.25, (0.01, 1))
    ii = g.prop_float("Intensidad de interseccion", "_IntersectIntensity", 2.5, (0, 5))
    bg = g.prop_float("Brillo base", "_BaseGlow", 0.06, (0, 0.5))

    g.section("1 · Borde brillante", "Fresnel: el escudo brilla mas en el contorno.")
    fres = g.fresnel(fp, name="Fresnel").show()

    g.section("2 · Plasma que fluye", "Ruido que sube con el tiempo. Gradient Noise 2D evaluado en dos planos (XY y ZY) imita un ruido 3D.")
    t = g.time()["Time"]
    p = g.pos("object")
    scaled = g.mul(p, ns, name="Posicion * escala")
    shift = v3(g, 0.0, g.neg(g.mul(t, nsp)), 0.0, name="Desplazar hacia abajo")
    n = noise3(g, g.add(scaled, shift, name="Mover el ruido"))
    n = g.sat(g.remap(n, (0.15, 0.85), (0, 1)), name="Ampliar contraste").show()

    g.section("3 · Venas", "|2n - 1| vale 0 donde n=0.5: eso dibuja lineas finas. Se invierte y se afila con Power.")
    veins = g.pow(g.sat(g.oneminus(g.abs(g.sub(g.mul(n, 2.0), 1.0)))), vs, name="Venas").show()

    g.section("4 · Interseccion con objetos", "Scene Depth (Eye) - profundidad de este pixel (Screen Position Raw .w). Si es chico, hay algo tocando el escudo.")
    sd = g.scene_depth("eye")
    mine = g.split(g.screenpos("raw"))["A"]
    diff = g.sub(sd, mine, name="Distancia al objeto detras")
    inter = g.pow(g.sat(g.oneminus(g.div(diff, iw))), 2.0, name="Interseccion").show()

    g.section("5 · Sumar la energia", "Se suman todas las capas de brillo y se multiplican por el color HDR.")
    e = g.add(bg, g.mul(fres, fi))
    e = g.add(e, g.mul(g.mul(veins, vi), g.add(0.35, fres)))
    e = g.add(e, g.mul(inter, ii), name="Energia total")
    g.block("SurfaceDescription.BaseColor", g.rgb(g.mul(col, e, name="Color * energia")))
    g.block("SurfaceDescription.Alpha", 1.0)
    save(g, "Objects", "SG_03_PlasmaShield")
    reg(g, "Objects/SG_03_PlasmaShield", "Escudo de plasma", 3, "objeto", "PlasmaShield",
        ["Surface Type Transparent + Additive", "Gradient Noise", "Scene Depth", "Screen Position Raw"], "ShaderLab/Objects/PlasmaShield")


# ===========================================================================================
#  5. HOLOGRAM
# ===========================================================================================
def hologram():
    g = Graph("SG_04_Hologram", PO, "unlit", surface="transparent", blend="additive", render_face="both", cast_shadows=False)
    col = g.prop_color("Color del holograma", "_Color", (0.2, 0.9, 1.4, 1), hdr=True)
    fp = g.prop_float("Potencia del borde", "_FresnelPower", 2, (0.5, 6))
    ba = g.prop_float("Brillo base", "_BaseAlpha", 0.25, (0, 1))
    sd = g.prop_float("Densidad de lineas", "_ScanDensity", 40, (1, 120))
    ss = g.prop_float("Velocidad de lineas", "_ScanSpeed", 2, (-10, 10))
    si = g.prop_float("Intensidad de lineas", "_ScanIntensity", 0.9, (0, 2))
    ga = g.prop_float("Cantidad de glitch", "_GlitchAmount", 0.12, (0, 1))
    gs = g.prop_float("Fuerza del glitch (m)", "_GlitchStrength", 0.12, (0, 0.5))
    fls = g.prop_float("Velocidad del parpadeo", "_FlickerSpeed", 25, (0, 60))
    fla = g.prop_float("Intensidad del parpadeo", "_FlickerAmount", 0.25, (0, 1))

    # ---- Vertex ----
    g.section("V1 · Bandas horizontales", "VERTEX: dividimos el objeto en franjas de 0.15 m segun su altura en mundo.")
    t = g.time()["Time"]
    pw = g.pos("world")
    spw = g.split(pw)
    band = g.floor(g.div(spw["G"], 0.15), name="Numero de banda")
    step8 = g.floor(g.mul(t, 8.0), name="Paso de tiempo (8/s)")
    g.section("V2 · Dado por banda", "Random Range con semilla (banda, tiempo) -> numero 0..1 estable dentro del mismo paso.")
    dice = g.rand(v2(g, band, step8), 0, 1, name="Dado")
    glitch = g.step(g.oneminus(ga), dice, name="Hay glitch en esta banda?").show()
    dice2 = g.rand(v2(g, g.add(band, 7.0), step8), 0, 1, name="Dado 2 (direccion)")
    dx = g.mul(g.mul(g.sub(g.mul(dice2, 2.0), 1.0), glitch), gs, name="Desplazamiento X")
    g.section("V3 · Mover el vertice", "Sumamos dx a la X mundial y devolvemos el resultado a Object Space.")
    newWS = v3(g, g.add(spw["R"], dx), spw["G"], spw["B"], name="Posicion glitcheada")
    g.block("VertexDescription.Position", g.transform(newWS, "world", "object", "position", False, name="A Objeto"))

    # ---- Fragment ----
    g.section("1 · Borde", "Fresnel de nuevo.")
    fres = g.fresnel(fp)
    g.section("2 · Lineas de escaneo", "sin(altura*densidad - tiempo*velocidad) -> smoothstep para lineas definidas.")
    wy = g.split(g.pos("world"))["G"]
    ang = g.sub(g.mul(wy, sd), g.mul(t, ss))
    scan = g.smooth(0.55, 0.95, g.add(g.mul(g.sin(ang), 0.5), 0.5), name="Lineas").show()
    g.section("3 · Parpadeo", "Un valor aleatorio nuevo cada 1/FlickerSpeed segundos reduce el brillo.")
    fl = g.oneminus(g.mul(fla, g.rand(v2(g, g.floor(g.mul(t, fls)), 3.0), 0, 1)), name="Factor de parpadeo")
    g.section("4 · Intensidad final", "(brillo base + borde + lineas) * parpadeo. La misma cantidad controla el color y la opacidad.")
    inten = g.mul(g.add(g.add(ba, fres), g.mul(scan, si)), fl, name="Intensidad").show()
    g.block("SurfaceDescription.BaseColor", g.rgb(g.mul(col, inten)))
    g.block("SurfaceDescription.Alpha", g.sat(inten))
    save(g, "Objects", "SG_04_Hologram")
    reg(g, "Objects/SG_04_Hologram", "Holograma con glitch", 3, "objeto", "Hologram",
        ["Vertex stage", "Random Range", "Transform", "Scanlines"], "ShaderLab/Objects/Hologram")


# ===========================================================================================
#  6. DISSOLVE
# ===========================================================================================
def dissolve():
    g = Graph("SG_05_Dissolve", PO, "lit", render_face="both", alpha_clip=True)
    base = g.prop_color("Color base", "_BaseColor", (0.75, 0.75, 0.8, 1))
    dis = g.prop_float("Disolver", "_Dissolve", 0.3, (0, 1))
    auto = g.prop_bool("Animar automaticamente", "_AutoAnimate", True)
    aspd = g.prop_float("Velocidad de la animacion", "_AutoSpeed", 0.25, (0.05, 1))
    ph = g.prop_float("Desfase de la animacion", "_Phase", 0, (0, 1))
    ns = g.prop_float("Escala del ruido", "_NoiseScale", 3, (0.3, 10))
    wsn = g.prop_bool("Ruido en espacio mundo", "_WorldSpaceNoise", False)
    ew = g.prop_float("Ancho del borde", "_EdgeWidth", 0.08, (0.005, 0.3))
    ec = g.prop_color("Color del borde", "_EdgeColor", (4, 1.2, 0.15, 1), hdr=True)

    g.section("1 · Cuanto disolver", "Boolean + Branch: manual (slider) o automatico (triangulo que sube y baja con el tiempo).")
    t = g.time()["Time"]
    tri = g.abs(g.sub(g.mul(g.frac(g.add(g.mul(t, aspd), ph)), 2.0), 1.0), name="Onda triangular")
    d = g.branch(auto, tri, dis, name="Automatico?").show()
    g.section("2 · Ruido", "Un ruido 0..1 decide QUE zonas desaparecen primero. Branch elige posicion local o de mundo.")
    npos = g.branch(wsn, g.pos("world"), g.pos("object"), name="Mundo u Objeto")
    n = noise3(g, g.mul(npos, ns))
    n = g.sat(g.remap(n, (0.15, 0.85), (0, 1)), name="Ampliar contraste").show()
    g.section("3 · Umbral y recorte", "cut = d*(1+ancho) - ancho. Donde ruido < cut: Alpha 0 (se corta).")
    cut = g.sub(g.mul(d, g.add(1.0, ew)), ew, name="Umbral")
    keep = g.step(cut, n, name="Conservar?").show()
    g.section("4 · Borde brillante", "Zona justo antes de cortarse: (n - cut)/ancho cerca de 0 -> brilla con EdgeColor (Emission).")
    edge = g.oneminus(g.sat(g.div(g.sub(n, cut), ew)))
    edge = g.mul(edge, g.step(0.0001, d), name="Borde")
    g.block("SurfaceDescription.BaseColor", g.rgb(g.lerp(base, ec, edge)))
    g.block("SurfaceDescription.Smoothness", 0.3)
    g.block("SurfaceDescription.Emission", g.rgb(g.mul(ec, edge)))
    g.block("SurfaceDescription.Alpha", keep)
    g.block("SurfaceDescription.AlphaClipThreshold", 0.5)
    save(g, "Objects", "SG_05_Dissolve")
    reg(g, "Objects/SG_05_Dissolve", "Disolver", 3, "objeto", "Dissolve",
        ["Boolean + Branch", "Alpha Clip", "Emission", "Gradient Noise"], "ShaderLab/Objects/Dissolve")


# ===========================================================================================
#  7. LAVA
# ===========================================================================================
def lava():
    g = Graph("SG_06_Lava", PO, "unlit")
    crust = g.prop_color("Color de la costra", "_CrustColor", (0.06, 0.02, 0.015, 1))
    hot = g.prop_color("Color caliente", "_HotColor", (2.2, 0.55, 0.05, 1), hdr=True)
    core = g.prop_color("Color del nucleo", "_CoreColor", (4.0, 2.4, 0.5, 1), hdr=True)
    tiling = g.prop_float("Repeticiones del patron", "_Tiling", 4, (1, 12))
    flow = g.prop_float("Velocidad del flujo", "_FlowSpeed", 0.12, (-1, 1))
    dist = g.prop_float("Fuerza de la distorsion", "_DistortStrength", 0.55, (0, 1.5))
    th = g.prop_float("Cantidad de lava", "_Threshold", 0.48, (0.2, 0.8))
    pulse = g.prop_float("Velocidad de pulso", "_PulseSpeed", 1.2, (0, 5))

    g.section("1 · UV y tiempo", "UV = coordenadas de textura (0..1). Las repetimos con Tiling And Offset.")
    time = g.time()["Time"]
    t = g.mul(time, flow, name="Tiempo del flujo")
    uv = g.swizzle(g.uv(), "xy", name="UV")
    uv = g.tiling(uv, v2(g, g.round(tiling), g.round(tiling)), v2(g, 0.0, 0.0), name="UV repetidas").show()
    g.section("2 · Distorsion (domain warp)", "Dos ruidos que se mueven desplazan las UV: asi el patron 'fluye' como liquido espeso.")
    wx = g.gnoise(g.tiling(uv, v2(g, 1.0, 1.0), v2(g, t, 0.0)), 3.0)
    wy = g.gnoise(g.tiling(uv, v2(g, 1.0, 1.0), v2(g, 3.7, g.sub(1.3, t))), 3.0)
    off = g.mul(g.mul(g.sub(v2(g, wx, wy), 0.5), 2.0), dist, name="Desplazamiento")
    warped = g.add(uv, off, name="UV distorsionadas")
    g.section("3 · Patron de lava", "Dos octavas de Gradient Noise (grande + detalle).")
    uvm = g.tiling(warped, v2(g, 1.0, 1.0), v2(g, 0.0, g.mul(t, 2.0)))
    n1 = g.gnoise(uvm, 4.0)
    n2 = g.gnoise(uvm, 9.0)
    n = g.add(g.mul(n1, 0.65), g.mul(n2, 0.35), name="Ruido final").show()
    g.section("4 · Mascaras", "hot: zonas calientes; core: nucleos mas brillantes. smoothstep(umbral) = borde suave.")
    hotm = g.oneminus(g.smooth(g.sub(th, 0.06), g.add(th, 0.06), n), name="Mascara caliente").show()
    corem = g.oneminus(g.smooth(g.sub(th, 0.20), g.sub(th, 0.04), n), name="Mascara nucleo")
    g.section("5 · Pulso y color", "El brillo palpita con un seno que depende del tiempo y del ruido.")
    pl = g.add(0.85, g.mul(0.15, g.sin(g.add(g.mul(time, pulse), g.mul(n, 6.0)))), name="Pulso")
    c = g.lerp(g.rgb(crust), g.mul(g.rgb(hot), pl), hotm)
    c = g.lerp(c, g.mul(g.rgb(core), pl), corem, name="Color final")
    g.block("SurfaceDescription.BaseColor", c)
    save(g, "Objects", "SG_06_Lava")
    reg(g, "Objects/SG_06_Lava", "Lava", 3, "objeto", "Lava",
        ["UV", "Tiling And Offset", "Domain warp", "Gradient Noise", "Smoothstep"], "ShaderLab/Objects/Lava")


# ===========================================================================================
#  8. SLIME
# ===========================================================================================
def slime():
    g = Graph("SG_07_Slime", PO, "lit", surface="transparent", blend="alpha", render_face="front")
    g.zwrite = "on"
    col = g.prop_color("Color del cuerpo", "_Color", (0.25, 0.95, 0.35, 1))
    core = g.prop_color("Color del interior", "_CoreColor", (0.02, 0.35, 0.12, 1))
    rim = g.prop_color("Color de borde", "_RimColor", (0.7, 1.4, 0.8, 1), hdr=True)
    ac = g.prop_float("Opacidad al centro", "_AlphaCenter", 0.75, (0, 1))
    ar = g.prop_float("Opacidad en el borde", "_AlphaRim", 0.95, (0, 1))
    wa = g.prop_float("Fuerza del temblor", "_WobbleAmount", 0.05, (0, 0.15))
    wf = g.prop_float("Frecuencia del temblor", "_WobbleFreq", 7, (1, 20))
    wsp = g.prop_float("Velocidad del temblor", "_WobbleSpeed", 3, (0, 10))
    sa = g.prop_float("Aplastamiento", "_SquashAmount", 0.12, (0, 0.4))
    ssp = g.prop_float("Velocidad de respiracion", "_SquashSpeed", 3, (0, 8))
    sp = g.prop_float("Dureza del brillo", "_SpecPower", 48, (4, 128))
    sss = g.prop_float("Luz que atraviesa", "_SSS", 0.8, (0, 2))

    g.section("V1 · Temblor", "VERTEX: cada vertice se mueve a lo largo de su normal con un seno*seno.")
    time = g.time()["Time"]
    pos = g.pos("object")
    ps = g.split(pos)
    tt = g.mul(time, wsp)
    wob = g.mul(g.sin(g.add(g.mul(ps["G"], wf), tt)),
                g.sin(g.add(g.mul(g.mul(ps["R"], wf), 0.7), g.mul(tt, 1.3))), name="Temblor")
    p1 = g.add(pos, g.mul(g.normal("object"), g.mul(wob, wa)), name="Posicion + normal * temblor")
    g.section("V2 · Aplastar y estirar", "Escala Y por (1+s) y XZ por (1-s/2): como 'respirar' manteniendo el volumen.")
    sq = g.mul(g.sin(g.mul(time, ssp)), sa, name="Aplastamiento")
    q = g.split(p1)
    newp = v3(g, g.mul(q["R"], g.sub(1.0, g.mul(sq, 0.5))), g.mul(q["G"], g.add(1.0, sq)),
              g.mul(q["B"], g.sub(1.0, g.mul(sq, 0.5))), name="Posicion final")
    g.block("VertexDescription.Position", newp)

    g.section("1 · Borde", "Fresnel con potencia 2.5.")
    fres = g.fresnel(2.5).show()
    g.section("2 · Color del cuerpo", "El interior es oscuro abajo y claro arriba; hacia el borde tira al color base.")
    h = g.sat(g.add(g.split(g.pos("object"))["G"], 0.5), name="Altura 0..1")
    body = g.lerp(g.rgb(core), g.rgb(col), g.add(g.mul(h, 0.7), 0.3))
    body = g.lerp(body, g.rgb(col), fres, name="Cuerpo")
    g.block("SurfaceDescription.BaseColor", body)
    g.section("3 · Brillo y luz que atraviesa", "Smoothness (Lit) da el brillo; Emission agrega el borde luminoso y el 'subsurface'.")
    g.block("SurfaceDescription.Smoothness", g.sat(g.remap(sp, (4, 128), (0.4, 0.95)), name="Dureza -> Smoothness"))
    em = g.add(g.mul(g.rgb(rim), g.mul(fres, 0.6)), g.mul(g.mul(g.rgb(col), g.mul(fres, sss)), 0.25), name="Emision")
    g.block("SurfaceDescription.Emission", em)
    g.section("4 · Transparencia", "Mas opaco en los bordes que en el centro.")
    g.block("SurfaceDescription.Alpha", g.lerp(ac, ar, fres, name="Alpha centro->borde"))
    save(g, "Objects", "SG_07_Slime")
    reg(g, "Objects/SG_07_Slime", "Slime gelatinoso", 4, "objeto", "Slime",
        ["Vertex displacement", "Lit Target", "Smoothness", "Emission", "Alpha"], "ShaderLab/Objects/Slime")


# ===========================================================================================
#  9. WATER
# ===========================================================================================
WAVE_SLOPE_NOTE = "Cada ola: sin(dot(direccion, XZ) * frecuencia + tiempo). Sumamos 3 olas con distinta direccion/frecuencia/velocidad."


def water():
    g = Graph("SG_08_Water", PO, "lit", surface="transparent", blend="alpha", render_face="both", cast_shadows=False)
    shallow = g.prop_color("Color poco profundo", "_ShallowColor", (0.20, 0.85, 0.80, 0.35))
    deep = g.prop_color("Color profundo", "_DeepColor", (0.02, 0.18, 0.40, 0.95))
    sky = g.prop_color("Color del reflejo (cielo)", "_SkyColor", (0.55, 0.75, 1.0, 1))
    dmax = g.prop_float("Profundidad maxima de color (m)", "_DepthMax", 1.2, (0.1, 5))
    fw = g.prop_float("Ancho de la espuma (m)", "_FoamWidth", 0.25, (0.01, 1))
    fns = g.prop_float("Escala del ruido de espuma", "_FoamNoiseScale", 10, (1, 30))
    wh = g.prop_float("Altura de las olas (m)", "_WaveHeight", 0.07, (0, 0.5))
    wf = g.prop_float("Frecuencia de las olas", "_WaveFreq", 1.6, (0.2, 6))
    ws = g.prop_float("Velocidad de las olas", "_WaveSpeed", 1.2, (0, 4))
    nst = g.prop_float("Fuerza de la normal", "_NormalStrength", 1.5, (0, 4))
    gl = g.prop_float("Dureza del brillo", "_Glossiness", 90, (8, 256))
    refl = g.prop_float("Fuerza del reflejo", "_ReflectionStrength", 0.6, (0, 1))

    g.section("V1 · Olas (posicion XZ)", "VERTEX: usamos la posicion de MUNDO (x,z) para que las olas sean continuas entre planos vecinos. " + WAVE_SLOPE_NOTE)
    time = g.time()["Time"]
    pw = g.pos("world")
    xz = g.swizzle(pw, "xz", name="XZ")
    tt = g.mul(time, ws, name="Tiempo * velocidad")
    waves = [((0.9578, 0.2873), 1.0, 1.0, 0.5), ((-0.5145, 0.8575), 1.7, 1.3, 0.3), ((0.1961, -0.9806), 2.9, 1.9, 0.2)]
    hs, slopes = [], []
    for i, (d, fm, tm, amp) in enumerate(waves, 1):
        dd = g.vec2(d[0], d[1], name="Direccion %d" % i)
        ph = g.add(g.mul(g.dot(xz, dd, name="dot %d" % i), g.mul(wf, fm)), g.mul(tt, tm), name="Fase %d" % i)
        hs.append(g.mul(g.sin(ph, name="Ola %d" % i), amp))
        slopes.append(g.mul(dd, g.mul(g.cos(ph), amp * fm), name="Pendiente %d" % i))
    h = g.add(g.add(hs[0], hs[1]), hs[2], name="Altura total de la ola").show()
    sl = g.add(g.add(slopes[0], slopes[1]), slopes[2], name="Pendiente total")
    g.section("V2 · Subir los vertices", "Desplazamos a lo largo de la normal (mundo) y volvemos a Object.")
    nws = g.normal("world")
    newws = g.add(pw, g.mul(nws, g.mul(h, wh)), name="Posicion con olas")
    g.block("VertexDescription.Position", g.transform(newws, "world", "object", "position", False, name="A Objeto"))
    g.section("V3 · Inclinar la normal", "La pendiente de la ola inclina la normal: asi la luz y el reflejo 'ven' las olas.")
    ssl = g.split(sl)
    tilt = v3(g, g.neg(ssl["R"]), 0.0, g.neg(ssl["G"]))
    bent = g.normalize(g.add(nws, g.mul(tilt, g.mul(wh, nst))), name="Normal inclinada")
    g.block("VertexDescription.Normal", g.transform(bent, "world", "object", "normal", True, name="Normal a Objeto"))

    g.section("1 · Profundidad del agua", "Scene Depth - profundidad del pixel del agua = cuanta agua hay delante del fondo.")
    sd = g.scene_depth("eye")
    mine = g.split(g.screenpos("raw"))["A"]
    diff = g.max(g.sub(sd, mine), 0.0, name="Profundidad (m)").show()
    d01 = g.sat(g.div(diff, dmax), name="Profundidad 0..1")
    water = g.lerp(shallow, deep, g.sqrt(d01), name="Poco profundo -> profundo")
    wrgb = g.rgb(water)
    walpha = g.split(water)["A"]
    g.section("2 · Reflejo del cielo", "Fresnel: de lado el agua refleja el cielo; de frente se ve a traves.")
    fres = g.fresnel(4.0).show()
    color = g.lerp(wrgb, g.rgb(sky), g.mul(fres, refl), name="Con reflejo")
    g.section("3 · Espuma", "Dos fuentes: orilla (poca profundidad) y crestas de ola, rotas con ruido.")
    fn = g.gnoise(g.add(g.mul(xz, fns), g.mul(time, 0.3), name="XZ * escala + tiempo"), 1.0, name="Ruido de espuma")
    shore = g.oneminus(g.sat(g.div(diff, fw)), name="Orilla")
    crest = g.smooth(0.75, 1.0, g.add(g.add(g.mul(h, 0.5), 0.5), g.mul(fn, 0.15)), name="Crestas")
    foam = g.sat(g.add(g.mul(shore, g.add(0.55, fn)), g.mul(crest, 0.35)))
    foam = g.smooth(0.35, 0.6, foam, name="Espuma").show()
    color = g.lerp(color, g.vec3(1, 1, 1), foam, name="Mezclar espuma")
    g.section("4 · Salida", "Smoothness alto = reflejo de luz (brillo). Alpha = agua + espuma + fresnel.")
    g.block("SurfaceDescription.BaseColor", color)
    g.block("SurfaceDescription.Smoothness", g.sat(g.remap(gl, (8, 256), (0.5, 0.98)), name="Dureza -> Smoothness"))
    g.block("SurfaceDescription.Alpha", g.sat(g.add(g.add(walpha, foam), g.mul(fres, 0.3))))
    save(g, "Objects", "SG_08_Water")
    reg(g, "Objects/SG_08_Water", "Agua con espuma", 5, "objeto", "Water",
        ["Vertex waves", "Scene Depth", "Normal en Vertex", "Espuma", "Fresnel"], "ShaderLab/Objects/Water")


if __name__ == "__main__":
    for fn in (f1_color, f2_gradient, f3_time, f4_fresnel, liquid, toon, outline, plasma, hologram, dissolve, lava, slime, water):
        fn()
    json.dump(REG, open(os.path.join(os.path.dirname(os.path.abspath(__file__)), "_objects_manifest.json"), "w"), indent=1, ensure_ascii=False)
