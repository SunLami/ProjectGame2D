/// <summary>
/// One additive schema step, N -> N+1. Every GameSaveData version bump so far has only added new
/// top-level fields (see SaveAndWorldPersistence.md per-phase notes) -- JsonUtility already
/// tolerates missing/unknown JSON keys on its own, so a step's only job is to fill in a safe
/// default for whatever field(s) were introduced at ToVersion when the source save predates them.
/// Apply must be idempotent (safe to call on data that already has the field populated) and must
/// never grant reward/progression or mutate a ScriptableObject definition.
/// </summary>
public interface ISaveMigrationStep
{
    int FromVersion { get; }
    int ToVersion { get; }
    void Apply(GameSaveData data);
}
