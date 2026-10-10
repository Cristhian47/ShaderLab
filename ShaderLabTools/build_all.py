import json, os, subprocess, sys
here = os.path.dirname(os.path.abspath(__file__))
root = sys.argv[1]
for f in ("_objects_manifest.json", "_screen_manifest.json"):
    p = os.path.join(root, f)
    if os.path.exists(p): os.remove(p)
subprocess.check_call([sys.executable, os.path.join(here, "gen_objects.py"), root])
subprocess.check_call([sys.executable, os.path.join(here, "gen_screen.py"), root])
items = json.load(open(os.path.join(here, "_objects_manifest.json"), encoding="utf-8")) + json.load(open(os.path.join(here, "_screen_manifest.json"), encoding="utf-8"))
json.dump({"items": items}, open(os.path.join(root, "graphs.json"), "w", encoding="utf-8"), indent=1, ensure_ascii=False)
print("graphs.json", len(items))
