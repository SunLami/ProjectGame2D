using UnityEngine;

/// <summary>
/// Tracks whether a specific boss EnemyUniversal instance has been defeated. Deliberately its own
/// GameObject rather than a component on the boss itself: EnemyUniversal destroys its own
/// GameObject a few seconds after death (corpse lifetime), which would take a co-located tracker
/// down with it and orphan WorldObjectRegistry's serialized reference. Subscribes to
/// EnemyUniversal.Died (fires synchronously on death, well before the delayed Destroy), so the
/// flag is captured before that happens.
/// </summary>
public sealed class BossDefeatTracker : MonoBehaviour, IPersistentWorldObject
{
    [SerializeField] private string _persistentId;
    [SerializeField] private EnemyUniversal _boss;

    private bool _defeated;

    public string PersistentId => _persistentId;
    public WorldObjectKind Kind => WorldObjectKind.Boss;
    public bool IsDefeated => _defeated;

    internal void ConfigureForTests(string persistentId, EnemyUniversal boss)
    {
        _persistentId = persistentId;
        Bind(boss);
    }

    private void Awake() => Bind(_boss);

    private void Bind(EnemyUniversal boss)
    {
        if (_boss != null)
            _boss.Died -= HandleDied;

        _boss = boss;

        if (_boss != null)
            _boss.Died += HandleDied;
    }

    private void OnDestroy()
    {
        if (_boss != null)
            _boss.Died -= HandleDied;
    }

    private void HandleDied()
    {
        _defeated = true;
        WorldDomainEvents.RaiseWorldObjectChanged();
    }

    public WorldObjectState CaptureState() => new(_defeated, 0);

    /// <summary>Restore-only: if the boss was already defeated last session, silently remove it
    /// (EnemyUniversal.RestoreDefeated) instead of replaying its death -- no HealthChanged/Died/
    /// EnemyKilled event, no animation, no duplicate kill credit.</summary>
    public void RestoreState(WorldObjectState state)
    {
        _defeated = state.Flag;
        if (_defeated)
            _boss?.RestoreDefeated();
    }
}
