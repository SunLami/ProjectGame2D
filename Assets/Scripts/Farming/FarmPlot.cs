using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class FarmPlot : MonoBehaviour
{
    [SerializeField] private string _plotId;
    [SerializeField] private string _areaId = "area.farm";
    [SerializeField] private SpriteRenderer _cropRenderer;
    [SerializeField] private GameObject _emptyPlotHighlight;

    private FarmingManager _manager;
    private CropDefinition _crop;
    private long _plantedAtUtcTicks;
    private bool _isResolving;
    private bool _highlighted;
    private int _lastStage = -1;
    private Color _baseCropColor = Color.white;

    public string PlotId => _plotId;
    public bool HasCrop => _crop != null;
    public bool IsMature => _crop != null && CurrentStageIndex >= _crop.MatureStageIndex;
    public bool IsResolving => _isResolving;
    public CropDefinition Crop => _crop;
    private int CurrentStageIndex => _crop?.GetStageIndex(_plantedAtUtcTicks, DateTime.UtcNow.Ticks) ?? -1;

    private void Awake()
    {
        if (_cropRenderer != null) _baseCropColor = _cropRenderer.color;
        SetHighlighted(false);
        ApplyVisual();
    }

    private void Update()
    {
        if (_crop != null && CurrentStageIndex != _lastStage) ApplyVisual();
    }

    internal void Bind(FarmingManager manager) => _manager = manager;

    public bool IsInteractionAvailable(ItemSO selectedItem)
    {
        if (_isResolving) return false;
        if (_crop != null) return IsMature;
        return selectedItem is SeedItemSO seed
            && seed.Crop != null
            && InventoryManager.Instance != null
            && InventoryManager.Instance.HasItem(seed, 1);
    }

    public bool TryInteract(ItemSO selectedItem)
    {
        if (_isResolving) return false;
        if (_crop == null) return TryPlant(selectedItem as SeedItemSO);
        if (!IsMature) return false;
        return TryBeginHarvest();
    }

    public void SetHighlighted(bool highlighted)
    {
        _highlighted = highlighted;
        if (_emptyPlotHighlight != null) _emptyPlotHighlight.SetActive(highlighted && _crop == null);
        if (_cropRenderer != null)
            _cropRenderer.color = highlighted && _crop != null
                ? new Color(Mathf.Min(1f, _baseCropColor.r * 1.25f), Mathf.Min(1f, _baseCropColor.g * 1.25f), Mathf.Min(1f, _baseCropColor.b * 1.25f), _baseCropColor.a)
                : _baseCropColor;
    }

    private bool TryPlant(SeedItemSO seed)
    {
        if (seed?.Crop == null || InventoryManager.Instance == null || !InventoryManager.Instance.RemoveItem(seed, 1))
            return false;
        if (_manager?.Catalog != null && !_manager.Catalog.TryResolve(seed.Crop.CropId, out _))
        {
            InventoryManager.Instance.AddItem(seed, 1);
            return false;
        }

        _crop = seed.Crop;
        _plantedAtUtcTicks = DateTime.UtcNow.Ticks;
        _lastStage = -1;
        SetHighlighted(false);
        ApplyVisual();
        FarmingDomainEvents.RaiseCropPlanted(_plotId, _crop.CropId);
        return true;
    }

    private bool TryBeginHarvest()
    {
        if (_crop?.HarvestItem == null || InventoryManager.Instance == null) return false;
        int quantity = UnityEngine.Random.Range(_crop.MinimumHarvestQuantity, _crop.MaximumHarvestQuantity + 1);
        var grants = new List<InventoryItemGrant> { new(_crop.HarvestItem, quantity) };
        if (!InventoryManager.Instance.HasCapacityForBatch(grants)) return false;
        _isResolving = true;
        StartCoroutine(HarvestRoutine(grants, _crop.CropId));
        return true;
    }

    private IEnumerator HarvestRoutine(List<InventoryItemGrant> grants, string harvestedCropId)
    {
        Transform player = FindPlayer();
        if (player == null)
        {
            _isResolving = false;
            yield break;
        }

        if (_cropRenderer != null) _cropRenderer.enabled = false;
        yield return ResourceLootFlyVisual.Play(transform.position, player, grants);
        if (InventoryManager.Instance == null || !InventoryManager.Instance.TryAddBatch(grants))
        {
            _isResolving = false;
            ApplyVisual();
            yield break;
        }

        int quantity = grants[0].Quantity;
        _crop = null;
        _plantedAtUtcTicks = 0;
        _isResolving = false;
        _lastStage = -1;
        SetHighlighted(false);
        ApplyVisual();
        FarmingDomainEvents.RaiseCropHarvested(_plotId, harvestedCropId, quantity);
    }

    public FarmPlotSaveData CaptureState() => new()
    {
        plotId = _plotId,
        cropId = _crop?.CropId,
        plantedAtUtcTicks = _plantedAtUtcTicks
    };

    internal void RestoreEmpty()
    {
        _crop = null;
        _plantedAtUtcTicks = 0;
        _isResolving = false;
        _lastStage = -1;
        SetHighlighted(false);
        ApplyVisual();
    }

    internal void RestoreCrop(CropDefinition crop, long plantedAtUtcTicks)
    {
        _crop = crop;
        _plantedAtUtcTicks = plantedAtUtcTicks > 0 ? plantedAtUtcTicks : DateTime.UtcNow.Ticks;
        _isResolving = false;
        _lastStage = -1;
        SetHighlighted(false);
        ApplyVisual();
    }

    internal void ConfigureForTests(string plotId, SpriteRenderer cropRenderer = null)
    {
        _plotId = plotId;
        _cropRenderer = cropRenderer;
        if (cropRenderer != null) _baseCropColor = cropRenderer.color;
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        if (_cropRenderer == null) return;
        if (_crop == null)
        {
            _cropRenderer.sprite = null;
            _cropRenderer.enabled = false;
            _lastStage = -1;
            return;
        }

        _lastStage = CurrentStageIndex;
        _cropRenderer.sprite = _crop.GetStageSprite(_lastStage);
        _cropRenderer.enabled = !_isResolving && _cropRenderer.sprite != null;
        _cropRenderer.color = _highlighted ? Color.white : _baseCropColor;
    }

    private static Transform FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.transform : null;
    }
}
