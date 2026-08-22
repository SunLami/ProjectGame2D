using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class PlayerSpawnReadinessSourcePlayModeTests
{
    private ISaveSlotRepository _originalRepository;

    [SetUp]
    public void SetUp()
    {
        _originalRepository = GameSessionManager.Instance.SaveRepository;
        GameSessionManager.Instance.SetSaveRepositoryForTests(new InMemorySaveSlotRepository());
    }

    [TearDown]
    public void TearDown()
    {
        GameSessionManager.Instance.SetSaveRepositoryForTests(_originalRepository);
        GameSessionManager.Instance.ClearSession();
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.ResetToMainMenu();
    }

    private static (GameObject root, PlayerStat stat, Transform playerTransform, SpawnRegistry spawnRegistry)
        BuildFixture(Vector3 spawnPosition)
    {
        GameObject root = new("PlayerSpawnFixture");
        PlayerStat stat = root.AddComponent<PlayerStat>();

        GameObject spawnPoint = new("SpawnPoint");
        spawnPoint.transform.position = spawnPosition;
        spawnPoint.transform.SetParent(root.transform);

        GameObject registryObject = new("SpawnRegistry");
        registryObject.transform.SetParent(root.transform);
        SpawnRegistry registry = registryObject.AddComponent<SpawnRegistry>();
        registry.ConfigureForTests(NewGameFactory.TutorialStartSpawnId, spawnPoint.transform);

        return (root, stat, root.transform, registry);
    }

    [UnityTest]
    public IEnumerator NewGame_RestoresDefaultsAndPositionsAtTutorialSpawn()
    {
        Vector3 spawnPosition = new(5f, 7f, 0f);
        var (root, stat, playerTransform, registry) = BuildFixture(spawnPosition);
        playerTransform.position = Vector3.zero;

        GameSaveData saveData = NewGameFactory.CreateDefault();
        Assert.IsTrue(GameSessionManager.Instance.TryStartNewGame(1, "TestScene", saveData));

        GameObject sourceObject = new("PlayerSpawnReadinessSource");
        PlayerSpawnReadinessSource source = sourceObject.AddComponent<PlayerSpawnReadinessSource>();
        source.ConfigureForTests(stat, playerTransform, registry);

        yield return null;

        Assert.IsTrue(source.IsReady);
        Assert.AreEqual(1, stat.Level);
        Assert.AreEqual(spawnPosition.x, playerTransform.position.x, 0.001f);
        Assert.AreEqual(spawnPosition.y, playerTransform.position.y, 0.001f);

        Assert.IsTrue(GameSessionManager.Instance.SaveRepository.TryReadSave(1, out GameSaveData written),
            "New Game restore should write the initial save (D-011).");
        Assert.AreEqual(saveData.saveId, written.saveId);

        Object.Destroy(root);
        Object.Destroy(sourceObject);
    }

    [UnityTest]
    public IEnumerator Continue_DoesNotRewriteSaveAndRestoresSavedProgression()
    {
        var (root, stat, playerTransform, registry) = BuildFixture(new Vector3(1f, 1f, 0f));

        GameSaveData saveData = new()
        {
            saveId = "existing-save",
            player = new PlayerSaveData
            {
                level = 3,
                currentExperience = 20,
                health = 5f,
                location = new PlayerLocationSaveData
                {
                    areaId = "area.town",
                    positionX = 42f,
                    positionY = -3f
                }
            }
        };
        GameSessionManager.Instance.SaveRepository.WriteSave(2, saveData);

        Assert.IsTrue(GameSessionManager.Instance.TryStartLoadedGame(2, "TestScene", saveData));

        GameObject sourceObject = new("PlayerSpawnReadinessSource");
        PlayerSpawnReadinessSource source = sourceObject.AddComponent<PlayerSpawnReadinessSource>();
        source.ConfigureForTests(stat, playerTransform, registry);

        yield return null;

        Assert.IsTrue(source.IsReady);
        Assert.AreEqual(3, stat.Level);
        Assert.AreEqual(20, stat.CurrentExperience);
        Assert.AreEqual(42f, playerTransform.position.x, 0.001f);
        Assert.AreEqual(-3f, playerTransform.position.y, 0.001f);

        Object.Destroy(root);
        Object.Destroy(sourceObject);
    }

    [UnityTest]
    public IEnumerator CapturedSnapshot_RoundTripsThroughWriteAndContinueRestore()
    {
        // Simulates a real "leave with progress, come back" cycle: capture live state with
        // PlayerSaveCapture (not a hand-built PlayerSaveData), persist it, then restore it into a
        // fresh fixture the way Continue would.
        var (captureRoot, captureStat, captureTransform, _) = BuildFixture(Vector3.zero);
        captureStat.RestoreProgression(level: 6, currentExperience: 18, health: 30f);
        captureTransform.position = new Vector3(8f, -6f, 0f);

        PlayerSaveData captured = PlayerSaveCapture.Capture(
            captureStat, captureTransform, "area.town", "spawn.town.gate");
        GameSaveData saveData = new() { saveId = "captured-save", player = captured };
        GameSessionManager.Instance.SaveRepository.WriteSave(3, saveData);

        // PlayerStat is a static singleton; Object.Destroy() defers to end-of-frame, so building
        // the second fixture before that frame ends would see captureStat still "alive" and
        // self-destroy the new fixture instead. DestroyImmediate frees the singleton slot now.
        Object.DestroyImmediate(captureRoot);

        var (restoreRoot, restoreStat, restoreTransform, restoreRegistry) = BuildFixture(Vector3.zero);
        Assert.IsTrue(GameSessionManager.Instance.TryStartLoadedGame(3, "TestScene", saveData));

        GameObject sourceObject = new("PlayerSpawnReadinessSource");
        PlayerSpawnReadinessSource source = sourceObject.AddComponent<PlayerSpawnReadinessSource>();
        source.ConfigureForTests(restoreStat, restoreTransform, restoreRegistry);

        yield return null;

        Assert.AreEqual(6, restoreStat.Level);
        Assert.AreEqual(18, restoreStat.CurrentExperience);
        Assert.AreEqual(8f, restoreTransform.position.x, 0.001f);
        Assert.AreEqual(-6f, restoreTransform.position.y, 0.001f);

        Object.Destroy(restoreRoot);
        Object.Destroy(sourceObject);
    }

    [UnityTest]
    public IEnumerator NoActiveSession_ReportsReadyWithoutTouchingPlayer()
    {
        var (root, stat, playerTransform, registry) = BuildFixture(new Vector3(9f, 9f, 0f));
        playerTransform.position = Vector3.zero;

        GameObject sourceObject = new("PlayerSpawnReadinessSource");
        PlayerSpawnReadinessSource source = sourceObject.AddComponent<PlayerSpawnReadinessSource>();
        source.ConfigureForTests(stat, playerTransform, registry);

        yield return null;

        Assert.IsTrue(source.IsReady);
        Assert.AreEqual(Vector3.zero, playerTransform.position);

        Object.Destroy(root);
        Object.Destroy(sourceObject);
    }
}
