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

    public void EquipBodyByIndex(int index)
    {
        if (bodyEquipmentAssets == null || index < 0 || index >= bodyEquipmentAssets.Length) return;

        currentBodyIndex = index;
        bodySpriteLibrary.spriteLibraryAsset = bodyEquipmentAssets[index];

        if (headSpriteLibrary != null && headEquipmentAssets != null && index < headEquipmentAssets.Length)
            headSpriteLibrary.spriteLibraryAsset = headEquipmentAssets[index];
    }

    public void EquipSwordByIndex(int index)
    {
        if (swordEquipmentAssets == null || index < 0 || index >= swordEquipmentAssets.Length) return;

        currentSwordIndex = index;
        swordSpriteLibrary.spriteLibraryAsset = swordEquipmentAssets[index];
    }

    // --- Data-driven Equipment System (Inventory-integrated) ---

    [Header("Default Look (used on Unequip)")]
    [SerializeField] private SpriteLibraryAsset _defaultBodyAsset;
    [SerializeField] private SpriteLibraryAsset _defaultHeadAsset;
    [SerializeField] private SpriteLibraryAsset _defaultSwordAsset;

    private readonly Dictionary<EquipSlot, EquipmentItemSO> _equipped = new Dictionary<EquipSlot, EquipmentItemSO>();

    public event Action OnEquipmentChanged;

    private void Awake()
    {
        Instance = this;
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
        OnEquipmentChanged?.Invoke();
        return true;
    }

    public bool Unequip(EquipSlot slot)
    {
        EquipmentItemSO item = GetEquipped(slot);
        if (item == null) return false;

        ClearVisual(slot);
        _equipped[slot] = null;
        InventoryManager.Instance?.AddItem(item, 1);

        OnEquipmentChanged?.Invoke();
        return true;
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
                swordSpriteLibrary.spriteLibraryAsset = _defaultSwordAsset;
                break;
        }
    }
}
