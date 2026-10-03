using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Short dash (D-085). Presentation uses the first and last frame of the 4-direction Run animation
/// (clips DashDown/Left/Right/Up built by Tools &gt; Project Game &gt; Player &gt; Build Dash Animations),
/// so it works for every body/head SpriteLibrary level 1-9 (the clips only store sprite-label hashes).
/// DemoScene-first: enabled only in the scenes listed in <see cref="_dashScenes"/>.
/// </summary>
public partial class Player
{
    private static readonly int IsDashingHash = Animator.StringToHash("isDashing");

    [Header("Dash")]
    [Tooltip("Scenes that enable dash while loaded (empty = every scene). Dash is DemoScene-first; add scene names here after the feature is finalized.")]
    [SerializeField] private string[] _dashScenes = { "DemoScene" };
    [SerializeField, Min(0.1f)] private float _dashDistance = 3.2f;
    [SerializeField, Min(0.05f)] private float _dashDuration = 0.2f;
    [SerializeField, Min(0f)] private float _dashCooldown = 0.8f;
    [SerializeField, Min(0f)] private float _dashStaminaCost = 20f;
    [Tooltip("Seconds of damage immunity from the start of the dash (0 = none).")]
    [SerializeField, Min(0f)] private float _dashInvulnerableDuration = 0.12f;
    [SerializeField] private bool _dashAfterimage = true;
    [SerializeField] private Color _dashAfterimageColor = new Color(0.65f, 0.9f, 1f, 0.5f);

    private bool _isDashing;
    private float _nextDashTime;
    private float _dashInvulnerableUntil;
    private Coroutine _dashRoutine;
    private InputAction _dashAction;
    private int _dashAnimatorParameterState; // 0 unknown, 1 present, -1 missing

    public bool IsDashing => _isDashing;
    public bool IsDashInvulnerable => _isDashing && Time.time < _dashInvulnerableUntil;

    private bool DashAllowedInThisScene
    {
        get
        {
            if (_dashScenes == null || _dashScenes.Length == 0)
                return true;

            // The Player lives in DontDestroyOnLoad and the active scene is Bootstrap, so match any
            // currently loaded scene from the list (DemoScene is loaded additively next to Bootstrap).
            for (int i = 0; i < _dashScenes.Length; i++)
            {
                if (SceneManager.GetSceneByName(_dashScenes[i]).isLoaded)
                    return true;
            }

            return false;
        }
    }

    private void OnDisable()
    {
        UnbindDashInput();
        CancelDash();
    }

    // PlayerInput owns its own copy of the Gameplay action map and creates it in its own OnEnable,
    // which can run after this component's, so the binding is retried from Update until it works.
    private void TickDashInputBinding()
    {
        if (_dashAction != null || !DashAllowedInThisScene)
            return;

        PlayerInput playerInput = FindAnyObjectByType<PlayerInput>(FindObjectsInactive.Include);
        if (playerInput == null || playerInput.actions == null)
            return;

        InputAction action = playerInput.actions.FindAction("Dash", false);
        if (action == null)
            return;

        _dashAction = action;
        _dashAction.performed += OnDashPerformed;
    }

    private void UnbindDashInput()
    {
        if (_dashAction == null)
            return;

        _dashAction.performed -= OnDashPerformed;
        _dashAction = null;
    }

    private void OnDashPerformed(InputAction.CallbackContext context) => TryStartDash();

    public bool TryStartDash()
    {
        if (!DashAllowedInThisScene || _isDashing || Time.time < _nextDashTime
            || _isDead || _deathPending || _isHit || _isAttacking
            || _isAimingSkill || _isCastingSkill
            || !GameStateManager.AllowsGameplayInput)
            return false;

        if (!_stats.TryConsumeStamina(_dashStaminaCost))
            return false;

        Vector2 direction = _moveInput != Vector2.zero ? _moveInput.normalized : _lastMovementFacingDirection;
        _dashRoutine = StartCoroutine(DashRoutine(direction));
        return true;
    }

    private IEnumerator DashRoutine(Vector2 direction)
    {
        _isDashing = true;
        _dashInvulnerableUntil = Time.time + _dashInvulnerableDuration;
        _nextDashTime = Time.time + _dashDuration + _dashCooldown;

        // The dash animation is a 4-direction blend on LastInputX/Y; feed it the dominant axis.
        Vector2 facing = SnapToAxis(direction);
        _lastMovementFacingDirection = facing;
        SetFacingDirection(facing);
        SetRunning(false);
        SetDashAnimator(true);

        Vector2 velocity = direction * (_dashDistance / _dashDuration);
        float elapsed = 0f;
        float nextGhostTime = 0f;
        while (elapsed < _dashDuration && !_isHit && !_isDead && GameStateManager.AllowsGameplayInput)
        {
            _rigidbody.linearVelocity = velocity;
            if (_dashAfterimage && elapsed >= nextGhostTime)
            {
                SpawnDashAfterimage();
                nextGhostTime = elapsed + 0.04f;
            }

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        _rigidbody.linearVelocity = Vector2.zero;
        EndDash();
    }

    private void EndDash()
    {
        _isDashing = false;
        _dashRoutine = null;
        SetDashAnimator(false);
    }

    private void CancelDash()
    {
        if (_dashRoutine != null)
            StopCoroutine(_dashRoutine);

        if (_isDashing)
            EndDash();
    }

    private void SetDashAnimator(bool value)
    {
        if (_animator == null)
            return;

        if (_dashAnimatorParameterState == 0)
        {
            _dashAnimatorParameterState = -1;
            foreach (AnimatorControllerParameter parameter in _animator.parameters)
            {
                if (parameter.nameHash == IsDashingHash)
                {
                    _dashAnimatorParameterState = 1;
                    break;
                }
            }

            if (_dashAnimatorParameterState < 0)
                Debug.LogWarning("Player dash: Animator has no 'isDashing' parameter. Run Tools > Project Game > Player > Build Dash Animations.", this);
        }

        if (_dashAnimatorParameterState > 0)
            _animator.SetBool(IsDashingHash, value);
    }

    private void SpawnDashAfterimage()
    {
        foreach (SpriteRenderer source in GetComponentsInChildren<SpriteRenderer>())
        {
            if (!source.enabled || source.sprite == null || source == _attackFxRenderer)
                continue;

            var ghost = new GameObject("DashAfterimage");
            ghost.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            ghost.transform.localScale = source.transform.lossyScale;

            var renderer = ghost.AddComponent<SpriteRenderer>();
            renderer.sprite = source.sprite;
            renderer.flipX = source.flipX;
            renderer.flipY = source.flipY;
            renderer.sortingLayerID = source.sortingLayerID;
            renderer.sortingOrder = -50;
            renderer.color = _dashAfterimageColor;

            ghost.AddComponent<DashAfterimageFade>().Begin(renderer, 0.25f);
        }
    }
}

/// <summary>Fades a dash ghost sprite and destroys it.</summary>
public sealed class DashAfterimageFade : MonoBehaviour
{
    private SpriteRenderer _renderer;
    private float _duration;
    private float _elapsed;
    private Color _start;

    public void Begin(SpriteRenderer renderer, float duration)
    {
        _renderer = renderer;
        _duration = Mathf.Max(0.01f, duration);
        _start = renderer.color;
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float k = Mathf.Clamp01(_elapsed / _duration);
        Color color = _start;
        color.a = _start.a * (1f - k);
        _renderer.color = color;

        if (k >= 1f)
            Destroy(gameObject);
    }
}
