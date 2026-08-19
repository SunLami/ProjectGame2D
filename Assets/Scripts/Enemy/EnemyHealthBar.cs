using UnityEngine;

public sealed class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private EnemyUniversal _owner;
    [SerializeField] private RectTransform _fill;
    private Canvas _canvas;
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

        if (_owner == null)
            return;

        _owner.HealthChanged += UpdateBar;
        _owner.ReturnedHome += Hide;
        UpdateBar(_owner.Health, _owner.MaxHealth);
        Hide();
    }

    private void OnDisable()
    {
        if (_owner != null)
        {
            _owner.HealthChanged -= UpdateBar;
            _owner.ReturnedHome -= Hide;
        }
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

        if (currentHealth < maxHealth)
            Show();
    }

    private void Show()
    {
        if (_canvas != null)
            _canvas.enabled = true;
    }

    private void Hide()
    {
        if (_canvas != null)
            _canvas.enabled = false;
    }
}
