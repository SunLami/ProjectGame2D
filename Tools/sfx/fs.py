"""Tiny Freesound client that needs no login/API key: it reads the public search/sound pages and downloads the public
previews (hq mp3). Used by Tools/sfx/build_sfx.py and for interactive searching.

  python Tools/sfx/fs.py search "sword swing" [--cc0|--by] [--max 3.0] [--n 15]
  python Tools/sfx/fs.py info 60024

Licences: --cc0 (default) = Creative Commons 0; --by = Attribution (credit required, recorded in CREDITS.md).
Never used: NonCommercial, Sampling+ or unknown licences."""
import html
import json
import os
import re
import subprocess
import sys
import urllib.parse

CACHE = os.path.join(os.environ.get("TEMP", "/tmp"), "fs_cache")
os.makedirs(CACHE, exist_ok=True)
UA = "Mozilla/5.0 (ProjectGame2D SFX tool)"
LICENSES = {
    "cc0": 'license:"Creative Commons 0"',
    "by": 'license:"Attribution"',
}


def _curl(url, out=None, retries=3):
    for _ in range(retries):
        cmd = ["curl", "-sfL", "-m", "40", "-A", UA]
        if out:
            cmd += ["-o", out]
        r = subprocess.run(cmd + [url], capture_output=True)
        if r.returncode == 0:
            return r.stdout.decode("utf-8", "ignore") if not out else ""
    raise RuntimeError("download failed: " + url)


def _cached(url):
    path = os.path.join(CACHE, re.sub(r"[^A-Za-z0-9]+", "_", url)[-150:] + ".html")
    if not os.path.exists(path):
        open(path, "w", encoding="utf-8").write(_curl(url))
    return open(path, encoding="utf-8", errors="ignore").read()


def search(query, lic="cc0", max_duration=None, pages=1, sort="downloads desc"):
    results = []
    for page in range(1, pages + 1):
        params = {"q": query, "f": LICENSES[lic] + (f" duration:[0 TO {max_duration}]" if max_duration else ""), "s": sort, "page": page}
        h = _cached("https://freesound.org/search/?" + urllib.parse.urlencode(params))
        for block in h.split('class="bw-search__result"')[1:]:
            sid = re.search(r'data-sound-id="(\d+)"', block)
            if not sid:
                continue
            results.append({
                "id": int(sid.group(1)),
                "user": (re.search(r'data-username="([^"]*)"', block) or [0, ""])[1],
                "title": html.unescape((re.search(r'data-title="([^"]*)"', block) or [0, ""])[1]),
                "duration": float((re.search(r'data-duration="([\d.]+)"', block) or [0, 0])[1]),
                "downloads": int((re.search(r'data-num-downloads="(\d+)"', block) or [0, 0])[1]),
                "license": lic,
            })
    return results


def info(sound_id):
    """Metadata of one sound; also resolves the author from the redirecting short url."""
    h = _cached(f"https://freesound.org/s/{sound_id}/")
    og = re.search(r'data-title="([^"]*)"', h)
    user = re.search(r'property="og:audio:artist" content="([^"]*)"', h) or re.search(r'data-username="([^"]*)"', h)
    userid = re.search(r'data-user-id="(\d+)"', h)
    dur = re.search(r'data-duration="([\d.]+)"', h)
    mp3 = re.search(r'data-mp3="([^"]*)"', h)
    lic = re.search(r'href="(https?://creativecommons\.org/[^"]*)"', h)
    tags = re.findall(r'href="/browse/tags/([^/"]+)/"', h)
    desc = re.search(r'id="soundDescriptionSection"[^>]*>(.*?)</div>', h, re.S) or re.search(r'property="og:description" content="([^"]*)"', h)
    return {
        "id": int(sound_id),
        "title": html.unescape(og.group(1)) if og else "",
        "user": user.group(1) if user else "",
        "user_id": userid.group(1) if userid else "",
        "duration": float(dur.group(1)) if dur else 0.0,
        "license_url": lic.group(1) if lic else "",
        "tags": tags[:15],
        "preview_mp3": mp3.group(1) if mp3 else "",
        "description": re.sub(r"\s+", " ", html.unescape(re.sub(r"<[^>]+>", " ", desc.group(1))))[:300] if desc else "",
        "url": f"https://freesound.org/people/{user.group(1)}/sounds/{sound_id}/" if user else f"https://freesound.org/s/{sound_id}/",
    }


def license_name(url):
    if "publicdomain/zero" in url:
        return "CC0 1.0"
    m = re.search(r"/licenses/([a-z-]+)/([\d.]+)", url)
    return f"CC {m.group(1).upper()} {m.group(2)}" if m else "UNKNOWN"


def download_preview(sound_id, dest_mp3):
    """Downloads the best public preview (hq mp3, falls back to the page's own preview url)."""
    meta = info(sound_id)
    lq = meta["preview_mp3"]
    candidates = []
    if lq:
        candidates.append(lq.replace("-lq.mp3", "-hq.mp3"))
        candidates.append(lq)
    for url in candidates:
        try:
            _curl(url, out=dest_mp3, retries=2)
            if os.path.getsize(dest_mp3) > 1000:
                return meta, url
        except Exception:
            pass
    raise RuntimeError(f"no preview for {sound_id}")


if __name__ == "__main__":
    cmd = sys.argv[1]
    if cmd == "search":
        lic = "by" if "--by" in sys.argv else "cc0"
        mx = float(sys.argv[sys.argv.index("--max") + 1]) if "--max" in sys.argv else None
        n = int(sys.argv[sys.argv.index("--n") + 1]) if "--n" in sys.argv else 15
        for r in search(sys.argv[2], lic, mx)[:n]:
            print(f'{r["id"]:>8}  {r["duration"]:5.2f}s  {r["downloads"]:>6}dl  {r["user"][:18]:<18} {r["title"][:60]}')
    elif cmd == "info":
        print(json.dumps(info(int(sys.argv[2])), indent=1, ensure_ascii=False))
