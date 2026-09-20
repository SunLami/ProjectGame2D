using NUnit.Framework;
using UnityEngine;

public sealed class InventoryEquipmentSaveDataTests
{
    [Test]
    public void InventorySaveData_RoundTripsSlotsAndGold()
    {
        InventorySaveData data = new();
        data.gold = 250;
        data.slots.Add(new InventorySaveData.SlotData { itemId = "sword_lvl1", quantity = 1 });
        data.slots.Add(new InventorySaveData.SlotData { itemId = null, quantity = 0 });

        string json = JsonUtility.ToJson(data);
        InventorySaveData loaded = JsonUtility.FromJson<InventorySaveData>(json);

        Assert.AreEqual(250, loaded.gold);
        Assert.AreEqual(2, loaded.slots.Count);
        Assert.AreEqual("sword_lvl1", loaded.slots[0].itemId);
        Assert.AreEqual(1, loaded.slots[0].quantity);
    }

    [Test]
    public void InventorySaveData_RoundTripsUniqueFishInstance()
    {
        InventorySaveData data = new();
        data.slots.Add(new InventorySaveData.SlotData
        {
            itemId = "fish.river.minnow",
            quantity = 1,
            fish = new FishInstanceData { instanceId = "catch-001", weightGrams = 475 }
        });

        InventorySaveData loaded = JsonUtility.FromJson<InventorySaveData>(JsonUtility.ToJson(data));

        Assert.AreEqual("fish.river.minnow", loaded.slots[0].itemId);
        Assert.AreEqual("catch-001", loaded.slots[0].fish.instanceId);
        Assert.AreEqual(475, loaded.slots[0].fish.weightGrams);
    }

    [Test]
    public void EquipmentSaveData_RoundTripsSlots()
    {
        EquipmentSaveData data = new();
        data.slots.Add(new EquipmentSaveData.SlotData { slot = EquipSlot.Weapon, itemId = "sword_lvl1" });

        string json = JsonUtility.ToJson(data);
        EquipmentSaveData loaded = JsonUtility.FromJson<EquipmentSaveData>(json);

        Assert.AreEqual(1, loaded.slots.Count);
        Assert.AreEqual(EquipSlot.Weapon, loaded.slots[0].slot);
        Assert.AreEqual("sword_lvl1", loaded.slots[0].itemId);
    }

    [Test]
    public void GameSaveData_RoundTripsInventoryAndEquipment()
    {
        GameSaveData data = new()
        {
            saveId = "save-1",
            inventory = new InventorySaveData { gold = 10 },
            equipment = new EquipmentSaveData()
        };
        data.inventory.slots.Add(new InventorySaveData.SlotData { itemId = "sword_lvl1", quantity = 1 });
        data.equipment.slots.Add(new EquipmentSaveData.SlotData { slot = EquipSlot.Weapon, itemId = "sword_lvl1" });

        string json = JsonUtility.ToJson(data);
        GameSaveData loaded = JsonUtility.FromJson<GameSaveData>(json);

        Assert.AreEqual(GameSaveData.CurrentSaveVersion, loaded.saveVersion);
        Assert.AreEqual(10, loaded.inventory.gold);
        Assert.AreEqual("sword_lvl1", loaded.inventory.slots[0].itemId);
        Assert.AreEqual(EquipSlot.Weapon, loaded.equipment.slots[0].slot);
    }
}
