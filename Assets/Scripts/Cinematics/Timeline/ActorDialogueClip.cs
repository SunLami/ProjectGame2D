using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// One authored line of dialogue on an ActorDialogueTrack. Appears as a resizable clip in the
/// Timeline window; its start time is when the line's speech bubble opens above the bound actor.
/// </summary>
public sealed class ActorDialogueClip : PlayableAsset, ITimelineClipAsset
{
    [SerializeField] private string _speakerName;
    [TextArea(2, 4)]
    [SerializeField] private string _text;
    [Tooltip("Mirror the bubble art horizontally. The dialogbox_2 sprite's tail is authored "
        + "pointing down-left by default; flip this on for lines spoken by whichever actor is "
        + "standing to the right of the other, so the tail still reads naturally toward them "
        + "instead of jutting off to the wrong side.")]
    [SerializeField] private bool _flipHorizontal;

    public string SpeakerName => _speakerName;
    public string Text => _text;
    public bool FlipHorizontal => _flipHorizontal;

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        ScriptPlayable<ActorDialogueBehaviour> playable = ScriptPlayable<ActorDialogueBehaviour>.Create(graph);
        ActorDialogueBehaviour behaviour = playable.GetBehaviour();
        behaviour.SpeakerName = _speakerName;
        behaviour.Text = _text;
        behaviour.FlipHorizontal = _flipHorizontal;
        return playable;
    }
}
