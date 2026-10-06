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
    private Image _awakenImage;
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
        Image frame = NewImage("Socket" + index, _socketRow, GoldDim);
        var layout = frame.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 150f;
        layout.preferredHeight = 150f;

        Image inner = NewImage("Inner", frame.transform, new Color(0.07f, 0.06f, 0.05f, 1f));
        Inset(inner.rectTransform, 3f);

        Image icon = NewImage("Icon", inner.transform, Color.white);
        Inset(icon.rectTransform, 18f);
        icon.preserveAspect = true;
        icon.enabled = false;

        TMP_Text plus = NewText("Plus", inner.transform, "+", 64, new Color(1f, 1f, 1f, 0.18f), TextAlignmentOptions.Center);
        Inset(plus.rectTransform, 0f);

        var socket = new Socket { Frame = frame, Icon = icon, Plus = plus };
        int captured = index;
        var button = frame.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
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
            socket.Frame.color = filled ? Gold : GoldDim;
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
        _awakenImage.color = ready ? Sapphire : Disabled;
        _awakenLabel.color = ready ? Ivory : WarmGray;
    }

    private void Build()
    {
        Canvas canvas = NewOverlayCanvas("BossShrineCanvas", transform, 900);
        _root = canvas.gameObject;

        Image dim = NewImage("Dim", _root.transform, new Color(0f, 0f, 0f, 0.62f));
        Inset(dim.rectTransform, 0f);

        Image frame = NewImage("PanelFrame", _root.transform, Gold);
        SetRect(frame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960f, 600f));
        Image panel = NewImage("Panel", frame.transform, Charcoal);
        Inset(panel.rectTransform, 3f);

        TMP_Text title = NewText("Title", panel.transform, "SUMMONING SHRINE", 30, Gold, TextAlignmentOptions.Center);
        SetRect(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(-120f, 44f));
        _subtitle = NewText("Subtitle", panel.transform, "", 17, WarmGray, TextAlignmentOptions.Center);
        SetRect(_subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(-120f, 30f));

        Button close = NewButton("Close", panel.transform, "X", 24, Walnut, Gold, out _);
        SetRect(close.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-16f, -16f), new Vector2(44f, 44f));
        close.onClick.AddListener(Close);

        var row = new GameObject("Sockets", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        _socketRow = row.GetComponent<RectTransform>();
        _socketRow.SetParent(panel.transform, false);
        SetRect(_socketRow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(860f, 170f));
        var layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 24f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        _status = NewText("Status", panel.transform, "", 20, Ivory, TextAlignmentOptions.Center);
        SetRect(_status.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -78f), new Vector2(-80f, 32f));
        _hint = NewText("Hint", panel.transform, "", 16, WarmGray, TextAlignmentOptions.Center);
        SetRect(_hint.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -112f), new Vector2(-80f, 30f));

        // bottom action row
        var actions = new GameObject("Actions", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        var actionsRect = actions.GetComponent<RectTransform>();
        actionsRect.SetParent(panel.transform, false);
        SetRect(actionsRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(880f, 60f));
        var actionLayout = actions.GetComponent<HorizontalLayoutGroup>();
        actionLayout.spacing = 16f;
        actionLayout.childAlignment = TextAnchor.MiddleCenter;
        actionLayout.childControlWidth = false;
        actionLayout.childControlHeight = false;
        actionLayout.childForceExpandWidth = false;
        actionLayout.childForceExpandHeight = false;

        Button cancel = ActionButton(actionsRect, "Cancel", "CANCEL", 150f, Walnut, Gold, out _);
        cancel.onClick.AddListener(Close);
        _takeBackButton = ActionButton(actionsRect, "TakeBack", "TAKE BACK", 190f, Walnut, Gold, out _);
        _takeBackButton.onClick.AddListener(TakeBackOne);
        _placeButton = ActionButton(actionsRect, "Place", "PLACE ORB", 210f, Walnut, Ivory, out _);
        _placeButton.onClick.AddListener(PlaceOne);
        _awakenButton = ActionButton(actionsRect, "Awaken", "AWAKEN", 260f, Sapphire, Ivory, out _awakenLabel);
        _awakenImage = _awakenButton.GetComponent<Image>();
        _awakenButton.onClick.AddListener(Awaken);
    }

    private static Button ActionButton(RectTransform parent, string name, string label, float width, Color background, Color labelColor, out TMP_Text text)
    {
        Button button = NewButton(name, parent, label, 22, background, labelColor, out text);
        var element = button.gameObject.AddComponent<LayoutElement>();
        element.preferredWidth = width;
        element.preferredHeight = 58f;
        return button;
    }
}
