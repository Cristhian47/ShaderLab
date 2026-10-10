#!/usr/bin/env python3
"""
sg.py - generador de archivos .shadergraph (Unity 6 / Shader Graph 17.x, URP).

Usa plantillas minadas de los grafos de muestra de Unity (templates.json) para que cada nodo
tenga exactamente los slots y campos que Unity espera.

Ejemplo:
    g = Graph("Demo", "ShaderLab/ShaderGraph/Objects", target="unlit")
    col = g.prop_color("Color", "_BaseColor", (1, 0, 0, 1))
    g.block("SurfaceDescription.BaseColor", g.mul(col, g.fresnel(2)))
    g.save("Demo.shadergraph")
"""
import copy
import hashlib
import json
import os
import uuid

_HERE = os.path.dirname(os.path.abspath(__file__))
_TPL = None


def templates():
    global _TPL
    if _TPL is None:
        path = os.environ.get("SG_TEMPLATES", os.path.join(_HERE, "templates.json"))
        _TPL = json.load(open(path, encoding="utf-8"))
    return _TPL


def norm(s):
    return "".join(ch for ch in s.lower() if ch.isalnum())


def short(t):
    return t.split(".")[-1]


# ------------------------------------------------------------------------------------------
#  Slots
# ------------------------------------------------------------------------------------------
def set_slot_value(slot, v):
    """Asigna el valor por defecto (el que se usa si el slot no esta conectado)."""
    t = short(slot["m_Type"])
    if isinstance(v, bool):
        slot["m_Value"] = v
        return
    if isinstance(v, (int, float)):
        comps = [float(v)] * 4
    else:
        comps = [float(c) for c in v]
        while len(comps) < 4:
            comps.append(0.0)
    if "DynamicValue" in t:
        old = slot["m_Value"]
        if isinstance(v, (int, float)):
            for k in old:
                old[k] = float(v)
        else:
            for i, c in enumerate(comps):
                old["e0%d" % i] = c
        return
    if "Boolean" in t:
        slot["m_Value"] = bool(v)
        return
    if t == "Vector1MaterialSlot":
        slot["m_Value"] = comps[0]
        return
    cur = slot["m_Value"]
    if isinstance(cur, dict):
        keys = [k for k in ("x", "y", "z", "w") if k in cur]
        for i, k in enumerate(keys):
            cur[k] = comps[i]
        return
    slot["m_Value"] = comps[0]


class Out:
    """Referencia a una salida de un nodo."""

    def __init__(self, node, slot):
        self.node = node
        self.slot = slot  # dict del slot

    def show(self, on=True):
        self.node.show(on)
        return self

    def named(self, name):
        self.node.named(name)
        return self


class Node:
    def __init__(self, g, kind, obj, slots):
        self.g = g
        self.kind = kind
        self.obj = obj
        self.slots = slots
        self.section = None
        self.preview = False
        self.inputs = {}    # slot id -> Out
        self.pos = (0, 0)

    # -- acceso a slots ----------------------------------------------------------------
    def slot(self, name, want_out=None):
        key = norm(str(name))
        for s in self.slots:
            if want_out is not None and (s["m_SlotType"] == 1) != want_out:
                continue
            if norm(s["m_ShaderOutputName"]) == key or norm(s["m_DisplayName"]) == key:
                return s
        raise KeyError("%s: slot '%s' no existe. Slots: %s" % (
            self.kind, name, [(s["m_DisplayName"], s["m_SlotType"]) for s in self.slots]))

    def out(self, name=None):
        if name is None:
            outs = [s for s in self.slots if s["m_SlotType"] == 1]
            return Out(self, outs[0])
        return Out(self, self.slot(name, True))

    def __getitem__(self, name):
        return self.out(name)

    # Un Node usado como entrada equivale a su primera salida
    def as_out(self):
        return self.out()

    def show(self, on=True):
        """Muestra la vista previa (cuadrito con el resultado) dentro del nodo."""
        self.preview = on
        self.obj["m_PreviewExpanded"] = bool(on)
        return self

    def named(self, name):
        self.obj["m_Name"] = name
        return self


def as_out(x):
    if isinstance(x, Node):
        return x.out()
    return x


class Prop:
    def __init__(self, g, obj, kind, name, ref):
        self.g, self.obj, self.kind, self.name, self.ref = g, obj, kind, name, ref
        self._node = None

    def out(self, name=None):
        if self._node is None:
            self._node = self.g._property_node(self)
        return self._node.out()


