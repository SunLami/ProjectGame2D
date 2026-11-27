using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Map-selection modal opened by a <see cref="TeleportPillarInteractable"/> (D-089). Builds its own canvas
/// in code in the Dark Light Fantasy palette (charcoal / walnut / antique gold, sapphire primary button).
/// The player picks a destination card and confirms with Teleport; locked cards read "Coming soon".
/// Pushes GameState.Dialogue while open (same convention as QuestAcceptPopupUI) so gameplay input is off.
/// </summary>
public sealed class BossTeleportSelectUI : MonoBehaviour
{
    private static readonly Color Charcoal = new Color(0.09f, 0.08f, 0.07f, 0.97f);
    private static readonly Color Walnut = new Color(0.17f, 0.12f, 0.09f, 1f);
    private static readonly Color Gold = new Color(0.78f, 0.62f, 0.28f, 1f);
    private static readonly Color GoldDim = new Color(0.78f, 0.62f, 0.28f, 0.35f);
    private static readonly Color Ivory = new Color(0.96f, 0.92f, 0.82f, 1f);
    private static readonly Color WarmGray = new Color(0.66f, 0.62f, 0.56f, 1f);
    private static readonly Vector2 StandardPanelSize = new(1180f, 700f);
    private static readonly Vector2 SingleDestinationPanelSize = new(620f, 700f);
    private static readonly Color Sapphire = new Color(0.12f, 0.22f, 0.5f, 1f);
    private static readonly Color Disabled = new Color(0.2f, 0.2f, 0.22f, 1f);
    private static readonly Color Available = new Color(0.45f, 0.85f, 0.5f, 1f);

    private sealed class Card
    {
        public TeleportDestination Destination;
        public Image Border;
        public Outline Selection;
    }

    private GameObject _root;
    private RectTransform _cardContainer;
    private RectTransform _panelFrame;
    private Sprite _cardArtwork;
    private bool _hasConfirmArtwork;
    private Button _confirmButton;
    private Image _confirmImage;
    private TMP_Text _confirmLabel;
    private TMP_Text _titleLabel;
    private TMP_Text _subtitleLabel;
    private readonly List<Card> _cards = new List<Card>();
    private Card _selected;
    private Action<TeleportDestination> _onChosen;
    private bool _ownsState;

    public bool IsOpen => _root != null && _root.activeSelf;

    private void Awake()
    {
        Build();
        _root.SetActive(false);
    }

