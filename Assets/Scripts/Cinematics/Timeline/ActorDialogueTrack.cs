using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// A Timeline track of dialogue lines spoken by one actor. Bind it to that actor's Transform;
/// each ActorDialogueClip placed on the track opens a speech bubble above that actor when the
/// playhead reaches it, pausing the Timeline until the player advances past the line.
/// </summary>
[TrackClipType(typeof(ActorDialogueClip))]
[TrackBindingType(typeof(Transform))]
public sealed class ActorDialogueTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        ScriptPlayable<ActorDialogueMixerBehaviour> mixer = ScriptPlayable<ActorDialogueMixerBehaviour>.Create(graph, inputCount);
        // The graph (and this mixer instance) gets torn down and rebuilt on every
        // Pause()/Play() cycle -- see ActorDialogueMixerBehaviour's remarks -- so it can't keep
        // its own "already shown" bookkeeping. Handing it this track asset (stable across
        // rebuilds) lets it key that bookkeeping externally instead.
        mixer.GetBehaviour().SourceTrack = this;
        return mixer;
    }
}
