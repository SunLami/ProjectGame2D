"""Recipe types shared by build_sfx.py and the catalog_*.py files."""


class C:
    """One clip recipe."""

    def __init__(self, fs_id, start=None, dur=None, peak=-3.0, gain=0.0, lp=None, hp=None, pitch=1.0, fade=0.04,
                 lic="cc0", note="", xfade=None, tempo=None, rev=False, at=0.0):
        self.fs_id, self.start, self.dur, self.peak, self.gain = fs_id, start, dur, peak, gain
        self.lp, self.hp, self.pitch, self.fade, self.lic, self.note = lp, hp, pitch, fade, lic, note
        self.xfade, self.tempo, self.rev, self.at = xfade, tempo, rev, at


class Mix:
    """Several layers summed into one clip. Each layer is normalised to 0 dB, then `gain` dB is applied (relative
    level inside the mix) and it starts `at` seconds in; the sum is trimmed, faded and normalised to `peak`."""

    def __init__(self, *layers, peak=-3.0, fade=0.06, dur=None, note=""):
        self.layers, self.peak, self.fade, self.dur, self.note = layers, peak, fade, dur, note


class Synth:
    """A procedurally synthesised clip (see synth.py): `recipe` is a function name, extra kwargs go to the recipe."""

    def __init__(self, recipe, seed=1, peak=-3.0, fade=0.05, xfade=None, note="", **params):
        self.recipe, self.seed, self.peak, self.fade, self.xfade, self.note, self.params = recipe, seed, peak, fade, xfade, note, params

    def render(self):
        import synth
        return synth.RECIPES[self.recipe](self.seed, **self.params)


def S(cat, clips, vol=1.0, jitter=0.0, cooldown=0.0, voices=0, loop=False, desc="", rms=None):
    """`rms` = target loudness (dBFS RMS of the active part) of this id; default comes from the category."""
    return dict(cat=cat, clips=clips, vol=vol, jitter=jitter, cooldown=cooldown, voices=voices, loop=loop, desc=desc, rms=rms)
