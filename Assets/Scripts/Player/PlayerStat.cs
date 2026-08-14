using System;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerStat : MonoBehaviour
{
    public static PlayerStat Instance { get; private set; }

    [Header("Level 1 Base Stats")]
    [SerializeField, Min(1f)] private float _maxHealth = 100f;
    [FormerlySerializedAs("_baseAtkDmg")]
    [SerializeField, Min(0.1f)] private float _attackDamage = 10f;
    [FormerlySerializedAs("_atkDmg")]
    [SerializeField, HideInInspector] private float _legacyAttackDamage;
    [SerializeField, Min(0f)] private float _defense = 2f;
    [SerializeField, Min(0f)] private float _moveSpeed = 2f;
    [SerializeField, Min(1f)] private float _sprintMultiplier = 2f;
    [SerializeField, Range(0f, 0.75f)] private float _criticalChance = 0.05f;
    [SerializeField, Range(1f, 3f)] private float _criticalMultiplier = 1.5f;
    [SerializeField, Range(0f, 0.75f)] private float _damageReduction;
    [SerializeField, Min(0f)] private float _healthRegeneration;
    [SerializeField, Range(0f, 0.5f)] private float _dodgeChance;

    [Header("Growth Per Level")]
    [SerializeField, Min(0f)] private float _healthPerLevel = 3f;
    [SerializeField, Min(0f)] private float _attackPerLevel = 0.5f;
    [SerializeField, Min(0f)] private float _defensePerLevel = 0.15f;
    [SerializeField, Range(0f, 0.01f)] private float _criticalChancePerLevel = 0.001f;

    [Header("Progression")]
    [SerializeField, Min(1)] private int _level = 1;
    [SerializeField, Min(0)] private int _currentExperience;

    [Header("Runtime")]
    [SerializeField] private float _health;

    private PlayerStatModifiers _equipmentModifiers;

    public event Action OnStatsChanged;
    public event Action<float, float> OnHealthChanged;
    public event Action<int> OnLevelUp;
    public event Action<int, int> OnExperienceChanged;

    public int Level => _level;
    public int CurrentExperience => _currentExperience;
    public int ExperienceToNextLevel => CalculateExperienceRequirement(_level);
    public float Health => _health;
    public float MaxHealth => Mathf.Max(1f, _maxHealth + LevelBonus(_healthPerLevel) + _equipmentModifiers.maxHealth);
    public float AttackDamage => Mathf.Max(0f, _attackDamage + LevelBonus(_attackPerLevel) + _equipmentModifiers.attackDamage);
    public float Defense => Mathf.Max(0f, _defense + LevelBonus(_defensePerLevel) + _equipmentModifiers.defense);
    public float MoveSpeed => Mathf.Max(0f, _moveSpeed + _equipmentModifiers.moveSpeed);
    public float SprintMultiplier => Mathf.Max(1f, _sprintMultiplier + _equipmentModifiers.sprintMultiplier);
    public float CriticalChance => Mathf.Clamp(_criticalChance + LevelBonus(_criticalChancePerLevel) + _equipmentModifiers.criticalChance, 0f, 0.75f);
    public float CriticalMultiplier => Mathf.Clamp(_criticalMultiplier + _equipmentModifiers.criticalMultiplier, 1f, 3f);
    public float DamageReduction => Mathf.Clamp(_damageReduction + _equipmentModifiers.damageReduction, 0f, 0.75f);
    public float HealthRegeneration => Mathf.Max(0f, _healthRegeneration + _equipmentModifiers.healthRegeneration);
    public float DodgeChance => Mathf.Clamp(_dodgeChance + _equipmentModifiers.dodgeChance, 0f, 0.5f);
    public bool IsDead => _health <= 0f;

    // Compatibility with existing code that used the old property names.
    public float BaseAtkDmg => _attackDamage;
    public float AtkDmg { get => AttackDamage; set => _attackDamage = Mathf.Max(0f, value); }

    private float LevelBonus(float growth) => (_level - 1) * growth;

    private void Awake()
    {
        MigrateLegacyAttackDamage();

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _health = MaxHealth;
    }

    public PlayerDamageResult ReceiveDamage(float rawDamage)
    {
        if (IsDead || rawDamage <= 0f)
            return new PlayerDamageResult(PlayerDamageOutcome.Ignored, 0f);

        if (UnityEngine.Random.value < DodgeChance)
            return new PlayerDamageResult(PlayerDamageOutcome.Dodged, 0f);

        float afterDefense = Mathf.Max(1f, rawDamage - Defense);
        float finalDamage = afterDefense * (1f - DamageReduction);
        _health = Mathf.Clamp(_health - finalDamage, 0f, MaxHealth);
        OnHealthChanged?.Invoke(_health, MaxHealth);

        return new PlayerDamageResult(
            IsDead ? PlayerDamageOutcome.Killed : PlayerDamageOutcome.Damaged,
            finalDamage);
    }

    public bool TakeDamage(float amount) => ReceiveDamage(amount).Outcome == PlayerDamageOutcome.Killed;

    public float RollOutgoingDamage(out bool isCritical)
    {
        isCritical = UnityEngine.Random.value < CriticalChance;
        return isCritical ? AttackDamage * CriticalMultiplier : AttackDamage;
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f || _health >= MaxHealth)
            return;

        _health = Mathf.Min(_health + amount, MaxHealth);
        OnHealthChanged?.Invoke(_health, MaxHealth);
    }

    public void TickRegeneration(float deltaTime)
    {
        if (deltaTime > 0f && HealthRegeneration > 0f)
            Heal(HealthRegeneration * deltaTime);
    }

    public void AddExperience(int amount)
    {
        if (amount <= 0)
            return;

        _currentExperience += amount;
        while (_currentExperience >= ExperienceToNextLevel)
        {
            int requirement = ExperienceToNextLevel;
            float previousMaxHealth = MaxHealth;
            _currentExperience -= requirement;
            _level++;
            _health = Mathf.Min(_health + (MaxHealth - previousMaxHealth), MaxHealth);
            OnLevelUp?.Invoke(_level);
            OnStatsChanged?.Invoke();
        }

        OnExperienceChanged?.Invoke(_currentExperience, ExperienceToNextLevel);
        OnHealthChanged?.Invoke(_health, MaxHealth);
    }

    public void ApplyEquipmentModifiers(PlayerStatModifiers modifiers)
    {
        float previousMaxHealth = MaxHealth;
        _equipmentModifiers = modifiers;
        _health = Mathf.Clamp(_health + Mathf.Max(0f, MaxHealth - previousMaxHealth), 0f, MaxHealth);
        OnStatsChanged?.Invoke();
        OnHealthChanged?.Invoke(_health, MaxHealth);
    }

    public void SetBaseMoveSpeed(float value)
    {
        _moveSpeed = Mathf.Max(0f, value);
        OnStatsChanged?.Invoke();
    }

    public static int CalculateExperienceRequirement(int level)
    {
        return Mathf.Max(1, Mathf.RoundToInt(100f * Mathf.Pow(Mathf.Max(1, level), 1.35f)));
    }

    private void OnValidate()
    {
        MigrateLegacyAttackDamage();
        _maxHealth = Mathf.Max(1f, _maxHealth);
        _attackDamage = Mathf.Max(0.1f, _attackDamage);
        _defense = Mathf.Max(0f, _defense);
        _moveSpeed = Mathf.Max(0f, _moveSpeed);
        _sprintMultiplier = Mathf.Max(1f, _sprintMultiplier);
        _level = Mathf.Max(1, _level);
        _currentExperience = Mathf.Max(0, _currentExperience);
    }

    private void MigrateLegacyAttackDamage()
    {
        if (_attackDamage > 0f)
            return;

        _attackDamage = _legacyAttackDamage > 0f ? _legacyAttackDamage : 10f;
    }
}
