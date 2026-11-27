using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static CodeUi;

/// <summary>
/// Offering panel of the boss summoning shrine (D-090). Shows one socket per required summon item; the
/// player places orbs from the inventory (nothing leaves the bag until AWAKEN is pressed), then confirms.
/// Built in code in the Dark Light Fantasy palette; pushes GameState.Dialogue while open like the other
/// modal popups. The consume-and-summon rule itself lives in BossArenaController.TrySummonWithOffering.
/// </summary>
public sealed class BossShrineUI : MonoBehaviour
{
    private sealed class Socket
    {
        public Image Frame;
        public Image Icon;
        public TMP_Text Plus;
    }

    private GameObject _root;
    private TMP_Text _subtitle;
    private TMP_Text _status;
    private TMP_Text _hint;
    private RectTransform _socketRow;
    private Button _placeButton;
    private Button _takeBackButton;
    private Button _awakenButton;
    private Sprite _socketArtwork;
    private TMP_Text _awakenLabel;
    private readonly List<Socket> _sockets = new List<Socket>();

    private BossArenaController _arena;
    private Action _onAwaken;
    private int _placed;
    private bool _ownsState;

    public bool IsOpen => _root != null && _root.activeSelf;
    public int PlacedCount => _placed;

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
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= Refresh;

