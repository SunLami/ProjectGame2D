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
    public IEnumerator DoubleSubmit_SecondRequestIsRejectedWithoutOverwritingSession()
    {
        GameObject controllerObject = new("MainMenuControllerFixture");
        MainMenuController controller = controllerObject.AddComponent<MainMenuController>();

        int failureCount = 0;
        controller.OnOperationFailed += _ => failureCount++;

        // Two rapid clicks on the same frame, as a real double-click/double-submit would produce.
        controller.RequestNewGame(1);
        string firstSaveId = GameSessionManager.Instance.Current.SaveData.saveId;
        Assert.IsNotNull(firstSaveId);

        controller.RequestNewGame(1);

        Assert.AreEqual(1, failureCount, "The second concurrent RequestNewGame must be rejected.");
        Assert.AreEqual(firstSaveId, GameSessionManager.Instance.Current.SaveData.saveId,
            "A rejected double-submit must not overwrite the in-flight session's save data.");

        float waited = 0f;
        while (GameStateManager.Instance.CurrentState != GameState.Playing && waited < 10f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        Assert.AreEqual(GameState.Playing, GameStateManager.Instance.CurrentState);
        Assert.AreEqual(firstSaveId, GameSessionManager.Instance.Current.SaveData.saveId);

        Object.Destroy(controllerObject);
        SceneFlowService.Instance.TryReturnToMainMenu();

        waited = 0f;
        while (GameStateManager.Instance.CurrentState != GameState.MainMenu && waited < 10f)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
    }
}
