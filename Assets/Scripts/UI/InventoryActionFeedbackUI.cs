using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Small, non-blocking toast for inventory action failures (e.g. "requires level X to
/// equip"). Unlike WorldMessagePopupUI (modal, pauses into GameState.Dialogue), this never blocks
/// input or the Inventory window -- it just fades itself out after a short delay. Builds its own
/// Canvas at runtime, same self-contained pattern as WorldMessagePopupUI.Build(), so no prefab or
/// Inspector wiring is needed.</summary>
public sealed class InventoryActionFeedbackUI : MonoBehaviour
{
    private const float DisplaySeconds = 1.8f;

    private static InventoryActionFeedbackUI _instance;
    private TMP_Text _text;
    private float _hideAtUnscaledTime;

    public static void Show(string message)
    {
        if (_instance == null)
            _instance = Build();

        _instance.gameObject.SetActive(true);
        SoundFXManager.PlaySfx(SfxIds.UiNotification);
        _instance._text.text = message;
        _instance._hideAtUnscaledTime = Time.unscaledTime + DisplaySeconds;
        _instance.transform.SetAsLastSibling();
    }

    private static InventoryActionFeedbackUI Build()
    {
        GameObject root = new("InventoryActionFeedbackUI", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(InventoryActionFeedbackUI));
        DontDestroyOnLoad(root);

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1500; // above the Inventory window and WorldMessagePopupUI (1000).

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        InventoryActionFeedbackUI feedback = root.GetComponent<InventoryActionFeedbackUI>();
        feedback.BuildPresentation(root.transform);
        return feedback;
    }

    private void BuildPresentation(Transform parent)
    {
        GameObject boardObject = new("Board", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        boardObject.transform.SetParent(parent, false);
        RectTransform boardRect = (RectTransform)boardObject.transform;
        boardRect.anchorMin = boardRect.anchorMax = boardRect.pivot = new Vector2(0.5f, 1f);
        boardRect.sizeDelta = new Vector2(480f, 44f);
        boardRect.anchoredPosition = new Vector2(0f, -140f);
        Image board = boardObject.GetComponent<Image>();
        board.color = new Color(0.08f, 0.05f, 0.04f, 0.92f);
        board.raycastTarget = false;

        GameObject textObject = new("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(boardRect, false);
        RectTransform textRect = (RectTransform)textObject.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(14f, 2f);
        textRect.offsetMax = new Vector2(-14f, -2f);

        _text = textObject.GetComponent<TextMeshProUGUI>();
        _text.font = TMP_Settings.defaultFontAsset;
        _text.fontSize = 20f;
        _text.color = new Color(1f, 0.55f, 0.35f, 1f);
        _text.alignment = TextAlignmentOptions.Center;
        _text.raycastTarget = false;
    }

    private void Update()
    {
        if (gameObject.activeSelf && Time.unscaledTime >= _hideAtUnscaledTime)
            gameObject.SetActive(false);
    }
}
