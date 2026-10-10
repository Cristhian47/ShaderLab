#!/usr/bin/env python3
"""Extrae plantillas de nodos/slots/propiedades/targets de los .shadergraph de muestra
de los paquetes de Unity y las guarda en templates.json.
Uso: python3 mine_templates.py <carpeta PackageCache> <salida templates.json>
"""
import glob, json, sys, collections

def parse(txt):
    dec = json.JSONDecoder(); i = 0; out = []
    while i < len(txt):
        while i < len(txt) and txt[i].isspace():
            i += 1
        if i >= len(txt):
            break
        o, j = dec.raw_decode(txt, i); out.append(o); i = j
    return out

def ids_in(o):
    """Todas las referencias {"m_Id": ...} que contiene el objeto"""
    res = []
    def walk(x):
        if isinstance(x, dict):
            if set(x.keys()) == {"m_Id"}:
                res.append(x["m_Id"])
            else:
                for v in x.values():
                    walk(v)
        elif isinstance(x, list):
            for v in x:
                walk(v)
    walk(o)
    return res

cache = sys.argv[1]
out_path = sys.argv[2]
files = sorted(glob.glob(cache + "/**/*.shadergraph", recursive=True))

node_t = {}       # m_Type -> (score, node, slots)
block_t = {}      # descriptor -> (node, slots)
prop_t = {}       # m_Type -> property json
target_t = {}     # m_Type -> json (para targets, subtargets, data)
graph_t = {}      # nombre fichero -> graphdata (cabecera) para referencia
cat_t = None
group_t = None
sticky_t = None

for f in files:
    try:
        objs = parse(open(f, encoding="utf-8").read())
    except Exception as e:
        print("ERR", f, e); continue
    by = {o["m_ObjectId"]: o for o in objs if "m_ObjectId" in o}
    for o in objs:
        t = o.get("m_Type", "")
        if t == "UnityEditor.ShaderGraph.GraphData" and "Fullscreen Basic" in f:
            graph_t["fullscreen"] = o
        if t == "UnityEditor.ShaderGraph.GraphData" and "SG_SimpleLit" in f:
            graph_t["lit"] = o
        if t == "UnityEditor.ShaderGraph.CategoryData" and cat_t is None:
            cat_t = o
        if t == "UnityEditor.ShaderGraph.GroupData" and group_t is None:
            group_t = o
        if t == "UnityEditor.ShaderGraph.StickyNoteData" and sticky_t is None:
            sticky_t = o
        if t == "UnityEditor.ShaderGraph.BlockNode":
            d = o.get("m_SerializedDescriptor")
            if d and d not in block_t:
                slots = [by[i] for i in ids_in(o.get("m_Slots", [])) if i in by]
                block_t[d] = (o, slots)
        elif t.startswith("UnityEditor.ShaderGraph.") and (t.endswith("Node") or t.endswith("NodeData") or t.endswith("SampleGradient")) \
                and "m_Slots" in o and t not in ("UnityEditor.ShaderGraph.SubGraphNode",):
            slots = [by[i] for i in ids_in(o.get("m_Slots", [])) if i in by]
            score = len(json.dumps(o))
            if t not in node_t or score < node_t[t][0]:
                node_t[t] = (score, o, slots)
        elif t.startswith("UnityEditor.Rendering.Universal.UniversalSampleBufferNode") or t.endswith("UniversalSampleBufferNode"):
            slots = [by[i] for i in ids_in(o.get("m_Slots", [])) if i in by]
            if t not in node_t:
                node_t[t] = (0, o, slots)
        elif t.endswith("ShaderProperty"):
            key = t + ("|" + str(o.get("m_FloatType")) if "Vector1" in t else "") + ("|" + str(o.get("m_ColorMode")) if "Color" in t else "")
            if key not in prop_t:
                prop_t[key] = o
        elif "UniversalTarget" in t or "Universal" in t and ("SubTarget" in t) or "Fullscreen" in t and "Data" in t:
            if t not in target_t:
                target_t[t] = o

# Tambien guardar un par de grafos completos de referencia (targets reales por variante)
refs = {}
for f in files:
    if any(k in f for k in ["Fullscreen Basic URP", "SG_SimpleLit", "SamplesEmissive", "SamplesTransparentInfos"]):
        refs[f.split("/")[-1]] = parse(open(f, encoding="utf-8").read())

tpl = {
    "nodes": {t: {"node": v[1], "slots": v[2]} for t, v in node_t.items()},
    "blocks": {d: {"node": v[0], "slots": v[1]} for d, v in block_t.items()},
    "props": prop_t,
    "targets": target_t,
    "category": cat_t,
    "group": group_t,
    "sticky": sticky_t,
    "graph": graph_t,
}
json.dump(tpl, open(out_path, "w"), indent=1)
json.dump(refs, open(out_path.replace("templates", "refs"), "w"), indent=1)
print("nodos", len(tpl["nodes"]), "bloques", len(tpl["blocks"]), "props", len(prop_t), "targets", len(target_t))
