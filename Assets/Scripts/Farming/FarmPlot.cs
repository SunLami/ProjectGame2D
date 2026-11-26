using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Collider2D))]
public sealed class FarmPlot : MonoBehaviour
{
    [SerializeField] private string _plotId;
    [SerializeField] private string _areaId = "area.farm";
    [SerializeField] private SpriteRenderer _cropRenderer;
    [Tooltip("Soil sprite of an empty plot. Hidden while a crop occupies the plot, shown again after harvest.")]
    [SerializeField] private SpriteRenderer _plotRenderer;
    [Tooltip("Outline traced around the soil sprite when an empty plot is hovered (D-055).")]
    [SerializeField] private HoverOutline _plotOutline;
    [SerializeField] private HoverOutline _cropOutline;
    [Tooltip("Solid footprint at the crop's base (sprite pivot). Enabled only while a crop is visible; sized per growth stage.")]
    [SerializeField] private BoxCollider2D _cropCollider;
    [Tooltip("Footprint width as a fraction of the current stage sprite width, clamped to the min/max below.")]
    [SerializeField, Range(0.05f, 1f)] private float _cropColliderWidthFraction = 0.3f;
    [SerializeField] private Vector2 _cropColliderWidthRange = new(0.3f, 0.9f);
    [SerializeField, Min(0.05f)] private float _cropColliderHeight = 0.25f;

    private FarmingManager _manager;
    private CropDefinition _crop;
    private long _plantedAtUtcTicks;
    private bool _isResolving;
    private int _lastStage = -1;
    private Color _baseCropColor = Color.white;
    private RectTransform _growthDisplay;
    private RectTransform _growthBar;
    private Image _growthFill;
    private TMP_Text _growthCountdown;
    private Image _harvestArrow;
    private double _harvestWaitSeconds;
    private int _displayedSeconds = -1;
    private static Sprite _arrowSprite;

    public string AreaId => _areaId;
    public string PlotId => _plotId;
    public bool HasCrop => _crop != null;
    public bool IsMature => _crop != null && CurrentStageIndex >= _crop.MatureStageIndex;
    public bool CanHarvest => _crop != null && RemainingHarvestSeconds(DateTime.UtcNow.Ticks) <= 0d;
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
        if (_crop == null || _isResolving)
        {
            if (_growthDisplay != null) _growthDisplay.gameObject.SetActive(false);
            return;
        }

