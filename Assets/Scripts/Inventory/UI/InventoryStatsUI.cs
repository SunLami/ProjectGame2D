using TMPro;
using UnityEngine;

public sealed class InventoryStatsUI : MonoBehaviour
{
    [SerializeField] private TMP_Text[] _valueTexts;
    private PlayerStat _stats;

    private void OnEnable()
    {
        _stats = PlayerStat.Instance != null
            ? PlayerStat.Instance
            : FindAnyObjectByType<PlayerStat>();
        if (_stats == null) return;
        _stats.OnStatsChanged += Refresh;
        _stats.OnHealthChanged += HandleResourceChanged;
        _stats.OnStaminaChanged += HandleResourceChanged;
        Refresh();
    }

    private void OnDisable()
    {
        if (_stats == null) return;
        _stats.OnStatsChanged -= Refresh;
        _stats.OnHealthChanged -= HandleResourceChanged;
        _stats.OnStaminaChanged -= HandleResourceChanged;
        _stats = null;
    }

    private void HandleResourceChanged(float current, float maximum) => Refresh();

    private void Refresh()
    {
        if (_stats == null || _valueTexts == null || _valueTexts.Length < 6) return;
        _valueTexts[0].text = $"{_stats.Health:0}/{_stats.MaxHealth:0}";
        _valueTexts[1].text = _stats.AttackDamage.ToString("0.0");
        _valueTexts[2].text = _stats.Defense.ToString("0.0");
        _valueTexts[3].text = _stats.MoveSpeed.ToString("0.0");
        _valueTexts[4].text = $"{_stats.CriticalChance * 100f:0}%";
        _valueTexts[5].text = $"{_stats.Stamina:0}/{_stats.MaxStamina:0}";
    }
}
