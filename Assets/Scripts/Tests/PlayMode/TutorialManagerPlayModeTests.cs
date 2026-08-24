using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class TutorialManagerPlayModeTests
{
    private static TutorialStepDefinition MakeStep(string stepId, TutorialStepType type, string targetAreaId = null)
    {
        var step = ScriptableObject.CreateInstance<TutorialStepDefinition>();
        SetPrivate(step, "_stepId", stepId);
        SetPrivate(step, "_type", type);
        SetPrivate(step, "_targetAreaId", targetAreaId);
        return step;
    }

    private static TutorialDefinition MakeDefinition(params TutorialStepDefinition[] steps)
    {
        var definition = ScriptableObject.CreateInstance<TutorialDefinition>();
        SetPrivate(definition, "_tutorialId", "tutorial.test");
        SetPrivate(definition, "_steps", steps);
        return definition;
    }

    private static void SetPrivate(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

    private static TutorialManager CreateManager(TutorialDefinition definition, out GameObject root)
    {
        root = new GameObject("TutorialManagerFixture");
        TutorialManager manager = root.AddComponent<TutorialManager>();
        SetPrivate(manager, "_tutorialDefinition", definition);
        // RestoreState() is the only supported way to (re)enter a defined start step; Awake leaves
        // _currentIndex at its default (0), which already points at steps[0] here.
        return manager;
    }

    [Test]
    public void AdvancesOnMatchingEvent_IgnoresNonMatching()
    {
        TutorialStepDefinition moveStep = MakeStep("step.move", TutorialStepType.Move);
        TutorialStepDefinition sprintStep = MakeStep("step.sprint", TutorialStepType.Sprint);
        TutorialDefinition definition = MakeDefinition(moveStep, sprintStep);
        TutorialManager manager = CreateManager(definition, out GameObject root);
        try
        {
            manager.RestoreState(null, false);
            Assert.AreEqual(moveStep, manager.CurrentStep);

            InvokeAttacked(); // wrong event for current step
            Assert.AreEqual(moveStep, manager.CurrentStep, "Non-matching event must not advance the step.");

            InvokeMoved();
            Assert.AreEqual(sprintStep, manager.CurrentStep);
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(moveStep);
            Object.DestroyImmediate(sprintStep);
            Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void CompletesAfterLastStep_FiresCompletedExactlyOnce()
    {
        TutorialStepDefinition onlyStep = MakeStep("step.move", TutorialStepType.Move);
        TutorialDefinition definition = MakeDefinition(onlyStep);
        TutorialManager manager = CreateManager(definition, out GameObject root);
        int completedCount = 0;
        manager.OnTutorialCompleted += () => completedCount++;
        try
        {
            manager.RestoreState(null, false);
            InvokeMoved();

            Assert.IsTrue(manager.IsCompleted);
            Assert.IsNull(manager.CurrentStep);
            Assert.AreEqual(1, completedCount);

            InvokeMoved(); // already completed -- must not fire again or throw
            Assert.AreEqual(1, completedCount);
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(onlyStep);
            Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void Skip_MarksCompletedWithoutAdvancingThroughIntermediateSteps()
    {
        TutorialStepDefinition moveStep = MakeStep("step.move", TutorialStepType.Move);
        TutorialStepDefinition sprintStep = MakeStep("step.sprint", TutorialStepType.Sprint);
        TutorialDefinition definition = MakeDefinition(moveStep, sprintStep);
        TutorialManager manager = CreateManager(definition, out GameObject root);
        int stepChangedCount = 0;
        manager.OnStepChanged += _ => stepChangedCount++;
        try
        {
            manager.RestoreState(null, false);
            manager.Skip();

            Assert.IsTrue(manager.IsCompleted);
            Assert.IsNull(manager.CurrentStep);
            Assert.AreEqual(0, stepChangedCount, "Skip must not walk through intermediate steps.");
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(moveStep);
            Object.DestroyImmediate(sprintStep);
            Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void RestoreState_SetsStepWithoutFiringEvents()
    {
        TutorialStepDefinition moveStep = MakeStep("step.move", TutorialStepType.Move);
        TutorialStepDefinition sprintStep = MakeStep("step.sprint", TutorialStepType.Sprint);
        TutorialDefinition definition = MakeDefinition(moveStep, sprintStep);
        TutorialManager manager = CreateManager(definition, out GameObject root);
        int stepChangedCount = 0;
        int completedCount = 0;
        manager.OnStepChanged += _ => stepChangedCount++;
        manager.OnTutorialCompleted += () => completedCount++;
        try
        {
            manager.RestoreState("step.sprint", false);

            Assert.AreEqual(sprintStep, manager.CurrentStep);
            Assert.AreEqual(0, stepChangedCount);
            Assert.AreEqual(0, completedCount);

            manager.RestoreState(null, true);
            Assert.IsTrue(manager.IsCompleted);
            Assert.AreEqual(0, completedCount, "Restoring a completed save must not fire OnTutorialCompleted.");
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(moveStep);
            Object.DestroyImmediate(sprintStep);
            Object.DestroyImmediate(definition);
        }
    }

    [Test]
    public void ReachArea_CompletesOnlyOnMatchingAreaId()
    {
        TutorialStepDefinition townStep = MakeStep("step.town", TutorialStepType.ReachArea, "area.town");
        TutorialDefinition definition = MakeDefinition(townStep);
        TutorialManager manager = CreateManager(definition, out GameObject root);
        try
        {
            manager.RestoreState(null, false);

            AreaTriggerZone.RaiseEnteredForTests("area.tutorial");
            Assert.AreEqual(townStep, manager.CurrentStep, "Wrong area id must not complete the step.");

            AreaTriggerZone.RaiseEnteredForTests("area.town");
            Assert.IsTrue(manager.IsCompleted);
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(townStep);
            Object.DestroyImmediate(definition);
        }
    }

    [UnityTest]
    public System.Collections.IEnumerator DisabledManager_DoesNotReactToEvents()
    {
        TutorialStepDefinition moveStep = MakeStep("step.move", TutorialStepType.Move);
        TutorialDefinition definition = MakeDefinition(moveStep);
        TutorialManager manager = CreateManager(definition, out GameObject root);
        try
        {
            manager.RestoreState(null, false);
            root.SetActive(false);
            yield return null;

            InvokeMoved();
            Assert.AreEqual(moveStep, manager.CurrentStep, "A disabled manager must not react to domain events.");
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(moveStep);
            Object.DestroyImmediate(definition);
        }
    }

    private static void InvokeMoved() => Player.RaiseMovedForTests();
    private static void InvokeAttacked() => Player.RaiseAttackedForTests();
}
