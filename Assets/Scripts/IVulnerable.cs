/// <summary>Defense-down debuff hook (D-076): while active, every hit on the target deals
/// `damageTakenMultiplier` times its normal damage (>1 = takes more). Used by Earth Skill 2 so other
/// skills combo into bigger damage on enemies caught inside the arena. Re-applying keeps the stronger
/// multiplier and the later expiry.</summary>
public interface IVulnerable
{
    void ApplyVulnerability(float damageTakenMultiplier, float duration);
}
