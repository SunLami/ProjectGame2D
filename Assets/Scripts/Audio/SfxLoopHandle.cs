using UnityEngine;

/// <summary>
/// Handle of a looping SFX started with <see cref="SoundFXManager.StartLoop"/> (whirlpool, beam, wing flaps, ambience...).
/// Always non-null; every call is a safe no-op once the loop ended or the manager is gone. The owner is expected to
/// call <see cref="Stop"/> when the effect ends; loops that are never stopped end with their scene's manager.
/// </summary>
public sealed class SfxLoopHandle
{
    internal AudioSource Source;
    internal float TargetLevel = 1f;   // 0..1 multiplier the owner asked for
    internal float CurrentLevel;       // faded value actually applied
    internal float FadeInSeconds = 0.2f;
    internal float FadeOutSeconds = 0.2f;
    internal float ProfileVolume = 1f;
    internal bool Stopping;

    public bool IsPlaying => Source != null && !Stopping;

    public void SetLevel(float level)
    {
        TargetLevel = Mathf.Clamp01(level);
    }

    public void SetPitch(float pitch)
    {
        if (Source != null)
            Source.pitch = Mathf.Clamp(pitch, 0.25f, 3f);
    }

    /// <summary>Fades out and releases the source.</summary>
    public void Stop(float fadeSeconds = 0.2f)
    {
        if (Source == null || Stopping)
            return;

        Stopping = true;
        FadeOutSeconds = Mathf.Max(0.01f, fadeSeconds);
    }
}
