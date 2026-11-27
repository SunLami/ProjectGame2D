using UnityEngine;

/// <summary>Per-actor timing for enemy / wildlife voices (D-122): the "spotted the player" call is rate-limited so an
/// actor flickering in and out of sight range does not spam it, and idle voices are rare and only play near the
/// player (positional SFX would be inaudible further away anyway, this just avoids wasting voices).</summary>
public sealed class MobVoiceTimer
{
    public const float AlertCooldown = 4f;
    public const float IdleRange = 8f;
    private const float IdleMin = 8f, IdleMax = 16f;

    private float _nextAlert;
    private float _nextIdle = -1f;

    public bool TryAlert()
    {
        if (!GameStateManager.AllowsGameplayInput || Time.time < _nextAlert)
            return false;
        _nextAlert = Time.time + AlertCooldown;
        return true;
    }

    public bool TryIdle(Vector2 position)
    {
        if (_nextIdle < 0f)
        {
            // First call: stagger so a group spawned together does not voice in unison.
            _nextIdle = Time.time + Random.Range(2f, IdleMax);
            return false;
        }
        if (Time.time < _nextIdle || !GameStateManager.AllowsGameplayInput)
            return false;
        _nextIdle = Time.time + Random.Range(IdleMin, IdleMax);
        PlayerStat player = PlayerStat.Instance;
        return player != null && !player.IsDead
            && ((Vector2)player.transform.position - position).sqrMagnitude < IdleRange * IdleRange;
    }
}
