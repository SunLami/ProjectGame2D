using System;
using UnityEngine;

[Serializable]
public sealed class DialogueChoiceDefinition
{
    [SerializeField] private string _text;
    [SerializeField] private string _nextNodeId;

    public string Text => _text;
    public string NextNodeId => _nextNodeId;
}
