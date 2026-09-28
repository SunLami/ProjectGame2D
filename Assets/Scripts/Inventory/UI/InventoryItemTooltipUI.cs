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
        _statsText.text = BuildStats(item);
        _descriptionText.text = string.IsNullOrWhiteSpace(item.description)
            ? "No description available."
            : item.description;

        _panel.gameObject.SetActive(true);
        MoveToPointer(pointerScreenPosition);
        _panel.SetAsLastSibling();
    }

    public void Hide()
    {
        if (_panel != null) _panel.gameObject.SetActive(false);
    }

    public void MoveToPointer(Vector2 pointerScreenPosition)
    {
        if (_panel == null || !_panel.gameObject.activeInHierarchy) return;

        Canvas canvas = _canvas != null ? _canvas.rootCanvas : GetComponentInParent<Canvas>()?.rootCanvas;
        RectTransform canvasRect = canvas?.transform as RectTransform;
        Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        if (canvasRect == null) return;

        float scale = canvas != null ? canvas.scaleFactor : 1f;
        Vector2 pixelSize = _panel.rect.size * scale;
        Vector2 screen = pointerScreenPosition + new Vector2(18f, -22f);

        if (screen.x + pixelSize.x > Screen.width - 8f)
            screen.x = pointerScreenPosition.x - pixelSize.x - 18f;
        if (screen.y - pixelSize.y < 8f)
            screen.y = pointerScreenPosition.y + pixelSize.y + 22f;

        screen.x = Mathf.Clamp(screen.x, 8f, Mathf.Max(8f, Screen.width - pixelSize.x - 8f));
        screen.y = Mathf.Clamp(screen.y, pixelSize.y + 8f, Screen.height - 8f);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, camera, out Vector2 local);
        _panel.anchoredPosition = local;
    }

    private static string BuildStats(ItemSO item)
    {
        var lines = new List<string>();
        if (item.isStackable) lines.Add($"Stack size: {item.maxStackSize}");
        if (item is EquipmentItemSO equipment)
        {
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
