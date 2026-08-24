using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class WorldObjectRegistryPlayModeTests
{
    private GameObject _root;
    private WorldObjectRegistry _registry;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("WorldObjectRegistryFixture");
        _registry = _root.AddComponent<WorldObjectRegistry>();
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(_root);

    private sealed class FakeWorldObject : IPersistentWorldObject
    {
        public FakeWorldObject(string persistentId, WorldObjectKind kind) { PersistentId = persistentId; Kind = kind; }
        public string PersistentId { get; }
        public WorldObjectKind Kind { get; }
        public bool RestoredCalled { get; private set; }
        public WorldObjectState LastRestoredState { get; private set; }
        private bool _flag;
        private long _ticks;
        public void SetState(bool flag, long ticks) { _flag = flag; _ticks = ticks; }
        public WorldObjectState CaptureState() => new(_flag, _ticks);
        public void RestoreState(WorldObjectState state) { RestoredCalled = true; LastRestoredState = state; _flag = state.Flag; _ticks = state.NextRespawnUtcTicks; }
    }

    [Test]
    public void ToSaveData_CapturesEveryRegisteredObject()
    {
        var chest = new FakeWorldObject("world.chest.01", WorldObjectKind.Chest);
        chest.SetState(true, 0);
        var node = new FakeWorldObject("world.resource.01", WorldObjectKind.ResourceNode);
        node.SetState(false, 12345L);
        _registry.ConfigureForTests(new IPersistentWorldObject[] { chest, node });

        WorldSaveData data = _registry.ToSaveData();

        Assert.AreEqual(2, data.objects.Count);
        Assert.AreEqual("world.chest.01", data.objects[0].persistentId);
        Assert.IsTrue(data.objects[0].flag);
        Assert.AreEqual("world.resource.01", data.objects[1].persistentId);
        Assert.AreEqual(12345L, data.objects[1].nextRespawnUtcTicks);
    }

    [Test]
    public void RestoreState_AppliesToMatchingObjectOnly()
    {
        var chest = new FakeWorldObject("world.chest.01", WorldObjectKind.Chest);
        var pickup = new FakeWorldObject("world.pickup.01", WorldObjectKind.UniquePickup);
        _registry.ConfigureForTests(new IPersistentWorldObject[] { chest, pickup });

        var data = new WorldSaveData();
        data.objects.Add(new WorldObjectSaveData { persistentId = "world.chest.01", flag = true });

        _registry.RestoreState(data);

        Assert.IsTrue(chest.RestoredCalled);
        Assert.IsTrue(chest.LastRestoredState.Flag);
        Assert.IsFalse(pickup.RestoredCalled, "An object with no matching save record must not be touched.");
    }

    [Test]
    public void RestoreState_UnknownPersistentId_ReportedNotThrown()
    {
        var chest = new FakeWorldObject("world.chest.01", WorldObjectKind.Chest);
        _registry.ConfigureForTests(new IPersistentWorldObject[] { chest });

        var data = new WorldSaveData();
        data.objects.Add(new WorldObjectSaveData { persistentId = "world.chest.removed_content", flag = true });

        List<string> missing = new();
        Assert.DoesNotThrow(() => _registry.RestoreState(data, missing));
        Assert.AreEqual(1, missing.Count);
        Assert.AreEqual("world.chest.removed_content", missing[0]);
        Assert.IsFalse(chest.RestoredCalled);
    }

    [Test]
    public void RestoreState_IsIdempotent()
    {
        var chest = new FakeWorldObject("world.chest.01", WorldObjectKind.Chest);
        _registry.ConfigureForTests(new IPersistentWorldObject[] { chest });

        var data = new WorldSaveData();
        data.objects.Add(new WorldObjectSaveData { persistentId = "world.chest.01", flag = true });

        _registry.RestoreState(data);
        _registry.RestoreState(data);

        Assert.IsTrue(chest.LastRestoredState.Flag);
        Assert.AreEqual(1, _registry.ToSaveData().objects.Count);
    }

    [Test]
    public void DuplicatePersistentId_KeepsFirstEntryOnly()
    {
        var first = new FakeWorldObject("world.chest.01", WorldObjectKind.Chest);
        var duplicate = new FakeWorldObject("world.chest.01", WorldObjectKind.Chest);
        LogAssert.Expect(LogType.Error, new Regex("duplicate persistentId"));
        _registry.ConfigureForTests(new IPersistentWorldObject[] { first, duplicate });

        Assert.AreEqual(1, _registry.Objects.Count);
        Assert.AreEqual(1, _registry.ToSaveData().objects.Count);
    }
}