        if (CurrentStageIndex != _lastStage) ApplyVisual();
        UpdateGrowthDisplay();
    }

    internal void Bind(FarmingManager manager) => _manager = manager;

    public bool IsInteractionAvailable(ItemSO selectedItem)
    {
        if (_isResolving) return false;
        if (_crop != null) return CanHarvest;
        return selectedItem is SeedItemSO seed
            && seed.Crop != null
            && InventoryManager.Instance != null
            && InventoryManager.Instance.HasItem(seed, 1);
    }

    public bool TryInteract(ItemSO selectedItem)
    {
        if (_isResolving) return false;
        if (_crop == null) return TryPlant(selectedItem as SeedItemSO);
        if (!CanHarvest) return false;
        return TryBeginHarvest();
    }

    // Both states use the shared HoverOutline (D-055): an empty plot outlines its soil sprite,
    // a planted plot outlines the crop sprite (the soil sprite is hidden while a crop grows).
    public void SetHighlighted(bool highlighted)
    {
        _plotOutline?.SetHighlighted(highlighted && _crop == null);
        _cropOutline?.SetHighlighted(highlighted && _crop != null);
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
        _harvestWaitSeconds = _crop.HarvestWaitSeconds;
        SoundFXManager.PlaySfxAt(SfxIds.FarmPlant, transform.position);
        _plantedAtUtcTicks = DateTime.UtcNow.Ticks;
        _lastStage = -1;
        SetHighlighted(false);
        ApplyVisual();
        FarmingDomainEvents.RaiseCropPlanted(_plotId, _crop.CropId);
        // Every planting counts toward a Replant objective -- there is no separate "first plant"
        // vs "replant" distinction at the farming-domain level (TutorialAndQuestProgression.md
        // Quest 2-2 "Back to the Fields").
        QuestDomainEvents.RaiseCropReplanted(_crop.CropId, _areaId);
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
        ApplyCropCollider();
        SoundFXManager.PlaySfxAt(SfxIds.FarmHarvest, transform.position);
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
        // Bridges Farming to the Quest Gather objective (TutorialAndQuestProgression.md Quest 2-2
        // "Back to the Fields") -- previously nothing connected harvest to ResourceGathered.
        QuestDomainEvents.RaiseResourceGathered(harvestedCropId, quantity, _areaId);
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
        _harvestWaitSeconds = crop?.HarvestWaitSeconds ?? 0d;
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
        if (_plotRenderer != null) _plotRenderer.enabled = _crop == null;
        if (_cropRenderer == null) return;
        if (_crop == null)
        {
            _cropRenderer.sprite = null;
            _cropRenderer.enabled = false;
            _lastStage = -1;
            ApplyCropCollider();
            return;
        }

        _lastStage = CurrentStageIndex;
        _cropRenderer.sprite = _crop.GetStageSprite(_lastStage);
        _cropRenderer.enabled = !_isResolving && _cropRenderer.sprite != null;
        _cropRenderer.color = _baseCropColor;
        ApplyCropCollider();
    }

    // Crop sprites are authored with the pivot at the plant base, so the footprint sits just above
    // the pivot and Y-sorting (spriteSortPoint = Pivot) matches where the player collides.
    private void ApplyCropCollider()
    {
        if (_cropCollider == null) return;
        Sprite sprite = _cropRenderer != null ? _cropRenderer.sprite : null;
        bool solid = sprite != null && _cropRenderer.enabled;
        _cropCollider.enabled = solid;
        if (!solid) return;

        float width = Mathf.Clamp(sprite.bounds.size.x * _cropColliderWidthFraction,
            _cropColliderWidthRange.x, _cropColliderWidthRange.y);
        _cropCollider.size = new Vector2(width, _cropColliderHeight);
        _cropCollider.offset = new Vector2(0f, _cropColliderHeight * 0.5f);
    }

    private double RemainingHarvestSeconds(long nowTicks) =>
        Math.Max(0d, _harvestWaitSeconds - TimeSpan.FromTicks(Math.Max(0L, nowTicks - _plantedAtUtcTicks)).TotalSeconds);

    private void UpdateGrowthDisplay()
    {
        if (_cropRenderer == null || _cropRenderer.sprite == null)
            return;

        if (_growthDisplay == null)
            BuildGrowthDisplay();

        Bounds bounds = _cropRenderer.bounds;
        _growthDisplay.position = new Vector3(bounds.center.x, bounds.max.y + 0.22f, bounds.center.z);
        _growthDisplay.gameObject.SetActive(true);

        double remaining = RemainingHarvestSeconds(DateTime.UtcNow.Ticks);
        bool ready = remaining <= 0d;
        _growthBar.gameObject.SetActive(!ready);
        _growthCountdown.gameObject.SetActive(!ready);
        _harvestArrow.enabled = ready;
        if (ready)
            return;

        float fraction = Mathf.Clamp01(1f - (float)(remaining / _harvestWaitSeconds));
        _growthFill.enabled = fraction > 0f;
        _growthFill.rectTransform.anchorMax = new Vector2(Mathf.Lerp(0.02f, 0.98f, fraction), 0.875f);
        _growthFill.color = _lastStage >= _crop.MatureStageIndex
            ? new Color(0.95f, 0.71f, 0.3f)
            : new Color(0.53f, 0.81f, 0.34f);
        int seconds = Mathf.CeilToInt((float)remaining);
        if (_displayedSeconds != seconds)
        {
            _displayedSeconds = seconds;
            _growthCountdown.SetText("{0}s", seconds);
        }
    }

    private void BuildGrowthDisplay()
    {
        GameObject root = new("Crop Growth", typeof(RectTransform), typeof(Canvas));
        _growthDisplay = (RectTransform)root.transform;
        _growthDisplay.SetParent(transform, false);
        _growthDisplay.localScale = Vector3.one * 0.01f;
        _growthDisplay.sizeDelta = new Vector2(66f, 36f);
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 20;

        _growthBar = CreateIndicatorImage("Bar", _growthDisplay, new Color(0.18f, 0.12f, 0.08f), new Vector2(60f, 8f)).rectTransform;
        _growthFill = CreateIndicatorImage("Fill", _growthBar, Color.white, Vector2.zero);
        RectTransform fillRect = _growthFill.rectTransform;
        fillRect.anchorMin = new Vector2(0.02f, 0.125f);
        fillRect.anchorMax = new Vector2(0.02f, 0.875f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        _growthCountdown = new GameObject("Time Remaining", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        _growthCountdown.transform.SetParent(_growthDisplay, false);
        _growthCountdown.rectTransform.sizeDelta = new Vector2(66f, 16f);
        _growthCountdown.rectTransform.anchoredPosition = new Vector2(0f, 12f);
        _growthCountdown.font = TMP_Settings.defaultFontAsset;
        _growthCountdown.alignment = TextAlignmentOptions.Center;
        _growthCountdown.fontSize = 14f;
        _growthCountdown.color = new Color(1f, 0.93f, 0.75f);
        _growthCountdown.raycastTarget = false;

        if (_arrowSprite == null)
            _arrowSprite = Resources.Load<Sprite>("UI/QuestDirection/quest_target_arrow");
        _harvestArrow = CreateIndicatorImage("Harvest Ready", _growthDisplay, Color.white, new Vector2(25f, 29f));
        _harvestArrow.sprite = _arrowSprite;
        _harvestArrow.preserveAspect = true;
        _harvestArrow.rectTransform.localEulerAngles = new Vector3(0f, 0f, 180f);
    }

    private static Image CreateIndicatorImage(string name, Transform parent, Color color, Vector2 size)
    {
        Image image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        image.rectTransform.sizeDelta = size;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Transform FindPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player != null ? player.transform : null;
    }
}
