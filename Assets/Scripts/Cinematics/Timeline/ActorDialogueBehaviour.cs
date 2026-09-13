using UnityEngine.Playables;

/// <summary>Per-clip data carrier for ActorDialogueClip. Holds no behaviour of its own --
/// the track's mixer reads SpeakerName/Text off this when the clip becomes active.</summary>
public sealed class ActorDialogueBehaviour : PlayableBehaviour
{
    public string SpeakerName;
    public string Text;
    public bool FlipHorizontal;
}
