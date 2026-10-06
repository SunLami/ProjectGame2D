using System;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class Player
{
    private bool _isAimingSkill;
    private float _skillAimMaxRange;
    private float _skillAimWidth;
    private float _skillAimFanAngle;
    private float _skillAimEndRadius;
    private LineRenderer _skillAimEndCircle;
    private Vector2 _skillAimDirection = Vector2.down;
    private LineRenderer _skillAimIndicator;
    private Action<Vector2> _pendingSkillConfirmCallback;

    private bool _isCastingSkill;

    [Tooltip("Offset from the Player's pivot (feet) to the point skills originate from -- roughly body center. Shared by the aim indicator and directional skills like the beam so preview and effect line up.")]
    [SerializeField] private Vector2 _skillOriginOffset = new Vector2(0f, 0.5f);

    public Vector2 SkillOriginOffset => _skillOriginOffset;
    public Vector2 SkillOrigin => (Vector2)transform.position + _skillOriginOffset;

    public bool IsAimingSkill => _isAimingSkill;
    public Vector2 SkillAimDirection => _skillAimDirection;

    /// <summary>True while the Attack animation is playing because of a skill cast rather than a
    /// melee attack — checked by PlayerCombat to skip the melee hitbox for that swing
    /// (SkillVfxPipeline.md §8.4).</summary>
    public bool IsCastingSkill => _isCastingSkill;

    /// <summary>Starts the aim phase for a directional skill cast (SkillVfxPipeline.md §8): shows a
    /// straight rectangle indicator (length `maxRange`, width `indicatorWidth`) that follows the mouse,
    /// hides the Weapon (BeginSkillCast). Pass the real hitbox width (e.g. a beam's thickness) so the
    /// preview matches what the skill will actually cover; defaults to a thin arrow-like strip for
    /// skills without a meaningful width (e.g. a thin projectile). Left-click confirms the aim and
    /// fires onConfirm with the free (unsnapped) aim direction. Only blocks on an aim already in
    /// progress — intentionally does NOT block while a previous cast's swing/flight is still resolving,
    /// so spamming the skill key casts again immediately; ExtendSkillCastHide (PlayerSkillFX.cs) is
    /// what keeps Weapon/AttackFX correctly hidden across overlapping casts.</summary>
    public void BeginAimSkill(float maxRange, Action<Vector2> onConfirm, float indicatorWidth = 0.15f, float fanAngleDegrees = 0f, float endCircleRadius = 0f)
    {
        if (_isAimingSkill)
            return;

        _isAimingSkill = true;
        _skillAimMaxRange = maxRange;
        _skillAimWidth = Mathf.Max(0.02f, indicatorWidth);
        _skillAimFanAngle = Mathf.Max(0f, fanAngleDegrees);
        // Optional circle drawn around the far end of the rectangle (e.g. the blast radius of a skill that
        // detonates where its line ends); 0 = no circle.
        _skillAimEndRadius = Mathf.Max(0f, endCircleRadius);
        _pendingSkillConfirmCallback = onConfirm;
        SoundFXManager.PlaySfx(SfxIds.CombatSkillAimStart);
        BeginSkillCast();

        EnsureSkillAimIndicator();
        _skillAimIndicator.gameObject.SetActive(true);
        UpdateSkillAimDirectionFromPointer();
        UpdateSkillAimIndicatorVisual();
    }

    /// <summary>Cancels an in-progress aim without firing (does not call the confirm callback).</summary>
    public void CancelAimSkill()
    {
        if (!_isAimingSkill)
            return;

        _isAimingSkill = false;
        _pendingSkillConfirmCallback = null;
        if (_skillAimIndicator != null)
            _skillAimIndicator.gameObject.SetActive(false);
        SoundFXManager.PlaySfx(SfxIds.CombatSkillAimCancel);
        EndSkillCast();
    }

    private void TickSkillAim()
    {
        if (!_isAimingSkill)
            return;

        UpdateSkillAimDirectionFromPointer();
        UpdateSkillAimIndicatorVisual();

        if (Mouse.current == null)
            return;

        if (Mouse.current.rightButton.wasPressedThisFrame)
            CancelAimSkill();
        else if (Mouse.current.leftButton.wasPressedThisFrame)
            ConfirmSkillCast();
    }

    private void ConfirmSkillCast()
    {
        _isAimingSkill = false;
        if (_skillAimIndicator != null)
            _skillAimIndicator.gameObject.SetActive(false);

        // Player body snaps to the nearest of the existing 4 facing directions; the skill VFX itself
        // uses the free aim angle directly (SkillVfxPipeline.md §8.1) — the two are intentionally
        // decoupled.
        SetFacingDirection(_skillAimDirection);

        // Reuses the existing Attack trigger/animation as the cast swing (SkillVfxPipeline.md §1/§8.4)
        // — IsCastingSkill tells PlayerCombat to skip the melee hitbox for this swing so casting a
        // skill doesn't also deal a melee hit. FinishAttack() (existing OnAttackEnd animation event)
        // clears both _isAttacking and _isCastingSkill at the end of the clip.
        _isAttacking = true;
        _isCastingSkill = true;
        _animator.SetTrigger(AttackHash);

        Action<Vector2> callback = _pendingSkillConfirmCallback;
        _pendingSkillConfirmCallback = null;
        SoundFXManager.PlaySfx(SfxIds.CombatSkillAimConfirm);
        callback?.Invoke(_skillAimDirection);
    }

    private void UpdateSkillAimDirectionFromPointer()
    {
        if (_mainCamera == null)
            _mainCamera = Camera.main;
        if (_mainCamera == null || Pointer.current == null)
            return;

        Vector2 screenPosition = Pointer.current.position.ReadValue();
        Vector3 worldPosition = _mainCamera.ScreenToWorldPoint(screenPosition);
        Vector2 direction = (Vector2)worldPosition - SkillOrigin;
        if (direction.sqrMagnitude > 0.0001f)
            _skillAimDirection = direction.normalized;
    }

    /// <summary>Draws a closed rectangle (length `_skillAimMaxRange`, width `_skillAimWidth`) from the
    /// player outward along the aim direction, so the preview matches the actual hitbox corridor.</summary>
    private void UpdateSkillAimIndicatorVisual()
    {
        if (_skillAimIndicator == null)
            return;

        // Indicator is a child of the Player (local space), so lifting it by the origin offset keeps
        // it centered on the body like the skill it previews.
        _skillAimIndicator.transform.localPosition = (Vector3)_skillOriginOffset;
        UpdateSkillAimEndCircle();

        if (_skillAimFanAngle > 0f)
        {
            DrawSkillAimFan();
            return;
        }

        if (_skillAimIndicator.positionCount != 4)
            _skillAimIndicator.positionCount = 4;

        Vector2 forward = _skillAimDirection * _skillAimMaxRange;
        Vector2 perpendicular = new Vector2(-_skillAimDirection.y, _skillAimDirection.x) * (_skillAimWidth / 2f);

        _skillAimIndicator.SetPosition(0, (Vector3)(-perpendicular));
        _skillAimIndicator.SetPosition(1, (Vector3)(perpendicular));
        _skillAimIndicator.SetPosition(2, (Vector3)(forward + perpendicular));
        _skillAimIndicator.SetPosition(3, (Vector3)(forward - perpendicular));
    }

    /// <summary>Closed sector outline: from the origin out to `_skillAimMaxRange`, spanning
    /// `_skillAimFanAngle` degrees centered on the aim direction.</summary>
    private void DrawSkillAimFan()
    {
        const int arcSegments = 16;
        int pointCount = arcSegments + 2;
        if (_skillAimIndicator.positionCount != pointCount)
            _skillAimIndicator.positionCount = pointCount;

        float centerAngle = Mathf.Atan2(_skillAimDirection.y, _skillAimDirection.x);
        float halfAngle = _skillAimFanAngle * 0.5f * Mathf.Deg2Rad;

        _skillAimIndicator.SetPosition(0, Vector3.zero);
        for (int i = 0; i <= arcSegments; i++)
        {
            float t = i / (float)arcSegments;
            float angle = centerAngle - halfAngle + t * halfAngle * 2f;
            Vector3 point = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * _skillAimMaxRange;
            _skillAimIndicator.SetPosition(i + 1, point);
        }
    }

    /// <summary>Draws the optional end circle (child of the indicator, same local space) centred on the far end of
    /// the aim line so the player can see the radius that skill will hit.</summary>
    private void UpdateSkillAimEndCircle()
    {
        if (_skillAimEndRadius <= 0f)
        {
            if (_skillAimEndCircle != null)
                _skillAimEndCircle.gameObject.SetActive(false);
            return;
        }

        if (_skillAimEndCircle == null)
        {
            var go = new GameObject("SkillAimEndCircle");
            go.transform.SetParent(_skillAimIndicator.transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 40;
            line.startWidth = 0.05f;
            line.endWidth = 0.05f;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = new Color(1f, 1f, 1f, 0.9f);
            line.endColor = new Color(1f, 1f, 1f, 0.9f);
            line.sortingLayerName = "Player";
            line.sortingOrder = 4;
            _skillAimEndCircle = line;
        }

        _skillAimEndCircle.gameObject.SetActive(true);
        Vector2 center = _skillAimDirection * _skillAimMaxRange;
        int count = _skillAimEndCircle.positionCount;
        for (int i = 0; i < count; i++)
        {
            float angle = i / (float)count * Mathf.PI * 2f;
            Vector2 point = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _skillAimEndRadius;
            _skillAimEndCircle.SetPosition(i, (Vector3)point);
        }
    }

    private void EnsureSkillAimIndicator()
    {
        if (_skillAimIndicator != null)
            return;

        var go = new GameObject("SkillAimIndicator");
        go.transform.SetParent(transform, false);

        var line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = 4;
        line.loop = true;
        line.startWidth = 0.05f;
        line.endWidth = 0.05f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = new Color(1f, 1f, 1f, 0.9f);
        line.endColor = new Color(1f, 1f, 1f, 0.9f);
        line.sortingLayerName = "Player";
        line.sortingOrder = 4;
        go.SetActive(false);

        _skillAimIndicator = line;
    }
}