    private void Update()
    {
        if (IsOpen && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    private void OnDestroy()
    {
        if (_ownsState && GameStateManager.Instance != null
            && GameStateManager.Instance.CurrentState == GameState.Dialogue)
            GameStateManager.Instance.ReturnToPreviousState();
    }

    /// <summary>Shows the destinations. `onChosen` runs after the UI has closed and the player confirmed.</summary>
    public bool Open(TeleportDestination[] destinations, Action<TeleportDestination> onChosen, string title = null, string subtitle = null)
    {
        if (IsOpen || GameStateManager.Instance == null
            || GameStateManager.Instance.CurrentState != GameState.Playing
            || destinations == null || destinations.Length == 0)
            return false;

        _onChosen = onChosen;
        _titleLabel.text = string.IsNullOrEmpty(title) ? "BOSS TELEPORT" : title;
        _subtitleLabel.text = string.IsNullOrEmpty(subtitle) ? "Choose a sealed arena to enter." : subtitle;
        Vector2 panelSize = destinations.Length == 1 ? SingleDestinationPanelSize : StandardPanelSize;
        _panelFrame.sizeDelta = panelSize;
        PopupViewportFit.Configure(_panelFrame.gameObject, panelSize);
        _cardContainer.sizeDelta = new Vector2(destinations.Length == 1 ? 360f : 1100f, 400f);
        PopulateCards(destinations);
        Select(null);
        _root.SetActive(true);
        GameStateManager.Instance.PushState(GameState.Dialogue);
        _ownsState = true;
        return true;
    }

    public void Close()
    {
        if (_root != null)
            _root.SetActive(false);

        if (_ownsState && GameStateManager.Instance != null
            && GameStateManager.Instance.CurrentState == GameState.Dialogue)
            GameStateManager.Instance.ReturnToPreviousState();
        _ownsState = false;
    }

    private void Confirm()
    {
        if (_selected == null || !_selected.Destination.available)
            return;

        TeleportDestination destination = _selected.Destination;
        Action<TeleportDestination> callback = _onChosen;
        SoundFXManager.PlaySfx(SfxIds.BossTeleport);
        Close();
        callback?.Invoke(destination);
    }

    // ------------------------------------------------------------------ cards

    private void PopulateCards(TeleportDestination[] destinations)
    {
        foreach (Card card in _cards)
        {
            if (card.Border != null)
            {
                card.Border.gameObject.SetActive(false);
                Destroy(card.Border.gameObject);
            }
        }

        _cards.Clear();
        foreach (TeleportDestination destination in destinations)
            _cards.Add(BuildCard(destination));
    }

    private Card BuildCard(TeleportDestination destination)
    {
        Image border = NewImage("Card_" + destination.id, _cardContainer, destination.available ? GoldDim : new Color(1f, 1f, 1f, 0.12f));
        RectTransform borderRect = border.rectTransform;
        borderRect.sizeDelta = new Vector2(318f, 390f);
        var layout = border.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 318f;
        layout.preferredHeight = 390f;

        border.sprite = _cardArtwork;
        if (_cardArtwork != null)
            border.color = destination.available ? Color.white : WarmGray;

        var selection = border.gameObject.AddComponent<Outline>();
        selection.effectColor = destination.accent;
        selection.effectDistance = new Vector2(2.5f, -2.5f);
        selection.enabled = false;

        Image background = NewImage("Content", border.transform, _cardArtwork != null ? Color.clear : Walnut);
        background.raycastTarget = false;
        Inset(background.rectTransform, 10f);

        var card = new Card { Destination = destination, Border = border, Selection = selection };

        // illustration
        Image iconBack = NewImage("IconBack", background.transform, new Color(0.07f, 0.06f, 0.05f, 1f));
        SetRect(iconBack.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -11f), new Vector2(-18f, 155f));
        Image icon = NewImage("Icon", iconBack.transform, destination.available ? Color.white : new Color(0.15f, 0.15f, 0.17f, 1f));
        Inset(icon.rectTransform, 8f);
        icon.preserveAspect = true;
        if (destination.icon != null)
        {
            icon.sprite = destination.icon;
            if (!destination.available)
                icon.color = new Color(0.1f, 0.1f, 0.12f, 1f); // silhouette
        }
        else
        {
            icon.enabled = false;
            TMP_Text question = NewText("Unknown", iconBack.transform, "?", 96, destination.accent * new Color(1f, 1f, 1f, 0.55f), TextAlignmentOptions.Center);
            Inset(question.rectTransform, 0f);
        }

        TMP_Text title = NewText("Name", background.transform, destination.displayName.ToUpperInvariant(), 26, destination.available ? Gold : WarmGray, TextAlignmentOptions.Center);
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -176f), new Vector2(-24f, 36f));

