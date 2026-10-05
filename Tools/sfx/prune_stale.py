"""Deletes SFX files (and metas) that no catalog entry references any more, and purges them from report.json."""
import glob
import json
import os

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
os.chdir(ROOT)
entries = json.load(open("Assets/Editor/Sfx/sfx_catalog.json", encoding="utf-8"))["entries"]
keep = {f for e in entries for f in e["files"]}
cats = {os.path.dirname(f) for f in keep}
for folder in cats:
    for f in glob.glob(folder + "/*"):
        f = f.replace("\\", "/")
        if f.endswith(".meta") or f in keep:
            continue
        if not f.endswith((".wav", ".ogg")):
            continue
        # hand-made legacy UI clips are not in the catalog: never touch files without a generated "_<n>" suffix
        stem = os.path.splitext(os.path.basename(f))[0]
        if not stem.rsplit("_", 1)[-1].isdigit():
            continue
        print("stale", f)
        os.remove(f)
        if os.path.exists(f + ".meta"):
            os.remove(f + ".meta")
report_path = "Tools/sfx/out/report.json"
report = json.load(open(report_path, encoding="utf-8"))
for k in [k for k in report if not os.path.exists(k)]:
    del report[k]
json.dump(report, open(report_path, "w", encoding="utf-8"), indent=1)
