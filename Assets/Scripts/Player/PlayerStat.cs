using System;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerStat : MonoBehaviour
{
    public static PlayerStat Instance { get; private set; }
    public SkillProgressionState Skills { get; } = new();
    [SerializeField] private SkillCatalog _skillCatalog;

    [Header("Level 1 Base Stats")]
    [SerializeField, Min(1f)] private float _maxHealth = 100f;
    [FormerlySerializedAs("_baseAtkDmg")]
    [SerializeField, Min(0.1f)] private float _attackDamage = 10f;
    [FormerlySerializedAs("_atkDmg")]
    [SerializeField, HideInInspector] private float _legacyAttackDamage;
    [SerializeField, Min(0f)] private float _defense = 2f;
    [SerializeField, Min(0f)] private float _moveSpeed = 2f;
    [SerializeField, Min(1f)] private float _sprintMultiplier = 2f;
    [SerializeField, Min(1f)] private float _maxStamina = 100f;
    [SerializeField, Min(1f)] private float _maxMana = 100f;
    [SerializeField, Min(0f)] private float _staminaDrainPerSecond = 25f;
    [SerializeField, Min(0f)] private float _staminaRegenerationPerSecond = 18f;
    [SerializeField, Min(0f)] private float _attackStaminaCost = 15f;
    [SerializeField, Range(0f, 0.75f)] private float _criticalChance = 0.05f;
    [SerializeField, Range(1f, 3f)] private float _criticalMultiplier = 1.5f;
    [SerializeField, Range(0f, 0.75f)] private float _damageReduction;
    [SerializeField, Min(0f)] private float _healthRegeneration;
    [SerializeField, Range(0f, 0.5f)] private float _dodgeChance;

    [Header("Shield Block (hold RMB)")]
    [SerializeField, Min(0f)] private float _manaRegenerationPerSecond = 3f;
    [SerializeField, Min(0f)] private float _manaRegenerationDelay = 2f;
    private float _manaRegenerateAfter;
    private float _shieldReadyAt;
    public const float ShieldCooldownSeconds = 5f;
    public float ShieldCooldownRemaining => Mathf.Max(0f, _shieldReadyAt - Time.time);
    public bool CanStartShield => !IsDead && ShieldCooldownRemaining <= 0f;

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
    [SerializeField] private float _stamina;
    [SerializeField] private float _mana;

    private PlayerStatModifiers _equipmentModifiers;

    public event Action OnStatsChanged;
    public event Action<float, float> OnHealthChanged;
    public event Action<float, float> OnStaminaChanged;
    public event Action<float, float> OnManaChanged;
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
    public float Stamina => _stamina;
    public float MaxStamina => Mathf.Max(1f, _maxStamina);
    public float Mana => _mana;
    public float MaxMana => Mathf.Max(1f, _maxMana);
    public bool HasStamina => _stamina > 0f;
    public float CriticalChance => Mathf.Clamp(_criticalChance + LevelBonus(_criticalChancePerLevel) + _equipmentModifiers.criticalChance, 0f, 0.75f);
    public float CriticalMultiplier => Mathf.Clamp(_criticalMultiplier + _equipmentModifiers.criticalMultiplier, 1f, 3f);
    public float DamageReduction => Mathf.Clamp(_damageReduction + _equipmentModifiers.damageReduction, 0f, 0.75f);
    public float HealthRegeneration => Mathf.Max(0f, _healthRegeneration + _equipmentModifiers.healthRegeneration);
    public float DodgeChance => Mathf.Clamp(_dodgeChance + _equipmentModifiers.dodgeChance, 0f, 0.5f);
    public bool IsDead => _health <= 0f;

    /// <summary>Dev-only invulnerability toggle (God Mode). Not persisted; defaults to false on
    /// every scene load. Set exclusively by DevPanelController.</summary>
    public bool IsInvulnerable { get; set; }

    /// <summary>True while the player is holding the Shield block (RMB). Set exclusively by
    /// PlayerShieldBlock; not persisted (resets to false on every scene load like IsInvulnerable).
    /// Blocks one hit for free, then breaks with a five-second lockout.</summary>
    public bool IsBlocking { get; set; }

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
        _stamina = MaxStamina;
        _mana = MaxMana;
        // Resolve content once at the player composition boundary, independently of UI lifetime.
        Skills.ConfigureCatalog(_skillCatalog != null ? _skillCatalog : Resources.Load<SkillCatalog>("Skills/PlayerSkillCatalog"));
        Skills.Restore(null, _level);
    }

    public PlayerDamageResult ReceiveDamage(float rawDamage)
    {
        if (IsDead || rawDamage <= 0f || IsInvulnerable)
            return new PlayerDamageResult(PlayerDamageOutcome.Ignored, 0f);

        if (IsBlocking && ShieldCooldownRemaining <= 0f)
        {
            IsBlocking = false;
            _shieldReadyAt = Time.time + ShieldCooldownSeconds;
            return new PlayerDamageResult(PlayerDamageOutcome.Blocked, 0f);
        }

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

    /// <summary>Restores stamina directly, mirroring Heal's clamp/event pattern.</summary>
    public void RestoreStamina(float amount)
    {
        if (amount <= 0f || _stamina >= MaxStamina)
            return;

        _stamina = Mathf.Min(_stamina + amount, MaxStamina);
        OnStaminaChanged?.Invoke(_stamina, MaxStamina);
    }

    // Confirmed skill casts consume mana; recovery waits after each positive spend.
    public bool TryConsumeMana(float cost)
    {
        if (float.IsNaN(cost) || float.IsInfinity(cost) || cost < 0f) return false;
        if (IsDead || _mana < cost) return false;
        if (cost == 0f) return true;
        _mana -= cost;
        _manaRegenerateAfter = Time.time + _manaRegenerationDelay;
        OnManaChanged?.Invoke(_mana, MaxMana);
        return true;
    }

    public void RestoreMana(float amount)
    {
        if (float.IsNaN(amount) || float.IsInfinity(amount) || amount <= 0f || _mana >= MaxMana) return;
        _mana = Mathf.Min(_mana + amount, MaxMana);
        OnManaChanged?.Invoke(_mana, MaxMana);
    }

    public void TickRegeneration(float deltaTime)
    {
        if (IsDead || deltaTime <= 0f || !GameStateManager.AllowsGameplayInput) return;
        if (Time.time >= _manaRegenerateAfter)
            RestoreMana(_manaRegenerationPerSecond * deltaTime);
        if (HealthRegeneration > 0f)
            Heal(HealthRegeneration * deltaTime);
    }

    public void TickStamina(bool isSprinting, float deltaTime)
    {
        if (deltaTime <= 0f || IsDead)
            return;

        float previous = _stamina;
        float change = isSprinting ? -_staminaDrainPerSecond : _staminaRegenerationPerSecond;
        _stamina = Mathf.Clamp(_stamina + change * deltaTime, 0f, MaxStamina);

        if (!Mathf.Approximately(previous, _stamina))
            OnStaminaChanged?.Invoke(_stamina, MaxStamina);
    }

    public bool TryConsumeAttackStamina()
    {
        if (_attackStaminaCost <= 0f)
            return true;
        if (_stamina < _attackStaminaCost)
            return false;

        _stamina -= _attackStaminaCost;
        OnStaminaChanged?.Invoke(_stamina, MaxStamina);
        return true;
    }

    /// <summary>Spends `cost` stamina if available (all-or-nothing). Used by actions other than the
    /// basic attack, e.g. the dash. A non-positive cost is free.</summary>
    public bool TryConsumeStamina(float cost)
    {
        if (cost <= 0f)
            return true;
        if (_stamina < cost)
            return false;

        _stamina -= cost;
        OnStaminaChanged?.Invoke(_stamina, MaxStamina);
        return true;
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
            Skills.AwardLevel(_level);
            _health = Mathf.Min(_health + (MaxHealth - previousMaxHealth), MaxHealth);
            SoundFXManager.PlaySfx(SfxIds.PlayerLevelUp);
            OnLevelUp?.Invoke(_level);
            OnStatsChanged?.Invoke();
        }

        OnExperienceChanged?.Invoke(_currentExperience, ExperienceToNextLevel);
        OnHealthChanged?.Invoke(_health, MaxHealth);
    }

    /// <summary>Applies saved progression without firing OnLevelUp/reward-adjacent events.
    /// health &lt; 0 restores to full MaxHealth (fresh New Game character).</summary>
    public void RestoreProgression(int level, int currentExperience, float health)
    {
        RestoreProgression(level, currentExperience);
        RestoreHealth(health);
    }

    /// <summary>Restores level/experience only, without touching health. Use this before
    /// restoring equipment (so MaxHealth reflects final modifiers), then call RestoreHealth
    /// afterward to clamp the saved health against the final MaxHealth.</summary>
    public void RestoreProgression(int level, int currentExperience)
    {
        _level = Mathf.Max(1, level);
        _currentExperience = Mathf.Max(0, currentExperience);

        OnStatsChanged?.Invoke();
        OnExperienceChanged?.Invoke(_currentExperience, ExperienceToNextLevel);
    }

    /// <summary>Sets health directly against the current MaxHealth (health &lt; 0 means full).
    /// Unlike ApplyEquipmentModifiers, this does not add a live-equip delta -- it sets the exact
    /// saved value, which is what restore needs.</summary>
    public void RestoreHealth(float health)
    {
        _health = health < 0f ? MaxHealth : Mathf.Clamp(health, 0f, MaxHealth);
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
        _maxStamina = Mathf.Max(1f, _maxStamina);
        _maxMana = Mathf.Max(1f, _maxMana);
        _staminaDrainPerSecond = Mathf.Max(0f, _staminaDrainPerSecond);
        _staminaRegenerationPerSecond = Mathf.Max(0f, _staminaRegenerationPerSecond);
        _attackStaminaCost = Mathf.Max(0f, _attackStaminaCost);
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
