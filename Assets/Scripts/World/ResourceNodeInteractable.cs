using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class ResourceNodeInteractable : MonoBehaviour, IPersistentWorldObject
{
    [SerializeField] private string _persistentId;
    [SerializeField] private string _areaId;
    [SerializeField] private ResourceNodeDefinition _definition;
    [SerializeField] private GameObject _visualRoot;
    [SerializeField] private SpriteRenderer _flashRenderer;

    private long _nextRespawnUtcTicks;
    private float _currentHealth;
    private bool _isResolving;
    private Coroutine _respawnRoutine;
    private IItemResolver _legacyItemResolver;
    private string _legacyResourceId;
    private string _legacyItemId;
    private int _legacyQuantity = 1;
    private float _legacyRespawnSeconds = 60f;
    private ResourceHarvestType _legacyHarvestType = ResourceHarvestType.Gathering;

    public string PersistentId => _persistentId;
    public WorldObjectKind Kind => WorldObjectKind.ResourceNode;
    public bool IsAvailable => !_isResolving && (_nextRespawnUtcTicks == 0 || DateTime.UtcNow.Ticks >= _nextRespawnUtcTicks);
    public ResourceHarvestType HarvestType => _definition != null ? _definition.HarvestType : _legacyHarvestType;
    public float CurrentHealth => _currentHealth;
    public ResourceNodeDefinition Definition => _definition;

    private void Awake()
    {
        _currentHealth = MaximumHealth;
        ApplyVisual();
    }

    private void OnEnable() => ScheduleRespawnIfNeeded();

    private void OnDisable()
    {
        if (_respawnRoutine == null) return;
        StopCoroutine(_respawnRoutine);
        _respawnRoutine = null;
    }

    public bool TryApplyHarvestHit(HarvestToolType equippedTool = HarvestToolType.None)
    {
        if (!IsAvailable || HarvestType == ResourceHarvestType.Gathering || !CanUseTool(equippedTool))
            return false;

        _currentHealth = Mathf.Max(0f, _currentHealth - HarvestDamage);
        StartCoroutine(FlashRoutine(0.12f));
        if (_currentHealth <= 0f)
            TryBeginLootResolution();
        return true;
    }

    public bool TryBeginGathering()
    {
        if (!IsAvailable || HarvestType != ResourceHarvestType.Gathering)
            return false;

        StartCoroutine(GatherRoutine());
        return true;
    }

    // Compatibility path for the existing proximity UI and Phase 8 fixtures. New authored
    // ResourceNodeDefinition instances use attack/click flows above.
    public bool TryHarvest(out bool granted)
    {
        granted = false;
        if (_definition != null)
            return HarvestType == ResourceHarvestType.Gathering && TryBeginGathering();
        if (!IsAvailable || _legacyItemResolver == null
            || !_legacyItemResolver.TryResolve(_legacyItemId, out ItemSO item)
            || InventoryManager.Instance == null
            || !InventoryManager.Instance.HasCapacityFor(item, _legacyQuantity))
            return false;

        InventoryManager.Instance.AddItem(item, _legacyQuantity);
        QuestDomainEvents.RaiseResourceGathered(_legacyResourceId, _legacyQuantity, _areaId);
        _nextRespawnUtcTicks = DateTime.UtcNow.Ticks + TimeSpan.FromSeconds(_legacyRespawnSeconds).Ticks;
        granted = true;
        WorldDomainEvents.RaiseWorldObjectChanged();
        ScheduleRespawnIfNeeded();
        return true;
    }

    private IEnumerator GatherRoutine()
    {
        _isResolving = true;
        float duration = _definition != null ? _definition.GatheringDuration : 1.2f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetRendererVisible(Mathf.FloorToInt(elapsed / 0.12f) % 2 == 0);
            yield return null;
        }

        SetRendererVisible(true);
        _isResolving = false;
        TryBeginLootResolution();
    }

    private bool TryBeginLootResolution()
    {
        if (_isResolving || !TryRollLoot(out List<InventoryItemGrant> grants))
        {
            ResetRuntimeState();
            return false;
        }

        InventoryManager inventory = InventoryManager.Instance;
        if (inventory == null || !inventory.HasCapacityForBatch(grants))
        {
            ResetRuntimeState();
            return false;
        }

        _isResolving = true;
        StartCoroutine(LootRoutine(grants));
        return true;
    }

    private IEnumerator LootRoutine(List<InventoryItemGrant> grants)
    {
        Transform player = FindPlayer();
        if (player == null)
        {
            ResetRuntimeState();
            yield break;
        }

        SetNodeVisible(false);
        yield return ResourceLootFlyVisual.Play(transform.position, player, grants);

        InventoryManager inventory = InventoryManager.Instance;
        if (inventory == null || !inventory.TryAddBatch(grants))
        {
            ResetRuntimeState();
            yield break;
        }

        int gatheredQuantity = 0;
        foreach (InventoryItemGrant grant in grants)
            gatheredQuantity += grant.Quantity;
        QuestDomainEvents.RaiseResourceGathered(ResourceId, Mathf.Max(1, gatheredQuantity), _areaId);

        _nextRespawnUtcTicks = DateTime.UtcNow.Ticks + TimeSpan.FromSeconds(RespawnSeconds).Ticks;
        _isResolving = false;
        WorldDomainEvents.RaiseWorldObjectChanged();
        ScheduleRespawnIfNeeded();
    }

    private bool TryRollLoot(out List<InventoryItemGrant> grants)
    {
        grants = new List<InventoryItemGrant>();
        if (_definition != null)
        {
            ResourceLootEntry[] table = _definition.LootTable;
            if (table != null)
            {
                foreach (ResourceLootEntry entry in table)
                {
                    if (entry != null && entry.TryRoll(out InventoryItemGrant grant))
                        grants.Add(grant);
                }
            }
            return grants.Count > 0;
        }

        if (_legacyItemResolver != null && _legacyItemResolver.TryResolve(_legacyItemId, out ItemSO item))
            grants.Add(new InventoryItemGrant(item, Mathf.Max(1, _legacyQuantity)));
        return grants.Count > 0;
    }

    private bool CanUseTool(HarvestToolType equippedTool)
    {
        HarvestToolType required = _definition != null ? _definition.RequiredToolType : HarvestToolType.None;
        return required == HarvestToolType.None || required == equippedTool;
    }

    private IEnumerator FlashRoutine(float interval)
    {
        SetRendererVisible(false);
        yield return new WaitForSecondsRealtime(interval);
        SetRendererVisible(true);
    }

    private void ScheduleRespawnIfNeeded()
    {
        if (_respawnRoutine != null)
            StopCoroutine(_respawnRoutine);

        if (_nextRespawnUtcTicks == 0 || DateTime.UtcNow.Ticks >= _nextRespawnUtcTicks)
        {
            RespawnNow();
            return;
        }

        SetNodeVisible(false);
        double remaining = TimeSpan.FromTicks(_nextRespawnUtcTicks - DateTime.UtcNow.Ticks).TotalSeconds;
        _respawnRoutine = StartCoroutine(RespawnAfterRealtime((float)Math.Max(0d, remaining)));
    }

    private IEnumerator RespawnAfterRealtime(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        _respawnRoutine = null;
        RespawnNow();
    }

    private void RespawnNow()
    {
        _nextRespawnUtcTicks = 0;
        ResetRuntimeState();
    }

    private void ResetRuntimeState()
    {
        _currentHealth = MaximumHealth;
        _isResolving = false;
        SetNodeVisible(true);
        SetRendererVisible(true);
    }

    private void ApplyVisual()
    {
        bool available = _nextRespawnUtcTicks == 0 || DateTime.UtcNow.Ticks >= _nextRespawnUtcTicks;
        SetNodeVisible(available);
        if (available) _currentHealth = MaximumHealth;
    }

    private void SetNodeVisible(bool visible)
    {
        if (_visualRoot != null) _visualRoot.SetActive(visible);
        else if (_flashRenderer != null) _flashRenderer.enabled = visible;
    }

    private void SetRendererVisible(bool visible)
    {
        if (_flashRenderer != null) _flashRenderer.enabled = visible;
    }

    private static Transform FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.transform : null;
    }

    private float MaximumHealth => _definition != null ? _definition.MaximumHealth : 1f;
    private float HarvestDamage => _definition != null ? _definition.HarvestDamage : 1f;
    private float RespawnSeconds => _definition != null ? _definition.RespawnSeconds : _legacyRespawnSeconds;
    private string ResourceId => _definition != null ? _definition.ResourceId : _legacyResourceId;

    public WorldObjectState CaptureState() => new(false, _nextRespawnUtcTicks);

    public void RestoreState(WorldObjectState state)
    {
        _nextRespawnUtcTicks = state.NextRespawnUtcTicks;
        _isResolving = false;
        ApplyVisual();
        ScheduleRespawnIfNeeded();
    }

    internal void ConfigureForTests(
        string persistentId, string resourceId, string itemId, int quantity, float respawnSeconds,
        IItemResolver itemResolver, ResourceHarvestType harvestType = ResourceHarvestType.Gathering)
    {
        _persistentId = persistentId;
        _legacyResourceId = resourceId;
        _legacyItemId = itemId;
        _legacyQuantity = quantity;
        _legacyRespawnSeconds = respawnSeconds;
        _legacyItemResolver = itemResolver;
        _legacyHarvestType = harvestType;
        _currentHealth = MaximumHealth;
    }

    internal void ConfigureDefinitionForTests(string persistentId, string areaId, ResourceNodeDefinition definition)
    {
        _persistentId = persistentId;
        _areaId = areaId;
        _definition = definition;
        _currentHealth = MaximumHealth;
    }
}
