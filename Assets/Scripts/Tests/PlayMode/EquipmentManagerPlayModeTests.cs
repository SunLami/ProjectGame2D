using NUnit.Framework;
using UnityEngine;

// Uses non-visual slots (Ring/Necklace/Shield) throughout so tests don't need real SpriteLibrary
// components wired up -- ApplyVisual/ClearVisual only handle Head/Body/Weapon, which is existing
// presentation behavior outside Phase 4's scope.
public sealed class EquipmentManagerPlayModeTests
{
    private GameObject _root;
    private EquipmentItemSO _ring;
    private EquipmentItemSO _necklace;

    [SetUp]
    public void SetUp()
    {
        _root = new GameObject("EquipmentManagerFixture");
        _root.AddComponent<InventoryManager>();
        _root.AddComponent<PlayerStat>();
        _root.AddComponent<EquipmentManager>();

        _ring = ScriptableObject.CreateInstance<EquipmentItemSO>();
        _ring.itemId = "test.ring";
        _ring.slot = EquipSlot.Ring;
        _ring.isStackable = false;
        _ring.maxStackSize = 1;
        _ring.statModifiers = new PlayerStatModifiers { attackDamage = 5f };

        _necklace = ScriptableObject.CreateInstance<EquipmentItemSO>();
        _necklace.itemId = "test.necklace";
        _necklace.slot = EquipSlot.Necklace;
        _necklace.isStackable = false;
        _necklace.maxStackSize = 1;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_root);
        Object.DestroyImmediate(_ring);
        Object.DestroyImmediate(_necklace);
    }

    [Test]
    public void Equip_MovesItemFromInventoryToEquippedAndAppliesStat()
    {
        InventoryManager.Instance.AddItem(_ring, 1);
        InventorySlot slot = InventoryManager.Instance.Slots[0];

        Assert.IsTrue(EquipmentManager.Instance.Equip(_ring, slot));
        Assert.AreEqual(_ring, EquipmentManager.Instance.GetEquipped(EquipSlot.Ring));
        Assert.IsTrue(slot.IsEmpty, "Item must leave the inventory slot once equipped.");
        Assert.AreEqual(15f, PlayerStat.Instance.AttackDamage, 0.001f); // base 10 + ring 5
    }

    [Test]
    public void Equip_ReplacingPrevious_ReturnsPreviousToSourceSlotWhenItEmpties()
    {
        var secondRing = ScriptableObject.CreateInstance<EquipmentItemSO>();
        secondRing.itemId = "test.ring2";
        secondRing.slot = EquipSlot.Ring;
        secondRing.isStackable = false;
        secondRing.maxStackSize = 1;

        try
        {
            InventoryManager.Instance.AddItem(_ring, 1);
            InventoryManager.Instance.AddItem(secondRing, 1);
            InventorySlot firstSlot = InventoryManager.Instance.Slots[0];
            InventorySlot secondSlot = InventoryManager.Instance.Slots[1];

            EquipmentManager.Instance.Equip(_ring, firstSlot);
            EquipmentManager.Instance.Equip(secondRing, secondSlot);

            Assert.AreEqual(secondRing, EquipmentManager.Instance.GetEquipped(EquipSlot.Ring));
            Assert.AreEqual(_ring, secondSlot.item, "Replaced ring must return to the slot the new one came from.");
        }
        finally
        {
            Object.DestroyImmediate(secondRing);
        }
    }

    [Test]
    public void Equip_ReplacedItemNeedsAnotherSlotButNoneFree_FailsWithoutLosingAnything()
    {
        // Equip the first ring so EquipSlot.Ring is occupied.
        InventoryManager.Instance.AddItem(_ring, 1);
        EquipmentManager.Instance.Equip(_ring, InventoryManager.Instance.Slots[0]);

        // Real equipment is never stackable, but a stacked source slot is the only way the
        // "replaced item needs a *different* inventory slot" branch can trigger (quantity > 1
        // means the source slot doesn't empty out from taking one) -- exercise it directly.
        var stackableRing = ScriptableObject.CreateInstance<EquipmentItemSO>();
        stackableRing.itemId = "test.ring.stackable";
        stackableRing.slot = EquipSlot.Ring;
        stackableRing.isStackable = true;
        stackableRing.maxStackSize = 5;
        var filler = ScriptableObject.CreateInstance<ItemSO>();
        filler.itemId = "test.filler";
        filler.isStackable = false;
        try
        {
            InventoryManager.Instance.AddItem(stackableRing, 2);
            InventorySlot stackSlot = null;
            foreach (InventorySlot slot in InventoryManager.Instance.Slots)
            {
                if (slot.item == stackableRing) { stackSlot = slot; break; }
            }

            // Fill every other empty slot so there is no room left for the ring being replaced.
            foreach (InventorySlot slot in InventoryManager.Instance.Slots)
            {
                if (slot == stackSlot || !slot.IsEmpty) continue;
                slot.item = filler;
                slot.quantity = 1;
            }

            bool result = EquipmentManager.Instance.Equip(stackableRing, stackSlot);

            Assert.IsFalse(result, "Equip must fail when the replaced item has no inventory slot to return to.");
            Assert.AreEqual(_ring, EquipmentManager.Instance.GetEquipped(EquipSlot.Ring),
                "Original equipped ring must be untouched after a failed equip.");
            Assert.AreEqual(stackableRing, stackSlot.item, "Source slot must be untouched after a failed equip.");
            Assert.AreEqual(2, stackSlot.quantity);
        }
        finally
        {
            Object.DestroyImmediate(stackableRing);
            Object.DestroyImmediate(filler);
        }
    }

    [Test]
    public void Unequip_FullInventory_FailsWithoutLosingItem()
    {
        InventoryManager.Instance.AddItem(_ring, 1);
        InventorySlot slot = InventoryManager.Instance.Slots[0];
        EquipmentManager.Instance.Equip(_ring, slot);

        var filler = ScriptableObject.CreateInstance<ItemSO>();
        filler.itemId = "test.filler";
        filler.isStackable = false;
        try
        {
            InventoryManager.Instance.AddItem(filler, InventoryManager.Instance.Slots.Count);

            bool result = EquipmentManager.Instance.Unequip(EquipSlot.Ring);

            Assert.IsFalse(result, "Unequip must fail when inventory has no room for the item.");
            Assert.AreEqual(_ring, EquipmentManager.Instance.GetEquipped(EquipSlot.Ring));
        }
        finally
        {
            Object.DestroyImmediate(filler);
        }
    }

    [Test]
    public void RestoreEquipped_DoesNotTouchInventory_RecalculateStatsAppliesModifiersOnce()
    {
        EquipmentManager.Instance.RestoreEquipped(EquipSlot.Ring, _ring);
        EquipmentManager.Instance.RestoreEquipped(EquipSlot.Necklace, _necklace);
        EquipmentManager.Instance.RecalculateStats();

        Assert.AreEqual(0, CountUsedSlots(), "RestoreEquipped must never touch inventory slots.");
        Assert.AreEqual(15f, PlayerStat.Instance.AttackDamage, 0.001f);

        // Calling RecalculateStats again (idempotent restore-twice check) must not double-apply.
        EquipmentManager.Instance.RecalculateStats();
        Assert.AreEqual(15f, PlayerStat.Instance.AttackDamage, 0.001f);
    }

    [Test]
    public void ToSaveData_CapturesOnlyOccupiedSlots()
    {
        EquipmentManager.Instance.RestoreEquipped(EquipSlot.Ring, _ring);

        EquipmentSaveData data = EquipmentManager.Instance.ToSaveData();

        Assert.AreEqual(1, data.slots.Count);
        Assert.AreEqual(EquipSlot.Ring, data.slots[0].slot);
        Assert.AreEqual(_ring.itemId, data.slots[0].itemId);
    }

    private static int CountUsedSlots()
    {
        int count = 0;
        foreach (InventorySlot slot in InventoryManager.Instance.Slots)
        {
            if (!slot.IsEmpty) count++;
        }
        return count;
    }
}
