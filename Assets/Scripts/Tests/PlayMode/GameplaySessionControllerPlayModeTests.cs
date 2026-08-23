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
        private readonly Dictionary<int, SaveSlotStatus> _statusOverride = new();
        public bool FailNextWrite;

        /// <summary>Forces GetSlotInfo(slotId) to report Corrupted/IncompatibleVersion regardless of
        /// what _inner actually holds -- InMemorySaveSlotRepository only ever knows Empty/Valid, so
        /// this is the only way tests can exercise the "don't silently overwrite" paths for those
        /// two statuses without standing up a real FileSaveSlotRepository.</summary>
        public void ForceStatus(int slotId, SaveSlotStatus status) => _statusOverride[slotId] = status;

        public SaveSlotInfo GetSlotInfo(int slotId) =>
            _statusOverride.TryGetValue(slotId, out SaveSlotStatus forced)
                ? new SaveSlotInfo(slotId, forced, null)
                : _inner.GetSlotInfo(slotId);

        public SaveSlotInfo[] GetAllSlotInfo()
        {
            int count = GameSessionManager.MaximumSlotId - GameSessionManager.MinimumSlotId + 1;
            SaveSlotInfo[] result = new SaveSlotInfo[count];
            for (int i = 0; i < count; i++)
                result[i] = GetSlotInfo(GameSessionManager.MinimumSlotId + i);
            return result;
        }

        public bool TryReadSave(int slotId, out GameSaveData data) => _inner.TryReadSave(slotId, out data);

        public SaveOperationResult WriteSave(int slotId, GameSaveData data)
        {
            if (FailNextWrite)
            {
                FailNextWrite = false;
                return SaveOperationResult.Failure("Simulated write failure.");
            }
            SaveOperationResult result = _inner.WriteSave(slotId, data);
            if (result.Success)
                _statusOverride.Remove(slotId);
            return result;
        }

        public SaveOperationResult DeleteSlot(int slotId)
        {
            _statusOverride.Remove(slotId);
            return _inner.DeleteSlot(slotId);
        }
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

    // ---- Save Game -- slot picker (RequestSaveToSlot / Save As / Delete) ----

    [Test]
    public void RequestSaveToSlot_EmptySlot_WritesImmediatelyWithoutConfirmation()
    {
        BeginPausedSession(1, MakeActiveSave());
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        GameplaySessionController controller = BuildController(stat, transform, null, out GameObject controllerObject);

        int confirmationCount = 0;
        controller.OnSaveSlotConfirmationRequired += (_, _) => confirmationCount++;
        int succeededCount = 0;
        controller.OnSaveSucceeded += () => succeededCount++;

        controller.RequestSaveToSlot(2); // slot 2 was never written -- Empty

        Assert.AreEqual(0, confirmationCount, "An Empty slot must save directly, no confirm.");
        Assert.AreEqual(1, succeededCount);
        Assert.AreEqual(SaveSlotStatus.Valid, controller.RefreshSlots()[1].Status);

        Object.DestroyImmediate(playerRoot);
        Object.Destroy(controllerObject);
    }

    [Test]
    public void RequestSaveToSlot_SaveAs_DifferentSlot_MovesActiveSlotId()
    {
        BeginPausedSession(1, MakeActiveSave());
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        GameplaySessionController controller = BuildController(stat, transform, null, out GameObject controllerObject);

        Assert.AreEqual(1, controller.ActiveSlotId);

        controller.RequestSaveToSlot(3); // Empty -> writes directly

        Assert.AreEqual(3, controller.ActiveSlotId, "Saving into a different (Empty) slot must be a Save As -- ActiveSlotId follows the write.");
        Assert.AreEqual(SaveSlotStatus.Valid, _fakeRepository.GetSlotInfo(3).Status);

        Object.DestroyImmediate(playerRoot);
        Object.Destroy(controllerObject);
    }

    [Test]
    public void RequestSaveToSlot_SaveAs_DoesNotTouchOtherSlots()
    {
        _fakeRepository.WriteSave(2, MakeActiveSave("slot-2-untouched"));
        BeginPausedSession(1, MakeActiveSave("slot-1-original"));
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        GameplaySessionController controller = BuildController(stat, transform, null, out GameObject controllerObject);

        controller.RequestSaveToSlot(3); // Save As into a third, Empty slot

        Assert.IsTrue(_fakeRepository.TryReadSave(1, out GameSaveData slot1));
        Assert.AreEqual("slot-1-original", slot1.saveId, "Saving into slot 3 must never touch slot 1's file.");
        Assert.IsTrue(_fakeRepository.TryReadSave(2, out GameSaveData slot2));
        Assert.AreEqual("slot-2-untouched", slot2.saveId, "Saving into slot 3 must never touch slot 2's file.");

        Object.DestroyImmediate(playerRoot);
        Object.Destroy(controllerObject);
    }

    [Test]
    public void RequestSaveToSlot_ValidSlot_RequestsConfirmation_DoesNotWriteYet()
    {
        BeginPausedSession(1, MakeActiveSave());
        _fakeRepository.WriteSave(2, MakeActiveSave("slot-2-existing"));
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        GameplaySessionController controller = BuildController(stat, transform, null, out GameObject controllerObject);

        (int slotId, SaveSlotStatus status)? pending = null;
        controller.OnSaveSlotConfirmationRequired += (slotId, status) => pending = (slotId, status);
        int succeededCount = 0;
        controller.OnSaveSucceeded += () => succeededCount++;

        controller.RequestSaveToSlot(2);

        Assert.AreEqual((2, SaveSlotStatus.Valid), pending);
        Assert.AreEqual(0, succeededCount, "Must not write until ConfirmOverwriteAndSave is called.");
        Assert.IsTrue(_fakeRepository.TryReadSave(2, out GameSaveData stillOriginal));
        Assert.AreEqual("slot-2-existing", stillOriginal.saveId, "The existing slot 2 save must be untouched before confirmation.");

        Object.DestroyImmediate(playerRoot);
        Object.Destroy(controllerObject);
    }

    [Test]
    public void ConfirmOverwriteAndSave_WritesThePendingSlotAndMovesActiveSlotId()
    {
        BeginPausedSession(1, MakeActiveSave());
        _fakeRepository.WriteSave(2, MakeActiveSave("slot-2-existing"));
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        GameplaySessionController controller = BuildController(stat, transform, null, out GameObject controllerObject);

        controller.RequestSaveToSlot(2);
        int succeededCount = 0;
        controller.OnSaveSucceeded += () => succeededCount++;

        controller.ConfirmOverwriteAndSave();

        Assert.AreEqual(1, succeededCount);
        Assert.AreEqual(2, controller.ActiveSlotId);
        Assert.IsTrue(_fakeRepository.TryReadSave(2, out GameSaveData overwritten));
        Assert.AreNotEqual("slot-2-existing", overwritten.saveId, "Confirmed overwrite must actually replace the old save.");

        Object.DestroyImmediate(playerRoot);
        Object.Destroy(controllerObject);
    }

    [Test]
    public void CancelSaveToSlot_LeavesPendingSlotUntouched()
    {
        BeginPausedSession(1, MakeActiveSave());
        _fakeRepository.WriteSave(2, MakeActiveSave("slot-2-existing"));
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        GameplaySessionController controller = BuildController(stat, transform, null, out GameObject controllerObject);
        controller.RequestSaveToSlot(2);
        int succeededCount = 0;
        controller.OnSaveSucceeded += () => succeededCount++;

        controller.CancelSaveToSlot();
        controller.ConfirmOverwriteAndSave(); // must now be a no-op -- nothing pending anymore

        Assert.AreEqual(0, succeededCount);
        Assert.AreEqual(1, controller.ActiveSlotId, "Cancel must never move ActiveSlotId.");
        Assert.IsTrue(_fakeRepository.TryReadSave(2, out GameSaveData stillOriginal));
        Assert.AreEqual("slot-2-existing", stillOriginal.saveId);

        Object.DestroyImmediate(playerRoot);
        Object.Destroy(controllerObject);
    }

    [Test]
    public void RequestSaveToSlot_CorruptedSlot_RequestsConfirmation_NeverSilentlyOverwritten()
    {
        BeginPausedSession(1, MakeActiveSave());
        _fakeRepository.ForceStatus(2, SaveSlotStatus.Corrupted);
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        GameplaySessionController controller = BuildController(stat, transform, null, out GameObject controllerObject);

        (int slotId, SaveSlotStatus status)? pending = null;
        controller.OnSaveSlotConfirmationRequired += (slotId, status) => pending = (slotId, status);

        controller.RequestSaveToSlot(2);

        Assert.AreEqual((2, SaveSlotStatus.Corrupted), pending, "A Corrupted slot must still ask for confirmation, never write silently.");

        Object.DestroyImmediate(playerRoot);
        Object.Destroy(controllerObject);
    }

    [Test]
    public void RequestSaveToSlot_IncompatibleVersionSlot_RequestsConfirmation_NeverSilentlyOverwritten()
    {
        BeginPausedSession(1, MakeActiveSave());
        _fakeRepository.ForceStatus(2, SaveSlotStatus.IncompatibleVersion);
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        GameplaySessionController controller = BuildController(stat, transform, null, out GameObject controllerObject);

        (int slotId, SaveSlotStatus status)? pending = null;
        controller.OnSaveSlotConfirmationRequired += (slotId, status) => pending = (slotId, status);

        controller.RequestSaveToSlot(2);

        Assert.AreEqual((2, SaveSlotStatus.IncompatibleVersion), pending, "An IncompatibleVersion slot must still ask for confirmation, never write silently.");

        Object.DestroyImmediate(playerRoot);
        Object.Destroy(controllerObject);
    }

    [Test]
    public void DoubleClickSaveToSlot_SecondCallRejectedWhileFirstInFlight()
    {
        BeginPausedSession(1, MakeActiveSave());
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        GameplaySessionController controller = BuildController(stat, transform, null, out GameObject controllerObject);

        GameStateManager.Instance.PushState(GameState.Saving);
        Assert.IsTrue(controller.IsBusy);

        GameplaySessionOperationResult? failure = null;
        controller.OnOperationFailed += (result, _) => failure = result;
        controller.RequestSaveToSlot(2);
        Assert.AreEqual(GameplaySessionOperationResult.AlreadyBusy, failure);
        Assert.AreEqual(SaveSlotStatus.Empty, _fakeRepository.GetSlotInfo(2).Status, "A rejected double-submit must not write anything.");

        GameStateManager.Instance.ReturnToPreviousState();
        Object.DestroyImmediate(playerRoot);
        Object.Destroy(controllerObject);
    }

    [Test]
    public void RequestSaveToSlot_InvalidSlotId_Rejected()
    {
        BeginPausedSession(1, MakeActiveSave());
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        GameplaySessionController controller = BuildController(stat, transform, null, out GameObject controllerObject);

        GameplaySessionOperationResult? failure = null;
        controller.OnOperationFailed += (result, _) => failure = result;

        controller.RequestSaveToSlot(0);
        Assert.AreEqual(GameplaySessionOperationResult.InvalidSlot, failure);

        Object.DestroyImmediate(playerRoot);
        Object.Destroy(controllerObject);
    }

    [Test]
    public void RequestSaveToSlot_WriteFailure_KeepsOldSaveAndActiveSlotUnchanged()
    {
        BeginPausedSession(1, MakeActiveSave());
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        GameplaySessionController controller = BuildController(stat, transform, null, out GameObject controllerObject);
        _fakeRepository.FailNextWrite = true;

        GameplaySessionOperationResult? failure = null;
        controller.OnOperationFailed += (result, _) => failure = result;

        controller.RequestSaveToSlot(2); // Empty slot -- writes immediately, but the write fails

        Assert.AreEqual(GameplaySessionOperationResult.WriteFailed, failure);
        Assert.AreEqual(1, controller.ActiveSlotId, "A failed Save As must never move ActiveSlotId.");
        Assert.AreEqual(SaveSlotStatus.Empty, _fakeRepository.GetSlotInfo(2).Status, "A failed write must leave the target slot exactly as it was.");

        Object.DestroyImmediate(playerRoot);
        Object.Destroy(controllerObject);
    }

    [Test]
    public void DeleteSlot_EmptiesTheSlotAndRefreshesList()
    {
        _fakeRepository.WriteSave(2, MakeActiveSave("slot-2"));
        BeginPausedSession(1, MakeActiveSave());
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        GameplaySessionController controller = BuildController(stat, transform, null, out GameObject controllerObject);

        SaveSlotInfo[] refreshed = null;
        controller.OnSaveSlotListChanged += slots => refreshed = slots;

        Assert.IsTrue(controller.DeleteSlot(2));

        Assert.AreEqual(SaveSlotStatus.Empty, _fakeRepository.GetSlotInfo(2).Status);
        Assert.IsNotNull(refreshed);
        Assert.AreEqual(SaveSlotStatus.Empty, refreshed[1].Status);

        Object.DestroyImmediate(playerRoot);
        Object.Destroy(controllerObject);
    }

    [Test]
    public void DeleteSlot_ActiveSlot_SessionKeepsPlaying_NextSaveTreatsItAsEmpty()
    {
        BeginPausedSession(1, MakeActiveSave());
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        GameplaySessionController controller = BuildController(stat, transform, null, out GameObject controllerObject);

        Assert.IsTrue(controller.DeleteSlot(1)); // delete the slot the live session is currently in

        Assert.IsTrue(GameSessionManager.Instance.HasActiveSession, "Deleting the active slot's file must not tear down the live session.");
        Assert.AreEqual(1, controller.ActiveSlotId, "DeleteSlot must never itself change which slot is active.");
        Assert.IsFalse(controller.SlotRequiresOverwriteConfirm(1), "Slot 1 must now look Empty to a fresh save.");

        int succeededCount = 0;
        controller.OnSaveSucceeded += () => succeededCount++;
        controller.RequestSaveToSlot(1);

        Assert.AreEqual(1, succeededCount, "A save to the just-deleted active slot must go straight through as an Empty-slot save, not silently target a stale file.");

        Object.DestroyImmediate(playerRoot);
        Object.Destroy(controllerObject);
    }

    [Test]
    public void DeleteSlot_DoesNotAffectOtherSlots()
    {
        _fakeRepository.WriteSave(2, MakeActiveSave("slot-2"));
        _fakeRepository.WriteSave(3, MakeActiveSave("slot-3"));
        BeginPausedSession(1, MakeActiveSave());
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        GameplaySessionController controller = BuildController(stat, transform, null, out GameObject controllerObject);

        Assert.IsTrue(controller.DeleteSlot(2));

        Assert.AreEqual(SaveSlotStatus.Empty, _fakeRepository.GetSlotInfo(2).Status);
        Assert.AreEqual(SaveSlotStatus.Valid, _fakeRepository.GetSlotInfo(3).Status, "Deleting slot 2 must never touch slot 3.");
        Assert.IsTrue(_fakeRepository.TryReadSave(3, out GameSaveData slot3));
        Assert.AreEqual("slot-3", slot3.saveId);

        Object.DestroyImmediate(playerRoot);
        Object.Destroy(controllerObject);
    }

    [Test]
    public void DeleteSlot_WhileBusy_Rejected()
    {
        BeginPausedSession(1, MakeActiveSave());
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        GameplaySessionController controller = BuildController(stat, transform, null, out GameObject controllerObject);
        GameStateManager.Instance.PushState(GameState.Saving);

        GameplaySessionOperationResult? failure = null;
        controller.OnOperationFailed += (result, _) => failure = result;

        Assert.IsFalse(controller.DeleteSlot(1));
        Assert.AreEqual(GameplaySessionOperationResult.AlreadyBusy, failure);

        GameStateManager.Instance.ReturnToPreviousState();
        Object.DestroyImmediate(playerRoot);
        Object.Destroy(controllerObject);
    }

    [Test]
    public void CanSaveToSlot_And_SlotRequiresOverwriteConfirm_ReflectRealStatus()
    {
        _fakeRepository.WriteSave(2, MakeActiveSave("slot-2"));
        BeginPausedSession(1, MakeActiveSave());
        var (playerRoot, stat, transform) = BuildPlayerFixture();
        GameplaySessionController controller = BuildController(stat, transform, null, out GameObject controllerObject);

        Assert.IsTrue(controller.CanSaveToSlot(3));
        Assert.IsFalse(controller.CanSaveToSlot(0), "Out-of-range slot ids are never saveable.");
        Assert.IsFalse(controller.SlotRequiresOverwriteConfirm(3), "Slot 3 is Empty -- no confirm needed.");
        Assert.IsTrue(controller.SlotRequiresOverwriteConfirm(2), "Slot 2 already holds data -- confirm needed.");

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
