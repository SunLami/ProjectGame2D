using UnityEngine;
using UnityEngine.InputSystem;

public partial class Player
{
    [Header("Shield Block (hold RMB)")]
    [Tooltip("Stamina drained per second while holding the block. Releases automatically at 0 stamina.")]
    [SerializeField, Min(0f)] private float _shieldStaminaDrainPerSecond = 20f;
    // Two quads sharing ShieldBubbleFX.shader (_HalfSide 0/1): the fresnel rim's centre is fully
    // transparent, but the character must still read as "standing inside" it, so the top half
    // renders behind the character (ShieldFXBack, sortingOrder below Body/Head) and the bottom
    // half in front (ShieldFXFront, sortingOrder above). See Assets/Shaders/ShieldBubbleFX.shader.
    [SerializeField] private MeshRenderer _shieldFxBackRenderer;
    [SerializeField] private MeshRenderer _shieldFxFrontRenderer;

    private bool _isBlocking;

    /// <summary>True while the player is actively holding the Shield block (RMB). Mirrors
    /// PlayerStat.IsBlocking (the value ReceiveDamage actually reads) for UI consumers like
    /// SkillHUD's Shield slot, so they don't need a PlayerStat reference just for this.</summary>
    public bool IsBlocking => _isBlocking;

    private void TickShieldBlock()
    {
        if (Mouse.current == null)
            return;

        bool wantsToBlock = Mouse.current.rightButton.isPressed
            && GameStateManager.AllowsGameplayInput
            && !_isDead && !_isAttacking && !IsAimingSkill && !IsGroundTargeting;

        if (wantsToBlock && !_isBlocking)
            StartBlocking();
        else if (_isBlocking && (!wantsToBlock || !_stats.TryConsumeStamina(_shieldStaminaDrainPerSecond * Time.deltaTime)))
            StopBlocking();
    }

    private void StartBlocking()
    {
        _isBlocking = true;
        _stats.IsBlocking = true;
        SetShieldFxEnabled(true);
    }

    private void StopBlocking()
    {
        _isBlocking = false;
        _stats.IsBlocking = false;
        SetShieldFxEnabled(false);
    }

    private void SetShieldFxEnabled(bool enabled)
    {
        if (_shieldFxBackRenderer != null)
            _shieldFxBackRenderer.enabled = enabled;
        if (_shieldFxFrontRenderer != null)
            _shieldFxFrontRenderer.enabled = enabled;
    }

    private void CacheShieldFxReferences()
    {
        if (_shieldFxBackRenderer == null)
            _shieldFxBackRenderer = transform.Find("ShieldFXBack")?.GetComponent<MeshRenderer>();
        if (_shieldFxFrontRenderer == null)
            _shieldFxFrontRenderer = transform.Find("ShieldFXFront")?.GetComponent<MeshRenderer>();
    }
}
