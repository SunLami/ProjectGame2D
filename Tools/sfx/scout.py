"""Run many Freesound searches at once: python Tools/sfx/scout.py "label|query|maxdur[|by]" ...
Prints the best CC0 (or CC BY with the 4th field) candidates per query: id, duration, downloads, author, title."""
import os
import sys

sys.path.insert(0, os.path.dirname(__file__))
import fs  # noqa: E402

n = int(os.environ.get("N", "6"))
for spec in sys.argv[1:]:
    parts = spec.split("|")
    label, query = parts[0], parts[1]
    mx = float(parts[2]) if len(parts) > 2 and parts[2] else None
    lic = "by" if len(parts) > 3 and parts[3] == "by" else "cc0"
    res, used = [], query
    words = query.split()
    try:
        # Freesound ANDs every word: progressively drop trailing words until something matches
        for k in range(len(words), 0, -1):
            used = " ".join(words[:k])
            res = fs.search(used, lic, mx)
            if len(res) >= 3:
                break
    except Exception as ex:
        print(f"## {label}: ERROR {ex}")
        continue
    query = used if used != query else query
    print(f"## {label}  [{query}]  ({lic}, max {mx}s)")
    for r in res[:n]:
        print(f'  {r["id"]:>8} {r["duration"]:5.2f}s {r["downloads"]:>6}dl {r["user"][:16]:<16} {r["title"][:58]}')
