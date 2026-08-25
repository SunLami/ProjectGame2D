using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class PlayerHUDController : MonoBehaviour
{
    [SerializeField] private Image _healthFill;
    [SerializeField] private Image _staminaFill;
    [SerializeField] private TMP_Text _levelText;

    private PlayerStat _playerStat;

    private void OnEnable()
    {
        TryBindPlayerStat();
    }

    private void Update()
    {
        if (_playerStat == null)
            TryBindPlayerStat();
    }

    private void OnDisable()
    {
        UnbindPlayerStat();
    }

    public void Bind(PlayerStat playerStat)
    {
        if (_playerStat == playerStat)
            return;

        UnbindPlayerStat();
        _playerStat = playerStat;

        if (_playerStat == null)
            return;

        _playerStat.OnHealthChanged += UpdateHealth;
        _playerStat.OnStaminaChanged += UpdateStamina;
        _playerStat.OnLevelUp += UpdateLevel;
        UpdateHealth(_playerStat.Health, _playerStat.MaxHealth);
        UpdateStamina(_playerStat.Stamina, _playerStat.MaxStamina);
        UpdateLevel(_playerStat.Level);
    }

    private void TryBindPlayerStat()
    {
        Bind(PlayerStat.Instance != null
            ? PlayerStat.Instance
            : FindAnyObjectByType<PlayerStat>());
    }

    private void UnbindPlayerStat()
    {
        if (_playerStat == null)
            return;

        _playerStat.OnHealthChanged -= UpdateHealth;
        _playerStat.OnStaminaChanged -= UpdateStamina;
        _playerStat.OnLevelUp -= UpdateLevel;
        _playerStat = null;
    }

    private void UpdateHealth(float current, float maximum)
    {
        SetFill(_healthFill, current, maximum);
    }

    private void UpdateStamina(float current, float maximum)
    {
        SetFill(_staminaFill, current, maximum);
    }

    private void UpdateLevel(int level)
    {
        if (_levelText != null)
            _levelText.text = $"LV. {Mathf.Max(1, level)}";
    }

    private static void SetFill(Image image, float current, float maximum)
    {
        if (image != null)
            image.fillAmount = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
    }
}
