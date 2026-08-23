using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class GameSessionManagerPlayModeTests
{
    [TearDown]
    public void TearDown() => GameSessionManager.Instance.ClearSession();

    [Test]
    public void DefaultSaveRepository_IsConstructedWithoutError()
    {
        // Regression: SaveRepository used to be a field initializer that called
        // Application.persistentDataPath outside Awake, which Unity forbids and throws for.
        LogAssert.NoUnexpectedReceived();
        Assert.IsNotNull(GameSessionManager.Instance.SaveRepository);
        Assert.IsInstanceOf<FileSaveSlotRepository>(GameSessionManager.Instance.SaveRepository);
    }

    [Test]
    public void MarkDirty_FiresDirtyStateChangedOnlyOnRealTransition()
    {
        GameSessionManager.Instance.TryStartDevelopment("TestScene");
        int changedCount = 0;
        bool lastValue = false;
        void OnDirtyStateChanged(bool v) { changedCount++; lastValue = v; }
        GameSessionManager.Instance.DirtyStateChanged += OnDirtyStateChanged;
        try
        {
            Assert.IsFalse(GameSessionManager.Instance.IsDirty);

            GameSessionManager.Instance.MarkDirty();
            Assert.IsTrue(GameSessionManager.Instance.IsDirty);
            Assert.AreEqual(1, changedCount);
            Assert.IsTrue(lastValue);

            GameSessionManager.Instance.MarkDirty(); // already dirty -- must not re-fire
            Assert.AreEqual(1, changedCount);

            GameSessionManager.Instance.ClearDirty();
            Assert.IsFalse(GameSessionManager.Instance.IsDirty);
            Assert.AreEqual(2, changedCount);
            Assert.IsFalse(lastValue);

            GameSessionManager.Instance.ClearDirty(); // already clean -- must not re-fire
            Assert.AreEqual(2, changedCount);
        }
        finally
        {
            GameSessionManager.Instance.DirtyStateChanged -= OnDirtyStateChanged;
        }
    }

    [Test]
    public void MarkDirty_WhileRestoring_IsSuppressed()
    {
        GameSessionManager.Instance.TryStartDevelopment("TestScene");
        GameSessionManager.Instance.BeginRestore();
        try
        {
            GameSessionManager.Instance.MarkDirty();
            Assert.IsFalse(GameSessionManager.Instance.IsDirty, "Restore-time change events must never dirty the session.");
        }
        finally
        {
            GameSessionManager.Instance.EndRestore();
        }

        GameSessionManager.Instance.MarkDirty();
        Assert.IsTrue(GameSessionManager.Instance.IsDirty, "After EndRestore, real gameplay changes must dirty normally.");
    }

    [Test]
    public void StartingANewSession_ResetsDirtyAndRestoringFlags()
    {
        GameSessionManager.Instance.TryStartDevelopment("TestScene");
        GameSessionManager.Instance.MarkDirty();
        Assert.IsTrue(GameSessionManager.Instance.IsDirty);

        GameSessionManager.Instance.TryStartNewGame(1, "TestScene", NewGameFactory.CreateDefault());

        Assert.IsFalse(GameSessionManager.Instance.IsDirty, "A brand new session must never start dirty.");
        Assert.IsFalse(GameSessionManager.Instance.IsRestoring);
    }

    [Test]
    public void GetTotalPlayTimeSeconds_AddsElapsedTimeOnTopOfTheLoadedBase()
    {
        var saveData = new GameSaveData { saveId = "s1", totalPlayTimeSeconds = 500 };
        GameSessionManager.Instance.TryStartLoadedGame(1, "TestScene", saveData);

        long total = GameSessionManager.Instance.GetTotalPlayTimeSeconds();
        Assert.GreaterOrEqual(total, 500, "Total play time must never be less than what the loaded save already carried.");
    }
}
