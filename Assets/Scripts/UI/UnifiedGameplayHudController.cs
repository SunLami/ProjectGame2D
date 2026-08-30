using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class UnifiedGameplayHudController : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private Image _healthFill;
    [SerializeField] private Image _staminaFill;
    [SerializeField] private Image _experienceFill;
    [SerializeField] private TMP_Text _experienceText;
    [SerializeField] private TMP_Text _levelText;

    [Header("Popups")]
    [SerializeField] private GameObject _characterPopup;
    [SerializeField] private TMP_Text _characterStatsText;
    [SerializeField] private GameObject _mapPopup;

    private PlayerStat _playerStat;

    private void OnEnable()
    {
        TryBindPlayerStat();
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.StateChanged += HandleStateChanged;
        RefreshPopups();
    }

    private void Start()
    {
        TryBindPlayerStat();
        RefreshAll();
    }

    private void Update()
    {
        if (_playerStat == null)
            TryBindPlayerStat();

        if (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame)
            OpenCharacterPopup();
    }

    private void OnDisable()
    {
        UnbindPlayerStat();
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.StateChanged -= HandleStateChanged;
    }

    public void OpenCharacterPopup() => TogglePage(GameplayMenuPage.Character);
    public void OpenMapPopup() => TogglePage(GameplayMenuPage.Map);
    public void ClosePopup() => GameStateManager.Instance?.ReturnToPreviousState();

    public void Bind(PlayerStat playerStat)
    {
        if (_playerStat == playerStat)
        {
            RefreshAll();
            return;
        }

        UnbindPlayerStat();
        _playerStat = playerStat;
        if (_playerStat == null)
            return;

        _playerStat.OnHealthChanged += UpdateHealth;
        _playerStat.OnStaminaChanged += UpdateStamina;
        _playerStat.OnExperienceChanged += UpdateExperience;
        _playerStat.OnLevelUp += UpdateLevel;
        _playerStat.OnStatsChanged += UpdateCharacterStats;
        RefreshAll();
    }

    private void TogglePage(GameplayMenuPage page)
    {
        GameStateManager state = GameStateManager.Instance;
        if (state == null)
            return;

        if (state.CurrentState == GameState.GameplayMenu && state.CurrentMenuPage == page)
            state.ReturnToPreviousState();
        else
            state.OpenMenu(page);
    }

    private void TryBindPlayerStat() => Bind(PlayerStat.Instance != null
        ? PlayerStat.Instance
        : FindAnyObjectByType<PlayerStat>());

    private void UnbindPlayerStat()
    {
        if (_playerStat == null)
            return;

        _playerStat.OnHealthChanged -= UpdateHealth;
        _playerStat.OnStaminaChanged -= UpdateStamina;
        _playerStat.OnExperienceChanged -= UpdateExperience;
        _playerStat.OnLevelUp -= UpdateLevel;
        _playerStat.OnStatsChanged -= UpdateCharacterStats;
        _playerStat = null;
    }

    private void RefreshAll()
    {
        if (_playerStat == null)
            return;

        UpdateHealth(_playerStat.Health, _playerStat.MaxHealth);
        UpdateStamina(_playerStat.Stamina, _playerStat.MaxStamina);
        UpdateExperience(_playerStat.CurrentExperience, _playerStat.ExperienceToNextLevel);
        UpdateLevel(_playerStat.Level);
        UpdateCharacterStats();
    }

    private void UpdateHealth(float current, float maximum) => SetFill(_healthFill, current, maximum);
    private void UpdateStamina(float current, float maximum) => SetFill(_staminaFill, current, maximum);

    private void UpdateExperience(int current, int required)
    {
        int safeRequired = Mathf.Max(1, required);
        int safeCurrent = Mathf.Clamp(current, 0, safeRequired);
        SetFill(_experienceFill, safeCurrent, safeRequired);
        if (_experienceText != null)
            _experienceText.text = $"{safeCurrent} / {safeRequired}";
    }

    private void UpdateLevel(int level)
    {
        if (_levelText != null)
            _levelText.text = $"LV. {Mathf.Max(1, level)}";
        UpdateCharacterStats();
    }

    private void UpdateCharacterStats()
    {
        if (_characterStatsText == null || _playerStat == null)
            return;

        _characterStatsText.text =
            $"LEVEL  {_playerStat.Level}\n\n"
            + $"HEALTH  {_playerStat.Health:0} / {_playerStat.MaxHealth:0}\n"
            + $"ATTACK  {_playerStat.AttackDamage:0.0}\n"
            + $"DEFENSE  {_playerStat.Defense:0.0}\n"
            + $"MOVE SPEED  {_playerStat.MoveSpeed:0.0}\n"
            + $"CRITICAL  {_playerStat.CriticalChance * 100f:0.0}%\n"
            + $"DODGE  {_playerStat.DodgeChance * 100f:0.0}%";
    }

    private void HandleStateChanged(GameStateChange change) => RefreshPopups();

    private void RefreshPopups()
    {
        GameStateManager state = GameStateManager.Instance;
        bool menuOpen = state != null && state.CurrentState == GameState.GameplayMenu;
        if (_characterPopup != null)
            _characterPopup.SetActive(menuOpen && state.CurrentMenuPage == GameplayMenuPage.Character);
        if (_mapPopup != null)
            _mapPopup.SetActive(menuOpen && state.CurrentMenuPage == GameplayMenuPage.Map);
        UpdateCharacterStats();
    }

    private static void SetFill(Image image, float current, float maximum)
    {
        if (image != null)
            image.fillAmount = maximum > 0f ? Mathf.Clamp01(current / maximum) : 0f;
    }
}
