"""SFX build pipeline: Freesound previews -> game-ready mono WAV/OGG + QA report + CREDITS + catalog.json for Unity.

  python Tools/sfx/build_sfx.py [--only sfx.combat.*] [--force]

The sound list lives in Tools/sfx/catalog_*.py (each file defines `ENTRIES`). A clip is a recipe, not a copy: the same
Freesound id can be trimmed/filtered/pitched differently for several game ids. Outputs:
  Assets/Resources/Audio/SFX/<Category>/<id with dots->underscores>_<n>.wav  (loops: .ogg)
  Assets/Editor/Sfx/sfx_catalog.json   (read by the Unity tool that fills SoundFXLibrary)
  Assets/Resources/Audio/SFX/CREDITS.md  (generated section between the markers)
  Tools/sfx/out/report.json            (duration / peak / rms / centroid / clipping / leading silence per file)
"""
import fnmatch
import glob
import importlib.util
import json
import os
import subprocess
import sys
import wave

import numpy as np

sys.path.insert(0, os.path.dirname(__file__))
import fs  # noqa: E402

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SFX_DIR = os.path.join(ROOT, "Assets", "Resources", "Audio", "SFX")
CATALOG_JSON = os.path.join(ROOT, "Assets", "Editor", "Sfx", "sfx_catalog.json")
OUT_DIR = os.path.join(os.path.dirname(__file__), "out")
WORK = os.path.join(os.environ.get("TEMP", "/tmp"), "sfx_work")
SR = 44100
CAT_RMS = {"Combat": -17, "Skill": -17, "Boss": -15, "World": -19, "UI": -20, "Quest": -19, "Commerce": -19,
           "Fishing": -19, "Farming": -20, "Player": -17, "System": -19, "Ambience": -26}
PEAK_CAP_DB = -1.5
MARK_BEGIN = "<!-- GENERATED:BEGIN (Tools/sfx/build_sfx.py) -->"
MARK_END = "<!-- GENERATED:END -->"


from sfxtypes import C, Mix, S, Synth  # noqa: E402


def load_entries():
    entries = {}
    for path in sorted(glob.glob(os.path.join(os.path.dirname(__file__), "catalog_*.py"))):
        spec = importlib.util.spec_from_file_location(os.path.basename(path)[:-3], path)
        mod = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(mod)
        for key, value in mod.ENTRIES.items():
            if key in entries:
                raise SystemExit("duplicate sfx id: " + key)
            entries[key] = value
    return entries


def decode(mp3, clip):
    """ffmpeg: decode + filters -> mono float32 array at 44.1k."""
    filters = []
    if clip.hp:
        filters.append(f"highpass=f={clip.hp}")
    if clip.lp:
        filters.append(f"lowpass=f={clip.lp}")
    if clip.pitch and abs(clip.pitch - 1.0) > 1e-3:
        filters.append(f"asetrate={int(SR * clip.pitch)},aresample={SR}")
    if clip.tempo and abs(clip.tempo - 1.0) > 1e-3:
        filters.append(f"atempo={clip.tempo}")
    cmd = ["ffmpeg", "-v", "error", "-i", mp3, "-ac", "1", "-ar", str(SR)]
    if filters:
        cmd += ["-af", ",".join(filters)]
    cmd += ["-f", "f32le", "-"]
    raw = subprocess.run(cmd, capture_output=True, check=True).stdout
    return np.frombuffer(raw, dtype=np.float32).copy()


def db(x):
    return 20 * np.log10(max(x, 1e-9))


def trim_leading(a, thresh_db=-42.0, keep=0.004):
    thresh = 10 ** (thresh_db / 20)
    idx = np.where(np.abs(a) > thresh)[0]
    if len(idx) == 0:
        return a
    start = max(0, idx[0] - int(keep * SR))
    return a[start:]


def trim_trailing(a, thresh_db=-50.0):
    thresh = 10 ** (thresh_db / 20)
    idx = np.where(np.abs(a) > thresh)[0]
    return a[: idx[-1] + int(0.01 * SR)] if len(idx) else a


def prepare(clip, mp3, loop):
    """Decode + trim one layer (no level normalisation yet)."""
    a = decode(mp3, clip)
    if clip.start:
        a = a[int(clip.start * SR):]
    if clip.rev:
        a = a[::-1].copy()
    if not loop:
        a = trim_leading(a)
    if clip.dur:
        a = a[: int(clip.dur * SR)]
    if not loop:
        a = trim_trailing(a)
    return a


