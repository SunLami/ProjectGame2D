using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// Drives the ActorDialogueTrack: watches which clip is active on this track and, each time a
/// new one takes over, pauses the owning PlayableDirector and opens the bound actor's speech
/// bubble for that line. The bubble itself resumes the director once the player advances past it.
///
/// Dedup state (which clip index was last shown) is intentionally NOT kept on an instance field
/// of this PlayableBehaviour. PlayableDirector.Pause()/Play() -- exactly the pair this class uses
/// to hold on a line and then resume -- tears down and rebuilds the entire PlayableGraph under
/// the hood (confirmed by instrumenting OnGraphStart/OnGraphStop: both fire again on every single
/// dialogue line, not just once per cutscene). A fresh graph means a fresh PlayableBehaviour
/// instance with every field back at its default, so an instance field here would forget it had
/// already shown a line the moment the player closed it and the director resumed -- observed as
/// already-closed lines re-opening with their original text moments later. TrackAsset (the source
/// asset, not a graph object) survives rebuilds, so LastShownIndex below is keyed by it instead.
/// </summary>
public sealed class ActorDialogueMixerBehaviour : PlayableBehaviour
{
    // Keyed by the owning ActorDialogueTrack asset, which is stable across graph rebuilds (unlike
    // this behaviour instance). GameplayTimelineController.Play() clears this via ResetAll() at
    // the start of each real cutscene playthrough, so a track that reuses the same asset across
    // separate playthroughs (e.g. replaying in the Editor) starts clean each time.
    private static readonly Dictionary<TrackAsset, int> LastShownIndex = new();

    public TrackAsset SourceTrack;

    public static void ResetAll() => LastShownIndex.Clear();

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        Transform actor = playerData as Transform;
        if (actor == null || SourceTrack == null)
            return;

        int rawActiveInput = -1;
        int inputCount = playable.GetInputCount();
        for (int i = 0; i < inputCount; i++)
        {
            if (playable.GetInputWeight(i) > 0.5f)
            {
                rawActiveInput = i;
                break;
            }
        }

        if (rawActiveInput < 0)
            return;

        // Dictionary<>.TryGetValue defaults a missing key's out value to 0 (int's default),
        // which would misread "never shown anything on this track yet" as "clip 0 already
        // shown" and skip straight to clip 1 -- explicitly default to -1 instead.
        int lastShown = LastShownIndex.TryGetValue(SourceTrack, out int shown) ? shown : -1;
        if (rawActiveInput <= lastShown)
            return;

        // Only ever step forward exactly one clip at a time from wherever we last stopped,
        // regardless of how far rawActiveInput jumped -- this is what guarantees no line is
        // silently skipped, even if a single frame's delta spans an entire clip (a GC/asset
        // hitch, an unfocused/backgrounded Editor catching up, or simply a clip shorter than one
        // frame's worth of unscaled time can otherwise leave a clip's weight already back below
        // 0.5, its successor's already active, before this ever samples it). The bound
        // animation/motion tracks will show whatever later pose the skipped-to time landed on
        // for that one held line -- a lesser cost than either skipping the line or (as tried and
        // reverted) writing to director.time from inside ProcessFrame to pull the pose back into
        // view, which mutates the director's own playhead while it is mid-evaluation of this very
        // frame and was observed to trigger the same graph-rebuild-wipes-state issue described
        // above on every other track too, not just this one.
        int nextInput = lastShown + 1;
        LastShownIndex[SourceTrack] = nextInput;

        var clipPlayable = (ScriptPlayable<ActorDialogueBehaviour>)playable.GetInput(nextInput);
        ActorDialogueBehaviour behaviour = clipPlayable.GetBehaviour();

        PlayableDirector director = playable.GetGraph().GetResolver() as PlayableDirector;
        if (director == null)
            return;

        ActorSpeechBubble bubble = actor.GetComponentInChildren<ActorSpeechBubble>(true);
        if (bubble == null)
            return;

        // NOTE: do NOT call director.Evaluate() here. ProcessFrame is itself invoked from inside
        // the director's own graph evaluation, so a synchronous Evaluate() call here re-enters
        // that same evaluation while it's still on the stack -- this reliably hung the Editor
        // (reproduced twice) exactly at the moment this track's clip activates. Show() doesn't
        // need it anyway: it sets the bubble's text directly rather than depending on another
        // track's evaluated frame.
        director.Pause();
        bubble.Show(behaviour.SpeakerName, behaviour.Text, director, behaviour.FlipHorizontal);
    }
}
