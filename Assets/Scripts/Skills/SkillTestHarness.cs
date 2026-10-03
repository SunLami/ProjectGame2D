using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Skill hotbar entry point for DemoScene: Q/E/R/T cast skill slots 1-4 directly (no UI yet
/// — Nâng cấp/Setting Skill UI is planned for later, per user request 2026-10-01). Also still exposes
/// CastTestProjectile() for the debug UI button wired earlier. Only Skill 1 (Giọt Nước Xoáy, Q) has
/// VFX built so far (SkillVfxPipeline.md §8.3); slots 2-4 (E/R/T) log instead of silently doing
/// nothing until their VFX/mechanic exist. The aim indicator + left-click-to-confirm flow
/// (Player.BeginAimSkill) is the real mechanic (§8).</summary>
public class SkillTestHarness : MonoBehaviour
{
    [SerializeField] private Player _player;
    [SerializeField] private SkillProjectile _projectilePrefab;
    [SerializeField] private float _projectileSpeed = 8f;
    [SerializeField] private float _projectileRange = 6f;
    [SerializeField] private float _castHideDuration = 0.4f;
    [SerializeField] private float _spawnHeightOffset = 0.7f;

    [Header("Earth kit (Tab toggles Water/Earth for Q/E/R/T)")]
    [SerializeField] private SkillProjectile _earthProjectilePrefab;
    [SerializeField] private float _earthProjectileSpeed = 6f;
    [SerializeField] private float _earthProjectileRange = 6f;

    [SerializeField] private SkillRockArena _earthArenaPrefab;
    [SerializeField] private float _earthArenaRange = 5f;
    [Tooltip("Wall centerline radius; must match SkillRockArena._radius on the prefab.")]
    [SerializeField] private float _earthArenaRadius = 1.8f;
    [SerializeField] private float _earthArenaWallThickness = 0.4f;
    [SerializeField] private float _earthArenaCastHideDuration = 0.6f;

    [SerializeField] private SkillSpikeCone _earthSpikeConePrefab;
    [SerializeField] private float _earthSpikeConeRange = 6f;
    [SerializeField] private float _earthSpikeConeAngle = 60f;
    [SerializeField] private float _earthSpikeConeCastHideDuration = 1.2f;

    [SerializeField] private SkillMeteorStorm _earthMeteorStormPrefab;
    [SerializeField] private float _earthMeteorStormRange = 6f;
    [Tooltip("Outer indicator circle; keep equal to SkillMeteorStorm._radius on the prefab.")]
    [SerializeField] private float _earthMeteorStormRadius = 3.5f;
    [Tooltip("Inner indicator circle; keep equal to SkillMeteorStorm._finaleRadius on the prefab.")]
    [SerializeField] private float _earthMeteorStormFinaleRadius = 2.2f;
    [SerializeField] private float _earthMeteorStormCastHideDuration = 1.5f;

    [Header("Wind kit (Tab cycles Water -> Earth -> Wind)")]
    [SerializeField] private SkillBoomerang _windBladePrefab;
    [SerializeField] private float _windBladeRange = 7f;
    [SerializeField] private float _windBladeCastHideDuration = 1.6f;

    [SerializeField] private SkillWhirlwind _windWhirlwindPrefab;
    [SerializeField] private float _windWhirlwindRange = 5f;
    [Tooltip("Outer indicator circle; keep equal to SkillWhirlwind._radius on the prefab.")]
    [SerializeField] private float _windWhirlwindRadius = 2f;
    [Tooltip("Inner indicator circle; keep equal to SkillWhirlwind._coreRadius on the prefab.")]
    [SerializeField] private float _windWhirlwindCoreRadius = 0.9f;
    [SerializeField] private float _windWhirlwindCastHideDuration = 0.8f;

    [SerializeField] private SkillGustFan _windGustFanPrefab;
    [SerializeField] private float _windGustFanRange = 6f;
    [SerializeField] private float _windGustFanAngle = 35f;
    [SerializeField] private float _windGustFanCastHideDuration = 1.6f;

    [SerializeField] private SkillWindRoc _windRocPrefab;
    [Tooltip("Keep the whole path (and especially the finale) inside the camera view: the screen shows only about 10 units to the right of the player.")]
    [SerializeField] private float _windRocLength = 8f;
    [Tooltip("Aim indicator width; keep equal to SkillWindRoc._pathWidth on the prefab.")]
    [SerializeField] private float _windRocPathWidth = 2.5f;
    [Tooltip("Radius of the circle drawn at the end of the aim line; keep equal to SkillWindRoc._blastRadius on the prefab (the damage zone).")]
    [SerializeField] private float _windRocBlastRadius = 2.2f;
    [SerializeField] private float _windRocCastHideDuration = 5f;

    private bool _earthActive;
    private bool _windActive;

    [Header("Skill 2 - Ap Luc Nuoc")]
    [SerializeField] private SkillGroundImpact _groundImpactPrefab;
    [SerializeField] private float _groundTargetMaxRange = 5f;
    [SerializeField] private float _groundTargetRadius = 2f;
    [SerializeField] private float _groundTargetInnerRadius = 0.93f;
    [SerializeField] private float _groundCastHideDuration = 0.6f;

    [Header("Skill 3 - Tia Nuoc Xoay")]
    [SerializeField] private SkillBeam _beamPrefab;
    [SerializeField] private float _beamRange = 6f;
    [SerializeField] private float _beamCastHideDuration = 2.6f;
    [Tooltip("Must match SkillBeam's own _thickness on the prefab so the aim-phase preview matches the real hitbox width.")]
    [SerializeField] private float _beamIndicatorWidth = 0.5f;

    [Header("Skill 4 - Song Than Tam Trung")]
    [SerializeField] private SkillTsunamiWave _tsunamiWavePrefab;
    [SerializeField] private float _tsunamiRange = 7f;
    [Tooltip("Keep chord width at range (2 * range * tan(angle/2)) close to SkillTsunamiWave._maxWidth, otherwise the indicator promises more width than the wave covers.")]
    [SerializeField] private float _tsunamiFanAngle = 50f;
    [SerializeField] private int _tsunamiWaveCount = 3;
    [Tooltip("Delay between wave 1 and wave 2.")]
    [SerializeField] private float _tsunamiWaveInterval = 0.9f;
    [Tooltip("Each following gap is this much longer than the previous one (wave 2 -> 3 waits interval + this).")]
    [SerializeField] private float _tsunamiWaveIntervalGrowth = 0.25f;
    [Tooltip("Each later wave deals this much more damage than the previous one.")]
    [SerializeField] private float _tsunamiDamageGrowthPerWave = 0.35f;
    [SerializeField] private float _tsunamiTravelTime = 1.2f;
    [Tooltip("Optional ultimate wrapper (D-079): adds gather/ripple/spray/dragon/wet/finale effects around the three waves. Leave empty to cast the plain three-wave version.")]
    [SerializeField] private SkillTsunamiUltimate _tsunamiUltimatePrefab;
    [SerializeField] private float _tsunamiUltimateCastHideDuration = 6f;

    private void Reset()
    {
        _player = FindFirstObjectByType<Player>();
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            // Water -> Earth -> Wind -> Water
            if (_windActive)
            {
                _windActive = false;
            }
            else if (_earthActive)
            {
                _earthActive = false;
                _windActive = true;
            }
            else
            {
                _earthActive = true;
            }

            Debug.Log($"SkillTestHarness: active kit = {(_windActive ? "Wind" : _earthActive ? "Earth" : "Water")}");
            return;
        }

        if (Keyboard.current.qKey.wasPressedThisFrame)
            CastTestProjectile();
        else if (_windActive)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
                CastWindWhirlwind();
            else if (Keyboard.current.rKey.wasPressedThisFrame)
                CastWindGustFan();
            else if (Keyboard.current.tKey.wasPressedThisFrame)
                CastWindRoc();
            return;
        }
        else if (_earthActive)
        {
            if (Keyboard.current.eKey.wasPressedThisFrame)
                CastEarthArena();
            else if (Keyboard.current.rKey.wasPressedThisFrame)
                CastEarthSpikeCone();
            else if (Keyboard.current.tKey.wasPressedThisFrame)
                CastEarthMeteorStorm();
            return;
        }
        else if (Keyboard.current.eKey.wasPressedThisFrame)
            CastGroundTargetSkill();
        else if (Keyboard.current.rKey.wasPressedThisFrame)
            CastBeamSkill();
        else if (Keyboard.current.tKey.wasPressedThisFrame)
            CastTsunamiSkill();
    }

    public void CastTestProjectile()
    {
        if (_windActive)
        {
            CastWindBlade();
            return;
        }

        if (_earthActive)
        {
            CastEarthProjectile();
            return;
        }

        if (_player == null || _projectilePrefab == null)
        {
            Debug.LogWarning("SkillTestHarness: missing Player or projectile prefab reference.");
            return;
        }

        // Only blocks on an aim already in progress -- intentionally allows starting a new cast while
        // a previous one's swing/flight is still resolving (spamming the skill key). ExtendSkillCastHide
        // below is what keeps Weapon/AttackFX correctly hidden across overlapping casts.
        if (_player.IsAimingSkill)
            return;

        _player.BeginAimSkill(_projectileRange, OnAimConfirmed);
    }

    private void OnAimConfirmed(Vector2 direction)
    {
        Vector3 spawnPosition = _player.transform.position + Vector3.up * _spawnHeightOffset;
        SkillProjectile instance = Instantiate(_projectilePrefab, spawnPosition, Quaternion.identity);
        instance.Launch(direction, _projectileSpeed, _projectileRange);

        // Weapon must stay hidden at least until the projectile could plausibly finish its flight
        // (reaches max range or hits something) — a fixed _castHideDuration shorter than the actual
        // travel time (range/speed) restores the weapon while the projectile is still visibly in
        // flight, which looks wrong. ExtendSkillCastHide only ever pushes the restore deadline forward,
        // so casting again before this cast finishes correctly keeps things hidden until the LATER of
        // the two casts finishes, instead of an earlier cast's timer firing mid-way through a newer one.
        float travelTime = _projectileSpeed > 0f ? _projectileRange / _projectileSpeed : 0f;
        _player.ExtendSkillCastHide(Mathf.Max(_castHideDuration, travelTime));
    }

    private void CastEarthProjectile()
    {
        if (_player == null || _earthProjectilePrefab == null)
        {
            Debug.LogWarning("SkillTestHarness: missing Player or Earth projectile prefab reference.");
            return;
        }

        if (_player.IsAimingSkill)
            return;

        _player.BeginAimSkill(_earthProjectileRange, OnEarthAimConfirmed);
    }

    private void CastEarthArena()
    {
        if (_player == null || _earthArenaPrefab == null)
        {
            Debug.LogWarning("SkillTestHarness: missing Player or Earth arena prefab reference.");
            return;
        }

        if (_player.IsGroundTargeting)
            return;

        // Indicator shows the wall band: outer edge and inner edge of the ring.
        float half = _earthArenaWallThickness * 0.5f;
        _player.BeginGroundTargetSkill(_earthArenaRange, _earthArenaRadius + half, _earthArenaRadius - half, OnEarthArenaConfirmed);
    }

    private void CastWindBlade()
    {
        if (_player == null || _windBladePrefab == null)
        {
            Debug.LogWarning("SkillTestHarness: missing Player or Wind blade prefab reference.");
            return;
        }

        if (_player.IsAimingSkill)
            return;

        _player.BeginAimSkill(_windBladeRange, OnWindBladeAimConfirmed);
    }

    private void CastWindRoc()
    {
        if (_player == null || _windRocPrefab == null)
        {
            Debug.LogWarning("SkillTestHarness: missing Player or Wind roc prefab reference.");
            return;
        }

        if (_player.IsAimingSkill)
            return;

        _player.BeginAimSkill(_windRocLength, OnWindRocAimConfirmed, _windRocPathWidth, 0f, _windRocBlastRadius);
    }

    private void OnWindRocAimConfirmed(Vector2 direction)
    {
        SkillWindRoc roc = Instantiate(_windRocPrefab, _player.SkillOrigin, Quaternion.identity);
        roc.Launch(_player, direction, _windRocLength);
        _player.ExtendSkillCastHide(_windRocCastHideDuration);
    }

    private void CastWindGustFan()
    {
        if (_player == null || _windGustFanPrefab == null)
        {
            Debug.LogWarning("SkillTestHarness: missing Player or Wind gust fan prefab reference.");
            return;
        }

        if (_player.IsAimingSkill)
            return;

        _player.BeginAimSkill(_windGustFanRange, OnWindGustFanAimConfirmed, 0.15f, _windGustFanAngle);
    }

    private void OnWindGustFanAimConfirmed(Vector2 direction)
    {
        SkillGustFan fan = Instantiate(_windGustFanPrefab, _player.SkillOrigin, Quaternion.identity);
        fan.Launch(_player.SkillOrigin, direction, _windGustFanRange, _windGustFanAngle);
        _player.ExtendSkillCastHide(_windGustFanCastHideDuration);
    }

    private void CastWindWhirlwind()
    {
        if (_player == null || _windWhirlwindPrefab == null)
        {
            Debug.LogWarning("SkillTestHarness: missing Player or Wind whirlwind prefab reference.");
            return;
        }

        if (_player.IsGroundTargeting)
            return;

        _player.BeginGroundTargetSkill(_windWhirlwindRange, _windWhirlwindRadius, _windWhirlwindCoreRadius, OnWindWhirlwindConfirmed);
    }

    private void OnWindWhirlwindConfirmed(Vector2 position)
    {
        Instantiate(_windWhirlwindPrefab, (Vector3)position, Quaternion.identity);
        _player.ExtendSkillCastHide(_windWhirlwindCastHideDuration);
    }

    private void OnWindBladeAimConfirmed(Vector2 direction)
    {
        SkillBoomerang blade = Instantiate(_windBladePrefab, _player.SkillOrigin, Quaternion.identity);
        blade.Launch(_player, direction, _windBladeRange);
        _player.ExtendSkillCastHide(_windBladeCastHideDuration);
    }

    private void CastEarthMeteorStorm()
    {
        if (_player == null || _earthMeteorStormPrefab == null)
        {
            Debug.LogWarning("SkillTestHarness: missing Player or Earth meteor storm prefab reference.");
            return;
        }

        if (_player.IsGroundTargeting)
            return;

        _player.BeginGroundTargetSkill(_earthMeteorStormRange, _earthMeteorStormRadius, _earthMeteorStormFinaleRadius, OnEarthMeteorStormConfirmed);
    }

    private void OnEarthMeteorStormConfirmed(Vector2 position)
    {
        SkillMeteorStorm storm = Instantiate(_earthMeteorStormPrefab, (Vector3)position, Quaternion.identity);
        storm.Launch(position, _player);
        _player.ExtendSkillCastHide(_earthMeteorStormCastHideDuration);
    }

    private void CastEarthSpikeCone()
    {
        if (_player == null || _earthSpikeConePrefab == null)
        {
            Debug.LogWarning("SkillTestHarness: missing Player or Earth spike cone prefab reference.");
            return;
        }

        if (_player.IsAimingSkill)
            return;

        _player.BeginAimSkill(_earthSpikeConeRange, OnEarthSpikeConeAimConfirmed, 0.15f, _earthSpikeConeAngle);
    }

    private void OnEarthSpikeConeAimConfirmed(Vector2 direction)
    {
        SkillSpikeCone cone = Instantiate(_earthSpikeConePrefab, _player.SkillOrigin, Quaternion.identity);
        cone.Launch(_player.SkillOrigin, direction, _earthSpikeConeRange, _earthSpikeConeAngle);
        _player.ExtendSkillCastHide(_earthSpikeConeCastHideDuration);
    }

    private void OnEarthArenaConfirmed(Vector2 position)
    {
        Instantiate(_earthArenaPrefab, (Vector3)position, Quaternion.identity);
        _player.ExtendSkillCastHide(_earthArenaCastHideDuration);
    }

    private void OnEarthAimConfirmed(Vector2 direction)
    {
        Vector3 spawnPosition = _player.SkillOrigin;
        SkillProjectile instance = Instantiate(_earthProjectilePrefab, spawnPosition, Quaternion.identity);
        instance.Launch(direction, _earthProjectileSpeed, _earthProjectileRange);

        float travelTime = _earthProjectileSpeed > 0f ? _earthProjectileRange / _earthProjectileSpeed : 0f;
        _player.ExtendSkillCastHide(Mathf.Max(_castHideDuration, travelTime));
    }

    public void CastGroundTargetSkill()
    {
        if (_player == null || _groundImpactPrefab == null)
        {
            Debug.LogWarning("SkillTestHarness: missing Player or ground-impact prefab reference.");
            return;
        }

        if (_player.IsGroundTargeting)
            return;

        _player.BeginGroundTargetSkill(_groundTargetMaxRange, _groundTargetRadius, _groundTargetInnerRadius, OnGroundTargetConfirmed);
    }

    private void OnGroundTargetConfirmed(Vector2 position)
    {
        Instantiate(_groundImpactPrefab, (Vector3)position, Quaternion.identity);
        _player.ExtendSkillCastHide(_groundCastHideDuration);
    }

    public void CastTsunamiSkill()
    {
        if (_player == null || _tsunamiWavePrefab == null)
        {
            Debug.LogWarning("SkillTestHarness: missing Player or tsunami wave prefab reference.");
            return;
        }

        if (_player.IsAimingSkill)
            return;

        _player.BeginAimSkill(_tsunamiRange, OnTsunamiAimConfirmed, 0.15f, _tsunamiFanAngle);
    }

    private void OnTsunamiAimConfirmed(Vector2 direction)
    {
        if (_tsunamiUltimatePrefab != null)
        {
            SkillTsunamiUltimate ultimate = Instantiate(_tsunamiUltimatePrefab, _player.SkillOrigin, Quaternion.identity);
            ultimate.Launch(_player, direction, _tsunamiRange, _tsunamiFanAngle);
            _player.ExtendSkillCastHide(_tsunamiUltimateCastHideDuration);
            return;
        }

        StartCoroutine(SpawnTsunamiWaves(direction));

        float totalTime = _tsunamiTravelTime;
        for (int i = 0; i < _tsunamiWaveCount - 1; i++)
            totalTime += WaveGap(i);

        _player.ExtendSkillCastHide(totalTime);
    }

    private System.Collections.IEnumerator SpawnTsunamiWaves(Vector2 direction)
    {
        for (int i = 0; i < _tsunamiWaveCount; i++)
        {
            if (_player == null)
                yield break;

            float damageMultiplier = 1f + _tsunamiDamageGrowthPerWave * i;

            SkillTsunamiWave wave = Instantiate(_tsunamiWavePrefab, _player.SkillOrigin, Quaternion.identity);
            wave.Launch(_player.SkillOrigin, direction, _tsunamiRange, _tsunamiFanAngle, damageMultiplier);

            if (i < _tsunamiWaveCount - 1)
                yield return new WaitForSeconds(WaveGap(i));
        }
    }

    /// <summary>Wait after wave `index` before the next one; grows with each wave.</summary>
    private float WaveGap(int index) => _tsunamiWaveInterval + _tsunamiWaveIntervalGrowth * index;

    public void CastBeamSkill()
    {
        if (_player == null || _beamPrefab == null)
        {
            Debug.LogWarning("SkillTestHarness: missing Player or beam prefab reference.");
            return;
        }

        if (_player.IsAimingSkill)
            return;

        _player.BeginAimSkill(_beamRange, OnBeamAimConfirmed, _beamIndicatorWidth);
    }

    private void OnBeamAimConfirmed(Vector2 direction)
    {
        // Spawned at the player's skill origin (body center, shared with the aim indicator so preview
        // and beam line up) and set to follow it every frame so the beam doesn't stay planted at the
        // cast-time spot.
        SkillBeam instance = Instantiate(_beamPrefab, _player.SkillOrigin, Quaternion.identity);
        instance.SetFollowTarget(_player.transform, _player.SkillOriginOffset);
        instance.Launch(direction, _beamRange);

        // The beam self-sustains for its own _duration (D-073) -- Weapon/AttackFX must stay hidden for
        // at least that long, not just the swing animation's length.
        _player.ExtendSkillCastHide(_beamCastHideDuration);
    }
}
