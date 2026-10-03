using UnityEngine;

/// <summary>
/// The summoning statue as a clickable shrine (D-090): left-click in range (GameCursorManager shows the
/// Interact cursor) opens <see cref="BossShrineUI"/>, where the player offers the arena's summon item
/// from the inventory and confirms. Available only while the arena statue is idle (not during a summon
/// or the fight). The offering rules live on <see cref="BossArenaController"/>.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class BossShrineInteractable : MonoBehaviour
{
    [SerializeField] private BossArenaController _arena;
    [SerializeField] private BossShrineUI _ui;

    public bool IsAvailable => _arena != null && _ui != null && !_ui.IsOpen
        && _arena.State == BossArenaController.ArenaState.StatueIdle;

    public bool TryOpen()
    {
        if (!IsAvailable)
            return false;

        return _ui.Open(_arena, StartSummon);
    }

    private void StartSummon()
    {
        if (_arena == null || _arena.TrySummonWithOffering())
            return;

        WorldMessagePopupUI.Show("The shrine does not respond.");
    }
}
