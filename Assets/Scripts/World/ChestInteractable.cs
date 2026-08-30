using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Minimal persistent chest: grants one reward item stack exactly once. Reward is authored inline
/// (itemId + quantity) rather than via a shared Definition asset -- Phase 8's scope is the
/// persistence mechanism (opened survives save/load), not a general loot-table system.
/// </summary>
public sealed class ChestInteractable : MonoBehaviour, IPersistentWorldObject
{
    [SerializeField] private string _persistentId;
    [SerializeField] private string _rewardItemId;
    [SerializeField, Min(1)] private int _rewardQuantity = 1;

    [Header("Opening Presentation")]
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private Texture2D[] _openFrameTextures;
    [SerializeField, Min(0.03f)] private float _frameSeconds = 0.12f;

    [Tooltip("Optional -- toggled to reflect the opened/closed visual. Safe to leave unassigned.")]
    [SerializeField] private GameObject _openedIndicator;

    private bool _opened;
    private bool _opening;
    private IItemResolver _itemResolver;
    private Sprite[] _runtimeOpenFrames;

    public string PersistentId => _persistentId;
    public WorldObjectKind Kind => WorldObjectKind.Chest;
    public bool IsOpened => _opened;
    public bool IsOpening => _opening;

    internal void ConfigureForTests(string persistentId, string rewardItemId, int rewardQuantity, IItemResolver itemResolver)
    {
        _persistentId = persistentId;
        _rewardItemId = rewardItemId;
        _rewardQuantity = rewardQuantity;
        _itemResolver = itemResolver;
    }

    private IItemResolver ItemResolver => _itemResolver ??= new ResourcesItemResolver();

    /// <summary>Opens the chest and grants the reward exactly once. Returns false (chest stays
    /// closed, nothing consumed) if already opened or if inventory has no room -- retryable.</summary>
    public bool TryOpen(out bool granted)
    {
        granted = false;
        if (_opened || _opening)
            return false;

        if (!ItemResolver.TryResolve(_rewardItemId, out ItemSO item)
            || InventoryManager.Instance == null
            || !InventoryManager.Instance.HasCapacityFor(item, _rewardQuantity))
        {
            return false;
        }

        InventoryManager.Instance.AddItem(item, _rewardQuantity);
        _opened = true;
        ApplyVisual();
        granted = true;
        WorldDomainEvents.RaiseWorldObjectChanged();
        return true;
    }

    /// <summary>Starts the authored opening presentation. The reward is committed only after the
    /// final frame and loot-flight presentation complete. A failed capacity check leaves the chest
    /// closed and retryable.</summary>
    public bool TryBeginOpen()
    {
        if (_opened || _opening || !TryResolveReward(out ItemSO item, out InventoryManager inventory))
            return false;

        var grants = new List<InventoryItemGrant> { new(item, _rewardQuantity) };
        if (!inventory.HasCapacityForBatch(grants))
            return false;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
            return false;

        StartCoroutine(OpenRoutine(playerObject.transform, inventory, grants));
        return true;
    }

    public WorldObjectState CaptureState() => new(_opened, 0);

    public void RestoreState(WorldObjectState state)
    {
        StopAllCoroutines();
        _opening = false;
        _opened = state.Flag;
        ApplyVisual();
    }

    private IEnumerator OpenRoutine(
        Transform player,
        InventoryManager inventory,
        IReadOnlyList<InventoryItemGrant> grants)
    {
        _opening = true;
        Sprite[] frames = GetOpenFrames();
        if (frames.Length > 0 && _spriteRenderer != null)
        {
            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] != null)
                    _spriteRenderer.sprite = frames[i];
                yield return new WaitForSecondsRealtime(_frameSeconds);
            }
        }

        if (inventory == null || !inventory.HasCapacityForBatch(grants))
        {
            _opening = false;
            ApplyVisual();
            yield break;
        }

        yield return ResourceLootFlyVisual.Play(transform.position, player, grants);
        if (inventory == null || !inventory.TryAddBatch(grants))
        {
            _opening = false;
            ApplyVisual();
            yield break;
        }

        _opened = true;
        _opening = false;
        ApplyVisual();
        WorldDomainEvents.RaiseWorldObjectChanged();
    }

    private bool TryResolveReward(out ItemSO item, out InventoryManager inventory)
    {
        inventory = InventoryManager.Instance;
        return ItemResolver.TryResolve(_rewardItemId, out item) && inventory != null;
    }

    private void ApplyVisual()
    {
        if (_openedIndicator != null)
            _openedIndicator.SetActive(_opened);

        Sprite[] frames = GetOpenFrames();
        if (_spriteRenderer != null && frames.Length > 0)
            _spriteRenderer.sprite = _opened
                ? frames[frames.Length - 1]
                : frames[0];
    }

    private Sprite[] GetOpenFrames()
    {
        if (_runtimeOpenFrames != null)
            return _runtimeOpenFrames;
        if (_openFrameTextures == null || _openFrameTextures.Length == 0)
            return System.Array.Empty<Sprite>();

        _runtimeOpenFrames = new Sprite[_openFrameTextures.Length];
        for (int i = 0; i < _openFrameTextures.Length; i++)
        {
            Texture2D texture = _openFrameTextures[i];
            if (texture != null)
            {
                _runtimeOpenFrames[i] = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    32f);
                _runtimeOpenFrames[i].name = $"{texture.name}_RuntimeSprite";
            }
        }
        return _runtimeOpenFrames;
    }

    private void OnDestroy()
    {
        if (_runtimeOpenFrames == null)
            return;
        foreach (Sprite frame in _runtimeOpenFrames)
            if (frame != null)
                Destroy(frame);
    }
}
