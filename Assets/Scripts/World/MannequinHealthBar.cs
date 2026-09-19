using UnityEngine;

/// <summary>World-space health bar for a training mannequin, mirroring EnemyHealthBar's
/// presentation pattern (hidden at full health, shown once damaged) but bound to
/// MannequinHurtbox instead of EnemyUniversal -- the two targets aren't a shared base type.</summary>
public sealed class MannequinHealthBar : MonoBehaviour
{
    [SerializeField] private MannequinHurtbox _owner;
    [SerializeField] private RectTransform _fill;
    private Canvas _canvas;
    private float _emptyAnchorX;
    private float _fullAnchorX;

    private void Awake()
    {
        if (_owner == null)
            _owner = GetComponentInParent<MannequinHurtbox>();

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
            _owner = GetComponentInParent<MannequinHurtbox>();

        if (_owner == null)
            return;

        _owner.HealthChanged += UpdateBar;
        _owner.Respawned += Hide;
        UpdateBar(_owner.Health, _owner.MaxHealth);
        Hide();
    }

    private void OnDisable()
    {
        if (_owner != null)
        {
            _owner.HealthChanged -= UpdateBar;
            _owner.Respawned -= Hide;
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

        if (currentHealth < maxHealth && currentHealth > 0f)
            Show();
        else if (currentHealth <= 0f)
            Hide();
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
