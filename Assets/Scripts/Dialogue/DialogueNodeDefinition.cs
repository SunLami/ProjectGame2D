using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class DialogueNodeDefinition
{
    [SerializeField] private string _nodeId;
    [SerializeField] private string _speakerName;
    [SerializeField] private Sprite _portrait;
    [TextArea(2, 6)]
    [SerializeField] private string _text;
    [SerializeField] private string _nextNodeId;
    [SerializeField] private string _outcomeId;
    [SerializeField] private List<DialogueChoiceDefinition> _choices = new();

    public string NodeId => _nodeId;
    public string SpeakerName => _speakerName;
    public Sprite Portrait => _portrait;
    public string Text => _text;
    public string NextNodeId => _nextNodeId;
    public string OutcomeId => _outcomeId;
    public IReadOnlyList<DialogueChoiceDefinition> Choices => _choices;
}
