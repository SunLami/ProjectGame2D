/// <summary>Airborne (launched into the air) status hook (D-080): for `duration` seconds the target is lifted
/// off the ground -- it cannot move, chase or attack (stunned) -- and its sprite is drawn raised by up
/// to `height` world units with a shadow left on the ground. Re-applying extends to the later end.
/// Landing damage is dealt by the skill that applied it (it knows the amount), not by the target.
/// Used by Wind Skill 2 (Whirlwind) and Skill 4 (Wind Roc).</summary>
public interface IAirborne
{
    void ApplyAirborne(float duration, float height);
}
