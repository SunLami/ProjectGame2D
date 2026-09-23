using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Adds quick-bar behavior on top of the authored HUD at runtime. The HUD prefab remains untouched,
/// so installing gameplay logic cannot replace or reserialize its visual skin.
/// </summary>
public sealed class QuickBarHudRuntimeInstaller : MonoBehaviour
{
    private Coroutine _bindingRoutine;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        QueueBinding();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (_bindingRoutine != null) StopCoroutine(_bindingRoutine);
        _bindingRoutine = null;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => QueueBinding();

    private void QueueBinding()
    {
        if (_bindingRoutine != null) StopCoroutine(_bindingRoutine);
        _bindingRoutine = StartCoroutine(BindWhenHudIsReady());
    }

    private IEnumerator BindWhenHudIsReady()
    {
        const int maxFrames = 120;
        for (int frame = 0; frame < maxFrames; frame++)
        {
            if (TryInstall())
            {
                _bindingRoutine = null;
                yield break;
            }
            yield return null;
        }

        _bindingRoutine = null;
        Debug.LogWarning("Quick bar could not find the authored QuickSlot1Key..QuickSlot8Key HUD anchors.", this);
    }

    private static bool TryInstall()
    {
        TextMeshProUGUI[] texts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include);
        var keys = new TextMeshProUGUI[QuickBarSaveData.SlotCount];
        foreach (TextMeshProUGUI text in texts)
        {
            for (int i = 0; i < keys.Length; i++)
                if (text.name == $"QuickSlot{i + 1}Key") keys[i] = text;
        }

        for (int i = 0; i < keys.Length; i++)
            if (keys[i] == null) return false;

        Transform bottomHud = keys[0].transform.parent;
        for (int i = 0; i < keys.Length; i++)
        {
            if (keys[i].transform.parent != bottomHud) return false;
            InstallSlot(bottomHud, keys[i], i);
        }
        return true;
    }

    private static void InstallSlot(Transform bottomHud, TextMeshProUGUI key, int index)
    {
        string overlayName = $"QuickBarSlot{index + 1}Runtime";
        if (bottomHud.Find(overlayName) != null) return;

        RectTransform keyRect = key.rectTransform;
        GameObject overlay = new(overlayName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.SetParent(bottomHud, false);
        overlayRect.anchorMin = keyRect.anchorMin;
        overlayRect.anchorMax = keyRect.anchorMax;
        overlayRect.pivot = new Vector2(0.5f, 0.5f);
        overlayRect.anchoredPosition = keyRect.anchoredPosition + new Vector2(0f, -14f);
        overlayRect.sizeDelta = new Vector2(48f, 48f);
        overlay.transform.SetSiblingIndex(key.transform.GetSiblingIndex());

        Image hitArea = overlay.GetComponent<Image>();
        hitArea.color = new Color(1f, 1f, 1f, 0.001f);
        hitArea.raycastTarget = true;

        Image selection = CreateImage("Selection", overlay.transform);
        Stretch(selection.rectTransform);
        selection.color = new Color(1f, 0.78f, 0.2f, 0.24f);

        Image icon = CreateImage("ItemIcon", overlay.transform);
        Place(icon.rectTransform, 0f, -1f, 31f, 31f);
        icon.preserveAspect = true;

        GameObject quantityObject = new("Quantity", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform quantityRect = quantityObject.GetComponent<RectTransform>();
        quantityRect.SetParent(overlay.transform, false);
        Place(quantityRect, 13f, -15f, 22f, 14f);
        TextMeshProUGUI quantity = quantityObject.GetComponent<TextMeshProUGUI>();
        quantity.font = key.font;
        quantity.fontSharedMaterial = key.fontSharedMaterial;
        quantity.text = string.Empty;
        quantity.fontSize = 9f;
        quantity.alignment = TextAlignmentOptions.BottomRight;
        quantity.raycastTarget = false;
        quantity.color = Color.white;

        QuickBarSlotUI slot = overlay.AddComponent<QuickBarSlotUI>();
        slot.Configure(index, icon, quantity, selection);
    }

    private static Image CreateImage(string name, Transform parent)
    {
        GameObject child = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        child.transform.SetParent(parent, false);
        Image image = child.GetComponent<Image>();
        image.raycastTarget = false;
        return image;
    }

    private static void Place(RectTransform rect, float x, float y, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
