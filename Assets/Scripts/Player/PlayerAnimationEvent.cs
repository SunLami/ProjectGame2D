using UnityEngine;

public class PlayerAnimationEvent : MonoBehaviour
{
    Player player;

    private float lastStepTime;
    private const float STEP_COOLDOWN = 0.2f;

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    public void OnAttackEnd()
    {
        player.IsAttacking = false;
    }

    public void OnHitEnd()
    {
         player.IsHit = false;
    }

    public void PlayFootSteps()
    {
        if (Time.time - lastStepTime < STEP_COOLDOWN) return;

        SoundFXManager.PlayFootSteps(1f);

        lastStepTime = Time.time;
    }
}
