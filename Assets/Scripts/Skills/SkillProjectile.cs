using UnityEngine;

/// <summary>Generic flying projectile for the "Projectile định hướng" skill shape
/// (SkillVfxPipeline.md §7.1/§8). Element-agnostic: element-specific sprite frames are assigned per
/// prefab instance, this script only owns motion + frame animation + free-angle rotation.
///
/// Sprites are drawn for a single reference direction (nose pointing along +X / right) and the whole
/// Transform is rotated to the real aim angle at spawn (SkillVfxPipeline.md §8.2 — matches how the
/// reference craftpix magic-effect VFX are actually used; blob-shaped VFX without limbs/asymmetric
/// detail don't break when rotated, unlike character sprites).</summary>
public class SkillProjectile : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private Sprite[] _frames;
    [SerializeField] private float _speed = 8f;
    [SerializeField] private float _maxRange = 6f;
    [SerializeField] private float _frameRate = 12f;
    [SerializeField] private float _damage = 15f;
    [SerializeField] private float _knockbackForce = 3f;
    [SerializeField] private GameObject _impactVfxPrefab;
    [Tooltip("Non-damageable colliders on these layers also stop the projectile (e.g. future trees/terrain), instead of it passing through silently. Enemies always stop it regardless of layer, via IDamageable.")]
    [SerializeField] private LayerMask _obstacleLayers;

    private string _impactSfxId = SfxIds.SkillWaterS1Impact;
    private Vector2 _direction;
    private Vector3 _startPosition;
    private float _frameTimer;
    private int _frameIndex;

    /// <summary>Overrides the impact SFX (default: water bolt) - the earth kit reuses this script with its own id.</summary>
    public void SetImpactSfx(string sfxId) => _impactSfxId = sfxId;

    public void Launch(Vector2 direction, float speed, float maxRange)
    {
        _direction = direction.normalized;
        _speed = speed;
        _maxRange = maxRange;
        _startPosition = transform.position;
        _frameIndex = 0;
        _frameTimer = 0f;

        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (_renderer != null && _frames is { Length: > 0 })
            _renderer.sprite = _frames[0];
    }

    private void Update()
    {
        transform.position += (Vector3)(_direction * (_speed * Time.deltaTime));

        if (_frames is { Length: > 1 })
        {
            _frameTimer += Time.deltaTime;
            float frameDuration = 1f / _frameRate;
            if (_frameTimer >= frameDuration)
            {
                _frameTimer -= frameDuration;
                _frameIndex = (_frameIndex + 1) % _frames.Length;
                if (_renderer != null)
                    _renderer.sprite = _frames[_frameIndex];
            }
        }

        if (Vector3.Distance(_startPosition, transform.position) >= _maxRange)
        {
            SoundFXManager.PlaySfxAt(_impactSfxId, transform.position, 0.6f);
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<Player>() != null)
            return;

        MonoBehaviour[] candidates = other.GetComponentsInParent<MonoBehaviour>();
        foreach (MonoBehaviour candidate in candidates)
        {
            if (candidate is not IDamageable target || target.IsDead)
                continue;

            target.TakeDamage(_damage, _direction, _knockbackForce);

            var flash = candidate.GetComponent<EnemyHitFlash>();
            if (flash == null)
                flash = candidate.gameObject.AddComponent<EnemyHitFlash>();
            flash.PlayFlash();

            if (_impactVfxPrefab != null)
                Instantiate(_impactVfxPrefab, transform.position, Quaternion.identity);

            SoundFXManager.PlaySfxAt(_impactSfxId, transform.position);
            Destroy(gameObject);
            return;
        }

        // Not a damageable target -- still stop at solid obstacles (e.g. trees/terrain) on
        // _obstacleLayers instead of silently passing through, matching SkillBeam's blocking
        // behavior. Other triggers (tutorial/quest zones etc.) are left alone by default since they
        // aren't on _obstacleLayers.
        if (((1 << other.gameObject.layer) & _obstacleLayers) != 0)
        {
            if (_impactVfxPrefab != null)
                Instantiate(_impactVfxPrefab, transform.position, Quaternion.identity);

            SoundFXManager.PlaySfxAt(_impactSfxId, transform.position);
            Destroy(gameObject);
        }
    }
}