        if (_ownsState && GameStateManager.Instance != null
            && GameStateManager.Instance.CurrentState == GameState.Dialogue)
            GameStateManager.Instance.ReturnToPreviousState();
    }

    /// <summary>Opens the panel for `arena`'s offering. `onAwaken` runs after the panel closed on AWAKEN.</summary>
    public bool Open(BossArenaController arena, Action onAwaken)
    {
        if (IsOpen || arena == null || GameStateManager.Instance == null
            || GameStateManager.Instance.CurrentState != GameState.Playing)
            return false;

        _arena = arena;
        _onAwaken = onAwaken;
        _placed = 0;
        RebuildSockets(arena.SummonItemCount);
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged += Refresh;

        _root.SetActive(true);
        GameStateManager.Instance.PushState(GameState.Dialogue);
        _ownsState = true;
        Refresh();
        return true;
    }

    public void Close()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.OnInventoryChanged -= Refresh;

        if (_root != null)
            _root.SetActive(false);

        if (_ownsState && GameStateManager.Instance != null
            && GameStateManager.Instance.CurrentState == GameState.Dialogue)
            GameStateManager.Instance.ReturnToPreviousState();
        _ownsState = false;
    }

    // ------------------------------------------------------------------ actions

    private int InBag()
    {
        ItemSO item = _arena != null ? _arena.SummonItem : null;
        return item != null && InventoryManager.Instance != null
            ? InventoryManager.Instance.GetTotalQuantity(item.itemId) : 0;
    }

    private int Required() => _arena != null ? _arena.SummonItemCount : 1;

    private void PlaceOne()
    {
        if (_placed >= Required() || InBag() - _placed <= 0)
            return;

        _placed++;
        SoundFXManager.PlaySfx(SfxIds.BossShrineOrbPlace);
        Refresh();
    }

    private void TakeBackOne()
    {
        if (_placed <= 0)
            return;

        _placed--;
        Refresh();
    }

    private void Awaken()
    {
        if (_placed < Required() || InBag() < _placed)
            return;

        Action callback = _onAwaken;
        Close();
        callback?.Invoke();
    }

    // ------------------------------------------------------------------ presentation

    private void RebuildSockets(int count)
    {
        foreach (Socket socket in _sockets)
        {
            if (socket.Frame != null)
                Destroy(socket.Frame.gameObject);
        }

        _sockets.Clear();
        for (int i = 0; i < count; i++)
            _sockets.Add(BuildSocket(i));
    }

    private Socket BuildSocket(int index)
    {
        Image frame = NewImage("Socket" + index, _socketRow, Color.white);
        frame.sprite = _socketArtwork;
        frame.preserveAspect = true;
        frame.rectTransform.sizeDelta = new Vector2(116f, 116f);

        Image icon = NewImage("Icon", frame.transform, Color.white);
        Inset(icon.rectTransform, 18f);
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = false;

        TMP_Text plus = NewText("Plus", frame.transform, "+", 44, Ivory, TextAlignmentOptions.Center);
        Inset(plus.rectTransform, 0f);

        var socket = new Socket { Frame = frame, Icon = icon, Plus = plus };
        int captured = index;
        var button = frame.gameObject.AddComponent<Button>();
        button.targetGraphic = frame;
        button.onClick.AddListener(() =>
        {
            if (captured < _placed)
                TakeBackOne();
            else
                PlaceOne();
        });
        return socket;
    }

    private void Refresh()
    {
        if (_arena == null)
            return;

        ItemSO item = _arena.SummonItem;
        int required = Required();
        int inBag = InBag();
        if (_placed > inBag)
            _placed = inBag; // items left the bag while the panel was open

        string itemName = item != null ? item.itemName : "offering";
        _subtitle.text = required == 1
            ? $"Offer a {itemName} to awaken {_arena.BossDisplayName}."
            : $"Offer {required} {itemName} to awaken {_arena.BossDisplayName}.";

        for (int i = 0; i < _sockets.Count; i++)
        {
            bool filled = i < _placed;
            Socket socket = _sockets[i];
            socket.Frame.color = _socketArtwork != null
                ? (filled ? Color.white : new Color(0.72f, 0.66f, 0.57f))
                : (filled ? Gold : Walnut);
            socket.Icon.enabled = filled && item != null;
            socket.Icon.sprite = item != null ? item.icon : null;
            socket.Plus.enabled = !filled;
        }

        _status.text = $"Placed {_placed} / {required}      In your bag: {inBag}";
        bool missing = inBag < required;
        _hint.text = missing
            ? $"You need {required - inBag} more {itemName} to awaken the guardian."
            : _placed < required ? "Click a socket or PLACE to offer an orb." : "The shrine is ready.";
        _hint.color = missing ? new Color(0.9f, 0.55f, 0.4f, 1f) : WarmGray;

        _placeButton.interactable = _placed < required && inBag - _placed > 0;
        _takeBackButton.interactable = _placed > 0;
        bool ready = _placed >= required;
        _awakenButton.interactable = ready;
        _awakenLabel.color = ready ? Ivory : WarmGray;
    }

    private void Build()
    {
        Canvas canvas = NewOverlayCanvas("BossShrineCanvas", transform, 900);
        PopupViewportFit.ConfigureCanvas(canvas.GetComponent<CanvasScaler>());
        _root = canvas.gameObject;

        Image dim = NewImage("Dim", _root.transform, new Color(0f, 0f, 0f, 0.43f));
        Inset(dim.rectTransform, 0f);

        Vector2 panelSize = new Vector2(960f, 600f);
        Image frame = NewImage("PanelFrame", _root.transform, Charcoal);
        frame.sprite = Resources.Load<Sprite>("UI/BossShrine/summoning_shrine_board_v1");
        if (frame.sprite != null)
            frame.color = Color.white;
        SetRect(frame.rectTransform, Vector2.one * 0.5f, Vector2.one * 0.5f,
            Vector2.one * 0.5f, Vector2.zero, panelSize);
        PopupViewportFit.Configure(frame.gameObject, panelSize);

        Transform panel = frame.transform;
        TMP_Text title = NewText("Title", panel, "SUMMONING SHRINE", 36, Gold, TextAlignmentOptions.Center);
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(-210f, 44f));
        _subtitle = NewText("Subtitle", panel, "", 23, Ivory, TextAlignmentOptions.Center);
        SetRect(_subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -132f), new Vector2(-200f, 38f));

        Button close = NewButton("Close", panel, "X", 24, Walnut, Gold, out TMP_Text closeLabel);
        Image closeImage = close.GetComponent<Image>();
        closeImage.sprite = Resources.Load<Sprite>("UI/Map/DarkInventoryStyle/map_close_button_v1");
        if (closeImage.sprite != null)
        {
            closeImage.color = Color.white;
            closeLabel.enabled = false;
        }
        close.targetGraphic = closeImage;
        SetRect(closeImage.rectTransform, Vector2.one, Vector2.one, Vector2.one,
            new Vector2(-62f, -80f), new Vector2(44f, 44f));
        close.onClick.AddListener(Close);

        _socketArtwork = Resources.Load<Sprite>("UI/Inventory/DarkInventoryTabbed/Processed/inventory_slot_v5");
        var row = new GameObject("Sockets", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        _socketRow = row.GetComponent<RectTransform>();
        _socketRow.SetParent(panel, false);
        SetRect(_socketRow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 38f), new Vector2(800f, 140f));
        var layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 24f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        _status = NewText("Status", panel, "", 23, Ivory, TextAlignmentOptions.Center);
        SetRect(_status.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -78f), new Vector2(-80f, 32f));
        _hint = NewText("Hint", panel, "", 19, WarmGray, TextAlignmentOptions.Center);
        SetRect(_hint.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -112f), new Vector2(-80f, 30f));

        // bottom action row
        var actions = new GameObject("Actions", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        var actionsRect = actions.GetComponent<RectTransform>();
        actionsRect.SetParent(panel, false);
        SetRect(actionsRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 70f), new Vector2(800f, 52f));
        var actionLayout = actions.GetComponent<HorizontalLayoutGroup>();
        actionLayout.spacing = 16f;
        actionLayout.childAlignment = TextAnchor.MiddleCenter;
        actionLayout.childControlWidth = false;
        actionLayout.childControlHeight = false;
        actionLayout.childForceExpandWidth = false;
        actionLayout.childForceExpandHeight = false;

        Sprite secondary = Resources.Load<Sprite>("UI/BossTeleport/boss_teleport_cancel_v1");
        Sprite primary = Resources.Load<Sprite>("UI/BossTeleport/boss_teleport_confirm_v1");
        Button cancel = ActionButton(actionsRect, "Cancel", "CANCEL", secondary, Walnut, out _);
        cancel.onClick.AddListener(Close);
        _takeBackButton = ActionButton(actionsRect, "TakeBack", "TAKE BACK", secondary, Walnut, out _);
        _takeBackButton.onClick.AddListener(TakeBackOne);
        _placeButton = ActionButton(actionsRect, "Place", "PLACE ORB", secondary, Walnut, out _);
        _placeButton.onClick.AddListener(PlaceOne);
        _awakenButton = ActionButton(actionsRect, "Awaken", "AWAKEN", primary, Sapphire, out _awakenLabel);
        _awakenButton.onClick.AddListener(Awaken);
    }

    private static Button ActionButton(RectTransform parent, string name, string label, Sprite artwork, Color background, out TMP_Text text)
    {
        Button button = NewButton(name, parent, label, 22, background, Ivory, out text);
        Image image = button.GetComponent<Image>();
        image.sprite = artwork;
        if (artwork != null)
            image.color = Color.white;
        button.targetGraphic = image;
        image.rectTransform.sizeDelta = new Vector2(176f, 52f);
        Inset(text.rectTransform, 8f);
        return button;
    }
}
