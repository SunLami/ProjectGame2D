using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// Drives this actor's local position directly from a PlayableDirector's own time, piecewise
/// linear between authored keyframes. Used in place of an Animator-driven AnimationClip: Unity's
/// root motion (the only way Timeline can animate a GameObject's own Transform through an
/// Animator) turned out to depend on per-frame timing and drifted/glitched between runs for
/// these 2D sprite actors, so movement here is computed straight from the Timeline's own
/// playhead instead -- deterministic and frame-rate independent, still fully synced to the cut.
///
/// A native Timeline Animation Track driving position directly was tried instead (so the
/// Timeline window's own scrub/preview would move the actor without needing Play mode at all)
/// and reverted: ActorDialogueMixerBehaviour pauses the PlayableDirector to hold on each line,
/// which tears down and rebuilds the whole PlayableGraph (see that class's remarks) -- and the
/// freshly rebuilt graph's Animation Track output doesn't reliably re-push the actor's current
/// pose to its Transform, leaving it stuck at the clip's very first keyframe through an entire
/// dialogue pause even after forcing an extra Evaluate() from a safe, non-reentrant call site.
/// [ExecuteAlways] instead makes Update() ALSO run in Edit mode, so scrubbing the Timeline
/// window's playhead (which does call PlayableDirector.Evaluate() under the hood, same as Play
/// mode) repositions the actor correctly without needing to press Play at all.
/// </summary>
[ExecuteAlways]
[DefaultExecutionOrder(-750)]
public sealed class TimelineActorMotion : MonoBehaviour
{
    [System.Serializable]
    public struct PositionKey
    {
        public float time;
        public Vector2 localPosition;
    }

    [SerializeField] private PlayableDirector _director;
    [SerializeField] private PositionKey[] _keyframes;

    private void Update()
    {
        if (_director == null || _keyframes == null || _keyframes.Length == 0)
            return;

        if (_director.state != PlayState.Playing && _director.state != PlayState.Paused)
            return;

        Vector2 pos = Evaluate((float)_director.time);
        Vector3 local = transform.localPosition;
        local.x = pos.x;
        local.y = pos.y;
        transform.localPosition = local;
    }

    private Vector2 Evaluate(float time)
    {
        if (time <= _keyframes[0].time)
            return _keyframes[0].localPosition;

        for (int i = 0; i < _keyframes.Length - 1; i++)
        {
            PositionKey a = _keyframes[i];
            PositionKey b = _keyframes[i + 1];
            if (time <= b.time)
            {
                float span = b.time - a.time;
                float f = span > 0f ? Mathf.Clamp01((time - a.time) / span) : 1f;
                return Vector2.Lerp(a.localPosition, b.localPosition, f);
            }
        }

        return _keyframes[_keyframes.Length - 1].localPosition;
    }
}
