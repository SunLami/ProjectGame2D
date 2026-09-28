using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Small modal message popup used by world interactions that need acknowledgement.</summary>
public sealed class WorldMessagePopupUI : MonoBehaviour
{
    private const string BoardResource =
        "UI/SessionUX/DarkInventoryStyle/session_confirmation_board_v3";

    private static WorldMessagePopupUI _instance;
    private bool _ownsDialogueState;

    public static void Show(string message)
    {
        if (GameStateManager.Instance == null
            || GameStateManager.Instance.CurrentState != GameState.Playing)
            return;

        if (_instance == null)
            _instance = Build();
        _instance.Open(message);
    }

    private static WorldMessagePopupUI Build()
    {
        GameObject root = new("WorldMessagePopupUI", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(WorldMessagePopupUI));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        WorldMessagePopupUI popup = root.GetComponent<WorldMessagePopupUI>();
        popup.BuildPresentation(root.transform);
        return popup;
    }

    private void BuildPresentation(Transform parent)
    {
        Image dim = CreateImage("Dim", parent, Color.black * new Color(1f, 1f, 1f, 0.55f));
        Stretch(dim.rectTransform);

        Image board = CreateImage("Board", parent, new Color(0.12f, 0.09f, 0.07f, 0.98f));
        RectTransform boardRect = board.rectTransform;
        boardRect.anchorMin = boardRect.anchorMax = new Vector2(0.5f, 0.5f);
        boardRect.sizeDelta = new Vector2(480f, 224f);
        Sprite[] boardSprites = Resources.LoadAll<Sprite>(BoardResource);
        if (boardSprites.Length > 0)
        {
            board.sprite = boardSprites[0];
            board.preserveAspect = true;
        }

        TMP_Text title = CreateText("Title", board.transform, "AREA UNAVAILABLE", 28f,
            new Color(1f, 0.83f, 0.34f, 1f));
        SetRect(title.rectTransform, new Vector2(0f, 55f), new Vector2(380f, 44f));

        TMP_Text body = CreateText("Message", board.transform, string.Empty, 25f,
            new Color(0.94f, 0.9f, 0.78f, 1f));
        SetRect(body.rectTransform, new Vector2(0f, 5f), new Vector2(380f, 52f));

        Image buttonImage = CreateImage("OkButton", board.transform,
            new Color(0.05f, 0.22f, 0.43f, 1f));
        SetRect(buttonImage.rectTransform, new Vector2(0f, -62f), new Vector2(130f, 40f));
        Button button = buttonImage.gameObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(Close);
        TMP_Text label = CreateText("Label", button.transform, "OK", 20f, Color.white);
        Stretch(label.rectTransform);

        _messageText = body;
    }

    private TMP_Text _messageText;

    private void Open(string message)
    {
        _messageText.text = message;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        GameStateManager.Instance.PushState(GameState.Dialogue);
        _ownsDialogueState = true;
    }

    private void Update()
    {
        if (!_ownsDialogueState)
            return;

        bool close = Keyboard.current != null
            && (Keyboard.current.escapeKey.wasPressedThisFrame
                || Keyboard.current.enterKey.wasPressedThisFrame
                || Keyboard.current.spaceKey.wasPressedThisFrame);
        close |= Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame;
        if (close)
            Close();
    }

    public void Close()
    {
        if (!_ownsDialogueState)
            return;

        _ownsDialogueState = false;
        if (GameStateManager.Instance != null
            && GameStateManager.Instance.CurrentState == GameState.Dialogue)
            GameStateManager.Instance.ReturnToPreviousState();
        Destroy(gameObject);
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        GameObject child = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        child.transform.SetParent(parent, false);
        Image image = child.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static TMP_Text CreateText(string name, Transform parent, string value, float size,
        Color color)
    {
        GameObject child = new(name, typeof(RectTransform), typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));
        child.transform.SetParent(parent, false);
        TMP_Text text = child.GetComponent<TMP_Text>();
        text.text = value;
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        return text;
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }
}
