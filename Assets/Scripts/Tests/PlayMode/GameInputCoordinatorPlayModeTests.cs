using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

public sealed class GameInputCoordinatorPlayModeTests
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

    private GameObject _fixture;
    private InputActionAsset _projectActions;
    private Keyboard _keyboard;

    // MainMenu.unity and DemoScene.unity each own a real GameInputCoordinator wired to the real
    // project InputActionAsset. Other PlayMode test classes (GameplaySessionControllerPlayModeTests)
    // legitimately load those scenes for real and correctly leave the run sitting in MainMenu when
    // they finish -- there is no "neutral" scene to return to, and domain reload is disabled for
    // this project (ProjectSettings/EditorSettings.asset), so nothing tears that coordinator down
    // before this fixture runs later in the same full-suite Play session. Its <Keyboard>/escape
    // binding matches ANY keyboard device, including the one this fixture creates, so a real
    // Escape keypress would double-fire HandleCancelPerformed on both coordinators and immediately
    // flip Paused back to Playing (Pause() from the first, ReturnToPreviousState() from the second)
    // -- indistinguishable from a genuine double-subscribe bug from the test's point of view, but
    // it is cross-test-class scene leakage, not a defect in GameInputCoordinator itself (the
    // physical Player build acceptance already proved single-coordinator Escape works end to end).
    // Isolate this fixture from that leakage rather than weaken what it asserts.
    private readonly List<GameObject> _suppressedCoordinators = new();

    // Simulates MainMenu.unity/DemoScene.unity's own real GameInputCoordinator surviving for the
    // rest of a full-suite Play session after another test class's real scene transition -- created
    // once and kept alive/enabled across every test in this fixture (only torn down in
    // OneTimeTearDown), exactly like the real leftover would be. If SetUp's suppression logic is
    // ever removed or broken, every test below regresses to the double-fire flip-back instead of
    // needing to depend on incidental ordering against other test classes to catch it.
    private GameObject _leakedSceneCoordinator;
    private InputActionAsset _leakedSceneActions;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _leakedSceneActions = ScriptableObject.CreateInstance<InputActionAsset>();
        InputActionMap ui = _leakedSceneActions.AddActionMap("UI");
        ui.AddAction("Cancel", InputActionType.Button, "<Keyboard>/escape");
        _leakedSceneActions.AddActionMap("Gameplay");

        _leakedSceneCoordinator = new GameObject("LeakedSceneGameInputCoordinator");
        _leakedSceneCoordinator.SetActive(false);
        GameInputCoordinator coordinator = _leakedSceneCoordinator.AddComponent<GameInputCoordinator>();
        typeof(GameInputCoordinator).GetField("_projectActions", PrivateInstance)
            .SetValue(coordinator, _leakedSceneActions);
        _leakedSceneCoordinator.SetActive(true);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        if (_leakedSceneCoordinator != null)
            Object.DestroyImmediate(_leakedSceneCoordinator);
        if (_leakedSceneActions != null)
            Object.DestroyImmediate(_leakedSceneActions);
    }

    [SetUp]
    public void SetUp()
    {
        GameStateManager.Instance.ResetToPlaying();
        _keyboard = InputSystem.AddDevice<Keyboard>();
        SuppressForeignCoordinators();

        _projectActions = ScriptableObject.CreateInstance<InputActionAsset>();
        InputActionMap ui = _projectActions.AddActionMap("UI");
        ui.AddAction("Cancel", InputActionType.Button, "<Keyboard>/escape");
        _projectActions.AddActionMap("Gameplay");

        _fixture = new GameObject("GameInputCoordinatorFixture");
        _fixture.SetActive(false);
        GameInputCoordinator coordinator = _fixture.AddComponent<GameInputCoordinator>();
        typeof(GameInputCoordinator).GetField("_projectActions", PrivateInstance)
            .SetValue(coordinator, _projectActions);
        _fixture.SetActive(true);
    }

    [TearDown]
    public void TearDown()
    {
        if (_fixture != null)
            Object.DestroyImmediate(_fixture);
        if (_projectActions != null)
            Object.DestroyImmediate(_projectActions);
        if (_keyboard != null && _keyboard.added)
            InputSystem.RemoveDevice(_keyboard);
        GameStateManager.Instance.ResetToPlaying();

        foreach (GameObject leftover in _suppressedCoordinators)
        {
            if (leftover != null)
                leftover.SetActive(true);
        }
        _suppressedCoordinators.Clear();
    }

    /// <summary>Disables every enabled GameInputCoordinator that isn't this fixture's own. Called
    /// from SetUp, and again immediately before every simulated Escape press -- SetUp alone leaves a
    /// window between it and the actual key-press simulation in which another test's real scene
    /// transition (e.g. GameplaySessionControllerPlayModeTests' async scene-load UnityTests running
    /// concurrently-scheduled coroutines) could activate a fresh coordinator that SetUp never saw.
    /// Idempotent and cheap -- finding zero foreign coordinators is the common case.</summary>
    private void SuppressForeignCoordinators()
    {
        foreach (GameInputCoordinator candidate in Object.FindObjectsByType<GameInputCoordinator>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            GameObject go = candidate.gameObject;
            if (go == _fixture || _suppressedCoordinators.Contains(go))
                continue;

            go.SetActive(false);
            _suppressedCoordinators.Add(go);
        }
    }

    /// <summary>Diagnostic dump of every GameInputCoordinator (active or not) for an assertion
    /// failure message -- if this ever flakes again, the failure itself names exactly which
    /// GameObject/scene fired a second time instead of requiring another investigation pass.</summary>
    private static string DescribeAllCoordinators()
    {
        var sb = new System.Text.StringBuilder("Live GameInputCoordinator instances:");
        foreach (GameInputCoordinator c in Object.FindObjectsByType<GameInputCoordinator>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            GameObject go = c.gameObject;
            sb.Append("\n- ").Append(go.name)
                .Append(" scene=").Append(go.scene.name)
                .Append(" activeInHierarchy=").Append(go.activeInHierarchy)
                .Append(" enabled=").Append(c.enabled);
        }
        return sb.ToString();
    }

    [UnityTest]
    public IEnumerator SharedProjectActionDisabledAfterEnable_CancelStillPauses()
    {
        GameInputCoordinator coordinator = _fixture.GetComponent<GameInputCoordinator>();
        InputActionAsset runtimeActions = (InputActionAsset)typeof(GameInputCoordinator)
            .GetField("_runtimeActions", PrivateInstance).GetValue(coordinator);

        Assert.AreNotSame(_projectActions, runtimeActions);
        Assert.IsTrue(runtimeActions.FindAction("UI/Cancel").enabled);

        // Reproduces the outgoing MainMenu InputSystemUIInputModule disabling its shared asset
        // after the incoming DemoScene coordinator has already enabled Cancel.
        _projectActions.Enable();
        _projectActions.Disable();

        SuppressForeignCoordinators();
        _keyboard.MakeCurrent();
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Escape));
        InputSystem.Update();
        yield return null;

        Assert.AreEqual(GameState.Paused, GameStateManager.Instance.CurrentState, DescribeAllCoordinators());
    }

    [UnityTest]
    public IEnumerator DisableEnable_DoesNotDoubleSubscribe()
    {
        _fixture.SetActive(false);
        _fixture.SetActive(true);

        SuppressForeignCoordinators();
        _keyboard.MakeCurrent();
        InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Escape));
        InputSystem.Update();
        yield return null;

        Assert.AreEqual(GameState.Paused, GameStateManager.Instance.CurrentState, DescribeAllCoordinators());
        Assert.IsTrue(GameStateManager.Instance.CanReturn,
            "One Cancel press must leave exactly the normal Pause return path available.");
    }

    [Test]
    public void SetUp_SuppressesLeftoverSceneCoordinator_TearDownRestoresIt()
    {
        // Directly asserts the isolation mechanism itself, rather than only relying on the two
        // tests above incidentally failing if it regresses.
        Assert.IsFalse(_leakedSceneCoordinator.activeSelf,
            "SetUp must disable any GameInputCoordinator that already existed (e.g. a leftover " +
            "real scene's), or it will double-fire alongside this fixture's own coordinator.");
        Assert.Contains(_leakedSceneCoordinator, _suppressedCoordinators);

        TearDown();

        Assert.IsTrue(_leakedSceneCoordinator.activeSelf,
            "TearDown must restore whatever it suppressed -- this test class must not leave a " +
            "real scene's coordinator permanently disabled for tests that run after it.");

        // Re-arm SetUp so the framework's own TearDown (which will run next) has a valid fixture.
        SetUp();
    }
}
