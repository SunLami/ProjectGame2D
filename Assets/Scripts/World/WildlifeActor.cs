using System;
using UnityEngine;

/// <summary>Ordinary scene actor: wander/flee or chase using definition data. Respawns on scene reload.</summary>
[RequireComponent(typeof(Rigidbody2D))]
public sealed class WildlifeActor : MonoBehaviour, IDamageable
{
    [SerializeField] private WildlifeDefinition _definition;
    [SerializeField] private Animator _animator;
    [SerializeField, Min(0f)] private float _deathAnimationDuration;

    [SerializeField] private bool _attackOnAnimationEvent;
    [SerializeField] private string _displayName;
    [Tooltip("Inclusive minimum and maximum encounter level.")]
    [SerializeField] private Vector2Int _levelRange = Vector2Int.one;

    private const float ContactRange = 0.85f;

    private static readonly int Facing = Animator.StringToHash("Facing");
    private static readonly int Motion = Animator.StringToHash("Motion");
    private static readonly int Attack = Animator.StringToHash("Attack");
    private static readonly int Hurt = Animator.StringToHash("Hurt");
    private static readonly int Dead = Animator.StringToHash("Dead");

    private Rigidbody2D _body;
    private Vector2 _home, _wander;
    private float _health, _nextWander, _nextDamage;
    private bool _attackPending;
    private PathFollower _navigator;
    private FacingFilter _facing;
    private bool _started;
    private bool _wasReacting;
    private readonly MobVoiceTimer _voice = new();

    private const float Acceleration = 12f;
    public event Action<float, float> HealthChanged;
    public event Action Died;
    public float Health => _health;
    public float MaxHealth => (_definition != null ? _definition.Health : 1f) * EncounterLevel.HealthMultiplier(Level);
    public int Level { get; private set; } = 1;
    public string DisplayName => string.IsNullOrWhiteSpace(_displayName)
        ? (_definition != null ? _definition.name : gameObject.name)
        : _displayName;
    private float ContactDamage => _definition.ContactDamage * EncounterLevel.DamageMultiplier(Level);
    public bool IsDead => _health <= 0f;
    public WildlifeDefinition Definition => _definition;
    private void Start()
    {
        _started = true;
        CrowdSeparation.Register(_body, _navigator.Footprint.HalfExtents.x);
    }

    private void OnEnable()
    {
        if (_started && !IsDead)
            CrowdSeparation.Register(_body, _navigator.Footprint.HalfExtents.x);
    }

    private void OnDisable() => CrowdSeparation.Unregister(_body);