        TMP_Text body = NewText("Description", background.transform, destination.description, 17, WarmGray, TextAlignmentOptions.Top);
        body.enableAutoSizing = true;
        body.fontSizeMin = 13;
        body.fontSizeMax = 17;
        body.overflowMode = TextOverflowModes.Ellipsis;
        SetRect(body.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -215f), new Vector2(-32f, 84f));

        TMP_Text status = NewText("Status", background.transform, destination.available ? "AVAILABLE" : "COMING SOON", 20, destination.available ? Available : WarmGray, TextAlignmentOptions.Center);
        SetRect(status.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 15f), new Vector2(-24f, 32f));

        var button = border.gameObject.AddComponent<Button>();
        button.targetGraphic = border;
        button.onClick.AddListener(() => Select(card));
        return card;
    }

    private void Select(Card card)
    {
        _selected = card;
        foreach (Card other in _cards)
        {
            bool isSelected = other == card;
            other.Selection.enabled = isSelected;
            if (_cardArtwork == null)
                other.Border.color = isSelected ? other.Destination.accent
                    : other.Destination.available ? GoldDim : new Color(1f, 1f, 1f, 0.12f);
        }

        bool canConfirm = card != null && card.Destination.available;
        _confirmButton.interactable = canConfirm;
        _confirmImage.color = _hasConfirmArtwork
            ? (canConfirm ? Color.white : new Color(0.63f, 0.63f, 0.63f))
            : (canConfirm ? Sapphire : Disabled);
        _confirmLabel.color = canConfirm ? Ivory : WarmGray;
        _confirmLabel.text = card == null ? "SELECT A DESTINATION" : card.Destination.available ? "TELEPORT" : "NOT AVAILABLE";
    }

    // ------------------------------------------------------------------ layout

    private void Build()
    {
        var canvasObject = new GameObject("BossTeleportSelectCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        PopupViewportFit.ConfigureCanvas(scaler);
        _root = canvasObject;

        Image dim = NewImage("Dim", canvasObject.transform, new Color(0f, 0f, 0f, 0.43f));
        Inset(dim.rectTransform, 0f);

        _cardArtwork = Resources.Load<Sprite>("UI/BossTeleport/boss_destination_card_v1");
        Image frame = NewImage("PanelFrame", canvasObject.transform, Gold);
        frame.sprite = Resources.Load<Sprite>("UI/BossTeleport/boss_teleport_board_v1");
        if (frame.sprite != null)
        {
            frame.color = Color.white;
            PopupBoardArtwork.Configure(frame, StandardPanelSize, new Vector2(0.07f, 0.13f));
        }
        _panelFrame = frame.rectTransform;
        SetRect(_panelFrame, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.one * 0.5f, Vector2.zero, StandardPanelSize);
        Image panel = NewImage("Panel", frame.transform, frame.sprite != null ? Color.clear : Charcoal);
        panel.raycastTarget = false;
        PopupViewportFit.Configure(frame.gameObject, StandardPanelSize);
        Inset(panel.rectTransform, 3f);

        TMP_Text title = NewText("Title", panel.transform, "BOSS TELEPORT", 34, Gold, TextAlignmentOptions.Center);
        _titleLabel = title;
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -79f), new Vector2(-120f, 36f));
        TMP_Text subtitle = NewText("Subtitle", panel.transform, "Choose a sealed arena to enter.", 17, WarmGray, TextAlignmentOptions.Center);
        _subtitleLabel = subtitle;
        SetRect(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -115f), new Vector2(-120f, 24f));

        Image closeImage = NewImage("Close", panel.transform, Color.white);
        closeImage.sprite = Resources.Load<Sprite>("UI/Map/DarkInventoryStyle/map_close_button_v1");
        closeImage.preserveAspect = true;
        SetRect(closeImage.rectTransform, Vector2.one, Vector2.one, Vector2.one,
            new Vector2(-48f, -60f), new Vector2(44f, 44f));
        var closeButton = closeImage.gameObject.AddComponent<Button>();
        closeButton.targetGraphic = closeImage;
        closeButton.onClick.AddListener(Close);

        var container = new GameObject("Cards", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        _cardContainer = container.GetComponent<RectTransform>();
        _cardContainer.SetParent(panel.transform, false);
        SetRect(_cardContainer, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(1100f, 400f));
        var row = container.GetComponent<HorizontalLayoutGroup>();
        row.spacing = 24f;
        row.childAlignment = TextAnchor.MiddleCenter;
        row.childControlWidth = false;
        row.childControlHeight = false;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = false;

        // confirm (primary) + cancel
        _confirmImage = NewImage("Confirm", panel.transform, Sapphire);
        _confirmImage.sprite = Resources.Load<Sprite>("UI/BossTeleport/boss_teleport_confirm_v1");
        _hasConfirmArtwork = _confirmImage.sprite != null;
        SetRect(_confirmImage.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(99f, 64f), new Vector2(278f, 52f));
        _confirmLabel = NewText("Label", _confirmImage.transform, "TELEPORT", 22, Ivory, TextAlignmentOptions.Center);
        Inset(_confirmLabel.rectTransform, 0f);
        _confirmButton = _confirmImage.gameObject.AddComponent<Button>();
        _confirmButton.targetGraphic = _confirmImage;
        _confirmButton.onClick.AddListener(Confirm);

        Image cancelImage = NewImage("Cancel", panel.transform, Walnut);
        cancelImage.sprite = Resources.Load<Sprite>("UI/BossTeleport/boss_teleport_cancel_v1");
        if (cancelImage.sprite != null)
            cancelImage.color = Color.white;
        SetRect(cancelImage.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-157f, 64f), new Vector2(178f, 52f));
        TMP_Text cancelLabel = NewText("Label", cancelImage.transform, "CANCEL", 20, Gold, TextAlignmentOptions.Center);
        Inset(cancelLabel.rectTransform, 0f);
        var cancelButton = cancelImage.gameObject.AddComponent<Button>();
        cancelButton.targetGraphic = cancelImage;
        cancelButton.onClick.AddListener(Close);
    }

    private static Image NewImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static TMP_Text NewText(string name, Transform parent, string text, float size, Color color, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var label = go.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.Normal; // code-created TMP defaults to NoWrap in this version
        label.raycastTarget = false;
        return label;
    }

    private static void Inset(RectTransform rect, float amount)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(amount, amount);
        rect.offsetMax = new Vector2(-amount, -amount);
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }
}