def shape(a, fade_s, peak_db, gain_db, loop, xfade):
    if loop:
        xf = int((xfade or 1.2) * SR)
        if len(a) <= xf * 2:
            raise SystemExit("loop clip too short for crossfade")
        head, tail = a[:xf], a[-xf:]
        t = np.linspace(0, np.pi / 2, xf)
        blended = tail * np.cos(t) + head * np.sin(t)
        a = np.concatenate([blended, a[xf:-xf]])  # loop point: end of body flows into the start of `blended`
    else:
        fade = min(int(fade_s * SR), len(a) // 2)
        if fade > 0:
            a[-fade:] *= np.linspace(1, 0, fade)
        ramp = int(0.002 * SR)
        a[:ramp] *= np.linspace(0, 1, ramp)  # de-click
    peak = float(np.max(np.abs(a))) if len(a) else 0.0
    if peak > 1e-6:
        a = a * (10 ** ((peak_db - db(peak) + gain_db) / 20))
    return np.clip(a, -1.0, 1.0)


def process(variant, mp3_for, loop):
    """mp3_for(fs_id) -> local mp3 path. Returns the finished mono float array."""
    if isinstance(variant, Synth):
        return shape(np.asarray(variant.render(), dtype=np.float64), variant.fade, variant.peak, 0.0, loop, variant.xfade)
    if isinstance(variant, Mix):
        length = 0
        parts = []
        for layer in variant.layers:
            a = prepare(layer, mp3_for(layer.fs_id), False)
            peak = float(np.max(np.abs(a))) if len(a) else 0.0
            if peak > 1e-6:
                a = a / peak * (10 ** (layer.gain / 20))
            if layer.fade:
                f = min(int(layer.fade * SR), len(a) // 2)
                if f:
                    a[-f:] *= np.linspace(1, 0, f)
            offset = int(layer.at * SR)
            parts.append((offset, a))
            length = max(length, offset + len(a))
        mixed = np.zeros(length, dtype=np.float32)
        for offset, a in parts:
            mixed[offset: offset + len(a)] += a
        if variant.dur:
            mixed = mixed[: int(variant.dur * SR)]
        mixed = trim_trailing(mixed)
        return shape(mixed, variant.fade, variant.peak, 0.0, False, None)
    a = prepare(variant, mp3_for(variant.fs_id), loop)
    return shape(a, variant.fade, variant.peak, variant.gain, loop, variant.xfade)


def active_rms_db(a):
    active = a[np.abs(a) > 10 ** (-40 / 20)]
    return db(float(np.sqrt(np.mean(active ** 2)))) if len(active) else -90.0


def balance(arrays, target_db):
    """Give every variant of an id the same loudness (RMS of the active part), without exceeding the peak cap."""
    cap = 10 ** (PEAK_CAP_DB / 20)
    out = []
    for a in arrays:
        gain = 10 ** ((target_db - active_rms_db(a)) / 20)
        b = a * gain
        peak = float(np.max(np.abs(b))) if len(b) else 0.0
        if peak > cap:
            b = b * (cap / peak)
        out.append(np.clip(b, -1.0, 1.0))
    return out


def metrics(a):
    if len(a) == 0:
        return {}
    peak = float(np.max(np.abs(a)))
    active = a[np.abs(a) > 10 ** (-40 / 20)]
    rms = float(np.sqrt(np.mean(active ** 2))) if len(active) else 0.0
    spec = np.abs(np.fft.rfft(a[: min(len(a), SR * 2)] * np.hanning(min(len(a), SR * 2))))
    freqs = np.fft.rfftfreq(min(len(a), SR * 2), 1 / SR)
    centroid = float(np.sum(freqs * spec) / max(np.sum(spec), 1e-9))
    lead = int(np.argmax(np.abs(a) > 10 ** (-42 / 20))) / SR
    return {"duration": round(len(a) / SR, 3), "peak_db": round(db(peak), 1), "rms_db": round(db(rms), 1),
            "centroid_hz": int(centroid), "clipped": int(np.sum(np.abs(a) >= 0.999)), "lead_s": round(lead, 3)}


def write_wav(path, a):
    pcm = (a * 32767).astype("<i2")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())


def write_ogg(path, a):
    tmp = path + ".tmp.wav"
    write_wav(tmp, a)
    subprocess.run(["ffmpeg", "-v", "error", "-y", "-i", tmp, "-c:a", "libvorbis", "-q:a", "4", path], check=True)
    os.remove(tmp)


def file_stem(sfx_id):
    return sfx_id.replace(".", "_")


def main():
    only = [a for a in sys.argv[1:] if not a.startswith("--")]
    force = "--force" in sys.argv
    os.makedirs(WORK, exist_ok=True)
    os.makedirs(OUT_DIR, exist_ok=True)
    os.makedirs(os.path.dirname(CATALOG_JSON), exist_ok=True)
    entries = load_entries()
    report_path = os.path.join(OUT_DIR, "report.json")
    report = json.load(open(report_path)) if os.path.exists(report_path) else {}
    catalog = []
    credits = {}
    problems = []
    for sfx_id, e in entries.items():
        selected = not only or any(fnmatch.fnmatch(sfx_id, p) for p in only)
        folder = os.path.join(SFX_DIR, e["cat"])
        os.makedirs(folder, exist_ok=True)
        files = []
        ext = "ogg" if e["loop"] else "wav"
        names = [f"{file_stem(sfx_id)}_{i}.{ext}" for i in range(1, len(e["clips"]) + 1)]
        outs = [os.path.join(folder, n) for n in names]
        rels = [f"Assets/Resources/Audio/SFX/{e['cat']}/{n}" for n in names]
        files.extend(rels)
        if selected and (force or any(not os.path.exists(o) for o in outs)):
            try:
                metas_per_clip, arrays = [], []
                for clip in e["clips"]:
                    metas = []

                    def mp3_for(fs_id, metas=metas):
                        mp3 = os.path.join(WORK, f"{fs_id}.mp3")
                        if not os.path.exists(mp3):
                            meta, _ = fs.download_preview(fs_id, mp3)
                        else:
                            meta = fs.info(fs_id)
                        lic = meta["license_url"].lower()
                        if fs.license_name(lic) == "UNKNOWN" or "-nc" in lic or "sampling" in lic:
                            raise RuntimeError("licence not allowed: " + lic)
                        metas.append(meta)
                        return mp3

                    arrays.append(process(clip, mp3_for, e["loop"]))
                    metas_per_clip.append(metas)
                arrays = balance(arrays, e["rms"] if e["rms"] is not None else CAT_RMS.get(e["cat"], -18))
                for clip, audio, out, rel, metas in zip(e["clips"], arrays, outs, rels, metas_per_clip):
                    (write_ogg if e["loop"] else write_wav)(out, audio)
                    m = metrics(audio)
                    m.update(sfx=sfx_id, sources=[{"fs": meta["id"], "title": meta["title"], "user": meta["user"],
                                                   "license": fs.license_name(meta["license_url"]), "url": meta["url"]} for meta in metas],
                             note=getattr(clip, "note", ""))
                    report[rel] = m
            except Exception as ex:  # keep going: one bad id must not stop the batch
                problems.append(f"{sfx_id}: {ex}")
        for rel in rels:
            if rel in report:
                credits[rel] = report[rel]
        catalog.append({"id": sfx_id, "category": e["cat"], "files": files, "volume": e["vol"], "pitchJitter": e["jitter"],
                        "minInterval": e["cooldown"], "maxVoices": e["voices"], "loop": e["loop"], "desc": e["desc"]})
    json.dump(report, open(report_path, "w"), indent=1)
    json.dump({"entries": catalog}, open(CATALOG_JSON, "w"), indent=1)
    write_credits(credits)
    qa(report, entries)
    if problems:
        print("\nPROBLEMS:")
        for p in problems:
            print("  " + p)
    print(f"\n{len(entries)} sfx ids, {len(report)} files in report")


def write_credits(credits):
    path = os.path.join(SFX_DIR, "CREDITS.md")
    text = open(path, encoding="utf-8").read() if os.path.exists(path) else "# Audio SFX Credits\n"
    if MARK_BEGIN in text:
        text = text[: text.index(MARK_BEGIN)].rstrip() + "\n"
    rows = ["| Output file | Freesound source(s) | Author(s) | License(s) |", "| --- | --- | --- | --- |"]
    for rel in sorted(credits):
        r = credits[rel]
        short = rel.replace("Assets/Resources/Audio/SFX/", "")
        srcs = r.get("sources", [])
        rows.append("| `{}` | {} | {} | {} |".format(
            short,
            "; ".join(f"[{x['title'].replace('|', '/')}]({x['url']})" for x in srcs) or "procedural (Tools/sfx/synth.py)",
            ", ".join(sorted({x["user"] for x in srcs})),
            ", ".join(sorted({x["license"] for x in srcs})) or "own work"))
    body = (f"\n\n{MARK_BEGIN}\n## Generated catalog (combat, skills, bosses, world, environment, UI additions)\n\n"
            "Every file below is a trimmed/filtered/normalised derivative of the linked Freesound preview "
            "(mono, 44.1 kHz). Files licensed CC BY require this attribution; CC0 files are listed for provenance.\n\n"
            + "\n".join(rows) + f"\n{MARK_END}\n")
    open(path, "w", encoding="utf-8").write(text + body)


def qa(report, entries):
    print("QA flags:")
    flagged = 0
    for rel, m in sorted(report.items()):
        flags = []
        if m.get("clipped", 0) > 0:
            flags.append("CLIPPED")
        if m.get("lead_s", 0) > 0.05:
            flags.append(f"lead {m['lead_s']}s")
        if m.get("duration", 0) < 0.06:
            flags.append("TOO SHORT")
        if flags:
            flagged += 1
            print("  " + rel.split("SFX/")[-1], flags)
    print(f"  {flagged} flagged of {len(report)}")


if __name__ == "__main__":
    main()
