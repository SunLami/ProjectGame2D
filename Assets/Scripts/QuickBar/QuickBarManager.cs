using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-850)]
public sealed class QuickBarManager : MonoBehaviour
{
    private readonly string[] _assignedItemIds = new string[QuickBarSaveData.SlotCount];
    private IItemResolver _resolver;
    private int _selectedIndex;

    public static QuickBarManager Instance { get; private set; }
    public int SelectedIndex => _selectedIndex;
    public event Action Changed;
    // Request only. The consuming domain owns validation, effect and atomic inventory consumption.
    public event Action<ItemSO> ActiveItemUseRequested;

    public ItemSO SelectedItem => TryGetAssignedItem(_selectedIndex, out ItemSO item) ? item : null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance == null)
            new GameObject(nameof(QuickBarManager)).AddComponent<QuickBarManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        _resolver = new ResourcesItemResolver();
        if (GetComponent<QuickBarHudRuntimeInstaller>() == null)
            gameObject.AddComponent<QuickBarHudRuntimeInstaller>();
        if (GetComponent<ConsumableUseHandler>() == null)
            gameObject.AddComponent<ConsumableUseHandler>();
        DontDestroyOnLoad(gameObject);
    }

    public string GetAssignedItemId(int index) => IsValidIndex(index) ? _assignedItemIds[index] : null;

    public bool TryGetAssignedItem(int index, out ItemSO item)
    {
        item = null;
        string itemId = GetAssignedItemId(index);
        return !string.IsNullOrWhiteSpace(itemId) && _resolver != null && _resolver.TryResolve(itemId, out item)
            && item != null && item.usedItems;
    }

    public bool Assign(int index, ItemSO item)
    {
        if (!IsValidIndex(index) || item == null || !item.usedItems || string.IsNullOrWhiteSpace(item.itemId))
            return false;

        if (_assignedItemIds[index] == item.itemId)
            return true;

        _assignedItemIds[index] = item.itemId;
        Changed?.Invoke();
        return true;
    }

    public bool Clear(int index)
    {
        if (!IsValidIndex(index) || string.IsNullOrEmpty(_assignedItemIds[index]))
            return false;

        _assignedItemIds[index] = null;
        Changed?.Invoke();
        return true;
    }

    public bool Select(int index)
    {
        if (!IsValidIndex(index)) return false;
        if (_selectedIndex == index) return true;

        _selectedIndex = index;
        Changed?.Invoke();
        return true;
    }

    public void ActivateSlot(int index)
    {
        if (!GameStateManager.AllowsGameplayInput || !Select(index) ||
            !TryGetAssignedItem(index, out var item) || item.quickBarUseMode != QuickBarUseMode.Active ||
            InventoryManager.Instance == null || InventoryManager.Instance.GetTotalQuantity(item.itemId) <= 0)
            return;
        ActiveItemUseRequested?.Invoke(item);
    }

    public QuickBarSaveData ToSaveData()
    {
        var data = new QuickBarSaveData { selectedIndex = _selectedIndex };
        data.assignedItemIds.Clear();
        for (int i = 0; i < _assignedItemIds.Length; i++)
            data.assignedItemIds.Add(_assignedItemIds[i]);
        return data;
    }

    public void RestoreState(QuickBarSaveData data, List<string> missingItemIds = null)
    {
        Array.Clear(_assignedItemIds, 0, _assignedItemIds.Length);
        _selectedIndex = IsValidIndex(data?.selectedIndex ?? 0) ? data?.selectedIndex ?? 0 : 0;

        if (data?.assignedItemIds != null)
        {
            int count = Mathf.Min(_assignedItemIds.Length, data.assignedItemIds.Count);
            for (int i = 0; i < count; i++)
            {
                string itemId = data.assignedItemIds[i];
                if (string.IsNullOrWhiteSpace(itemId)) continue;
                if (_resolver != null && _resolver.TryResolve(itemId, out _))
                    _assignedItemIds[i] = itemId;
                else
                    missingItemIds?.Add(itemId);
            }
        }

        Changed?.Invoke();
    }

    internal void ConfigureResolverForTests(IItemResolver resolver) => _resolver = resolver;

    private static bool IsValidIndex(int index) => index >= 0 && index < QuickBarSaveData.SlotCount;

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
