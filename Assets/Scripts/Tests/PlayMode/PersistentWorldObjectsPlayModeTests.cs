using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class PersistentWorldObjectsPlayModeTests
{
    private GameObject _inventoryRoot;
    private readonly List<GameObject> _scratchObjects = new();
    private readonly List<Object> _scratchAssets = new();

    [SetUp]
    public void SetUp()
    {
        _inventoryRoot = new GameObject("InventoryManagerFixture");
        _inventoryRoot.AddComponent<InventoryManager>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_inventoryRoot);
        foreach (GameObject go in _scratchObjects)
            Object.DestroyImmediate(go);
        _scratchObjects.Clear();
        foreach (Object asset in _scratchAssets)
            Object.DestroyImmediate(asset);
        _scratchAssets.Clear();
    }

    private ItemSO MakeItem(string itemId, bool stackable = true, int maxStack = 99)
    {
        var item = ScriptableObject.CreateInstance<ItemSO>();
        item.itemId = itemId;
        item.isStackable = stackable;
        item.maxStackSize = maxStack;
        _scratchAssets.Add(item);
        return item;
    }

    private sealed class FakeItemResolver : IItemResolver
    {
        private readonly Dictionary<string, ItemSO> _map = new();
        public void Register(ItemSO item) => _map[item.itemId] = item;
        public bool TryResolve(string itemId, out ItemSO item) => _map.TryGetValue(itemId ?? "", out item);
    }

    // ---- Chest ----

    [Test]
    public void Chest_TryOpen_GrantsRewardExactlyOnce()
    {
        ItemSO reward = MakeItem("item.reward.gem");
        var resolver = new FakeItemResolver();
        resolver.Register(reward);
        var go = new GameObject("Chest");
        _scratchObjects.Add(go);
        var chest = go.AddComponent<ChestInteractable>();
        chest.ConfigureForTests("world.chest.town.01", "item.reward.gem", 3, resolver);

        Assert.IsTrue(chest.TryOpen(out bool granted));
        Assert.IsTrue(granted);
        Assert.IsTrue(chest.IsOpened);
        Assert.IsTrue(InventoryManager.Instance.HasItem(reward, 3));

        Assert.IsFalse(chest.TryOpen(out bool secondGranted), "A second open must not regrant.");
        Assert.IsFalse(secondGranted);
        Assert.IsTrue(InventoryManager.Instance.HasItem(reward, 3), "Reward count must not double.");
    }

    [Test]
    public void Chest_InsufficientCapacity_StaysClosedAndGrantsNothing()
    {
        ItemSO reward = MakeItem("item.reward.gem", stackable: false, maxStack: 1);
        ItemSO filler = MakeItem("item.filler", stackable: false, maxStack: 1);
        var resolver = new FakeItemResolver();
        resolver.Register(reward);
        var go = new GameObject("Chest");
        _scratchObjects.Add(go);
        var chest = go.AddComponent<ChestInteractable>();
        chest.ConfigureForTests("world.chest.town.01", "item.reward.gem", 1, resolver);
        InventoryManager.Instance.AddItem(filler, InventoryManager.Instance.Slots.Count);

        Assert.IsFalse(chest.TryOpen(out bool granted));
        Assert.IsFalse(granted);
        Assert.IsFalse(chest.IsOpened, "A failed open must stay retryable, not silently consume the attempt.");
    }

    [Test]
    public void Chest_RestoreState_DoesNotGrantReward()
    {
        ItemSO reward = MakeItem("item.reward.gem");
        var resolver = new FakeItemResolver();
        resolver.Register(reward);
        var go = new GameObject("Chest");
        _scratchObjects.Add(go);
        var chest = go.AddComponent<ChestInteractable>();
        chest.ConfigureForTests("world.chest.town.01", "item.reward.gem", 3, resolver);

        chest.RestoreState(new WorldObjectState(true, 0));

        Assert.IsTrue(chest.IsOpened);
        Assert.IsFalse(InventoryManager.Instance.HasItem(reward, 1), "Restore must reproduce state, never grant.");

        // Idempotent: restoring again changes nothing further.
        chest.RestoreState(new WorldObjectState(true, 0));
        Assert.IsFalse(InventoryManager.Instance.HasItem(reward, 1));
    }

    [UnityTest]
    public IEnumerator Chest_TryBeginOpen_CommitsOnlyAfterLootPresentation()
    {
        ItemSO reward = MakeItem("item.reward.animated_chest");
        var resolver = new FakeItemResolver();
        resolver.Register(reward);

        var player = new GameObject("PlayerFixture");
        player.tag = "Player";
        _scratchObjects.Add(player);

        var go = new GameObject("AnimatedChest");
        _scratchObjects.Add(go);
        ChestInteractable chest = go.AddComponent<ChestInteractable>();
        chest.ConfigureForTests("world.chest.animated.01", reward.itemId, 2, resolver);

        Assert.IsTrue(chest.TryBeginOpen());
        Assert.IsTrue(chest.IsOpening);
        Assert.IsFalse(chest.IsOpened);
        Assert.IsFalse(InventoryManager.Instance.HasItem(reward, 1),
            "Reward must not be committed before the loot reaches Player.");

        yield return new WaitForSecondsRealtime(1.1f);

        Assert.IsFalse(chest.IsOpening);
        Assert.IsTrue(chest.IsOpened);
        Assert.IsTrue(InventoryManager.Instance.HasItem(reward, 2));
    }

    // ---- Unique pickup ----

    [Test]
    public void UniquePickup_TryCollect_GrantsOnceAndHides()
    {
        ItemSO relic = MakeItem("item.unique.relic", stackable: false, maxStack: 1);
        var resolver = new FakeItemResolver();
        resolver.Register(relic);
        var go = new GameObject("Pickup");
        _scratchObjects.Add(go);
        var pickup = go.AddComponent<UniquePickupInteractable>();
        pickup.ConfigureForTests("world.pickup.forest.relic.01", "item.unique.relic", 1, resolver);

        Assert.IsTrue(pickup.TryCollect(out bool granted));
        Assert.IsTrue(granted);
        Assert.IsTrue(pickup.IsCollected);
        Assert.IsFalse(go.activeSelf, "Collected pickup must hide itself.");
        Assert.IsTrue(InventoryManager.Instance.HasItem(relic, 1));

        go.SetActive(true); // re-enable manually to prove a second collect still refuses
        Assert.IsFalse(pickup.TryCollect(out bool secondGranted));
        Assert.IsFalse(secondGranted);
        Assert.IsTrue(InventoryManager.Instance.HasItem(relic, 1));
    }

    [Test]
    public void UniquePickup_RestoreCollected_HidesWithoutGranting()
    {
        ItemSO relic = MakeItem("item.unique.relic", stackable: false, maxStack: 1);
        var resolver = new FakeItemResolver();
        resolver.Register(relic);
        var go = new GameObject("Pickup");
        _scratchObjects.Add(go);
        var pickup = go.AddComponent<UniquePickupInteractable>();
        pickup.ConfigureForTests("world.pickup.forest.relic.01", "item.unique.relic", 1, resolver);

        pickup.RestoreState(new WorldObjectState(true, 0));

        Assert.IsFalse(go.activeSelf);
        Assert.IsFalse(InventoryManager.Instance.HasItem(relic, 1));
    }

    // ---- Resource node ----

    [Test]
    public void ResourceNode_TryHarvest_GrantsAndStartsCooldown()
    {
        ItemSO wood = MakeItem("item.material.wood");
        var resolver = new FakeItemResolver();
        resolver.Register(wood);
        var go = new GameObject("ResourceNode");
        _scratchObjects.Add(go);
        var node = go.AddComponent<ResourceNodeInteractable>();
        node.ConfigureForTests("world.resource.forest.log.01", "resource.wood.log", "item.material.wood", 2, 60f, resolver);

        Assert.IsTrue(node.IsAvailable);

        int gatheredCount = 0;
        void OnGathered(string resourceId, int qty, string areaId) => gatheredCount++;
        QuestDomainEvents.ResourceGathered += OnGathered;
        try
        {
            Assert.IsTrue(node.TryHarvest(out bool granted));
            Assert.IsTrue(granted);
            Assert.IsTrue(InventoryManager.Instance.HasItem(wood, 2));
            Assert.AreEqual(1, gatheredCount);
            Assert.IsFalse(node.IsAvailable, "Node must be on cooldown immediately after harvest.");

            Assert.IsFalse(node.TryHarvest(out bool secondGranted));
            Assert.IsFalse(secondGranted);
            Assert.IsTrue(InventoryManager.Instance.HasItem(wood, 2), "On cooldown, a second harvest must not grant again.");
        }
        finally
        {
            QuestDomainEvents.ResourceGathered -= OnGathered;
        }
    }

    [Test]
    public void ResourceNode_RestoreState_ReproducesCooldownWithoutGrantingOrFiringEvents()
    {
        ItemSO wood = MakeItem("item.material.wood");
        var resolver = new FakeItemResolver();
        resolver.Register(wood);
        var go = new GameObject("ResourceNode");
        _scratchObjects.Add(go);
        var node = go.AddComponent<ResourceNodeInteractable>();
        node.ConfigureForTests("world.resource.forest.log.01", "resource.wood.log", "item.material.wood", 2, 60f, resolver);

        int gatheredCount = 0;
        void OnGathered(string resourceId, int qty, string areaId) => gatheredCount++;
        QuestDomainEvents.ResourceGathered += OnGathered;
        try
        {
            long futureTicks = System.DateTime.UtcNow.AddMinutes(5).Ticks;
            node.RestoreState(new WorldObjectState(false, futureTicks));

            Assert.IsFalse(node.IsAvailable);
            Assert.AreEqual(0, gatheredCount);
            Assert.IsFalse(InventoryManager.Instance.HasItem(wood, 1));

            node.RestoreState(new WorldObjectState(false, 0));
            Assert.IsTrue(node.IsAvailable, "Restoring ticks=0 means available again.");
        }
        finally
        {
            QuestDomainEvents.ResourceGathered -= OnGathered;
        }
    }

    [Test]
    public void ResourceNode_RestoreState_HidesVisualAndColliderDuringCooldown()
    {
        var go = new GameObject("ResourceNodePresentation");
        _scratchObjects.Add(go);
        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        BoxCollider2D nodeCollider = go.AddComponent<BoxCollider2D>();

        ResourceNodeInteractable node = go.AddComponent<ResourceNodeInteractable>();
        node.ConfigurePresentationForTests(visual, renderer);

        node.RestoreState(new WorldObjectState(false, System.DateTime.UtcNow.AddMinutes(5).Ticks));
        Assert.IsFalse(visual.activeSelf, "A depleted resource node must disappear during cooldown.");
        Assert.IsFalse(nodeCollider.enabled, "An invisible resource node must not remain targetable.");

        node.RestoreState(new WorldObjectState(false, 0));
        Assert.IsTrue(visual.activeSelf);
        Assert.IsTrue(nodeCollider.enabled);
    }
}
