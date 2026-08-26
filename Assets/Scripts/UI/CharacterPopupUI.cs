using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CharacterPopupUI : MonoBehaviour
{
    [Serializable]
    private struct EquipmentSlotBinding
    {
        public EquipSlot slot;
        public Image icon;
    }

    [Header("Equipment Preview (Read Only)")]
    [SerializeField] private EquipmentSlotBinding[] _equipmentSlots;

    [Header("Character Stats")]
    [SerializeField] private TMP_Text _levelValue;
    [SerializeField] private TMP_Text _vitalsValues;
    [SerializeField] private TMP_Text _combatValues;
    [SerializeField] private TMP_Text _mobilityValues;
    [SerializeField] private TMP_Text _recoveryValues;

    private PlayerStat _playerStat;
    private EquipmentManager _equipmentManager;

    private void OnEnable()
    {
        BindRuntimeSources();
        RefreshAll();
    }

    private void Update()
    {
        if (_playerStat == null || _equipmentManager == null)
            BindRuntimeSources();
    }

    private void OnDisable()
    {
        UnbindRuntimeSources();
    }

    private void BindRuntimeSources()
    {
        PlayerStat playerStat = PlayerStat.Instance != null
            ? PlayerStat.Instance
            : FindAnyObjectByType<PlayerStat>();
        EquipmentManager equipmentManager = EquipmentManager.Instance != null
            ? EquipmentManager.Instance
            : FindAnyObjectByType<EquipmentManager>();

        if (_playerStat != playerStat)
        {
            if (_playerStat != null)
                _playerStat.OnStatsChanged -= RefreshStats;

            _playerStat = playerStat;
            if (_playerStat != null)
                _playerStat.OnStatsChanged += RefreshStats;
        }

        if (_equipmentManager != equipmentManager)
        {
            if (_equipmentManager != null)
                _equipmentManager.OnEquipmentChanged -= HandleEquipmentChanged;

            _equipmentManager = equipmentManager;
            if (_equipmentManager != null)
                _equipmentManager.OnEquipmentChanged += HandleEquipmentChanged;
        }

        RefreshAll();
    }

    private void UnbindRuntimeSources()
    {
        if (_playerStat != null)
            _playerStat.OnStatsChanged -= RefreshStats;
        if (_equipmentManager != null)
            _equipmentManager.OnEquipmentChanged -= HandleEquipmentChanged;

        _playerStat = null;
        _equipmentManager = null;
    }

    private void RefreshAll()
    {
        RefreshStats();
        RefreshEquipment();
    }

    private void HandleEquipmentChanged()
    {
        RefreshEquipment();
        RefreshStats();
    }

    private void RefreshEquipment()
    {
        if (_equipmentSlots == null)
            return;

        foreach (EquipmentSlotBinding binding in _equipmentSlots)
        {
            if (binding.icon == null)
                continue;

            EquipmentItemSO item = _equipmentManager != null
                ? _equipmentManager.GetEquipped(binding.slot)
                : null;
            binding.icon.sprite = item != null ? item.icon : null;
            binding.icon.enabled = item != null;
        }
    }

    private void RefreshStats()
    {
        if (_playerStat == null)
            return;

        SetText(_levelValue, $"LV. {_playerStat.Level}");
        SetText(_vitalsValues,
            $"{_playerStat.Health:0} / {_playerStat.MaxHealth:0}\n"
            + $"{_playerStat.Stamina:0} / {_playerStat.MaxStamina:0}");
        SetText(_combatValues,
            $"{_playerStat.AttackDamage:0.0}\n"
            + $"{_playerStat.Defense:0.0}\n"
            + $"{_playerStat.CriticalChance * 100f:0.0}%\n"
            + $"x{_playerStat.CriticalMultiplier:0.00}");
        SetText(_mobilityValues,
            $"{_playerStat.MoveSpeed:0.0}\n"
            + $"x{_playerStat.SprintMultiplier:0.00}\n"
            + $"{_playerStat.DodgeChance * 100f:0.0}%");
        SetText(_recoveryValues,
            $"{_playerStat.DamageReduction * 100f:0.0}%\n"
            + $"{_playerStat.HealthRegeneration:0.0} /s");
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
            target.text = value;
    }
}
