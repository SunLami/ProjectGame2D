using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance;

    [Header("Sprite Library Component")]
    public SpriteLibrary bodySpriteLibrary;
    public SpriteLibrary headSpriteLibrary;
    public SpriteLibrary swordSpriteLibrary;

    [Header("List Of LibraryAssets")]
    public SpriteLibraryAsset[] bodyEquipmentAssets;
    public SpriteLibraryAsset[] headEquipmentAssets;
    public SpriteLibraryAsset[] swordEquipmentAssets;

    private int currentBodyIndex = 0;
    private int currentSwordIndex = 0;
    private int currentHeadIndex = 0;

    public void EquipBodyByIndex(int index)
    {
        if (bodyEquipmentAssets == null || index < 0 || index >= bodyEquipmentAssets.Length) return;

        currentBodyIndex = index;
        bodySpriteLibrary.spriteLibraryAsset = bodyEquipmentAssets[index];
    }

    public void EquipSwordByIndex(int index)
    {
        if (swordEquipmentAssets == null || index < 0 || index >= swordEquipmentAssets.Length) return;

        currentSwordIndex = index;
        swordSpriteLibrary.spriteLibraryAsset = swordEquipmentAssets[index];
        SetSwordVisible(true);
    }

    public void EquipHeadByIndex(int index)
    {
        if (headEquipmentAssets == null || index < 0 || index >= headEquipmentAssets.Length) return;

        currentHeadIndex = index;
        headSpriteLibrary.spriteLibraryAsset = headEquipmentAssets[index];
    }

    // --- Data-driven Equipment System (Inventory-integrated) ---

    [Header("Default Look (used on Unequip)")]
    [SerializeField] private SpriteLibraryAsset _defaultBodyAsset;
    [SerializeField] private SpriteLibraryAsset _defaultHeadAsset;
    [SerializeField] private SpriteLibraryAsset _defaultSwordAsset;

    private readonly Dictionary<EquipSlot, EquipmentItemSO> _equipped = new Dictionary<EquipSlot, EquipmentItemSO>();
    private SpriteRenderer _swordRenderer;

    public event Action OnEquipmentChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        if (swordSpriteLibrary != null)
            _swordRenderer = swordSpriteLibrary.GetComponent<SpriteRenderer>();

        // Weapon starts unequipped (bare-handed) by default — unlike Head/Body, which always have a base look.
        SetSwordVisible(GetEquipped(EquipSlot.Weapon) != null);
    }

    private void Start()
    {
        RefreshPlayerStats();
    }

    private void SetSwordVisible(bool visible)
    {
        if (_swordRenderer != null)
            _swordRenderer.enabled = visible;
    }

    public EquipmentItemSO GetEquipped(EquipSlot slot)
    {
        return _equipped.TryGetValue(slot, out EquipmentItemSO item) ? item : null;
    }

    public bool Equip(EquipmentItemSO item, InventorySlot sourceSlot)
    {
        if (item == null || sourceSlot == null || sourceSlot.item != item) return false;
        if (InventoryManager.Instance == null) return false;

        EquipmentItemSO previous = GetEquipped(item.slot);

        sourceSlot.quantity -= 1;
        if (sourceSlot.quantity <= 0) sourceSlot.Clear();

        ApplyVisual(item);
        _equipped[item.slot] = item;

        if (previous != null)
        {
            // Put the replaced item back into the exact slot the new item came from,
            // so it doesn't jump to whatever the first empty slot happens to be.
            if (sourceSlot.IsEmpty)
            {
                sourceSlot.item = previous;
                sourceSlot.quantity = 1;
            }
            else
            {
                InventoryManager.Instance.AddItem(previous, 1);
            }
        }

        InventoryManager.Instance.NotifyChanged();
        RefreshPlayerStats();
        OnEquipmentChanged?.Invoke();
        return true;
    }

    public bool Unequip(EquipSlot slot)
    {
        return Unequip(slot, null);
    }

    // Unequips into a specific inventory slot (e.g. drag-and-drop target) when that slot is
    // empty; falls back to the first empty slot otherwise (or when no target slot is given).
    public bool Unequip(EquipSlot slot, InventorySlot targetSlot)
    {
        EquipmentItemSO item = GetEquipped(slot);
        if (item == null) return false;

        ClearVisual(slot);
        _equipped[slot] = null;

        if (targetSlot != null && targetSlot.IsEmpty)
        {
            targetSlot.item = item;
            targetSlot.quantity = 1;
            InventoryManager.Instance?.NotifyChanged();
        }
        else
        {
            InventoryManager.Instance?.AddItem(item, 1);
        }

        RefreshPlayerStats();
        OnEquipmentChanged?.Invoke();
        return true;
    }

    private void RefreshPlayerStats()
    {
        if (PlayerStat.Instance == null)
            return;

        PlayerStatModifiers total = default;
        foreach (EquipmentItemSO item in _equipped.Values)
        {
            if (item != null)
                total += item.statModifiers;
        }

        PlayerStat.Instance.ApplyEquipmentModifiers(total);
    }

    private void ApplyVisual(EquipmentItemSO item)
    {
        switch (item.slot)
        {
            case EquipSlot.Head:
                headSpriteLibrary.spriteLibraryAsset = item.spriteLibraryAsset;
                break;

            case EquipSlot.Body:
                bodySpriteLibrary.spriteLibraryAsset = item.spriteLibraryAsset;
                if (item.headSpriteLibraryAsset != null)
                {
                    headSpriteLibrary.spriteLibraryAsset = item.headSpriteLibraryAsset;
                }
                break;

            case EquipSlot.Weapon:
                swordSpriteLibrary.spriteLibraryAsset = item.spriteLibraryAsset;
                SetSwordVisible(true);
                break;
        }
    }

    private void ClearVisual(EquipSlot slot)
    {
        switch (slot)
        {
            case EquipSlot.Head:
                headSpriteLibrary.spriteLibraryAsset = _defaultHeadAsset;
                break;

            case EquipSlot.Body:
                bodySpriteLibrary.spriteLibraryAsset = _defaultBodyAsset;
                // Restore whatever the Head slot has equipped on its own, since Body may have been overriding it visually.
                EquipmentItemSO currentHead = GetEquipped(EquipSlot.Head);
                headSpriteLibrary.spriteLibraryAsset = currentHead != null ? currentHead.spriteLibraryAsset : _defaultHeadAsset;
                break;

            case EquipSlot.Weapon:
                // No default sword look — unequipping means bare-handed, so just hide it.
                SetSwordVisible(false);
                break;
        }
    }
}
