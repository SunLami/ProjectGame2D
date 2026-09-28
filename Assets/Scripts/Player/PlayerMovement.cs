using System;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class Player
{
    /// <summary>Fired whenever the player starts/continues moving under real input (not a raw key
    /// check) so tutorial/remap-agnostic systems can react. Static: there is one Player per scene
    /// and this is torn down with it, but subscribers must still unsubscribe symmetrically.</summary>
    public static event Action PlayerMoved;
    public static event Action PlayerSprinted;

    internal static void RaiseMovedForTests() => PlayerMoved?.Invoke();
    internal static void RaiseSprintedForTests() => PlayerSprinted?.Invoke();

    private static readonly int InputXHash = Animator.StringToHash("InputX");
    private static readonly int InputYHash = Animator.StringToHash("InputY");
    private static readonly int LastInputXHash = Animator.StringToHash("LastInputX");
    private static readonly int LastInputYHash = Animator.StringToHash("LastInputY");
    private static readonly int IsMovingHash = Animator.StringToHash("isMoving");
    private static readonly int IsRunningHash = Animator.StringToHash("isRunning");

    private Vector2 _moveInput;
    private Vector2 _lastFacingDirection = Vector2.down;
    private Vector2 _facingDirection = Vector2.down;

    // Separate from _lastFacingDirection: that field also tracks combat aim (e.g. OnAttack snaps it
    // toward the mouse cursor when idle-attacking), which would otherwise spin the Minimap/FullMap
    // marker away from the direction the player is actually walking. This one only ever moves when
    // real movement input does.
    private Vector2 _lastMovementFacingDirection = Vector2.down;

    // Axis-snapped (0,±1)/(±1,0) facing, exposed read-only for presentation that needs it (e.g. the
    // Minimap/FullMap player marker orientation) without letting anything outside Player mutate it.
    public Vector2 FacingDirection => _lastMovementFacingDirection;

    private float CurrentMoveSpeed => _stats.MoveSpeed
        * (_isRunning && _stats.HasStamina ? _stats.SprintMultiplier : 1f);

    public float MoveSpeed
    {
        get => _stats != null ? _stats.MoveSpeed : 0f;
        set
        {
            if (_stats != null)
                _stats.SetBaseMoveSpeed(value);
        }
    }

    private void FixedUpdate()
    {
        if (_isHit || _isDead || !GameStateManager.AllowsGameplayInput)
        {
            if (!GameStateManager.AllowsGameplayInput)
                _rigidbody.linearVelocity = Vector2.zero;
            return;
        }

        _rigidbody.linearVelocity = _moveInput * CurrentMoveSpeed;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        if (_isDead || !GameStateManager.AllowsGameplayInput)
        {
            StopMovement();
            return;
        }

        _moveInput = context.ReadValue<Vector2>();
        _animator.SetFloat(InputXHash, _moveInput.x);
        _animator.SetFloat(InputYHash, _moveInput.y);
        SetMoving(!context.canceled && _moveInput != Vector2.zero);

        if (_isMoving)
        {
            _lastMovementFacingDirection = SnapToAxis(_moveInput);
            if (!_isAttacking && !_isHit)
                SetFacingDirection(_moveInput);

            PlayerMoved?.Invoke();
        }
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (_isDead || !GameStateManager.AllowsGameplayInput)
        {
            SetRunning(false);
            return;
        }

        if (context.started && _stats.HasStamina)
        {
            SetRunning(true);
            PlayerSprinted?.Invoke();
        }
        else if (context.canceled)
        {
            SetRunning(false);
        }
    }

    /// <summary>Drives the regular walk animation while gameplay input is owned by a scripted
    /// sequence. Position is still controlled by the caller.</summary>
    public void BeginScriptedWalk(Vector2 direction)
    {
        StopMovement();
        Vector2 facing = SnapToAxis(direction);
        _animator.SetFloat(InputXHash, facing.x);
        _animator.SetFloat(InputYHash, facing.y);
        _lastMovementFacingDirection = facing;
        SetFacingDirection(facing);
        SetMoving(true);
    }

    public void EndScriptedWalk() => StopMovement();

    private void SetMoving(bool value)
    {
        _isMoving = value;
        _animator.SetBool(IsMovingHash, value);
    }

    private void SetRunning(bool value)
    {
        _isRunning = value;
        _animator.SetBool(IsRunningHash, value);
    }

    private void SetFacingDirection(Vector2 direction)
    {
        _lastFacingDirection = SnapToAxis(direction);
        _facingDirection = direction;
        _animator.SetFloat(LastInputXHash, direction.x);
        _animator.SetFloat(LastInputYHash, direction.y);
    }

    private static Vector2 SnapToAxis(Vector2 direction) =>
        Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
            ? new Vector2(Mathf.Sign(direction.x), 0f)
            : new Vector2(0f, Mathf.Sign(direction.y));

    private void StopMovement()
    {
        _moveInput = Vector2.zero;
        SetMoving(false);
        SetRunning(false);
        _rigidbody.linearVelocity = Vector2.zero;
    }
}
