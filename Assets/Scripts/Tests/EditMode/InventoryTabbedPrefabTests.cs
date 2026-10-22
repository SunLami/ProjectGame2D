using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public sealed class InventoryTabbedPrefabTests
{
    [Test]
    public void DecorationDoesNotBlockEmptyInventoryOrEquipmentDropTargets()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/InventoryUIController.prefab");
        var equipment=prefab.GetComponentsInChildren<Image>(true).First(x=>x.name=="EquipmentPanel");
        Assert.That(equipment.raycastTarget,Is.False);
        foreach(var cell in prefab.GetComponentsInChildren<InventorySlotUI>(true))
            Assert.That(cell.GetComponent<Image>().raycastTarget,Is.True,cell.name);
        foreach(var cell in prefab.GetComponentsInChildren<EquipmentSlotUI>(true))
            Assert.That(cell.GetComponent<Image>().raycastTarget,Is.True,cell.name);
        var layout=prefab.GetComponentInChildren<GridLayoutGroup>(true);
        var scroll=prefab.GetComponentsInChildren<RectTransform>(true).First(x=>x.name=="GridScrollView");
        Assert.That(layout.cellSize.x*6+layout.spacing.x*5+layout.padding.horizontal,
            Is.LessThanOrEqualTo(scroll.sizeDelta.x));
        Assert.That(layout.cellSize.y*5+layout.spacing.y*4+layout.padding.vertical,
            Is.LessThanOrEqualTo(scroll.sizeDelta.y));
    }

    [Test]
    public void InventoryContainsFiveBoundTabsAndFourDistinctQuickAssignments()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/InventoryUIController.prefab");
        var tabs=prefab.GetComponentsInChildren<InventoryCategoryTabUI>(true);
        Assert.That(tabs.Length,Is.EqualTo(5));
        foreach(var tab in tabs)
        {
            var data=new SerializedObject(tab);
            Assert.That(data.FindProperty("_inventory").objectReferenceValue,Is.Not.Null);
            Assert.That(data.FindProperty("_focus").objectReferenceValue,Is.Not.Null);
        }
        var quick=prefab.GetComponentsInChildren<QuickBarSlotUI>(true);
        Assert.That(quick.Length,Is.EqualTo(4));
        Assert.That(quick.Select(q=>new SerializedObject(q).FindProperty("_slotIndex").intValue),Is.EquivalentTo(new[]{0,1,2,3}));
        Assert.That(QuickBarSaveData.SlotCount,Is.EqualTo(4));
    }

    [TestCase("Assets/Resources/Items/Food/Bread.asset",InventoryCategory.Consumption)]
    [TestCase("Assets/Resources/Items/Shop/HealthPotion.asset",InventoryCategory.Consumption)]
    [TestCase("Assets/Resources/Items/Farming/Seed.Carrot.asset",InventoryCategory.Others)]
    [TestCase("Assets/Resources/Items/Fishing/WormBait.asset",InventoryCategory.Others)]
    [TestCase("Assets/Resources/Items/Weapon/SwordLv1.asset",InventoryCategory.Equipment)]
    public void CatalogMetadataMatchesAcceptedCategory(string path,InventoryCategory category)
    {
        Assert.That(AssetDatabase.LoadAssetAtPath<ItemSO>(path).inventoryCategory,Is.EqualTo(category));
    }

    [Test]
    public void ApprovedCompleteFramePreservesAspectAndCloseButtonRetainsCallback()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/InventoryUIController.prefab");
        var board=prefab.GetComponentsInChildren<Image>(true).First(x=>x.name=="InventoryPanel");
        Assert.That(board.type,Is.EqualTo(Image.Type.Simple));
        Assert.That(AssetDatabase.GetAssetPath(board.sprite),Does.EndWith("inventory_complete_frame_approved.png"));
        Assert.That(board.rectTransform.sizeDelta.x / board.rectTransform.sizeDelta.y,Is.EqualTo(1672f/941f).Within(.001f));
        var close=prefab.GetComponentsInChildren<Button>(true).First(x=>x.name=="CloseBtn");
        Assert.That(close.onClick.GetPersistentEventCount(),Is.GreaterThan(0));
    }
}
