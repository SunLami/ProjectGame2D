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

    public void OnAttackStart()
    {
        player.IsAttacking = true;
    }
    public void OnAttackEnd()
    {
        player.IsAttacking = false;
    }

    public void PlayFootSteps()
    {
        if (Time.time - lastStepTime < STEP_COOLDOWN) return;

        SoundFXManager.PlayFootSteps(1f);

        lastStepTime = Time.time;
    }

    //public void PlayWalkFootSteps()
    //{
    //    if (Time.time - lastStepTime < STEP_COOLDOWN) return;

    //    SoundFXManager.Play("PlayerGrassWalk", 0.5f);

    //    lastStepTime = Time.time;
    //}

    //public void PlayRunFootSteps()
    //{
    //    if (Time.time - lastStepTime < STEP_COOLDOWN) return;

    //    SoundFXManager.Play("PlayerGrassRun", 1f);

    //    lastStepTime = Time.time;
    //}
}
