using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(menuName = "Project Game 2D/Cinematics/Intro Cutscene Definition", fileName = "IntroCutsceneDefinition")]
public sealed class IntroCutsceneDefinition : ScriptableObject
{
    [SerializeField] private string _cutsceneId = "cutscene.intro.orynthals";
    [SerializeField] private List<IntroCutsceneSegment> _segments = new();

    public string CutsceneId => _cutsceneId;
    public IReadOnlyList<IntroCutsceneSegment> Segments => _segments;

    public bool TryGetSegment(int index, out IntroCutsceneSegment segment)
    {
        if (index >= 0 && index < _segments.Count && _segments[index] != null)
        {
            segment = _segments[index];
            return true;
        }

        segment = null;
        return false;
    }

    public IReadOnlyList<string> ValidateDefinition()
    {
        List<string> errors = new();
        if (string.IsNullOrWhiteSpace(_cutsceneId))
            errors.Add("Cutscene ID is required.");

        if (_segments.Count == 0)
            errors.Add("At least one intro segment is required.");

        HashSet<string> seenIds = new(StringComparer.Ordinal);
        for (int index = 0; index < _segments.Count; index++)
        {
            IntroCutsceneSegment segment = _segments[index];
            if (segment == null)
            {
                errors.Add($"Segment {index} is missing.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(segment.SegmentId))
                errors.Add($"Segment {index} requires a stable segment ID.");
            else if (!seenIds.Add(segment.SegmentId))
                errors.Add($"Duplicate segment ID: {segment.SegmentId}.");

            if (segment.Video == null)
                errors.Add($"Segment '{segment.SegmentId}' requires a video clip.");
        }

        return errors;
    }
}

[Serializable]
public sealed class IntroCutsceneSegment
{
    [SerializeField] private string _segmentId;
    [SerializeField] private string _displayName;
    [SerializeField] private VideoClip _video;
    [SerializeField] private List<IntroCutsceneLine> _lines = new();

    public string SegmentId => _segmentId;
    public string DisplayName => _displayName;
    public VideoClip Video => _video;
    public IReadOnlyList<IntroCutsceneLine> Lines => _lines;
    public bool RequiresPlayerAdvance => _lines.Count > 0;
}

[Serializable]
public sealed class IntroCutsceneLine
{
    [SerializeField] private string _speakerName;
    [TextArea(3, 6)] [SerializeField] private string _text;

    public string SpeakerName => _speakerName;
    public string Text => _text;
}
