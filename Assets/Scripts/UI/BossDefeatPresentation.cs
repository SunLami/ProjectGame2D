using TMPro;
using UnityEngine;

/// <summary>Persistent boss status visual hosted on the tracker object, never on the destructible boss.</summary>
public sealed class BossDefeatPresentation : MonoBehaviour
{
    [SerializeField] private BossDefeatTracker _tracker;
    [SerializeField] private GameObject _activeRoot;
    [SerializeField] private GameObject _defeatedRoot;
    [SerializeField] private TMP_Text _statusText;

    private bool? _lastDefeated;

    private void OnEnable() => Refresh();

    private void Update()
    {
        if (_tracker != null && _lastDefeated != _tracker.IsDefeated)
            Refresh();
    }

    private void Refresh()
    {
        bool defeated = _tracker != null && _tracker.IsDefeated;
        _lastDefeated = defeated;
        if (_activeRoot != null)
            _activeRoot.SetActive(!defeated);
        if (_defeatedRoot != null)
            _defeatedRoot.SetActive(defeated);
        if (_statusText != null)
            _statusText.text = defeated ? "FOREST GUARDIAN DEFEATED" : "FOREST GUARDIAN";
    }
}
