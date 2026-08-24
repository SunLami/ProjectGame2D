using NUnit.Framework;
using UnityEngine;

/// <summary>Regression coverage for the Phase 8 MapManager fix: it must be a scene-scoped service
/// (never DontDestroyOnLoad) so a scene reload always rebinds to that scene's own Player/Tilemap
/// instead of a persisted instance surviving with references from an already-unloaded scene
/// (ServiceOwnershipLifecycle.md, Roadmap Phase 8 "sua MapManager... rebind sau load").</summary>
public sealed class MapManagerPlayModeTests
{
    [TearDown]
    public void TearDown()
    {
        if (MapManager.Instance != null)
            Object.DestroyImmediate(MapManager.Instance.gameObject);
    }

    [Test]
    public void Awake_SetsInstance_AndIsNotDontDestroyOnLoad()
    {
        var go = new GameObject("MapManagerFixture");
        MapManager manager = go.AddComponent<MapManager>();

        Assert.AreSame(manager, MapManager.Instance);
        Assert.AreNotEqual("DontDestroyOnLoad", go.scene.name,
            "MapManager must be scene-scoped -- DontDestroyOnLoad would let it survive its owning scene's unload with stale Tilemap/Player references.");
    }

    [Test]
    public void SceneReload_DestroyingOldInstance_ClearsStaticInstance_SoNextLoadRebindsCleanly()
    {
        var sceneA = new GameObject("MapManagerFixture_SceneA");
        MapManager mapManagerA = sceneA.AddComponent<MapManager>();
        Assert.AreSame(mapManagerA, MapManager.Instance);

        // Simulates the previous scene unloading (Unity destroys its objects, including MapManager,
        // before the next scene's Awake runs).
        Object.DestroyImmediate(sceneA);
        Assert.IsNull(MapManager.Instance, "OnDestroy must clear Instance so a stale reference never survives to the next scene load.");

        var sceneB = new GameObject("MapManagerFixture_SceneB");
        MapManager mapManagerB = sceneB.AddComponent<MapManager>();

        Assert.AreSame(mapManagerB, MapManager.Instance);
        Assert.AreNotSame(mapManagerA, MapManager.Instance, "The new scene's MapManager must fully replace the old one, never be destroyed in favor of a stale singleton.");

        Object.DestroyImmediate(sceneB);
    }

    [Test]
    public void GetCurrentTileAudioClip_UsesOnlyThisInstancesOwnPlayerReference()
    {
        var go = new GameObject("MapManagerFixture");
        MapManager manager = go.AddComponent<MapManager>();

        var playerGo = new GameObject("PlayerFixture");
        playerGo.AddComponent<Rigidbody2D>();
        playerGo.AddComponent<Animator>();
        var stat = playerGo.AddComponent<PlayerStat>();
        Player player = playerGo.AddComponent<Player>();

        var tilemapGo = new GameObject("TilemapFixture");
        var tilemap = tilemapGo.AddComponent<UnityEngine.Tilemaps.Tilemap>();
        tilemapGo.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();

        typeof(MapManager).GetField("_player", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(manager, player);
        typeof(MapManager).GetField("_tilemap", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .SetValue(manager, tilemap);

        // No tile placed at this position -- proves the call resolves through this instance's own
        // bound references without throwing (an FindAnyObjectByType-based stale/null _player would
        // NRE here instead).
        Assert.DoesNotThrow(() => manager.GetCurrentTileAudioClip(Vector2.zero));

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(playerGo);
        Object.DestroyImmediate(tilemapGo);
    }
}