# ------------------------------------------------------------------------------------------
class Graph:
    SLOT_PROTOS = {}

    def __init__(self, name, path, target="unlit", surface="opaque", blend="alpha",
                 render_face="front", alpha_clip=False, cast_shadows=True, receive_shadows=True,
                 meta=None):
        self.name = name
        self.path = path
        self.target = target          # unlit | lit | fullscreen
        self.surface = surface        # opaque | transparent
        self.blend = blend            # alpha | additive | premultiply | multiply
        self.render_face = render_face  # front | back | both
        self.alpha_clip = alpha_clip
        self.cast_shadows = cast_shadows
        self.receive_shadows = receive_shadows
        self.meta = meta or {}
        self.nodes = []
        self.props = []
        self.blocks = []            # (descriptor, Node)
        self.edges = []
        self.groups = []            # (title, [nodes])
        self.stickies = []
        self._n = 0
        self._cur_section = None
        self._sections = []         # orden de secciones
        self.extra_objs = []
        self.zwrite = None            # None = auto | 'on' | 'off'

    # -- ids deterministas ---------------------------------------------------------------
    def oid(self):
        self._n += 1
        return hashlib.md5(("%s#%d" % (self.name, self._n)).encode()).hexdigest()

    def guid(self):
        self._n += 1
        return str(uuid.uuid5(uuid.NAMESPACE_URL, "%s#%d" % (self.name, self._n)))

    # -- secciones (grupos) ----------------------------------------------------------------
    def section(self, title, note=None):
        """Todo lo que se cree despues pertenece al grupo 'title'."""
        self._cur_section = title
        if title not in self._sections:
            self._sections.append(title)
        if note:
            self.stickies.append({"section": title, "title": title, "content": note})
        return self

    def end_section(self):
        self._cur_section = None

    def note(self, title, content, section=None):
        self.stickies.append({"section": section, "title": title, "content": content})

    # -- propiedades -----------------------------------------------------------------------
    def _prop(self, key, name, ref, mod):
        o = copy.deepcopy(templates()["props"][key])
        o["m_ObjectId"] = self.oid()
        o["m_Guid"] = {"m_GuidSerialized": self.guid()}
        o["m_Name"] = name
        o["m_RefNameGeneratedByDisplayName"] = name
        o["m_DefaultReferenceName"] = "_" + "".join(c if c.isalnum() else "_" for c in name)
        o["m_OverrideReferenceName"] = ref
        mod(o)
        p = Prop(self, o, key, name, ref)
        self.props.append(p)
        return p

    def prop_float(self, name, ref, default=0.0, rng=None, integer=False):
        pre = "UnityEditor.ShaderGraph.Internal.Vector1ShaderProperty|"
        key = pre + ("2" if integer else "1" if rng else "0")

        def mod(o):
            o["m_Value"] = float(default)
            o["m_FloatType"] = 2 if integer else (1 if rng else 0)
            if rng:
                o["m_RangeValues"] = {"x": float(rng[0]), "y": float(rng[1])}
        return self._prop(key, name, ref, mod)

    def prop_color(self, name, ref, rgba=(1, 1, 1, 1), hdr=False):
        key = "UnityEditor.ShaderGraph.Internal.ColorShaderProperty|%d" % (1 if hdr else 0)

        def mod(o):
            o["m_Value"] = {"r": float(rgba[0]), "g": float(rgba[1]), "b": float(rgba[2]), "a": float(rgba[3]) if len(rgba) > 3 else 1.0}
            o["m_ColorMode"] = 1 if hdr else 0
        return self._prop(key, name, ref, mod)

    def prop_bool(self, name, ref, default=False):
        def mod(o):
            o["m_Value"] = bool(default)
        return self._prop("UnityEditor.ShaderGraph.Internal.BooleanShaderProperty", name, ref, mod)

    def prop_vec3(self, name, ref, v=(0, 0, 0)):
        def mod(o):
            o["m_Value"] = {"x": float(v[0]), "y": float(v[1]), "z": float(v[2]), "w": 0.0}
        return self._prop("UnityEditor.ShaderGraph.Internal.Vector3ShaderProperty", name, ref, mod)

    def prop_vec2(self, name, ref, v=(0, 0)):
        def mod(o):
            o["m_Value"] = {"x": float(v[0]), "y": float(v[1])}
        return self._prop("UnityEditor.ShaderGraph.Internal.Vector2ShaderProperty", name, ref, mod)

    def _property_node(self, p):
        tpl = templates()["nodes"]["UnityEditor.ShaderGraph.PropertyNode"]
        obj = copy.deepcopy(tpl["node"])
        obj["m_ObjectId"] = self.oid()
        obj["m_Name"] = "Property"
        obj["m_Property"] = {"m_Id": p.obj["m_ObjectId"]}
        kind = short(p.obj["m_Type"])
        proto = {
            "Vector1ShaderProperty": ("Vector1MaterialSlot", 0.0),
            "ColorShaderProperty": ("Vector4MaterialSlot", (0, 0, 0, 0)),
            "BooleanShaderProperty": ("BooleanMaterialSlot", False),
            "Vector3ShaderProperty": ("Vector3MaterialSlot", (0, 0, 0)),
            "Vector2ShaderProperty": ("Vector2MaterialSlot", (0, 0)),
        }[kind]
        slot = self._make_slot(proto[0], 0, p.name, "Out", True, proto[1])
        obj["m_Slots"] = [{"m_Id": slot["m_ObjectId"]}]
        n = Node(self, "Property", obj, [slot])
        n.section = self._cur_section
        self.nodes.append(n)
        return n

    # -- slots ---------------------------------------------------------------------------
    def _make_slot(self, mtype, sid, display, outname, is_out, value=None, stage=3):
        s = {
            "m_SGVersion": 0,
            "m_Type": "UnityEditor.ShaderGraph." + mtype,
            "m_ObjectId": self.oid(),
            "m_Id": sid,
            "m_DisplayName": display,
            "m_SlotType": 1 if is_out else 0,
            "m_Hidden": False,
            "m_ShaderOutputName": outname,
            "m_StageCapability": stage,
        }
        n = {"Vector1MaterialSlot": 1, "Vector2MaterialSlot": 2, "Vector3MaterialSlot": 3, "Vector4MaterialSlot": 4,
             "ColorRGBMaterialSlot": 3, "ColorRGBAMaterialSlot": 4}.get(mtype)
        if mtype == "BooleanMaterialSlot":
            s["m_Value"] = bool(value)
            s["m_DefaultValue"] = bool(value)
        elif n == 1:
            s["m_Value"] = float(value or 0.0)
            s["m_DefaultValue"] = float(value or 0.0)
            s["m_Labels"] = []
        else:
            comps = list(value) if value is not None and not isinstance(value, (int, float)) else [float(value or 0.0)] * n
            while len(comps) < n:
                comps.append(0.0)
            d = {k: float(comps[i]) for i, k in enumerate("xyzw"[:n])}
            s["m_Value"] = dict(d)
            s["m_DefaultValue"] = dict(d)
            s["m_Labels"] = []
        return s

    # -- nodos -----------------------------------------------------------------------------
    def node(self, kind, name=None, params=None, preview=False, **inputs):
        """Crea un nodo (kind = nombre del tipo sin 'Node', p. ej. 'Multiply').
        Los kwargs son entradas: Out/Node/Prop = conexion; numero o tupla = valor constante."""
        key = kind if "." in kind else "UnityEditor.ShaderGraph.%sNode" % kind
        tpl = templates()["nodes"][key]
        obj = copy.deepcopy(tpl["node"])
        slots = copy.deepcopy(tpl["slots"])
        obj["m_ObjectId"] = self.oid()
        # los slots necesitan ids de objeto nuevos
        ids = []
        for s in slots:
            s["m_ObjectId"] = self.oid()
            ids.append({"m_Id": s["m_ObjectId"]})
        obj["m_Slots"] = ids
        obj["m_Group"] = {"m_Id": ""}
        obj["m_PreviewExpanded"] = bool(preview)
        if name:
            obj["m_Name"] = name
        if params:
            obj.update(params)
        n = Node(self, kind, obj, slots)
        n.section = self._cur_section
        n.preview = preview
        self.nodes.append(n)
        for k, v in inputs.items():
            self.set_input(n, k, v)
        return n

    SOURCE_KINDS = {"Property", "Time", "Position", "NormalVector", "ViewDirection", "ScreenPosition", "UV",
                    "Screen", "Vector1", "Vector2", "Vector3", "Vector4", "Color"}

    def _local(self, out):
        """Los nodos 'fuente' (propiedades, tiempo, posicion...) se duplican por seccion para que
        no haya cables larguisimos cruzando todo el grafo."""
        n = out.node
        if n.kind not in self.SOURCE_KINDS or n.inputs or n.section == self._cur_section:
            return out
        cache = self.__dict__.setdefault("_clones", {})
        key = (id(n), self._cur_section)
        if key not in cache:
            obj = copy.deepcopy(n.obj)
            slots = copy.deepcopy(n.slots)
            obj["m_ObjectId"] = self.oid()
            for sl in slots:
                sl["m_ObjectId"] = self.oid()
            obj["m_Slots"] = [{"m_Id": sl["m_ObjectId"]} for sl in slots]
            c = Node(self, n.kind, obj, slots)
            c.section = self._cur_section
            c.preview = n.preview
            self.nodes.append(c)
            cache[key] = c
        c = cache[key]
        for sl in c.slots:
            if sl["m_Id"] == out.slot["m_Id"] and sl["m_SlotType"] == out.slot["m_SlotType"]:
                return Out(c, sl)
        return out

    def set_input(self, n, slot_name, v):
        s = n.slot(slot_name, False)
        if isinstance(v, Prop):
            v = v.out()
        v = as_out(v)
        if isinstance(v, Out):
            v = self._local(v)
            n.inputs[s["m_Id"]] = v
            self.edges.append((v, n, s))
        else:
            set_slot_value(s, v)

    def custom(self, fname, inputs, outputs, body, name=None, preview=False, **conn):
        """Nodo Custom Function (HLSL en texto). inputs/outputs: [(nombre, tipo)] con tipo 1..4 o 'bool'."""
        tpl = templates()["nodes"]["UnityEditor.ShaderGraph.CustomFunctionNode"]
        obj = copy.deepcopy(tpl["node"])
        obj["m_ObjectId"] = self.oid()
        obj["m_Group"] = {"m_Id": ""}
        obj["m_Name"] = name or (fname + " (Custom Function)")
        obj["m_SourceType"] = 1
        obj["m_FunctionName"] = fname
        obj["m_FunctionSource"] = ""
        obj["m_FunctionSourceUsePragmas"] = True
        obj["m_FunctionBody"] = body
        obj["m_PreviewExpanded"] = bool(preview)
        slots = []
        sid = 0
        tmap = {1: "Vector1MaterialSlot", 2: "Vector2MaterialSlot", 3: "Vector3MaterialSlot", 4: "Vector4MaterialSlot", "bool": "BooleanMaterialSlot"}
        for nm, ty in inputs:
            slots.append(self._make_slot(tmap[ty], sid, nm, nm, False)); sid += 1
        for nm, ty in outputs:
            slots.append(self._make_slot(tmap[ty], sid, nm, nm, True)); sid += 1
        obj["m_Slots"] = [{"m_Id": s["m_ObjectId"]} for s in slots]
        n = Node(self, "CustomFunction", obj, slots)
        n.section = self._cur_section
        self.nodes.append(n)
        for k, v in conn.items():
            self.set_input(n, k, v)
        return n

    # -- bloques (salidas del Master Stack) ----------------------------------------------------
    def block(self, descriptor, value):
        tpl = templates()["blocks"][descriptor]
        obj = copy.deepcopy(tpl["node"])
        slots = copy.deepcopy(tpl["slots"])
        obj["m_ObjectId"] = self.oid()
        for s in slots:
            s["m_ObjectId"] = self.oid()
        obj["m_Slots"] = [{"m_Id": s["m_ObjectId"]} for s in slots]
        n = Node(self, "Block", obj, slots)
        n.block_descriptor = descriptor
        self.blocks.append((descriptor, n))
        if isinstance(value, Prop):
            value = value.out()
        value = as_out(value)
        s = slots[0]
        if value is None:
            return n
        if isinstance(value, Out):
            value = self._local(value)
            n.inputs[s["m_Id"]] = value
            self.edges.append((value, n, s))
        else:
            set_slot_value(s, value)
        return n

    # ---- atajos de nodos comunes -------------------------------------------------------------
    def add(self, a, b, **k): return self.node("Add", A=a, B=b, **k)
    def sub(self, a, b, **k): return self.node("Subtract", A=a, B=b, **k)
    def mul(self, a, b, **k): return self.node("Multiply", A=a, B=b, **k)
    def div(self, a, b, **k): return self.node("Divide", A=a, B=b, **k)
    def lerp(self, a, b, t, **k): return self.node("Lerp", A=a, B=b, T=t, **k)
    def sat(self, x, **k): return self.node("Saturate", In=x, **k)
    def pow(self, a, b, **k): return self.node("Power", A=a, B=b, **k)
    def smooth(self, e1, e2, x, **k): return self.node("Smoothstep", Edge1=e1, Edge2=e2, In=x, **k)
    def step(self, edge, x, **k): return self.node("Step", Edge=edge, In=x, **k)
    def oneminus(self, x, **k): return self.node("OneMinus", In=x, **k)
    def sin(self, x, **k): return self.node("Sine", In=x, **k)
    def cos(self, x, **k): return self.node("Cosine", In=x, **k)
    def frac(self, x, **k): return self.node("Fraction", In=x, **k)
    def floor(self, x, **k): return self.node("Floor", In=x, **k)
    def ceil(self, x, **k): return self.node("Ceiling", In=x, **k)
    def dot(self, a, b, **k): return self.node("DotProduct", A=a, B=b, **k)
    def length(self, x, **k): return self.node("Length", In=x, **k)
    def normalize(self, x, **k): return self.node("Normalize", In=x, **k)
    def abs(self, x, **k): return self.node("Absolute", In=x, **k)
    def neg(self, x, **k): return self.node("Negate", In=x, **k)
    def max(self, a, b, **k): return self.node("Maximum", A=a, B=b, **k)
    def min(self, a, b, **k): return self.node("Minimum", A=a, B=b, **k)
    def remap(self, x, inmm, outmm, **k): return self.node("Remap", In=x, InMinMax=inmm, OutMinMax=outmm, **k)
    def split(self, x, **k): return self.node("Split", In=x, **k)
    def combine(self, r=0, g=0, b=0, a=0, **k): return self.node("Combine", R=r, G=g, B=b, A=a, **k)
    def swizzle(self, x, mask, **k):
        return self.node("Swizzle", params={"_maskInput": mask, "convertedMask": mask}, In=x, **k)
    def time(self, **k): return self.node("Time", **k)
    def uv(self, **k): return self.node("UV", **k)
    def pos(self, space="object", **k):
        return self.node("Position", params={"m_Space": SPACE[space]}, **k)
    def normal(self, space="world", **k):
        return self.node("NormalVector", params={"m_Space": SPACE[space]}, **k)
    def viewdir(self, space="world", **k):
        return self.node("ViewDirection", params={"m_Space": SPACE[space]}, **k)
    def screenpos(self, mode="default", **k):
        return self.node("ScreenPosition", params={"m_ScreenSpaceType": {"default": 0, "raw": 1, "center": 2, "tiled": 3, "pixel": 4}[mode]}, **k)
    def scene_depth(self, mode="eye", **k):
        return self.node("SceneDepth", params={"m_DepthSamplingMode": {"linear01": 0, "raw": 1, "eye": 2}[mode]}, **k)
    def fresnel(self, power=None, normal=None, view=None, **k):
        kw = {}
        if power is not None: kw["Power"] = power
        if normal is not None: kw["Normal"] = normal
        if view is not None: kw["ViewDir"] = view
        return self.node("Fresnel", **kw, **k)
    def gnoise(self, uv, scale, **k): return self.node("GradientNoise", UV=uv, Scale=scale, **k)
    def snoise(self, uv, scale, **k): return self.node("Noise", UV=uv, Scale=scale, **k)
    def voronoi(self, uv, angle, density, **k): return self.node("Voronoi", UV=uv, AngleOffset=angle, CellDensity=density, **k)
    def tiling(self, uv, tiling, offset, **k): return self.node("TilingAndOffset", UV=uv, Tiling=tiling, Offset=offset, **k)
    def color(self, rgba, hdr=False, **k):
        return self.node("Color", params={"m_Color": {"color": {"r": float(rgba[0]), "g": float(rgba[1]), "b": float(rgba[2]), "a": float(rgba[3]) if len(rgba) > 3 else 1.0}, "mode": 1 if hdr else 0}}, **k)
    def float(self, v, **k): return self.node("Vector1", params={"m_Value": float(v)}, X=float(v), **k)
    def transform(self, x, frm, to, ctype="position", normalize=False, **k):
        sp = {"object": 0, "view": 1, "world": 2, "tangent": 3, "absolute": 4}
        return self.node("Transform", params={"m_Conversion": {"from": sp[frm], "to": sp[to]},
                         "m_ConversionType": {"position": 0, "direction": 1, "normal": 2}[ctype],
                         "m_Normalize": bool(normalize)}, In=x, **k)
    def rand(self, seed, lo=0.0, hi=1.0, **k): return self.node("RandomRange", Seed=seed, Min=lo, Max=hi, **k)
    def rgb(self, x, **k):
        """Quita el alfa: Split + Combine -> Vector3"""
        sp = self.split(x)
        return self.combine(sp["R"], sp["G"], sp["B"], **k)["RGB"]
    def round(self, x, **k): return self.node("Round", In=x, **k)
    def sqrt(self, x, **k): return self.node("SquareRoot", In=x, **k)
    def dither(self, x, sp, **k): return self.node("Dither", In=x, ScreenPosition=sp, **k)
    def vec2(self, x, y, **k): return self.node("Vector2", X=x, Y=y, **k)
    def vec3(self, x, y, z, **k): return self.node("Vector3", X=x, Y=y, Z=z, **k)
    def vec4(self, x, y, z, w, **k): return self.node("Vector4", X=x, Y=y, Z=z, W=w, **k)
    def branch(self, pred, t, f, **k):
        n = self.node("Branch", **k)
        self.set_input(n, "Predicate", pred); self.set_input(n, "True", t); self.set_input(n, "False", f)
        return n
    def compare(self, a, b, op="greater", **k):
        ops = {"equal": 0, "notequal": 1, "less": 2, "lessorequal": 3, "greater": 4, "greaterorequal": 5}
        return self.node("Comparison", params={"m_ComparisonType": ops[op]}, A=a, B=b, **k)
    def front_face(self, **k): return self.node("IsFrontFace", **k)
    def main_light_dir(self, **k): return self.node("MainLightDirection", **k)
    def screen_size(self, **k): return self.node("Screen", **k)
    def sample_buffer(self, uv, **k):
        n = self.node("UnityEditor.Rendering.Universal.UniversalSampleBufferNode", params={"m_BufferType": 2}, **k)
        self.set_input(n, "UV", uv)
        return n
    def posterize(self, x, steps, **k): return self.node("Posterize", In=x, Steps=steps, **k)
    def modulo(self, a, b, **k): return self.node("Modulo", A=a, B=b, **k)
    def distance(self, a, b, **k): return self.node("Distance", A=a, B=b, **k)
    def checker(self, uv, a, b, freq, **k): return self.node("Checkerboard", UV=uv, ColorA=a, ColorB=b, Frequency=freq, **k)
    def contrast(self, x, c, **k): return self.node("Contrast", In=x, Contrast=c, **k)
    def saturation(self, x, s, **k): return self.node("Saturation", In=x, Saturation=s, **k)
    def invert_colors(self, x, **k): return self.node("InvertColors", In=x, **k)
    def sign(self, x, **k): return self.node("Sign", In=x, **k)
    def clamp(self, x, lo, hi, **k): return self.node("Clamp", In=x, Min=lo, Max=hi, **k)
    def atan2(self, a, b, **k): return self.node("Arctangent2", A=a, B=b, **k)
    def rotate(self, uv, center, rot, **k): return self.node("Rotate", UV=uv, Center=center, Rotation=rot, **k)
    def polar(self, uv, center, rs=1, ls=1, **k): return self.node("PolarCoordinates", UV=uv, Center=center, RadialScale=rs, LengthScale=ls, **k)

    # ---- serializacion -------------------------------------------------------------------------
    @staticmethod
    def _note_size(text):
        import math
        w = 380.0
        h = 150.0 + 28.0 * math.ceil(max(1, len(text or "")) / 28.0)
        return w, h

    def _note_h(self, sec):
        hs = [self._note_size(st["content"])[1] for st in self.stickies if st.get("section") == sec]
        return max(hs) if hs else 0

    def _layout(self):
        """Posiciones: x por profundidad (de izquierda a derecha), y por seccion/orden."""
        depth = {}

        def d(n):
            if id(n) in depth:
                return depth[id(n)]
            ins = [o.node for o in n.inputs.values()]
            v = 0 if not ins else 1 + max(d(i) for i in ins)
            depth[id(n)] = v
            return v

        for n in self.nodes:
            d(n)
        for _, b in self.blocks:
            d(b)
        maxd = max(depth.values()) if depth else 0
        COLW, ROWH = 330, 150
        sections = [None] + [s for s in self._sections]
        # orden de secciones segun aparicion
        order = []
        for n in self.nodes:
            if n.section not in order:
                order.append(n.section)
        y_cursor = 0
        self._group_pos = {}
        for sec in order:
            ns = [n for n in self.nodes if n.section == sec]
            cols = {}
            for n in ns:
                cols.setdefault(depth[id(n)], []).append(n)
            band = 0
            for c, lst in cols.items():
                y = 0
                for n in lst:
                    n.pos = (c * COLW, y_cursor + y)
                    h = 330 if n.preview else (ROWH if n.kind != "CustomFunction" else 220)
                    y += h
                band = max(band, y)
            band = max(band, self._note_h(sec))
            y_cursor += band + 130
            self._group_pos[sec] = (0, y_cursor - band - 130)
        # bloques a la derecha, centrados
        bx = (maxd + 1) * COLW + 120
        vy, fy = -40, 0
        for desc, b in self.blocks:
            if desc.startswith("VertexDescription"):
                b.pos = (bx, vy); vy += 0
            else:
                b.pos = (bx, fy)
        self._block_x = bx

    def to_objects(self):
        T = templates()
        used = set(id(o.node) for o, _, _ in self.edges)
        self.nodes = [n for n in self.nodes if n.kind not in self.SOURCE_KINDS or id(n) in used]
        if self.target != "fullscreen":
            have = [d for d, _ in self.blocks]
            for d in ("VertexDescription.Position", "VertexDescription.Normal", "VertexDescription.Tangent"):
                if d not in have:
                    self.block(d, None)
        self._layout()
        objs = []
        graph = copy.deepcopy(T["graph"]["fullscreen"])
        graph["m_ObjectId"] = self.oid()

        # grupos
        group_ids = {}
        groups = []
        for sec in self._sections:
            gobj = copy.deepcopy(T["group"])
            gobj["m_ObjectId"] = self.oid()
            gobj["m_Title"] = sec
            gx, gy = self._group_pos.get(sec, (0, 0))
            gobj["m_Position"] = {"x": float(gx - 20), "y": float(gy - 70)}
            group_ids[sec] = gobj["m_ObjectId"]
            groups.append(gobj)
        for n in self.nodes:
            if n.section is not None and n.section in group_ids:
                n.obj["m_Group"] = {"m_Id": group_ids[n.section]}
        # nota general de lectura (arriba del primer grupo)
        if not getattr(self, "no_intro", False):
            self.stickies.insert(0, {"section": None, "title": "Como leer este grafo", "content":
                "Pulsa A para ver todo el grafo (F centra la seleccion).\n"
                "Los datos viajan de IZQUIERDA a DERECHA hasta el Master Stack (a la derecha del todo).\n"
                "Cada recuadro numerado es un PASO; la nota amarilla de su izquierda lo explica.\n"
                "Los circulos/cuadros dentro de los nodos son vistas previas del valor en ese punto."})
        # notas
        stickies = []
        sy_free = -420
        for st in self.stickies:
            sobj = copy.deepcopy(T["sticky"])
            sobj["m_ObjectId"] = self.oid()
            sobj["m_Title"] = st["title"] or ""
            sobj["m_Content"] = st["content"]
            sobj["m_TextSize"] = 1
            sobj["m_Theme"] = 0
            sec = st.get("section")
            if sec in group_ids:
                gx, gy = self._group_pos.get(sec, (0, 0))
                nw, nh = self._note_size(st["content"])
                sobj["m_Position"] = {"serializedVersion": "2", "x": float(gx - nw - 50), "y": float(gy), "width": nw, "height": nh}
                sobj["m_Group"] = {"m_Id": group_ids[sec]}
            else:
                sobj["m_Position"] = {"serializedVersion": "2", "x": -330.0, "y": -540.0, "width": 700.0, "height": 330.0}
                sobj["m_Group"] = {"m_Id": ""}
                sy_free += 0
            stickies.append(sobj)

        # target
        tgt = copy.deepcopy(T["targets"]["UnityEditor.Rendering.Universal.ShaderGraph.UniversalTarget"])
        tgt["m_ObjectId"] = self.oid()
        sub_key = {"unlit": "UniversalUnlitSubTarget", "lit": "UniversalLitSubTarget", "fullscreen": "UniversalFullscreenSubTarget"}[self.target]
        sub = copy.deepcopy(T["targets"]["UnityEditor.Rendering.Universal.ShaderGraph." + sub_key])
        sub["m_ObjectId"] = self.oid()
        tgt["m_ActiveSubTarget"] = {"m_Id": sub["m_ObjectId"]}
        tgt["m_Datas"] = []
        extra = []
        if self.target == "fullscreen":
            fd = copy.deepcopy(T["targets"]["UnityEditor.Rendering.Fullscreen.ShaderGraph.FullscreenData"])
            fd["m_ObjectId"] = self.oid()
            tgt["m_Datas"] = [{"m_Id": fd["m_ObjectId"]}]
            extra.append(fd)
        else:
            tgt["m_SurfaceType"] = 1 if self.surface == "transparent" else 0
            tgt["m_AlphaMode"] = {"alpha": 0, "premultiply": 1, "additive": 2, "multiply": 3}[self.blend]
            tgt["m_RenderFace"] = {"front": 2, "back": 1, "both": 0}[self.render_face]
            tgt["m_AlphaClip"] = bool(self.alpha_clip)
            tgt["m_CastShadows"] = bool(self.cast_shadows)
            tgt["m_ReceiveShadows"] = bool(self.receive_shadows)
            if self.zwrite == "on":
                tgt["m_ZWriteControl"] = 1
            elif self.zwrite == "off" or self.surface == "transparent":
                tgt["m_ZWriteControl"] = 2   # ForceDisabled
        graph["m_ActiveTargets"] = [{"m_Id": tgt["m_ObjectId"]}]

        # propiedades y categoria
        cat = copy.deepcopy(T["category"])
        cat["m_ObjectId"] = self.oid()
        cat["m_Name"] = ""
        cat["m_ChildObjectList"] = [{"m_Id": p.obj["m_ObjectId"]} for p in self.props]
        graph["m_Properties"] = [{"m_Id": p.obj["m_ObjectId"]} for p in self.props]
        graph["m_CategoryData"] = [{"m_Id": cat["m_ObjectId"]}]
        graph["m_Keywords"] = []
        graph["m_Dropdowns"] = []

        # nodos y bloques
        graph["m_Nodes"] = [{"m_Id": n.obj["m_ObjectId"]} for n in self.nodes] + \
                           [{"m_Id": b.obj["m_ObjectId"]} for _, b in self.blocks]
        graph["m_GroupDatas"] = [{"m_Id": g["m_ObjectId"]} for g in groups]
        graph["m_StickyNoteDatas"] = [{"m_Id": s["m_ObjectId"]} for s in stickies]
        vert = [b for d, b in self.blocks if d.startswith("VertexDescription")]
        frag = [b for d, b in self.blocks if d.startswith("SurfaceDescription")]
        graph["m_VertexContext"] = {"m_Position": {"x": float(self._block_x), "y": -260.0}, "m_Blocks": [{"m_Id": b.obj["m_ObjectId"]} for b in vert]}
        graph["m_FragmentContext"] = {"m_Position": {"x": float(self._block_x), "y": 40.0}, "m_Blocks": [{"m_Id": b.obj["m_ObjectId"]} for b in frag]}
        graph["m_Path"] = self.path
        graph["m_SubDatas"] = []

        # edges
        edges = []
        for out, node, slot in self.edges:
            edges.append({
                "m_OutputSlot": {"m_Node": {"m_Id": out.node.obj["m_ObjectId"]}, "m_SlotId": out.slot["m_Id"]},
                "m_InputSlot": {"m_Node": {"m_Id": node.obj["m_ObjectId"]}, "m_SlotId": slot["m_Id"]},
            })
        graph["m_Edges"] = edges

        # posiciones
        def setpos(obj, pos):
            obj["m_DrawState"] = {"m_Expanded": True, "m_Position": {"serializedVersion": "2", "x": float(pos[0]), "y": float(pos[1]), "width": 0.0, "height": 0.0}}
        for n in self.nodes:
            setpos(n.obj, n.pos)
        for _, b in self.blocks:
            setpos(b.obj, (0, 0))

        objs.append(graph)
        objs.append(tgt)
        objs.append(sub)
        objs.extend(extra)
        objs.append(cat)
        objs.extend(groups)
        objs.extend(stickies)
        for p in self.props:
            objs.append(p.obj)
        for n in self.nodes:
            objs.append(n.obj)
            objs.extend(n.slots)
        for _, b in self.blocks:
            objs.append(b.obj)
            objs.extend(b.slots)
        return objs

    def save(self, path):
        objs = self.to_objects()
        os.makedirs(os.path.dirname(os.path.abspath(path)), exist_ok=True)
        with open(path, "w", encoding="utf-8", newline="\n") as f:
            f.write("\n\n".join(json.dumps(o, indent=4) for o in objs))
            f.write("\n")
        return path


SPACE = {"object": 0, "view": 1, "world": 2, "tangent": 3, "absolute": 4}
