using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Aim/cast flow for the "Vòng tròn mục tiêu" skill shape (SkillVfxPipeline.md §7.1 khuôn #2):
/// player positions a ground-target circle clamped within a max range around themselves, left-click
/// confirms the world position, right-click cancels. Mirrors PlayerSkillCast.cs's directional-aim
/// pattern (same Begin/Extend/EndSkillCast Weapon/AttackFX hide hooks) but aims a world POSITION
/// instead of a direction. Draws two circles -- outer (vortex, slow + small DoT) and inner (column,
/// bigger DoT) -- so the player can see both damage tiers before confirming.</summary>
public partial class Player
{
    private bool _isGroundTargeting;
    private float _groundTargetMaxRange;
    private float _groundTargetRadius;
    private float _groundTargetInnerRadius;
    private Vector2 _groundTargetPosition;
    private LineRenderer _groundTargetIndicator;
    private LineRenderer _groundTargetInnerIndicator;
    private Action<Vector2> _pendingGroundTargetConfirmCallback;

    public bool IsGroundTargeting => _isGroundTargeting;
    public Vector2 GroundTargetPosition => _groundTargetPosition;

    /// <summary>Starts the ground-target aim phase: shows an outer circle (radius `aoeRadius`, the
    /// vortex slow/DoT zone) and an inner circle (radius `innerRadius`, the bigger-damage column zone)
    /// that follow the mouse, clamped within `maxRange` of the Player. Left-click confirms the
    /// position and fires onConfirm; right-click cancels.</summary>
    public void BeginGroundTargetSkill(float maxRange, float aoeRadius, float innerRadius, Action<Vector2> onConfirm)
    {
        if (_isGroundTargeting)
            return;

        _isGroundTargeting = true;
        _groundTargetMaxRange = maxRange;
        _groundTargetRadius = aoeRadius;
        _groundTargetInnerRadius = innerRadius;
        _pendingGroundTargetConfirmCallback = onConfirm;
        BeginSkillCast();

        EnsureGroundTargetIndicator();
        _groundTargetIndicator.gameObject.SetActive(true);
        _groundTargetInnerIndicator.gameObject.SetActive(true);
        UpdateGroundTargetPositionFromPointer();
        UpdateGroundTargetIndicatorVisual();
    }

    public void CancelGroundTargetSkill()
    {
        if (!_isGroundTargeting)
            return;

        _isGroundTargeting = false;
        _pendingGroundTargetConfirmCallback = null;
        if (_groundTargetIndicator != null)
            _groundTargetIndicator.gameObject.SetActive(false);
        if (_groundTargetInnerIndicator != null)
            _groundTargetInnerIndicator.gameObject.SetActive(false);
        EndSkillCast();
    }

    private void TickGroundTargetAim()
    {
        if (!_isGroundTargeting)
            return;

        UpdateGroundTargetPositionFromPointer();
        UpdateGroundTargetIndicatorVisual();

        if (Mouse.current == null)
            return;

        if (Mouse.current.rightButton.wasPressedThisFrame)
            CancelGroundTargetSkill();
        else if (Mouse.current.leftButton.wasPressedThisFrame)
            ConfirmGroundTargetCast();
    }

    private void ConfirmGroundTargetCast()
    {
        _isGroundTargeting = false;
        if (_groundTargetIndicator != null)
            _groundTargetIndicator.gameObject.SetActive(false);
        if (_groundTargetInnerIndicator != null)
            _groundTargetInnerIndicator.gameObject.SetActive(false);

        Vector2 facing = _groundTargetPosition - (Vector2)transform.position;
        if (facing.sqrMagnitude > 0.0001f)
            SetFacingDirection(facing);

        _isAttacking = true;
        _isCastingSkill = true;
        _animator.SetTrigger(AttackHash);

        Action<Vector2> callback = _pendingGroundTargetConfirmCallback;
        _pendingGroundTargetConfirmCallback = null;
        callback?.Invoke(_groundTargetPosition);
    }

    private void UpdateGroundTargetPositionFromPointer()
    {
        if (_mainCamera == null)
            _mainCamera = Camera.main;
        if (_mainCamera == null || Pointer.current == null)
            return;

        Vector2 screenPosition = Pointer.current.position.ReadValue();
        Vector3 worldPosition = _mainCamera.ScreenToWorldPoint(screenPosition);
        Vector2 origin = transform.position;
        Vector2 offset = (Vector2)worldPosition - origin;
        if (offset.magnitude > _groundTargetMaxRange)
            offset = offset.normalized * _groundTargetMaxRange;

        _groundTargetPosition = origin + offset;
    }

    private void UpdateGroundTargetIndicatorVisual()
    {
        if (_groundTargetIndicator == null)
            return;

        DrawCircle(_groundTargetIndicator, _groundTargetRadius);
        DrawCircle(_groundTargetInnerIndicator, _groundTargetInnerRadius);
    }

    private void DrawCircle(LineRenderer line, float radius)
    {
        if (line == null)
            return;

        const int segments = 24;
        for (int i = 0; i <= segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            Vector3 point = new(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            line.SetPosition(i, (Vector3)_groundTargetPosition + point);
        }
    }

    private void EnsureGroundTargetIndicator()
    {
        if (_groundTargetIndicator != null)
            return;

        _groundTargetIndicator = CreateGroundTargetCircle("GroundTargetIndicator", new Color(0.4f, 0.8f, 1f, 0.9f));
        _groundTargetInnerIndicator = CreateGroundTargetCircle("GroundTargetInnerIndicator", new Color(1f, 1f, 1f, 0.9f));
    }

    private static LineRenderer CreateGroundTargetCircle(string name, Color color)
    {
        var go = new GameObject(name);

        var line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 25;
        line.startWidth = 0.06f;
        line.endWidth = 0.06f;
        line.loop = true;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = color;
        line.endColor = color;
        line.sortingLayerName = "Player";
        line.sortingOrder = 4;
        go.SetActive(false);

        return line;
    }
}
