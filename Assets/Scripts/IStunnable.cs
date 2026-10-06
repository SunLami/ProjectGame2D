/// <summary>Stun hook (D-076): a stunned target cannot move, chase or start attacks until the stun
/// expires. Re-applying extends to the later expiry. Used by Earth Skill 2 (Rock Arena).</summary>
public interface IStunnable
{
    void ApplyStun(float duration);
}
