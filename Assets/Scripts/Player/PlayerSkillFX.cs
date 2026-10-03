using UnityEngine;

public partial class Player
{
    [Header("Skill FX")]
    [SerializeField] private SpriteRenderer _skillFxRenderer;

    // Weapon/AttackFX stay forced hidden until Time.time reaches these deadlines, instead of each
    // cast scheduling its own independent restore timer. ExtendSkillCastHide only ever pushes a
    // deadline forward (Mathf.Max), so spamming the skill key — casting again before an earlier cast
    // has finished — correctly keeps everything hidden until the LATEST cast finishes; an older cast's
    // timer can no longer fire in the middle of a newer one and restore visibility early
    // (SkillVfxPipeline.md §8.4).
    private float _skillWeaponRestoreTime;
    private float _attackFxRestoreTime;

    /// <summary>Call when a skill starts casting (aim phase begins). Hides the Weapon and AttackFX
    /// immediately. Does not by itself set how long they stay hidden -- call ExtendSkillCastHide once
    /// the cast's actual duration (e.g. projectile travel time) is known.</summary>
    public void BeginSkillCast()
    {
        if (_weaponRenderer != null)
            _weaponRenderer.enabled = false;
        if (_attackFxRenderer != null)
            _attackFxRenderer.enabled = false;
    }

    /// <summary>Pushes the Weapon/AttackFX restore deadline forward by `duration` seconds from now if
    /// that is later than the current deadline (never pulls it earlier) -- call this once per
    /// confirmed cast with that cast's expected duration (e.g. projectile travel time). AttackFX gets
    /// an extra 1s grace beyond Weapon so a repeated click right as one cast ends can't flicker the
    /// melee slash FX on before the next cast's swing is visually underway.</summary>
    public void ExtendSkillCastHide(float duration)
    {
        float restoreAt = Time.time + Mathf.Max(0f, duration);
        if (restoreAt > _skillWeaponRestoreTime)
            _skillWeaponRestoreTime = restoreAt;

        float fxRestoreAt = restoreAt + 1f;
        if (fxRestoreAt > _attackFxRestoreTime)
            _attackFxRestoreTime = fxRestoreAt;
    }

    /// <summary>Cancels an aim/cast immediately (no pending duration to honor) -- restores Weapon
    /// right away instead of waiting for a deadline. Used by CancelAimSkill (right-click).</summary>
    public void EndSkillCast()
    {
        _skillWeaponRestoreTime = Time.time;
        _attackFxRestoreTime = Time.time;
    }

    private void TickSkillVisualRestore()
    {
        if (_weaponRenderer == null)
            return;

        // Must stay hidden for the whole aim phase even though the real deadline (set by
        // ExtendSkillCastHide once the cast's duration is known) isn't pushed into the future until
        // the player actually confirms -- without this check the deadline defaults to 0 before any
        // cast has ever run, which is always in the past, so this would restore the Weapon the very
        // next frame after BeginSkillCast hid it, while still aiming.
        if (_isAimingSkill || Time.time < _skillWeaponRestoreTime)
            return;

        _weaponRenderer.enabled = EquipmentManager.Instance != null
            && EquipmentManager.Instance.GetEquipped(EquipSlot.Weapon) != null;
    }

    private void CacheSkillFxReferences()
    {
        if (_skillFxRenderer == null)
            _skillFxRenderer = transform.Find("SkillFX")?.GetComponent<SpriteRenderer>();
    }
}
