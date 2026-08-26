using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerExperienceBarController : MonoBehaviour
{
    [SerializeField] private Image _experienceFill;
    [SerializeField] private TMP_Text _experienceText;

    private PlayerStat _playerStat;

    private void OnEnable() => TryBindPlayerStat();

    private void Start()
    {
        TryBindPlayerStat();
        RefreshFromPlayerStat();
    }

    private void Update()
    {
        if (_playerStat == null)
            TryBindPlayerStat();
    }

    private void OnDisable() => UnbindPlayerStat();

    public void Bind(PlayerStat playerStat)
    {
        if (_playerStat == playerStat)
        {
            RefreshFromPlayerStat();
            return;
        }

        UnbindPlayerStat();
        _playerStat = playerStat;

        if (_playerStat == null)
        {
            UpdateExperience(0, 1);
            return;
        }

        _playerStat.OnExperienceChanged += UpdateExperience;
        RefreshFromPlayerStat();
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

        _playerStat.OnExperienceChanged -= UpdateExperience;
        _playerStat = null;
    }

    private void RefreshFromPlayerStat()
    {
        if (_playerStat != null)
            UpdateExperience(_playerStat.CurrentExperience, _playerStat.ExperienceToNextLevel);
    }

    private void UpdateExperience(int current, int required)
    {
        int safeRequired = Mathf.Max(1, required);
        int safeCurrent = Mathf.Clamp(current, 0, safeRequired);

        if (_experienceFill != null)
            _experienceFill.fillAmount = (float)safeCurrent / safeRequired;

        if (_experienceText != null)
            _experienceText.text = $"{safeCurrent} / {safeRequired}";
    }
}
