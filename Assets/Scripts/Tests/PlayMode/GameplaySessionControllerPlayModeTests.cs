using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class GameplaySessionControllerPlayModeTests
{
    private ISaveSlotRepository _originalRepository;
    private FakeSaveSlotRepository _fakeRepository;

    [SetUp]
    public void SetUp()
    {
        _originalRepository = GameSessionManager.Instance.SaveRepository;
        _fakeRepository = new FakeSaveSlotRepository();
        GameSessionManager.Instance.SetSaveRepositoryForTests(_fakeRepository);
        GameSessionManager.Instance.ClearSession();
        GameStateManager.Instance.ResetToMainMenu();
    }

    [TearDown]
    public void TearDown()
    {
        GameSessionManager.Instance.SetSaveRepositoryForTests(_originalRepository);
        GameSessionManager.Instance.ClearSession();
        GameStateManager.Instance.ResetToMainMenu();
    }

    private sealed class FakeSaveSlotRepository : ISaveSlotRepository
    {
        private readonly InMemorySaveSlotRepository _inner = new();
        public bool FailNextWrite;

        public SaveSlotInfo GetSlotInfo(int slotId) => _inner.GetSlotInfo(slotId);
        public SaveSlotInfo[] GetAllSlotInfo() => _inner.GetAllSlotInfo();
        public bool TryReadSave(int slotId, out GameSaveData data) => _inner.TryReadSave(slotId, out data);

        public SaveOperationResult WriteSave(int slotId, GameSaveData data)
        {
            if (FailNextWrite)
            {
                FailNextWrite = false;
                return SaveOperationResult.Failure("Simulated write failure.");
            }
            return _inner.WriteSave(slotId, data);
        }

        public SaveOperationResult DeleteSlot(int slotId) => _inner.DeleteSlot(slotId);
    }

    private sealed class FakeApplicationQuitter : IApplicationQuitter
    {
        public int QuitCount { get; private set; }
        public void Quit() => QuitCount++;
    }

    private static (GameObject root, PlayerStat stat, Transform playerTransform) BuildPlayerFixture()
    {
        GameObject root = new("PlayerFixture");
        PlayerStat stat = root.AddComponent<PlayerStat>();
        return (root, stat, root.transform);
    }

    private static GameplaySessionController BuildController(
        PlayerStat stat, Transform playerTransform, FakeApplicationQuitter quitter, out GameObject controllerObject)
    {
        controllerObject = new GameObject("GameplaySessionControllerFixture");
        GameplaySessionController controller = controllerObject.AddComponent<GameplaySessionController>();
        controller.ConfigureForTests(stat, playerTransform, worldRegistry: null, quitter: quitter, gameplaySceneName: "DemoScene");
        return controller;
    }

    private static GameSaveData MakeActiveSave(string saveId = "save-1") => new()
    {
        saveId = saveId,
        player = new PlayerSaveData
        {
            level = 1,
            location = new PlayerLocationSaveData { areaId = "area.tutorial", fallbackSpawnId = "spawn.tutorial.start" }
        },
        inventory = new InventorySaveData(),
        equipment = new EquipmentSaveData(),
        tutorial = new TutorialSaveData(),
        quests = new QuestSaveData(),
        world = new WorldSaveData()
    };

    private void BeginPausedSession(int slotId, GameSaveData saveData)
    {
        _fakeRepository.WriteSave(slotId, saveData);
        Assert.IsTrue(GameSessionManager.Instance.TryStartLoadedGame(slotId, "DemoScene", saveData));
        GameStateManager.Instance.ResetToPlaying();
        GameStateManager.Instance.Pause();
    }

    // ---- Save Game ----

    [Test]
    public void RequestSave_Succeeds_RefreshesMetadataAndClearsDirty()
    {
        BeginPausedSession(1, MakeActiveSave());
        GameSessionManager.Instance.MarkDirty();
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        GameplaySessionController controller = BuildController(stat, transform, null, out GameObject controllerObject);

        SaveSlotInfo[] refreshedSlots = null;
        controller.OnSaveSlotListChanged += slots => refreshedSlots = slots;
        int succeededCount = 0;
        controller.OnSaveSucceeded += () => succeededCount++;

        Assert.IsTrue(controller.RequestSave());

        Assert.AreEqual(1, succeededCount);
        Assert.IsFalse(GameSessionManager.Instance.IsDirty);
        Assert.AreEqual(GameState.Paused, GameStateManager.Instance.CurrentState);
        Assert.IsNotNull(refreshedSlots);
        Assert.AreEqual(SaveSlotStatus.Valid, refreshedSlots[0].Status);

        Object.DestroyImmediate(playerRoot);
        Object.Destroy(controllerObject);
    }

    [Test]
    public void RequestSave_NoActiveSession_Rejected()
    {
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        GameplaySessionController controller = BuildController(stat, transform, null, out GameObject controllerObject);

        GameplaySessionOperationResult? failure = null;
        controller.OnOperationFailed += (result, _) => failure = result;

        Assert.IsFalse(controller.RequestSave());
        Assert.AreEqual(GameplaySessionOperationResult.NoActiveSession, failure);

        Object.DestroyImmediate(playerRoot);
        Object.Destroy(controllerObject);
    }

    [Test]
    public void RequestSave_Failure_ReturnsToPreviousStateWithoutStuckTimescale()
    {
        BeginPausedSession(1, MakeActiveSave());
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        GameplaySessionController controller = BuildController(stat, transform, null, out GameObject controllerObject);
        _fakeRepository.FailNextWrite = true;

        GameplaySessionOperationResult? failure = null;
        controller.OnOperationFailed += (result, _) => failure = result;

        Assert.IsFalse(controller.RequestSave());
        Assert.AreEqual(GameplaySessionOperationResult.WriteFailed, failure);
        Assert.AreEqual(GameState.Paused, GameStateManager.Instance.CurrentState, "A failed save must return to the state before Saving.");
        Assert.AreEqual(0f, Time.timeScale, "Returning to Paused must apply Paused's own policy (timeScale 0), not get stuck on Saving's transient state.");

        Object.DestroyImmediate(playerRoot);
        Object.Destroy(controllerObject);
    }

    [Test]
    public void DoubleClickSave_SecondCallRejectedWhileFirstInFlight()
    {
        // Saving completes synchronously in this controller (no async I/O), so simulate a
        // double-click by re-entering the same frame's logical window: push Saving manually first
        // to represent "a save is already in flight" the way IsBusy sees it.
        BeginPausedSession(1, MakeActiveSave());
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        GameplaySessionController controller = BuildController(stat, transform, null, out GameObject controllerObject);

        GameStateManager.Instance.PushState(GameState.Saving);
        Assert.IsTrue(controller.IsBusy);

        GameplaySessionOperationResult? failure = null;
        controller.OnOperationFailed += (result, _) => failure = result;
        Assert.IsFalse(controller.RequestSave());
        Assert.AreEqual(GameplaySessionOperationResult.AlreadyBusy, failure);

        GameStateManager.Instance.ReturnToPreviousState();
        Object.DestroyImmediate(playerRoot);
        Object.Destroy(controllerObject);
    }

    // ---- Load Game (validation paths, no real scene transition needed) ----

    [Test]
    public void RequestLoad_EmptySlot_Rejected()
    {
        BeginPausedSession(1, MakeActiveSave());
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        GameplaySessionController controller = BuildController(stat, transform, null, out GameObject controllerObject);

        GameplaySessionOperationResult? failure = null;
        controller.OnOperationFailed += (result, _) => failure = result;

        Assert.IsFalse(controller.RequestLoad(2)); // slot 2 was never written
        Assert.AreEqual(GameplaySessionOperationResult.SlotNotValid, failure);

        Object.DestroyImmediate(playerRoot);
        Object.Destroy(controllerObject);
    }

    [Test]
    public void CanLoad_OnlyTrueForValidSlots()
    {
        BeginPausedSession(1, MakeActiveSave());
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        GameplaySessionController controller = BuildController(stat, transform, null, out GameObject controllerObject);

        Assert.IsFalse(controller.CanLoad(2));
        Assert.IsTrue(controller.CanLoad(1));

        Object.DestroyImmediate(playerRoot);
        Object.Destroy(controllerObject);
    }

    [Test]
    public void DoubleClickLoad_SecondCallRejectedWhileFirstInFlight()
    {
        BeginPausedSession(1, MakeActiveSave());
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        GameplaySessionController controller = BuildController(stat, transform, null, out GameObject controllerObject);

        GameStateManager.Instance.PushState(GameState.Loading);
        GameplaySessionOperationResult? failure = null;
        controller.OnOperationFailed += (result, _) => failure = result;

        Assert.IsFalse(controller.RequestLoad(1));
        Assert.AreEqual(GameplaySessionOperationResult.AlreadyBusy, failure);

        GameStateManager.Instance.ReturnToPreviousState();
        Object.DestroyImmediate(playerRoot);
        Object.Destroy(controllerObject);
    }

    // ---- Return to Main Menu ----

    [UnityTest]
    public IEnumerator RequestReturnToMainMenu_WhenClean_ReturnsDirectlyWithoutConfirmation()
    {
        BeginPausedSession(1, MakeActiveSave());
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        GameplaySessionController controller = BuildController(stat, transform, null, out GameObject controllerObject);

        int confirmationCount = 0;
        controller.OnConfirmationRequired += _ => confirmationCount++;

        controller.RequestReturnToMainMenu();

        Assert.AreEqual(0, confirmationCount);
        Assert.AreEqual(GameState.Loading, GameStateManager.Instance.CurrentState, "A clean session must start the return transition immediately.");

        yield return WaitForState(GameState.MainMenu, 15f);

        Object.DestroyImmediate(playerRoot);
        Object.Destroy(controllerObject);
    }

    [Test]
    public void RequestReturnToMainMenu_WhenDirty_RequestsConfirmationAndTakesNoAction()
    {
        BeginPausedSession(1, MakeActiveSave());
        GameSessionManager.Instance.MarkDirty();
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        GameplaySessionController controller = BuildController(stat, transform, null, out GameObject controllerObject);

        GameplaySessionConfirmationKind? kind = null;
        controller.OnConfirmationRequired += k => kind = k;

        controller.RequestReturnToMainMenu();

        Assert.AreEqual(GameplaySessionConfirmationKind.ReturnToMainMenu, kind);
        Assert.AreEqual(GameState.Paused, GameStateManager.Instance.CurrentState, "The confirm popup itself must not change GameState.");

        Object.DestroyImmediate(playerRoot);
        Object.Destroy(controllerObject);
    }

    [UnityTest]
    public IEnumerator ConfirmSaveAndReturn_SavesFirstThenReturns()
    {
        BeginPausedSession(1, MakeActiveSave());
        GameSessionManager.Instance.MarkDirty();
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        GameplaySessionController controller = BuildController(stat, transform, null, out GameObject controllerObject);
        int savedCount = 0;
        controller.OnSaveSucceeded += () => savedCount++;

        controller.ConfirmSaveAndReturn();

        Assert.AreEqual(1, savedCount);
        Assert.AreEqual(GameState.Loading, GameStateManager.Instance.CurrentState);

        yield return WaitForState(GameState.MainMenu, 15f);

        Object.DestroyImmediate(playerRoot);
        Object.Destroy(controllerObject);
    }

    [Test]
    public void ConfirmSaveAndReturn_SaveFails_DoesNotReturnToMainMenu()
    {
        BeginPausedSession(1, MakeActiveSave());
        GameSessionManager.Instance.MarkDirty();
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        GameplaySessionController controller = BuildController(stat, transform, null, out GameObject controllerObject);
        _fakeRepository.FailNextWrite = true;

        GameplaySessionOperationResult? failure = null;
        controller.OnOperationFailed += (result, _) => failure = result;

        controller.ConfirmSaveAndReturn();

        Assert.AreEqual(GameplaySessionOperationResult.WriteFailed, failure);
        Assert.AreEqual(GameState.Paused, GameStateManager.Instance.CurrentState, "A failed Save-and-Return must never leave gameplay.");
        Assert.IsTrue(GameSessionManager.Instance.IsDirty, "A failed save must not silently clear dirty state.");

        Object.DestroyImmediate(playerRoot);
        Object.Destroy(controllerObject);
    }

    [UnityTest]
    public IEnumerator ConfirmReturnWithoutSaving_ReturnsWithoutWritingSave()
    {
        BeginPausedSession(1, MakeActiveSave());
        GameSessionManager.Instance.MarkDirty();
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        GameplaySessionController controller = BuildController(stat, transform, null, out GameObject controllerObject);
        int savedCount = 0;
        controller.OnSaveSucceeded += () => savedCount++;

        controller.ConfirmReturnWithoutSaving();

        Assert.AreEqual(0, savedCount);
        Assert.AreEqual(GameState.Loading, GameStateManager.Instance.CurrentState);

        yield return WaitForState(GameState.MainMenu, 15f);

        Object.DestroyImmediate(playerRoot);
        Object.Destroy(controllerObject);
    }

    [Test]
    public void CancelReturnToMainMenu_ChangesNothing()
    {
        BeginPausedSession(1, MakeActiveSave());
        GameSessionManager.Instance.MarkDirty();
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        GameplaySessionController controller = BuildController(stat, transform, null, out GameObject controllerObject);

        controller.CancelReturnToMainMenu();

        Assert.AreEqual(GameState.Paused, GameStateManager.Instance.CurrentState);
        Assert.IsTrue(GameSessionManager.Instance.HasActiveSession);
        Assert.IsTrue(GameSessionManager.Instance.IsDirty);

        Object.DestroyImmediate(playerRoot);
        Object.Destroy(controllerObject);
    }

    // ---- Quit Desktop ----

    [Test]
    public void RequestQuit_WhenClean_QuitsDirectly()
    {
        BeginPausedSession(1, MakeActiveSave());
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        var quitter = new FakeApplicationQuitter();
        GameplaySessionController controller = BuildController(stat, transform, quitter, out GameObject controllerObject);

        controller.RequestQuit();

        Assert.AreEqual(1, quitter.QuitCount);

        Object.DestroyImmediate(playerRoot);
        Object.Destroy(controllerObject);
    }

    [Test]
    public void RequestQuit_WhenDirty_RequestsConfirmationAndDoesNotQuit()
    {
        BeginPausedSession(1, MakeActiveSave());
        GameSessionManager.Instance.MarkDirty();
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        var quitter = new FakeApplicationQuitter();
        GameplaySessionController controller = BuildController(stat, transform, quitter, out GameObject controllerObject);

        GameplaySessionConfirmationKind? kind = null;
        controller.OnConfirmationRequired += k => kind = k;

        controller.RequestQuit();

        Assert.AreEqual(GameplaySessionConfirmationKind.Quit, kind);
        Assert.AreEqual(0, quitter.QuitCount);

        Object.DestroyImmediate(playerRoot);
        Object.Destroy(controllerObject);
    }

    [Test]
    public void ConfirmSaveAndQuit_SavesThenQuits()
    {
        BeginPausedSession(1, MakeActiveSave());
        GameSessionManager.Instance.MarkDirty();
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        var quitter = new FakeApplicationQuitter();
        GameplaySessionController controller = BuildController(stat, transform, quitter, out GameObject controllerObject);
        int savedCount = 0;
        controller.OnSaveSucceeded += () => savedCount++;

        controller.ConfirmSaveAndQuit();

        Assert.AreEqual(1, savedCount);
        Assert.AreEqual(1, quitter.QuitCount);

        Object.DestroyImmediate(playerRoot);
        Object.Destroy(controllerObject);
    }

    [Test]
    public void ConfirmSaveAndQuit_SaveFails_DoesNotQuit()
    {
        BeginPausedSession(1, MakeActiveSave());
        GameSessionManager.Instance.MarkDirty();
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        var quitter = new FakeApplicationQuitter();
        GameplaySessionController controller = BuildController(stat, transform, quitter, out GameObject controllerObject);
        _fakeRepository.FailNextWrite = true;

        controller.ConfirmSaveAndQuit();

        Assert.AreEqual(0, quitter.QuitCount, "A failed save must never be followed by a quit.");

        Object.DestroyImmediate(playerRoot);
        Object.Destroy(controllerObject);
    }

    [Test]
    public void ConfirmQuitWithoutSaving_QuitsWithoutWritingSave()
    {
        BeginPausedSession(1, MakeActiveSave());
        GameSessionManager.Instance.MarkDirty();
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        var quitter = new FakeApplicationQuitter();
        GameplaySessionController controller = BuildController(stat, transform, quitter, out GameObject controllerObject);
        int savedCount = 0;
        controller.OnSaveSucceeded += () => savedCount++;

        controller.ConfirmQuitWithoutSaving();

        Assert.AreEqual(0, savedCount);
        Assert.AreEqual(1, quitter.QuitCount);

        Object.DestroyImmediate(playerRoot);
        Object.Destroy(controllerObject);
    }

    [Test]
    public void CancelQuit_ChangesNothing()
    {
        BeginPausedSession(1, MakeActiveSave());
        GameSessionManager.Instance.MarkDirty();
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        var quitter = new FakeApplicationQuitter();
        GameplaySessionController controller = BuildController(stat, transform, quitter, out GameObject controllerObject);

        controller.CancelQuit();

        Assert.AreEqual(0, quitter.QuitCount);
        Assert.IsTrue(GameSessionManager.Instance.IsDirty);

        Object.DestroyImmediate(playerRoot);
        Object.Destroy(controllerObject);
    }

    // ---- Real scene-reload integration: Load Game must not leak state between slots ----

    [UnityTest]
    public IEnumerator RequestLoad_DifferentSlot_RealSceneReload_DoesNotLeakInventoryFromPreviousSlot()
    {
        GameSaveData slotA = MakeActiveSave("slot-a");
        slotA.inventory.gold = 111;
        GameSaveData slotB = MakeActiveSave("slot-b");
        slotB.inventory.gold = 222;
        _fakeRepository.WriteSave(1, slotA);
        _fakeRepository.WriteSave(2, slotB);

        Assert.IsTrue(GameSessionManager.Instance.TryStartLoadedGame(1, "DemoScene", slotA));
        Assert.IsTrue(SceneFlowService.Instance.TryLoadGameplay("DemoScene"));
        yield return WaitForState(GameState.Playing, 15f);

        Assert.AreEqual(111, InventoryManager.Instance.Gold);

        GameObject controllerObject = new("GameplaySessionControllerFixture");
        GameplaySessionController controller = controllerObject.AddComponent<GameplaySessionController>();
        controller.ConfigureForTests(PlayerStat.Instance, PlayerStat.Instance.transform, worldRegistry: null, gameplaySceneName: "DemoScene");
        GameStateManager.Instance.Pause();

        Assert.IsTrue(controller.RequestLoad(2));
        yield return WaitForState(GameState.Playing, 15f);

        Assert.AreEqual(222, InventoryManager.Instance.Gold, "Loading slot B must never carry over slot A's gold.");

        SceneFlowService.Instance.TryReturnToMainMenu();
        yield return WaitForState(GameState.MainMenu, 15f);
    }

    private static IEnumerator WaitForState(GameState target, float timeoutSeconds)
    {
        float waited = 0f;
        while (GameStateManager.Instance.CurrentState != target && waited < timeoutSeconds)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
        Assert.AreEqual(target, GameStateManager.Instance.CurrentState, $"Timed out waiting for {target}.");
    }
}
