using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerHUDApprovedTests
{
    [Test]
    public void PrefabHasFourSlotsFillOnlyVitalsAndBottomLeftAnchor()
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/Gameplay/HUD/PlayerHUD.prefab");
        var rect = root.GetComponent<RectTransform>();
        Assert.AreEqual(Vector2.zero, rect.anchorMin); Assert.AreEqual(Vector2.zero, rect.anchorMax);
        Assert.AreEqual(Vector2.zero, rect.pivot);
        Assert.That(rect.anchoredPosition.x * 2.4f, Is.EqualTo(32).Within(.01f));
        Assert.That(rect.sizeDelta.x / rect.sizeDelta.y, Is.EqualTo(3).Within(.001f));
        Assert.That(rect.localScale.x, Is.EqualTo(.72f).Within(.001f));
        Assert.That(rect.localScale.y, Is.EqualTo(rect.localScale.x).Within(.001f));
        Assert.That(rect.sizeDelta.x * rect.localScale.x * 2.4f, Is.EqualTo(587.52f).Within(.1f));
        var slots = root.GetComponentsInChildren<QuickBarSlotUI>(true);
        Assert.AreEqual(4, slots.Length);
        CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3 }, slots.Select(s => new SerializedObject(s).FindProperty("_slotIndex").intValue));
        Assert.That(root.GetComponentsInChildren<Transform>(true).Any(t => t.name.EndsWith("Text") &&
            (t.name.Contains("Health") || t.name.Contains("Mana") || t.name.Contains("Stamina"))), Is.False);
        foreach (string name in new[] { "HealthFill", "ManaFill", "StaminaFill", "ExperienceFill" })
        {
            var image = root.GetComponentsInChildren<Image>(true).Single(x => x.name == name);
            Assert.NotNull(image.sprite); Assert.AreEqual(Image.Type.Filled, image.type);
            Assert.AreEqual(Image.FillMethod.Horizontal, image.fillMethod); Assert.IsFalse(image.raycastTarget);
        }
        Assert.IsFalse(root.GetComponentsInChildren<Image>(true).Single(x => x.name == "Frame").raycastTarget);
        Assert.AreEqual(4, root.GetComponentsInChildren<PlayerHUDMeterMask>(true).Length);
        foreach (var mask in root.GetComponentsInChildren<PlayerHUDMeterMask>(true))
        {
            Assert.IsFalse(mask.raycastTarget);
            Assert.IsFalse(mask.GetComponent<Mask>().showMaskGraphic);
        }
        Assert.IsTrue(new SerializedObject(root.GetComponent<PlayerExperienceBarController>()).FindProperty("_showLabel").boolValue);
        Assert.IsFalse(root.GetComponentsInChildren<Transform>(true).Any(t => t.name == "ExperienceLabel"));
        var unified = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/UI/Gameplay/UnifiedHUD/UnifiedGameplayHUD.prefab");
        Assert.IsNull(unified.transform.Find("BottomHUD"));
        foreach (string name in new[] { "CharacterPopup", "MapPopup", "Minimap" }) Assert.NotNull(unified.transform.Find(name));
    }

    [Test]
    public void V11MigrationKeepsFourAssignmentsAndInventoryButResetsLegacySelection()
    {
        var inventory = new InventorySaveData { gold = 1234 };
        var data = new GameSaveData { saveVersion = 11, inventory = inventory, quickBar = new QuickBarSaveData
        { selectedIndex = 7, assignedItemIds = new List<string> { "a", "b", "c", "d", "e", "f", "g", "h" } } };
        SaveMigration.Migrate(data);
        Assert.AreEqual(GameSaveData.CurrentSaveVersion, data.saveVersion); Assert.AreSame(inventory, data.inventory); Assert.AreEqual(1234, data.inventory.gold);
        CollectionAssert.AreEqual(new[] { "a", "b", "c", "d" }, data.quickBar.assignedItemIds);
        Assert.AreEqual(0, data.quickBar.selectedIndex);
        SaveMigration.Migrate(data);
        Assert.AreEqual(4, data.quickBar.assignedItemIds.Count);
    }

    [Test]
    public void V11MigrationPadsMissingAssignmentsAndRetainsValidSelection()
    {
        var data = new GameSaveData { saveVersion = 11, quickBar = new QuickBarSaveData
        { selectedIndex = 2, assignedItemIds = new List<string> { "a" } } };
        SaveMigration.Migrate(data);
        Assert.AreEqual(4, data.quickBar.assignedItemIds.Count); Assert.AreEqual("a", data.quickBar.assignedItemIds[0]);
        Assert.AreEqual(2, data.quickBar.selectedIndex);
        var empty = new GameSaveData { saveVersion = 11 };
        SaveMigration.Migrate(empty); Assert.AreEqual(4, empty.quickBar.assignedItemIds.Count);
    }

    [Test]
    public void ManaConsumesAtomicallyRejectsNonFiniteAndRestoresWithClamp()
    {
        var go = new GameObject("ManaTest");
        try
        {
            var stat = go.AddComponent<PlayerStat>();
            typeof(PlayerStat).GetField("_mana", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(stat, 100f);
            typeof(PlayerStat).GetField("_health", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(stat, 100f);
            int events = 0; stat.OnManaChanged += (current, max) => events++;
            Assert.IsTrue(stat.TryConsumeMana(30)); Assert.AreEqual(70f, stat.Mana);
            Assert.IsFalse(stat.TryConsumeMana(71)); Assert.AreEqual(70f, stat.Mana);
            Assert.IsFalse(stat.TryConsumeMana(float.NaN)); Assert.IsFalse(stat.TryConsumeMana(float.PositiveInfinity));
            stat.RestoreMana(1000); Assert.AreEqual(stat.MaxMana, stat.Mana); Assert.AreEqual(2, events);
        }
        finally { Object.DestroyImmediate(go); }
    }
}
