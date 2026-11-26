using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class InventoryItemTooltipUI : MonoBehaviour
{
    public static InventoryItemTooltipUI Instance { get; private set; }

    [SerializeField] private RectTransform _panel;
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _typeText;
    [SerializeField] private TMP_Text _statsText;
    [SerializeField] private TMP_Text _descriptionText;

    private Canvas _canvas;
    private EquipmentItemSO _hoveredEquipment;
    private bool _requiresHigherLevel;

    private void Awake()
    {
        Instance = this;
        _canvas = GetComponentInParent<Canvas>();
        Hide();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Show(ItemSO item, RectTransform source)
    {
        Show(item, source, Input.mousePosition);
    }

    public void Show(ItemSO item, RectTransform source, Vector2 pointerScreenPosition)
    {
        if (item == null || source == null || _panel == null) return;

        _icon.sprite = item.icon;
        _icon.enabled = item.icon != null;
        _nameText.text = string.IsNullOrWhiteSpace(item.itemName) ? item.itemId : item.itemName;
        _nameText.color = item is EquipmentItemSO
            ? new Color(0.78f, 0.48f, 1f, 1f)
            : new Color(0.95f, 0.82f, 0.35f, 1f);
        _typeText.text = item is EquipmentItemSO equipment
            ? $"{item.type.ToString().ToUpperInvariant()}   /   {equipment.slot.ToString().ToUpperInvariant()} SLOT"
            : item.type.ToString().ToUpperInvariant();
        _hoveredEquipment = item as EquipmentItemSO;
        _requiresHigherLevel = _hoveredEquipment != null && _hoveredEquipment.RequiresHigherLevel(PlayerStat.Instance);
        _statsText.text = BuildStats(item);
        _descriptionText.text = string.IsNullOrWhiteSpace(item.description)
            ? "No description available."
            : item.description;

        _panel.gameObject.SetActive(true);
        MoveToPointer(pointerScreenPosition);
        _panel.SetAsLastSibling();
    }

    private void LateUpdate()
    {
        if (_hoveredEquipment == null || _panel == null || !_panel.gameObject.activeInHierarchy) return;
        bool locked = _hoveredEquipment.RequiresHigherLevel(PlayerStat.Instance);
        if (locked == _requiresHigherLevel) return;

        _requiresHigherLevel = locked;
        _statsText.text = BuildStats(_hoveredEquipment);
    }

    public void Hide()
    {
        _hoveredEquipment = null;
        if (_panel != null) _panel.gameObject.SetActive(false);
    }

    public void MoveToPointer(Vector2 pointerScreenPosition)
    {
        if (_panel == null || !_panel.gameObject.activeInHierarchy) return;

        Canvas canvas = _canvas != null ? _canvas.rootCanvas : GetComponentInParent<Canvas>()?.rootCanvas;
        var parent = _panel.parent as RectTransform;
        if (parent == null) return;

        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        Vector2 origin = RectTransformUtility.WorldToScreenPoint(camera, _panel.position);
        Vector2 lower = RectTransformUtility.WorldToScreenPoint(camera, _panel.TransformPoint(_panel.rect.min)) - origin;
        Vector2 upper = RectTransformUtility.WorldToScreenPoint(camera, _panel.TransformPoint(_panel.rect.max)) - origin;
        Rect safe = Screen.safeArea;
        Vector2 screen = pointerScreenPosition + new Vector2(18f, -22f);

        if (screen.x + upper.x > safe.xMax - 8f)
            screen.x = pointerScreenPosition.x - upper.x - 18f;
        if (screen.y + lower.y < safe.yMin + 8f)
            screen.y = pointerScreenPosition.y - lower.y + 22f;

        Vector2 min = safe.min + Vector2.one * 8f - lower;
        Vector2 max = safe.max - Vector2.one * 8f - upper;
        screen.x = Mathf.Clamp(screen.x, min.x, Mathf.Max(min.x, max.x));
        screen.y = Mathf.Clamp(screen.y, min.y, Mathf.Max(min.y, max.y));
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(parent, screen, camera, out Vector3 world))
            _panel.position = world;
    }

    private static string BuildStats(ItemSO item)
    {
        var lines = new List<string>();
        if (item.isStackable && InventoryManager.Instance != null)
            lines.Add($"Owned: {InventoryManager.Instance.GetTotalQuantity(item.itemId)}");
        if (item is EquipmentItemSO equipment)
        {
            string color = equipment.RequiresHigherLevel(PlayerStat.Instance) ? "#FF7773" : "#FFFFFF";
            lines.Add($"<color={color}>Required level: {equipment.requiredLevel}</color>");
            PlayerStatModifiers m = equipment.statModifiers;
            Add(lines, "Max Health", m.maxHealth);
            Add(lines, "Attack Damage", m.attackDamage);
            Add(lines, "Defense", m.defense);
            Add(lines, "Move Speed", m.moveSpeed);
            AddPercent(lines, "Critical Chance", m.criticalChance);
            AddPercent(lines, "Damage Reduction", m.damageReduction);
            AddPercent(lines, "Dodge Chance", m.dodgeChance);
            Add(lines, "Health Regeneration", m.healthRegeneration);
        }
        return lines.Count == 0 ? "Standard item" : string.Join("\n", lines);
    }

    private static void Add(List<string> lines, string label, float value)
    {
        if (!Mathf.Approximately(value, 0f)) lines.Add($"<color=#70D879>+{value:0.##} {label}</color>");
    }

    private static void AddPercent(List<string> lines, string label, float value)
    {
        if (!Mathf.Approximately(value, 0f)) lines.Add($"<color=#70D879>+{value * 100f:0.#}% {label}</color>");
    }
}
