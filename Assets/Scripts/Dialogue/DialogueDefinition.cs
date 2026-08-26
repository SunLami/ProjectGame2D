using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Dialogue", menuName = "Project Game 2D/Dialogue/Dialogue Definition")]
public sealed class DialogueDefinition : ScriptableObject
{
    [SerializeField] private string _dialogueId;
    [SerializeField] private string _initialNodeId;
    [SerializeField] private List<DialogueNodeDefinition> _nodes = new();

    public string DialogueId => _dialogueId;
    public string InitialNodeId => _initialNodeId;
    public IReadOnlyList<DialogueNodeDefinition> Nodes => _nodes;

    public bool TryGetNode(string nodeId, out DialogueNodeDefinition node)
    {
        node = null;
        if (string.IsNullOrWhiteSpace(nodeId))
            return false;

        foreach (DialogueNodeDefinition candidate in _nodes)
        {
            if (candidate != null && candidate.NodeId == nodeId)
            {
                node = candidate;
                return true;
            }
        }
        return false;
    }

    public IReadOnlyList<string> ValidateDefinition()
    {
        var issues = new List<string>();
        if (string.IsNullOrWhiteSpace(_dialogueId))
            issues.Add("Dialogue ID is required.");
        if (_nodes == null || _nodes.Count == 0)
        {
            issues.Add("At least one dialogue node is required.");
            return issues;
        }

        var ids = new HashSet<string>();
        foreach (DialogueNodeDefinition node in _nodes)
        {
            if (node == null || string.IsNullOrWhiteSpace(node.NodeId))
            {
                issues.Add("Every dialogue node requires a node ID.");
                continue;
            }
            if (!ids.Add(node.NodeId))
                issues.Add($"Duplicate dialogue node ID: {node.NodeId}");
        }

        if (!ids.Contains(_initialNodeId))
            issues.Add($"Initial node does not exist: {_initialNodeId}");

        foreach (DialogueNodeDefinition node in _nodes)
        {
            if (node == null)
                continue;
            if (!string.IsNullOrEmpty(node.NextNodeId) && !ids.Contains(node.NextNodeId))
                issues.Add($"Node {node.NodeId} points to missing node {node.NextNodeId}.");
            foreach (DialogueChoiceDefinition choice in node.Choices)
            {
                if (choice == null || string.IsNullOrWhiteSpace(choice.Text))
                    issues.Add($"Node {node.NodeId} contains an empty choice.");
                else if (!ids.Contains(choice.NextNodeId))
                    issues.Add($"Choice '{choice.Text}' points to missing node {choice.NextNodeId}.");
            }
        }
        return issues;
    }
}
