using UnityEngine;

/// <summary>Single on/off gate for the Dev Panel (see DevPanelController). Defaults to disabled so a
/// build never ships with it visible by accident -- toggle this asset to true before building the
/// .exe used to demo, and back to false before any build meant for real players.</summary>
[CreateAssetMenu(fileName = "DevModeSettings", menuName = "Scriptable Objects/Debug/Dev Mode Settings")]
public sealed class DevModeSettings : ScriptableObject
{
    [SerializeField] private bool _isDevModeEnabled;

    public bool IsDevModeEnabled => _isDevModeEnabled;
}
