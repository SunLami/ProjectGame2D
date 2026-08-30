using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class DialogueDefinitionTests
{
    private DialogueDefinition _definition;

    [SetUp]
    public void SetUp() => _definition = ScriptableObject.CreateInstance<DialogueDefinition>();

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(_definition);

    [Test]
    public void ValidLinearConversation_ResolvesInitialAndNextNode()
    {
        Configure("dialogue.test", "start", ("start", "end"), ("end", ""));

        Assert.That(_definition.ValidateDefinition(), Is.Empty);
        Assert.That(_definition.TryGetNode("start", out DialogueNodeDefinition start), Is.True);
        Assert.That(start.NextNodeId, Is.EqualTo("end"));
        Assert.That(_definition.TryGetNode(start.NextNodeId, out DialogueNodeDefinition end), Is.True);
        Assert.That(end.NodeId, Is.EqualTo("end"));
    }

    [Test]
    public void MissingTarget_IsReportedWithoutThrowing()
    {
        Configure("dialogue.test", "start", ("start", "missing"));

        Assert.That(_definition.ValidateDefinition(), Has.Some.Contains("missing"));
        Assert.That(_definition.TryGetNode("missing", out _), Is.False);
    }

    [Test]
    public void DuplicateNodeId_IsReported()
    {
        Configure("dialogue.test", "same", ("same", ""), ("same", ""));

        Assert.That(_definition.ValidateDefinition(), Has.Some.Contains("Duplicate"));
    }

    private void Configure(string dialogueId, string initialNodeId, params (string Id, string Next)[] nodeSpecs)
    {
        SerializedObject data = new(_definition);
        data.FindProperty("_dialogueId").stringValue = dialogueId;
        data.FindProperty("_initialNodeId").stringValue = initialNodeId;
        SerializedProperty nodes = data.FindProperty("_nodes");
        nodes.arraySize = nodeSpecs.Length;
        for (int i = 0; i < nodeSpecs.Length; i++)
        {
            SerializedProperty node = nodes.GetArrayElementAtIndex(i);
            node.FindPropertyRelative("_nodeId").stringValue = nodeSpecs[i].Id;
            node.FindPropertyRelative("_speakerName").stringValue = "Speaker";
            node.FindPropertyRelative("_text").stringValue = "Text";
            node.FindPropertyRelative("_nextNodeId").stringValue = nodeSpecs[i].Next;
            node.FindPropertyRelative("_outcomeId").stringValue = string.Empty;
            node.FindPropertyRelative("_choices").arraySize = 0;
        }
        data.ApplyModifiedPropertiesWithoutUndo();
    }
}
