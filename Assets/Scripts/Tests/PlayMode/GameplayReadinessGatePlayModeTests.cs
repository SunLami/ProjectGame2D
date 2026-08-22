using System;
using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class GameplayReadinessGatePlayModeTests
{
    private sealed class FakeReadinessSource : IGameplayReadinessSource
    {
        public string SourceId => "Fake";
        public bool IsReady { get; private set; }
        public event Action ReadyChanged;

        public void SetReady(bool ready)
        {
            if (IsReady == ready)
                return;

            IsReady = ready;
            ReadyChanged?.Invoke();
        }
    }

    [TearDown]
    public void TearDown()
    {
        if (GameSessionManager.Instance != null)
            GameSessionManager.Instance.ClearSession();

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.ResetToMainMenu();
    }

    private static GameplayReadinessGate CreateGate(
        out GameObject gateObject, IGameplayReadinessSource[] sources, float timeoutSeconds)
    {
        gateObject = new GameObject(nameof(GameplayReadinessGate));
        GameplayReadinessGate gate = gateObject.AddComponent<GameplayReadinessGate>();
        gate.ConfigureForTests(sources, timeoutSeconds);
        return gate;
    }

    [UnityTest]
    public IEnumerator NotReadySource_DoesNotTransitionToPlaying()
    {
        GameStateManager.Instance.ReplaceState(GameState.Loading);
        Assert.IsTrue(GameSessionManager.Instance.TryStartDevelopment("PlayModeTestScene"));

        FakeReadinessSource source = new();
        CreateGate(out GameObject gateObject, new IGameplayReadinessSource[] { source }, timeoutSeconds: 5f);

        yield return null;
        yield return null;

        Assert.AreEqual(GameState.Loading, GameStateManager.Instance.CurrentState);

        UnityEngine.Object.Destroy(gateObject);
    }

    [UnityTest]
    public IEnumerator SourceBecomesReady_CompletesRestoreExactlyOnce()
    {
        GameStateManager.Instance.ReplaceState(GameState.Loading);
        Assert.IsTrue(GameSessionManager.Instance.TryStartDevelopment("PlayModeTestScene"));

        FakeReadinessSource source = new();
        CreateGate(out GameObject gateObject, new IGameplayReadinessSource[] { source }, timeoutSeconds: 5f);

        yield return null;
        Assert.AreEqual(GameState.Loading, GameStateManager.Instance.CurrentState);

        int playingTransitions = 0;
        void Handler(GameStateChange change)
        {
            if (change.Current.State == GameState.Playing)
                playingTransitions++;
        }

        GameStateManager.Instance.StateChanged += Handler;

        source.SetReady(true);
        yield return null;

        Assert.AreEqual(GameState.Playing, GameStateManager.Instance.CurrentState);
        Assert.AreEqual(1, playingTransitions, "Gate must complete the restore exactly once.");

        GameStateManager.Instance.StateChanged -= Handler;
        UnityEngine.Object.Destroy(gateObject);
    }

    [UnityTest]
    public IEnumerator DirectPlay_AlreadyPlaying_GateTakesNoAction()
    {
        GameStateManager.Instance.ResetToPlaying();

        FakeReadinessSource source = new();
        CreateGate(out GameObject gateObject, new IGameplayReadinessSource[] { source }, timeoutSeconds: 5f);

        yield return null;
        yield return null;

        Assert.AreEqual(GameState.Playing, GameStateManager.Instance.CurrentState);

        UnityEngine.Object.Destroy(gateObject);
    }

    [UnityTest]
    public IEnumerator TimeoutWithoutReadySource_FailsRestoreAndReturnsToMainMenu()
    {
        GameStateManager.Instance.ReplaceState(GameState.Loading);
        Assert.IsTrue(GameSessionManager.Instance.TryStartDevelopment("PlayModeTestScene"));

        string failureMessage = null;
        void HandleTransitionFailed(string message) => failureMessage = message;
        SceneFlowService.Instance.TransitionFailed += HandleTransitionFailed;

        LogAssert.Expect(LogType.Error, new Regex("Gameplay restore failed: Timed out.*Fake"));

        FakeReadinessSource source = new();
        CreateGate(out GameObject gateObject, new IGameplayReadinessSource[] { source }, timeoutSeconds: 0.2f);

        float waited = 0f;
        while (SceneManager.GetActiveScene().name != SceneFlowService.MainMenuSceneName && waited < 10f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        SceneFlowService.Instance.TransitionFailed -= HandleTransitionFailed;

        Assert.AreEqual(SceneFlowService.MainMenuSceneName, SceneManager.GetActiveScene().name,
            "Gate timeout must fall back to MainMenu instead of leaving the game stuck in Loading.");
        Assert.AreEqual(GameState.MainMenu, GameStateManager.Instance.CurrentState);
        Assert.IsFalse(GameSessionManager.Instance.HasActiveSession);
        Assert.IsNotNull(failureMessage);
        StringAssert.Contains("Fake", failureMessage);

        if (gateObject != null)
            UnityEngine.Object.Destroy(gateObject);
    }
}
