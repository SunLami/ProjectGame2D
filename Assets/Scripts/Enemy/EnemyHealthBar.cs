using TMPro;
using UnityEngine;

public sealed class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private EnemyUniversal _owner;
    [SerializeField] private RectTransform _fill;
    [SerializeField] private WildlifeActor _wildlifeOwner;
    private Canvas _canvas;
    private TextMeshProUGUI _label;
    private float _emptyAnchorX;
    private float _fullAnchorX;

    private void Awake()
    {
        if (_owner == null)
            _owner = GetComponentInParent<EnemyUniversal>();

        _canvas = GetComponent<Canvas>();

        if (_fill != null)
        {
            _emptyAnchorX = _fill.anchorMin.x;
            _fullAnchorX = _fill.anchorMax.x;
        }
    }

    private void OnEnable()
    {
        if (_owner == null)
            _owner = GetComponentInParent<EnemyUniversal>();

        if (_wildlifeOwner != null)
        {
            _wildlifeOwner.HealthChanged += UpdateWildlifeBar;
            UpdateWildlifeBar(_wildlifeOwner.Health, _wildlifeOwner.MaxHealth);
            return;
        }

        if (_owner == null)
            return;

        _owner.HealthChanged += UpdateBar;
        _owner.ReturnedHome += Hide;
        UpdateBar(_owner.Health, _owner.MaxHealth);
        Hide();
    }

    private void OnDisable()
    {
        if (_wildlifeOwner != null)
            _wildlifeOwner.HealthChanged -= UpdateWildlifeBar;
        if (_owner != null)
        {
            _owner.HealthChanged -= UpdateBar;
            _owner.ReturnedHome -= Hide;
        }
    }

    private void UpdateWildlifeBar(float currentHealth, float maxHealth)
    {
        UpdateBar(currentHealth, maxHealth);
        if (currentHealth <= 0f || currentHealth >= maxHealth)
            Hide();
    }

    private void UpdateBar(float currentHealth, float maxHealth)
    {
        if (_fill == null)
            return;

        float normalizedHealth = maxHealth > 0f
            ? Mathf.Clamp01(currentHealth / maxHealth)
            : 0f;

        Vector2 anchorMax = _fill.anchorMax;
        anchorMax.x = Mathf.Lerp(_emptyAnchorX, _fullAnchorX, normalizedHealth);
        _fill.anchorMax = anchorMax;

        if (currentHealth <= 0f)
            Hide();
        else if (currentHealth < maxHealth)
            Show();
    }

    private void Show()
    {
        if (_canvas == null)
            return;

        if (_label == null)
            CreateLabel();

        _canvas.enabled = true;
    }

    private void CreateLabel()
    {
        var labelObject = new GameObject("Name and level", typeof(RectTransform));
        var rect = (RectTransform)labelObject.transform;
        rect.SetParent(transform, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 0.008f);
        rect.sizeDelta = new Vector2(125f, 18f);
        rect.localScale = Vector3.one * 0.01f;

        _label = labelObject.AddComponent<TextMeshProUGUI>();
        _label.font = TMP_Settings.defaultFontAsset;
        _label.fontSize = 12f;
        _label.alignment = TextAlignmentOptions.Center;
        _label.textWrappingMode = TextWrappingModes.NoWrap;
        _label.raycastTarget = false;
        _label.richText = false;
        _label.color = new Color32(255, 236, 192, 255);

        string displayName = _wildlifeOwner != null ? _wildlifeOwner.DisplayName : _owner.DisplayName;
        int level = _wildlifeOwner != null ? _wildlifeOwner.Level : _owner.Level;
        _label.text = $"{displayName}  Lv. {level}";
    }

    private void Hide()
    {
        if (_canvas != null)
            _canvas.enabled = false;
    }
}
