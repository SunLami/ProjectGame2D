using UnityEngine;

[RequireComponent(typeof(Player))]
public class PlayerAnimationEvent : MonoBehaviour
{
    private const float StepCooldown = 0.2f;

    private Player _player;
    private float _lastStepTime;

    private void Awake()
    {
        _player = GetComponent<Player>();
    }

    public void OnAttackEnd()
    {
        _player.FinishAttack();
    }

    public void OnHitEnd()
    {
        _player.FinishHit();
    }

    public void PlayFootSteps()
    {
        if (Time.time - _lastStepTime < StepCooldown)
            return;

        SoundFXManager.PlayFootSteps(1f);
        _lastStepTime = Time.time;
    }
}

