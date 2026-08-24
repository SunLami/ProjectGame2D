using NUnit.Framework;
using UnityEngine;

public sealed class WorldSaveDataTests
{
    [Test]
    public void RoundTripsWorldObjectRecords()
    {
        WorldSaveData data = new();
        data.objects.Add(new WorldObjectSaveData
        {
            persistentId = "world.chest.town.blacksmith.01",
            kind = WorldObjectKind.Chest,
            flag = true,
            nextRespawnUtcTicks = 0
        });
        data.objects.Add(new WorldObjectSaveData
        {
            persistentId = "world.resource.forest.log.01",
            kind = WorldObjectKind.ResourceNode,
            flag = false,
            nextRespawnUtcTicks = 638000000000000000L
        });

        string json = JsonUtility.ToJson(data);
        WorldSaveData loaded = JsonUtility.FromJson<WorldSaveData>(json);

        Assert.AreEqual(2, loaded.objects.Count);
        Assert.AreEqual("world.chest.town.blacksmith.01", loaded.objects[0].persistentId);
        Assert.AreEqual(WorldObjectKind.Chest, loaded.objects[0].kind);
        Assert.IsTrue(loaded.objects[0].flag);
        Assert.AreEqual(638000000000000000L, loaded.objects[1].nextRespawnUtcTicks);
    }

    [Test]
    public void GameSaveData_RoundTripsWorld()
    {
        GameSaveData data = new() { saveId = "s1", world = new WorldSaveData() };
        data.world.objects.Add(new WorldObjectSaveData { persistentId = "world.boss.forest.guardian.01", kind = WorldObjectKind.Boss, flag = true });

        string json = JsonUtility.ToJson(data);
        GameSaveData loaded = JsonUtility.FromJson<GameSaveData>(json);

        Assert.AreEqual(GameSaveData.CurrentSaveVersion, loaded.saveVersion);
        Assert.AreEqual(1, loaded.world.objects.Count);
        Assert.AreEqual(WorldObjectKind.Boss, loaded.world.objects[0].kind);
        Assert.IsTrue(loaded.world.objects[0].flag);
    }
}
