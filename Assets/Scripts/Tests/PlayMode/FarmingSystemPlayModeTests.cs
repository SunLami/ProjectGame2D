using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class FarmingSystemPlayModeTests
{
    private GameObject _inventoryObject;
    private GameObject _plotObject;
    private GameObject _playerObject;
    private SeedItemSO _seed;
    private ItemSO _harvest;
    private CropDefinition _crop;
    private FarmingCatalog _catalog;
    private FarmPlot _plot;
    private FarmingManager _manager;

    [SetUp]
    public void SetUp()
    {
        // PlayMode tests can start from the currently open production scene. Remove its persistent
        // singletons so this fixture owns every dependency and is independent of editor scene state.
        if (FarmingManager.Instance != null)
            Object.DestroyImmediate(FarmingManager.Instance.gameObject);
        if (InventoryManager.Instance != null)
            Object.DestroyImmediate(InventoryManager.Instance.gameObject);

        _inventoryObject = new GameObject("FarmingInventory");
        _inventoryObject.AddComponent<InventoryManager>();
        _harvest = ScriptableObject.CreateInstance<ItemSO>();
        _harvest.itemId = "item.crop.test";
        _harvest.itemName = "Test Crop";
        _harvest.isStackable = true;
        _harvest.maxStackSize = 99;

        _crop = ScriptableObject.CreateInstance<CropDefinition>();
        _crop.ConfigureForTests("crop.test", _harvest, 2, 2, new[]
        {
            new CropGrowthStage { durationSeconds = 0.1f },
            new CropGrowthStage { durationSeconds = 0.1f }
        });
        _seed = ScriptableObject.CreateInstance<SeedItemSO>();
        _seed.itemId = "item.seed.test";
        _seed.itemName = "Test Seed";
        _seed.isStackable = true;
        _seed.maxStackSize = 99;
        _seed.ConfigureCropForTests(_crop);
        _catalog = ScriptableObject.CreateInstance<FarmingCatalog>();
        _catalog.ConfigureForTests(_crop);

        _plotObject = new GameObject("FarmPlotTest", typeof(BoxCollider2D), typeof(SpriteRenderer));
        _plot = _plotObject.AddComponent<FarmPlot>();
        _plot.ConfigureForTests("farm.test.plot.01", _plotObject.GetComponent<SpriteRenderer>());
        GameObject managerObject = new("FarmingManagerTest");
        _manager = managerObject.AddComponent<FarmingManager>();
        _manager.ConfigureForTests(_catalog, _plot);

        _playerObject = new GameObject("FarmingPlayer");
        _playerObject.tag = "Player";
    }

    [TearDown]
    public void TearDown()
    {
        if (_manager != null) Object.DestroyImmediate(_manager.gameObject);
        Object.DestroyImmediate(_plotObject);
        Object.DestroyImmediate(_playerObject);
        Object.DestroyImmediate(_inventoryObject);
        Object.DestroyImmediate(_seed);
        Object.DestroyImmediate(_harvest);
        Object.DestroyImmediate(_crop);
        Object.DestroyImmediate(_catalog);
    }

    [Test]
    public void Plant_ConsumesOneSeedAndPersistsCropIdentity()
    {
        InventoryManager.Instance.AddItem(_seed, 2);

        Assert.IsTrue(_plot.TryInteract(_seed));
        Assert.AreEqual(1, InventoryManager.Instance.GetTotalQuantity(_seed.itemId));
        FarmingSaveData saved = _manager.ToSaveData();
        Assert.AreEqual(1, saved.plots.Count);
        Assert.AreEqual("farm.test.plot.01", saved.plots[0].plotId);
        Assert.AreEqual("crop.test", saved.plots[0].cropId);
        Assert.Greater(saved.plots[0].plantedAtUtcTicks, 0);
    }

    [UnityTest]
    public IEnumerator MatureHarvest_GrantsAfterFlyAndReturnsPlotToEmpty()
    {
        _plot.RestoreCrop(_crop, DateTime.UtcNow.AddSeconds(-1).Ticks);
        Assert.IsTrue(_plot.IsMature);
        Assert.IsTrue(_plot.TryInteract(null));
        Assert.AreEqual(0, InventoryManager.Instance.GetTotalQuantity(_harvest.itemId));

        yield return new WaitForSecondsRealtime(1.1f);

        Assert.AreEqual(2, InventoryManager.Instance.GetTotalQuantity(_harvest.itemId));
        Assert.IsFalse(_plot.HasCrop);
        Assert.AreEqual(0, _manager.ToSaveData().plots.Count);
    }

    [Test]
    public void Restore_UnknownPlotOrCropIsReportedWithoutThrowing()
    {
        var saved = new FarmingSaveData();
        saved.plots.Add(new FarmPlotSaveData { plotId = "farm.missing.plot", cropId = "crop.test", plantedAtUtcTicks = 1 });
        saved.plots.Add(new FarmPlotSaveData { plotId = "farm.test.plot.01", cropId = "crop.missing", plantedAtUtcTicks = 1 });
        var missingPlots = new System.Collections.Generic.List<string>();
        var missingCrops = new System.Collections.Generic.List<string>();

        _manager.RestoreState(saved, missingPlots, missingCrops);

        CollectionAssert.AreEqual(new[] { "farm.missing.plot" }, missingPlots);
        CollectionAssert.AreEqual(new[] { "crop.missing" }, missingCrops);
        Assert.IsFalse(_plot.HasCrop);
    }
}
