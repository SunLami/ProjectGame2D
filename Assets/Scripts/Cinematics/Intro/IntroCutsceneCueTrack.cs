using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[TrackColor(0.94f, 0.68f, 0.18f)]
[TrackClipType(typeof(IntroCutsceneCueClip))]
[TrackBindingType(typeof(IntroCutsceneController))]
public sealed class IntroCutsceneCueTrack : TrackAsset
{
    public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
    {
        return ScriptPlayable<IntroCutsceneCueMixerBehaviour>.Create(graph, inputCount);
    }
}

public sealed class IntroCutsceneCueBehaviour : PlayableBehaviour
{
    public int SegmentIndex { get; set; }
}

public sealed class IntroCutsceneCueMixerBehaviour : PlayableBehaviour
{
    private int _lastSegmentIndex = -1;

    public override void ProcessFrame(Playable playable, FrameData info, object playerData)
    {
        if (playerData is not IntroCutsceneController controller)
            return;

        for (int input = 0; input < playable.GetInputCount(); input++)
        {
            if (playable.GetInputWeight(input) <= 0f)
                continue;

            ScriptPlayable<IntroCutsceneCueBehaviour> cue = (ScriptPlayable<IntroCutsceneCueBehaviour>)playable.GetInput(input);
            int segmentIndex = cue.GetBehaviour().SegmentIndex;
            if (segmentIndex != _lastSegmentIndex)
            {
                _lastSegmentIndex = segmentIndex;
                controller.BeginSegmentFromTimeline(segmentIndex);
            }
            return;
        }
    }
}
