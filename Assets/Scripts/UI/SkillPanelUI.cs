using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Presentation only. Learning/progression/save are intentionally not owned by this UI.</summary>
public sealed class SkillPanelUI : MonoBehaviour
{
    public static string LoadoutKey(int slot) => slot >= 0 && slot < 4 ? "QERT"[slot].ToString() : "";
    [Serializable]
    public sealed class Entry
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public int learningCost;
        public Button button;
    }

    [SerializeField] private GameObject _window;
    [SerializeField] private GameObject _tooltip;
    [SerializeField] private TMP_Text _title;
    [SerializeField] private TMP_Text _description;
    [SerializeField] private TMP_Text _cost;
    [SerializeField] private Button _close;
    [SerializeField] private Entry[] _entries;
    [SerializeField] private SkillCatalog _catalog;
    [SerializeField] private TMP_Text _pointsText;
    [SerializeField] private Button _learn;
    [SerializeField] private TMP_Text _learnLabel;
    [SerializeField] private TMP_Text _status;
    [SerializeField] private Image[] _loadoutIcons;
    private PlayerStat _player;
    private int _selected = -1;
    private Image _dragGhost;
    private string _dragId;
    private RectTransform[] _libraryIconRects;
    private RectTransform _tooltipRect;
    private float _tooltipLastHovered;
    private const float TooltipExitGrace = .2f;
    // Application/UI action: remains available while gameplay input is suspended by GameState.
    [SerializeField] private InputAction _toggle = new InputAction("Skills", InputActionType.Button, "<Keyboard>/p");
    private GameStateManager _state;
    public bool IsOpen => _state != null && _state.CurrentState == GameState.GameplayMenu
        && _state.CurrentMenuPage == GameplayMenuPage.Skills;

    private void Awake()
    {
        _tooltipRect = _tooltip.GetComponent<RectTransform>();
        _tooltipRect.pivot = new Vector2(.5f,.5f);
        _libraryIconRects = new RectTransform[_entries.Length];
        for (int i = 0; i < _entries.Length; i++)
            _libraryIconRects[i] = (RectTransform)_entries[i].button.transform.Find("Icon");
        // The tooltip itself is interactive so the pointer can travel to Learn without dismissal.
        var background = _tooltip.transform.Find("Background").GetComponent<Image>();
        background.raycastTarget = true;
        _close.onClick.AddListener(Close);
        _learn.onClick.AddListener(LearnSelected);
        for (int i = 0; i < _entries.Length; i++)
            _entries[i].button.GetComponent<SkillIconDragUI>().Bind(this, _entries[i].id, -1);
        for (int i = 0; i < _loadoutIcons.Length; i++)
        {
            var slot = _loadoutIcons[i].transform.parent;
            slot.GetComponent<SkillIconDragUI>().Bind(this, null, i);
            slot.GetComponent<SkillLoadoutDropUI>().Bind(this, i);
        }
    }

    private void OnEnable()
    {
        _state = GameStateManager.Instance;
        if (_state != null) _state.StateChanged += OnStateChanged;
        _toggle.performed += OnToggle;
        _toggle.Enable();
        Refresh();
        BindPlayer();
    }

    private void OnDisable()
    {
        HideTooltip();
        CancelDrag();
        if (_player != null) _player.Skills.Changed -= RefreshSkills;
        _player = null;
        if (_state != null) _state.StateChanged -= OnStateChanged;
        _toggle.performed -= OnToggle;
        _toggle.Disable();
    }

    private void OnDestroy() => _toggle.Dispose();
    private void Update()
    {
        if (_player != PlayerStat.Instance) BindPlayer();
        UpdateHoverTooltip();
    }

    private void UpdateHoverTooltip()
    {
        if (!IsOpen || Mouse.current == null || _dragGhost != null)
        {
            HideTooltip();
            return;
        }
        Vector2 pointer = Mouse.current.position.ReadValue();
        // Freeze while over the popup; following the pointer here would make Learn impossible to click.
        if (_tooltip.activeSelf && RectTransformUtility.RectangleContainsScreenPoint(_tooltipRect, pointer, null))
        {
            _tooltipLastHovered = Time.unscaledTime;
            return;
        }
        int hovered = -1;
        for (int i = 0; i < _libraryIconRects.Length; i++)
            if (RectTransformUtility.RectangleContainsScreenPoint(_libraryIconRects[i], pointer, null))
            { hovered = i; break; }
        if (hovered < 0 && _player != null)
            for (int i = 0; i < _loadoutIcons.Length; i++)
                if (_loadoutIcons[i].enabled && RectTransformUtility.RectangleContainsScreenPoint(_loadoutIcons[i].rectTransform, pointer, null))
                { hovered = Array.FindIndex(_entries, entry => entry.id == _player.Skills.Equipped(i)); break; }
        if (hovered >= 0)
        {
            if (_selected != hovered || !_tooltip.activeSelf) ShowSkill(hovered);
            if (_tooltip.activeSelf) PositionTooltip(pointer);
            _tooltipLastHovered = Time.unscaledTime;
        }
        else if (Time.unscaledTime - _tooltipLastHovered >= TooltipExitGrace) HideTooltip();
    }

    private void PositionTooltip(Vector2 pointer)
    {
        var canvasRect = (RectTransform)transform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, pointer, null, out var local)) return;
        Vector2 half = _tooltipRect.rect.size * .5f;
        Vector2 center = local + new Vector2(18f + half.x, -18f - half.y);
        Rect bounds = canvasRect.rect;
        center.x = Mathf.Clamp(center.x, bounds.xMin + half.x + 12f, bounds.xMax - half.x - 12f);
        center.y = Mathf.Clamp(center.y, bounds.yMin + half.y + 12f, bounds.yMax - half.y - 12f);
        _tooltipRect.position = canvasRect.TransformPoint(center);
        _tooltipRect.SetAsLastSibling();
    }

    private void HideTooltip()
    {
        _tooltip.SetActive(false);
        _selected = -1;
    }
    private void BindPlayer()
    {
        if (_player != null) _player.Skills.Changed -= RefreshSkills;
        _player = PlayerStat.Instance;
        if (_player != null) _player.Skills.Changed += RefreshSkills;
        RefreshSkills();
    }
    private void OnToggle(InputAction.CallbackContext context) => Toggle();
    private void OnStateChanged(GameStateChange change) => Refresh();

    public void Toggle()
    {
        if (_state == null) return;
        if (IsOpen) Close();
        else if (_state.CurrentState == GameState.Playing) _state.OpenMenu(GameplayMenuPage.Skills);
    }

    public void Close()
    {
        if (IsOpen) _state.ReturnToPreviousState();
    }

    public void ShowSkill(int index)
    {
        if (index < 0 || index >= _entries.Length) return;
        var entry = _entries[index];
        _selected = index;
        if (_catalog == null || !_catalog.TryResolve(entry.id, out var definition)) { HideTooltip(); return; }
        _title.text = definition.displayName;
        _description.text = definition.description;
        _cost.text = "Skill Point Cost: " + definition.learningCost;
        _tooltip.SetActive(true);
        RefreshSkills();
    }

    public void LearnSelected()
    {
        if (IsOpen && _tooltip.activeSelf && _player != null && _selected >= 0)
            _player.Skills.Learn(_catalog, _entries[_selected].id);
    }

    private void RefreshSkills()
    {
        if (_pointsText == null) return;
        _pointsText.text = "Skill Points: " + (_player == null ? "--" : _player.Skills.Points.ToString());
        foreach (var entry in _entries)
        {
            var icon = entry.button.transform.Find("Icon").GetComponent<Image>();
            bool known = _catalog != null && _catalog.TryResolve(entry.id, out var definition);
            if (known)
            {
                _catalog.TryResolve(entry.id, out definition);
                icon.sprite = definition.icon;
                entry.button.transform.Find("Name").GetComponent<TMP_Text>().text = definition.displayName;
                entry.button.transform.Find("Cost").GetComponent<TMP_Text>().text = definition.learningCost.ToString();
            }
            icon.color = _player != null && _player.Skills.IsLearned(entry.id) ? Color.white : new Color(.38f,.38f,.38f,1);
        }
        for (int i = 0; i < _loadoutIcons.Length; i++)
        {
            var id = _player?.Skills.Equipped(i);
            _loadoutIcons[i].enabled = _catalog != null && _catalog.TryResolve(id, out var definition);
            if (_loadoutIcons[i].enabled) { _catalog.TryResolve(id, out definition); _loadoutIcons[i].sprite = definition.icon; }
            _loadoutIcons[i].transform.parent.Find("SlotLabel").GetComponent<TMP_Text>().text = LoadoutKey(i);
        }
        _learn.interactable = false;
        if (_selected < 0 || _catalog == null || !_catalog.TryResolve(_entries[_selected].id, out var selected)) return;
        bool learned = _player != null && _player.Skills.IsLearned(selected.id);
        _learnLabel.text = learned ? "Learned" : "Learn";
        _learn.interactable = !learned && _player != null && _player.Skills.Points >= selected.learningCost;
        _status.text = learned ? "Drag this skill into a loadout slot." : _player == null ? "Player unavailable." :
            _learn.interactable ? "Spend Skill Points to learn this skill." : "Not enough Skill Points.";
    }

    public string EquippedId(int slot) => _player?.Skills.Equipped(slot);
    public bool BeginSkillDrag(string id, Vector2 screenPosition)
    {
        CancelDrag();
        if (!IsOpen || _player == null || !_player.Skills.IsLearned(id) || _catalog == null || !_catalog.TryResolve(id, out var definition)) return false;
        HideTooltip();
        _dragId = id;
        var go = new GameObject("SkillDragGhost", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(transform, false);
        _dragGhost = go.GetComponent<Image>(); _dragGhost.sprite = definition.icon;
        foreach (var entry in _entries)
        {
            if (entry.id != id) continue;
            var view = entry.button.transform.Find("Icon").GetComponent<SkillIconView>();
            if (view == null) break;
            view.Refresh();
            if (view.ForegroundSprite != null) _dragGhost.sprite = view.ForegroundSprite;
            break;
        }
        _dragGhost.preserveAspect = true; _dragGhost.raycastTarget = false;
        _dragGhost.rectTransform.sizeDelta = new Vector2(76,76);
        MoveSkillDrag(screenPosition); return true;
    }
    public void MoveSkillDrag(Vector2 screenPosition)
    {
        if (_dragGhost != null && RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform,screenPosition,null,out var point))
            _dragGhost.rectTransform.localPosition = point + new Vector2(18,-18);
    }
    public bool DropSkill(int slot) => IsOpen && _player != null && _dragId != null && _player.Skills.Equip(_catalog,slot,_dragId);
    public void CancelDrag()
    {
        if (_dragGhost != null) Destroy(_dragGhost.gameObject);
        _dragGhost = null; _dragId = null;
    }

    private void Refresh()
    {
        _window.SetActive(IsOpen);
        HideTooltip();
        if (!IsOpen) CancelDrag();
        RefreshSkills();
        if (IsOpen && EventSystem.current != null && _entries.Length > 0)
            EventSystem.current.SetSelectedGameObject(_entries[0].button.gameObject);
    }
}