    private void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
        _home = transform.position;
        _wander = _home;
        Level = EncounterLevel.Roll(_levelRange);
        _health = MaxHealth;
        _navigator = new PathFollower(_body);
        _facing = new FacingFilter(Vector2.down);
    }
    private void FixedUpdate()
    {
        if (IsDead || _definition == null) return;
        if (Time.time >= _nextDamage)
            _attackPending = false;

        bool animatedAttack = _attackOnAnimationEvent && _animator != null && _animator.isActiveAndEnabled;
        if (animatedAttack)
        {
            AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
            if (state.shortNameHash == Attack || state.shortNameHash == Hurt)
            {
                _body.linearVelocity = Vector2.zero;
                return;
            }
        }

        Vector2 position = _body.position;
        Vector2 direction = Vector2.zero;
        PlayerStat player = PlayerStat.Instance;
        bool reactingToPlayer = false;
        bool canMove = GameStateManager.AllowsGameplayInput;
        if (player != null && !player.IsDead && Vector2.Distance(position, player.transform.position) < _definition.ReactionRange)
        {
            reactingToPlayer = true;
            Vector2 toPlayer = ((Vector2)player.transform.position - position).normalized;
            direction = _definition.Aggressive
                ? Navigate(position, player.transform.position, toPlayer)
                : Flee(position, -toPlayer);
            if (canMove && _definition.Aggressive && Vector2.Distance(position, player.transform.position) < ContactRange && Time.time >= _nextDamage)
            {
                if (_animator != null && canMove)
                {
                    UpdateFacing(toPlayer, true);
                    _animator.SetTrigger(Attack);
                }
                WildlifeSfx.Attack(this, _definition.AnimalId);
                if (animatedAttack)
                {
                    _attackPending = true;
                    _body.linearVelocity = Vector2.zero;
                }
                else
                {
                    player.GetComponent<Player>()?.TakeDamage(ContactDamage, toPlayer, 0.5f);
                }
                _nextDamage = Time.time + 1.2f;
            }
        }
        else
        {
            if (Time.time >= _nextWander)
            {
                _wander = _home + UnityEngine.Random.insideUnitCircle * 2f;
                _nextWander = Time.time + 3f;
            }
            if (Vector2.Distance(position, _wander) > 0.2f)
            {
                direction = Navigate(position, _wander, (_wander - position).normalized, out bool reachable);
                if (!reachable)
                {
                    // Wander point is boxed in: rest here and choose another next tick.
                    _wander = position;
                    _nextWander = Time.time + 1f;
                }
            }
        }
        if (reactingToPlayer && !_wasReacting && _voice.TryAlert())
            WildlifeSfx.Alert(this, _definition.AnimalId);
        else if (!reactingToPlayer && _voice.TryIdle(position))
            WildlifeSfx.Idle(this, _definition.AnimalId);
        _wasReacting = reactingToPlayer;
        if (Vector2.Distance(position, _home) > _definition.LeashRange)
            direction = Navigate(position, _home, (_home - position).normalized);
        if (PathGridBuilder.Active != null && direction.sqrMagnitude > 0.0001f)
            direction = CrowdSeparation.Apply(_body, _navigator.Footprint.HalfExtents.x, direction);
        Vector2 targetVelocity = canMove && !_attackPending ? direction * _definition.Speed : Vector2.zero;
        // Ramp toward the steering velocity (D-109) but stop at once for attacks / paused input.
        _body.linearVelocity = targetVelocity == Vector2.zero && (!canMove || _attackPending)
            ? Vector2.zero
            : Vector2.MoveTowards(_body.linearVelocity, targetVelocity, Acceleration * Time.fixedDeltaTime);
        if (_animator != null)
        {
            if (_body.linearVelocity.sqrMagnitude > 0.001f)
                UpdateFacing(direction);
            _animator.SetFloat(Motion, _body.linearVelocity.sqrMagnitude <= 0.001f ? 0f : reactingToPlayer ? 2f : 1f);
        }
    }

    private Vector2 Navigate(Vector2 position, Vector2 target, Vector2 legacyDirection)
        => Navigate(position, target, legacyDirection, out _);

    /// <summary>Obstacle-aware direction toward <paramref name="target"/> (D-109). Without a scene
    /// <see cref="PathGridBuilder"/> this returns the legacy straight direction.</summary>
    private Vector2 Navigate(Vector2 position, Vector2 target, Vector2 legacyDirection, out bool reachable)
    {
        reachable = true;
        PathFollower.Status status = _navigator.Steer(position, target, out Vector2 direction);
        if (status == PathFollower.Status.Legacy)
            return legacyDirection;
        if (status == PathFollower.Status.NoPath)
        {
            reachable = false;
            return Vector2.zero;
        }
        return direction;
    }

    /// <summary>Flee away from the player but slide along trees/walls instead of running into them.</summary>
    private Vector2 Flee(Vector2 position, Vector2 away)
    {
        PathGridBuilder grid = PathGridBuilder.Active;
        if (grid == null)
            return away;
        return grid.SteerAround(position, away, _navigator.Footprint, 0.9f);
    }

    // Called by the contact frame of each directional Boar attack clip.
    public void ApplyAnimatedAttackHit()
    {
        if (!_attackPending)
            return;

        _attackPending = false;
        if (!isActiveAndEnabled || IsDead || !GameStateManager.AllowsGameplayInput)
            return;

        PlayerStat target = PlayerStat.Instance;
        if (target == null || target.IsDead)
            return;

        Vector2 offset = (Vector2)target.transform.position - _body.position;
        if (offset.sqrMagnitude < ContactRange * ContactRange)
            target.GetComponent<Player>()?.TakeDamage(ContactDamage, offset.normalized, 0.5f);
    }

    private void UpdateFacing(Vector2 direction, bool immediate = false)
    {
        if (direction.sqrMagnitude <= 0.001f)
            return;

        direction = _facing.Update(direction, Time.time, immediate);
        float facing = Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
            ? (direction.x < 0f ? 2f : 3f)
            : (direction.y < 0f ? 0f : 1f);
        _animator.SetFloat(Facing, facing);
    }
    public void TakeDamage(float damage, Vector2 knockbackDirection, float knockbackForce)
    {
        if (IsDead || damage <= 0f || float.IsNaN(damage)) return;
        CombatFeedback.MapDuyActorHit(transform, damage);
        _health = Mathf.Max(0f, _health - damage);
        HealthChanged?.Invoke(_health, MaxHealth);
        _attackPending = false;
        if (_animator != null && _attackOnAnimationEvent)
            _animator.ResetTrigger(Attack);
        if (!IsDead)
        {
            WildlifeSfx.Hurt(this, _definition != null ? _definition.AnimalId : null);
            if (_animator != null)
                _animator.SetTrigger(Hurt);
            return;
        }
        WildlifeSfx.Death(this, _definition != null ? _definition.AnimalId : null);
        _body.linearVelocity = Vector2.zero;
        CrowdSeparation.Unregister(_body);
        if (_definition != null && _definition.ExperienceReward > 0)
            PlayerStat.Instance?.AddExperience(_definition.ExperienceReward);
        Died?.Invoke();
        if (_definition != null) QuestDomainEvents.RaiseEnemyKilled(_definition.AnimalId, string.Empty);
        if (_animator != null && _deathAnimationDuration > 0f)
        {
            _animator.SetBool(Dead, true);
            foreach (Collider2D hitbox in GetComponentsInChildren<Collider2D>())
                hitbox.enabled = false;
            Destroy(gameObject, _deathAnimationDuration);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
