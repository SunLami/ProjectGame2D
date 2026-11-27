using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class MainMenuControllerPlayModeTests
{
    private ISaveSlotRepository _originalRepository;

    [SetUp]
    public void SetUp()
    {
        _originalRepository = GameSessionManager.Instance.SaveRepository;
        GameSessionManager.Instance.SetSaveRepositoryForTests(new InMemorySaveSlotRepository());
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

    [UnityTest]
    public IEnumerator NewGame_SaveAndReturn_ContinueRestoresProductionSlot()
    {
        Assert.IsTrue(SceneFlowService.Instance.TryReturnToMainMenu());
        yield return WaitFor(() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu"
            && GameStateManager.Instance.CurrentState == GameState.MainMenu, "MainMenu");
        var menu = Object.FindAnyObjectByType<MainMenuController>();
        Assert.IsNotNull(menu);
        Assert.IsFalse(menu.CanContinue(1));

        int failures = 0;
        menu.OnOperationFailed += _ => failures++;
        menu.RequestNewGame(1);
        string saveId = GameSessionManager.Instance.Current.SaveData?.saveId;
        Assert.IsNotNull(saveId);
        menu.RequestNewGame(2);
        Assert.AreEqual(1, failures);
        Assert.AreEqual(saveId, GameSessionManager.Instance.Current.SaveData.saveId);

        yield return WaitFor(() => Object.FindAnyObjectByType<IntroCutsceneController>()?.IsPlaying == true,
            "intro playback");
        Object.FindAnyObjectByType<IntroCutsceneController>().SkipIntro();
        yield return WaitFor(() =>
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MapNhat")
                return GameStateManager.Instance.CurrentState == GameState.Playing;

            if (!SceneFlowService.Instance.IsTransitioning)
                Object.FindAnyObjectByType<GameplayTimelineController>()?.SkipToNextScene();
            return false;
        }, "first entry into MapNhat");

        var repository = GameSessionManager.Instance.SaveRepository;
        Assert.IsTrue(repository.TryReadSave(1, out GameSaveData initial));
        Assert.AreEqual(saveId, initial.saveId);
        Assert.AreEqual(SaveSlotStatus.Empty, repository.GetSlotInfo(2).Status);

        int expectedGold = InventoryManager.Instance.Gold + 37;
        InventoryManager.Instance.AddGold(37);
        var pause = Object.FindAnyObjectByType<PauseMenuUI>();
        Assert.IsNotNull(pause);
        GameStateManager.Instance.Pause();
        pause.OnReturnToMainMenuClicked();

        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var confirmation = (GameObject)typeof(PauseMenuUI).GetField("_confirmationPopup", flags).GetValue(pause);
        Assert.IsTrue(confirmation.activeInHierarchy, "Back to Menu must show the actual Pause confirmation.");
        var save = (UnityEngine.UI.Button)typeof(PauseMenuUI).GetField("_confirmationSaveButton", flags).GetValue(pause);
        save.onClick.Invoke();

        yield return WaitFor(() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu"
            && GameStateManager.Instance.CurrentState == GameState.MainMenu, "Save and Return");
        Assert.IsTrue(repository.TryReadSave(1, out GameSaveData saved));
        Assert.AreEqual(expectedGold, saved.inventory.gold);
        Assert.AreEqual(saveId, saved.saveId);

        menu = Object.FindAnyObjectByType<MainMenuController>();
        Assert.IsTrue(menu.CanContinue(1));
        Assert.IsFalse(menu.CanContinue(2));
        menu.RequestContinue(1);
        yield return WaitFor(() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MapNhat"
            && GameStateManager.Instance.CurrentState == GameState.Playing, "Continue in MapNhat");
        Assert.AreEqual(expectedGold, InventoryManager.Instance.Gold);
        Assert.AreEqual(1, GameSessionManager.Instance.Current.SlotId);

        Assert.IsTrue(SceneFlowService.Instance.TryReturnToMainMenu());
        yield return WaitFor(() => GameStateManager.Instance.CurrentState == GameState.MainMenu, "test cleanup");
    }

    private static IEnumerator WaitFor(System.Func<bool> predicate, string description)
    {
        float deadline = Time.realtimeSinceStartup + 45f;
        while (!predicate() && Time.realtimeSinceStartup < deadline)
            yield return null;

        Assert.IsTrue(predicate(), $"Timed out waiting for {description}.");
    }
}
