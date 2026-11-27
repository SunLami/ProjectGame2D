using UnityEngine;

[RequireComponent(typeof(Player))]
public class PlayerAnimationEvent : MonoBehaviour
{
    private const float WalkStepCooldown = 0.2f;
    private const float RunStepCooldown = 0.14f;

    private Player _player;
    private float _lastStepTime = float.NegativeInfinity;

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
        bool isRunning = _player.IsRunning;
        if (Time.time - _lastStepTime < (isRunning ? RunStepCooldown : WalkStepCooldown))
            return;

        SoundFXManager.PlayFootSteps(_player.transform.position, isRunning);
        _lastStepTime = Time.time;
    }
}

