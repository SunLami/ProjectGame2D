"""Pixabay source for build_sfx.py. A clip id looks like "px:2022/06/07/audio_b9bd4170e4#112906" - the CDN path of
the sound (from the Pixabay page / search JSON, never guessed) plus its Pixabay id. Files are fetched from
cdn.pixabay.com (the site itself blocks scripted requests; search is done in a browser, see SfxPipeline.md).
Licence: Pixabay Content License - free for use in a game, no attribution required, do not redistribute the raw
files as a sound pack."""
import os
import subprocess

CDN = "https://cdn.pixabay.com/audio/"


def is_px(clip_id):
    return isinstance(clip_id, str) and clip_id.startswith("px:")


def parse(clip_id):
    path, _, pid = clip_id[3:].partition("#")
    return path, pid


def fetch(clip_id, work):
    path, pid = parse(clip_id)
    os.makedirs(work, exist_ok=True)
    mp3 = os.path.join(work, "px_" + path.replace("/", "_") + ".mp3")
    if not os.path.exists(mp3) or os.path.getsize(mp3) < 1000:
        subprocess.run(["curl", "-sSfL", "-A", "Mozilla/5.0", "-o", mp3, CDN + path + ".mp3"], check=True)
    meta = {"id": "px" + pid, "title": NAMES.get(clip_id, "Pixabay #" + pid), "user": USERS.get(clip_id, "Pixabay"),
            "license": "Pixabay Content License",
            "url": f"https://pixabay.com/sound-effects/{pid}/" if pid else "https://pixabay.com/sound-effects/"}
    return mp3, meta


# Filled by the catalog (P() registers the human-readable name / author for CREDITS.md).
NAMES = {}
USERS = {}
