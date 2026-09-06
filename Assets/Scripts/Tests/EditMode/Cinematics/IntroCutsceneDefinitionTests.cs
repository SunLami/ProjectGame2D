using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class IntroCutsceneDefinitionTests
{
    private IntroCutsceneDefinition _definition;

    [SetUp]
    public void SetUp() => _definition = ScriptableObject.CreateInstance<IntroCutsceneDefinition>();

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(_definition);

    [Test]
    public void DuplicateSegmentId_IsReported()
    {
        Configure("cutscene.test", "scene.same", "scene.same");

        Assert.That(_definition.ValidateDefinition(), Has.Some.Contains("Duplicate"));
    }

    [Test]
    public void MissingCutsceneId_IsReported()
    {
        Configure(string.Empty, "scene.one");

        Assert.That(_definition.ValidateDefinition(), Has.Some.Contains("Cutscene ID"));
    }

    [Test]
    public void UnknownSegmentIndex_DoesNotResolve()
    {
        Configure("cutscene.test", "scene.one");

        Assert.That(_definition.TryGetSegment(4, out _), Is.False);
    }

    private void Configure(string cutsceneId, params string[] segmentIds)
    {
        SerializedObject serialized = new(_definition);
        serialized.FindProperty("_cutsceneId").stringValue = cutsceneId;
        SerializedProperty segments = serialized.FindProperty("_segments");
        segments.arraySize = segmentIds.Length;
        for (int index = 0; index < segmentIds.Length; index++)
        {
            SerializedProperty segment = segments.GetArrayElementAtIndex(index);
            segment.FindPropertyRelative("_segmentId").stringValue = segmentIds[index];
            segment.FindPropertyRelative("_displayName").stringValue = "Test";
            segment.FindPropertyRelative("_lines").arraySize = 0;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
