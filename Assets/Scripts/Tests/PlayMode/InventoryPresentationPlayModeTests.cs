#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class InventoryPresentationPlayModeTests
{
    [UnityTest]
    public IEnumerator SourcePrefabFiltersRealSlotsAndEmptyDropTargetsReceivePointer()
    {
        // Bootstrap can already own the singleton in a PlayMode test session. Reuse and
        // restore its in-memory state instead of creating an uninitialized duplicate manager.
        GameObject managerObject=null;
        var inventory=InventoryManager.Instance;
        if(inventory==null)
        {
            managerObject=new GameObject("InventoryPresentationTestManager");
            inventory=managerObject.AddComponent<InventoryManager>();
        }
        var originalState=inventory.ToSaveData();
        inventory.LoadFromSaveData(new InventorySaveData(),new ResourcesItemResolver());
        var canvasObject=new GameObject("InventoryPresentationTestCanvas",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));
        canvasObject.SetActive(false);
        canvasObject.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        canvasObject.GetComponent<Canvas>().sortingOrder=1000;
        var eventObject=new GameObject("InventoryPresentationTestEvents",typeof(EventSystem));
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/InventoryUIController.prefab");
        var instance=Object.Instantiate(prefab,canvasObject.transform,false);
        var resource=Resources.Load<ItemSO>("Items/Farming/Seed.Carrot");
        var weapon=Resources.Load<ItemSO>("Items/Weapon/SwordLv1");
        try
        {
            Assert.That(inventory.Slots,Is.Not.Null,"Manager initialized");
            Assert.That(resource,Is.Not.Null);
            Assert.That(weapon,Is.Not.Null);
            Assert.That(inventory.AddItem(resource,3),Is.True);
            Assert.That(inventory.AddItem(weapon,1),Is.True);
            canvasObject.SetActive(true);
            instance.GetComponentsInChildren<Transform>(true).First(x=>x.name=="InventoryWindow").gameObject.SetActive(true);
            yield return null;
            var ui=instance.GetComponentInChildren<InventoryUI>(true);
            Assert.That(ui,Is.Not.Null);
            var snapshot=JsonUtility.ToJson(inventory.ToSaveData());
            ui.SelectCategory(InventoryCategory.Resources);
            var cells=ui.GetComponentsInChildren<InventorySlotUI>(false);
            var occupied=cells.Where(x=>x.Item!=null).ToArray();
            Assert.That(occupied.Length,Is.EqualTo(1));
            Assert.That(occupied[0].Item,Is.SameAs(resource));
            Assert.That(inventory.Slots.Contains(occupied[0].Slot),Is.True);
            var filler=cells.First(x=>x.Slot==null);
            filler.OnDrop(new PointerEventData(eventObject.GetComponent<EventSystem>()){pointerDrag=occupied[0].gameObject});
            Assert.That(JsonUtility.ToJson(inventory.ToSaveData()),Is.EqualTo(snapshot));

            ui.SelectCategory(InventoryCategory.All);
            Canvas.ForceUpdateCanvases();
            yield return null;
            var empty=ui.GetComponentsInChildren<InventorySlotUI>(false).First(x=>x.Slot!=null&&x.Slot.IsEmpty);
            var pointer=new PointerEventData(eventObject.GetComponent<EventSystem>())
            {position=RectTransformUtility.WorldToScreenPoint(null,empty.transform.position)};
            var hits=new System.Collections.Generic.List<RaycastResult>();
            eventObject.GetComponent<EventSystem>().RaycastAll(pointer,hits);
            Assert.That(hits.Count,Is.GreaterThan(0));
            Assert.That(hits[0].gameObject.GetComponentInParent<InventorySlotUI>(),Is.SameAs(empty));

            var scroll=ui.GetComponentInChildren<ScrollRect>();
            Assert.That(scroll,Is.Not.Null);
            scroll.verticalNormalizedPosition=0;
            ui.gameObject.SetActive(false);
            ui.gameObject.SetActive(true);
            yield return null;
            yield return null;
            Assert.That(scroll.verticalNormalizedPosition,Is.EqualTo(1).Within(.001f));
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
            Object.DestroyImmediate(eventObject);
            if(managerObject!=null) Object.DestroyImmediate(managerObject);
            else inventory.LoadFromSaveData(originalState,new ResourcesItemResolver());
        }
    }
}
#endif
