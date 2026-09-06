using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>One authored segment marker in the intro Timeline.</summary>
public sealed class IntroCutsceneCueClip : PlayableAsset, ITimelineClipAsset
{
    [SerializeField] private int _segmentIndex;

    public int SegmentIndex
    {
        get => _segmentIndex;
        set => _segmentIndex = value;
    }

    public ClipCaps clipCaps => ClipCaps.None;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        ScriptPlayable<IntroCutsceneCueBehaviour> playable = ScriptPlayable<IntroCutsceneCueBehaviour>.Create(graph);
        playable.GetBehaviour().SegmentIndex = _segmentIndex;
        return playable;
    }
}
