using System;
using System.Collections.Generic;
using UnityEngine;

// Scene service (ServiceOwnershipLifecycle.md), not an application service: lives and dies with
// the scene that owns the MapZoneTrigger colliders, matching MapManager's lifecycle rationale.
public sealed class MapZoneManager : MonoBehaviour
{
    // Fallback shown when the player isn't inside any authored MapZoneTrigger -- authored per
    // scene (e.g. the whole MapNhat map is "Heart Village" today, before any sub-zone triggers
    // are placed) rather than hardcoded, since it's content, not architecture.
    [SerializeField] private string _defaultZoneName = "Heart Village";

    public static MapZoneManager Instance { get; private set; }

    public event Action<string> OnZoneChanged;

    // Overlapping zones (e.g. a village inside a larger region) push/pop instead of overwriting,
    // so leaving the inner zone correctly falls back to whichever outer zone is still entered.
    private readonly List<string> _activeZoneStack = new();

    public string CurrentZoneName { get; private set; }

    private void Awake()
    {
        Instance = this;
        CurrentZoneName = _defaultZoneName;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void EnterZone(string zoneName)
    {
        if (string.IsNullOrEmpty(zoneName)) return;
        _activeZoneStack.Add(zoneName);
        Refresh();
    }

    public void ExitZone(string zoneName)
    {
        if (string.IsNullOrEmpty(zoneName)) return;
        _activeZoneStack.Remove(zoneName);
        Refresh();
    }

    private void Refresh()
    {
        string next = _activeZoneStack.Count > 0 ? _activeZoneStack[^1] : _defaultZoneName;
        if (next == CurrentZoneName) return;
        CurrentZoneName = next;
        OnZoneChanged?.Invoke(CurrentZoneName);
    }
}
