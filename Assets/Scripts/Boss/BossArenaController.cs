using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Runs the summon flow of a boss arena (D-083/D-087): the summoning statue stands in the middle; when
/// the summon starts the statue crumbles and vanishes, the floor seal flares, and the boss slowly
/// appears at the spawn point. After the boss dies the statue returns so the arena can be fought
/// again (replayable, for farming). The Shrine UI / gem check will call <see cref="TrySummon"/>; the
/// F6/F7 hotkeys are a debug stand-in.
/// </summary>
public sealed class BossArenaController : MonoBehaviour
{
    public enum ArenaState { StatueIdle, Summoning, BossFight, Resetting }

    [Header("Boss")]
    [SerializeField] private GameObject _bossPrefab;
    [SerializeField] private BossDefinition _definition;
    [SerializeField] private Transform _bossSpawnPoint;

    [Header("Summoning statue")]
    [SerializeField] private SpriteRenderer _statueRenderer;
    [SerializeField] private Collider2D _statueCollider;
    [SerializeField] private Sprite[] _statueIdleFrames;
    [SerializeField] private Sprite[] _statueVanishFrames;
    [SerializeField] private float _idleFrameRate = 8f;
    [SerializeField] private float _vanishFrameRate = 8f;

    [Header("Floor seal glow")]
    [SerializeField] private SpriteRenderer _sealGlow;
    [SerializeField] private Sprite[] _sealGlowFrames;
    [SerializeField, Range(0f, 1f)] private float _sealIdleAlpha = 0.45f;
    [SerializeField, Range(0f, 1f)] private float _sealFightAlpha = 0.2f;

    [Header("Effects")]
    [SerializeField] private Sprite[] _dustFrames;

    [Header("Summon ritual (D-091)")]
    [Tooltip("Eye points of the guardian statues; each fires a laser at the summoning statue. Empty = no ritual.")]
    [SerializeField] private Transform[] _ritualEmitters;
    [Tooltip("Where the beams hit. Empty = the middle of the summoning statue.")]
    [SerializeField] private Transform _ritualTarget;
    [SerializeField, Min(0f)] private float _ritualChargeSeconds = 1.2f;
    [SerializeField, Min(0.1f)] private float _ritualBeamSeconds = 1.8f;
    [Tooltip("Camera zoom while the guardians fire, so the corner statues are in view.")]
    [SerializeField, Min(1f)] private float _ritualCameraSize = 18.5f;
    [Tooltip("Beam colour of the ritual (green for Earth, aqua for Water).")]
    [SerializeField] private Color _ritualBeamColor = new Color(0.3f, 1f, 0.45f, 0.8f);

    [Header("Summon offering (Shrine UI)")]
    [Tooltip("Item the shrine consumes to summon the boss (e.g. Earth Orb). Empty = free summon.")]
    [SerializeField] private ItemSO _summonItem;
    [SerializeField, Min(1)] private int _summonItemCount = 1;

    [Header("Arena")]
    [Tooltip("Walkable area in world units. Boss skills keep their targets and lines inside it.")]
    [SerializeField] private Rect _walkableBounds = new Rect(-31.8f, -16f, 63.6f, 31.7f);
    [Tooltip("D-110: optional outline for non-rectangular arenas (Wind's cross-shaped platform); empty = the rectangle only.")]
    [SerializeField] private Vector2[] _walkablePolygon;

    [Header("Flow")]
    [SerializeField, Min(0f)] private float _resetDelaySeconds = 4f;
    [SerializeField] private bool _debugHotkeys = true;

    private SkillFrameAnimator _statueAnimator;
    private BossController _boss;
    private float _sealAlpha;

    public ArenaState State { get; private set; } = ArenaState.StatueIdle;
    public ItemSO SummonItem => _summonItem;
    public int SummonItemCount => Mathf.Max(1, _summonItemCount);
    public string BossDisplayName => _definition != null ? _definition.displayName : "the guardian";
    public BossController Boss => _boss;

    private void Start()
    {
        if (_statueRenderer != null)
            _statueAnimator = _statueRenderer.gameObject.AddComponent<SkillFrameAnimator>();

        if (_sealGlow != null && _sealGlowFrames != null && _sealGlowFrames.Length > 0)
            _sealGlow.gameObject.AddComponent<SkillFrameAnimator>().Play(_sealGlowFrames, 10f, true);

        ShowStatue(true);
        SetSealAlpha(_sealIdleAlpha);
        RegisterObstacles();
    }

    /// <summary>D-106: the boss walks around the teleport pillar and the ritual totems instead of through them.</summary>
    private void RegisterObstacles()
    {
        foreach (TeleportPillarInteractable pillar in FindObjectsByType<TeleportPillarInteractable>(FindObjectsInactive.Exclude))
        {
            Collider2D collider = pillar.GetComponent<Collider2D>();
            Vector2 center = collider != null ? (Vector2)collider.bounds.center : (Vector2)pillar.transform.position;
            float radius = collider != null ? Mathf.Max(collider.bounds.extents.x, collider.bounds.extents.y) : 0.8f;
            BossObstacle.Add(pillar.gameObject, center, Mathf.Max(0.8f, radius));
        }

        if (_ritualEmitters == null)
            return;

        foreach (Transform emitter in _ritualEmitters)
        {
            if (emitter == null)
                continue;
            Collider2D totemBody = emitter.parent != null ? emitter.parent.GetComponent<Collider2D>() : null;
            if (totemBody != null)
                BossObstacle.Add(emitter.gameObject, totemBody.bounds.center, Mathf.Max(totemBody.bounds.extents.x, totemBody.bounds.extents.y));
            else
                BossObstacle.Add(emitter.gameObject, emitter.position, 1.1f);
        }
    }

    private void Update()
    {
        if (!_debugHotkeys || Keyboard.current == null)
            return;

        if (Keyboard.current.f6Key.wasPressedThisFrame)
            TrySummon();
        else if (Keyboard.current.f7Key.wasPressedThisFrame)
            ForceReset();
    }

    /// <summary>Starts the summon if the statue is waiting. Returns false if a summon/fight is running.</summary>
    public bool TrySummon()
    {
        if (State != ArenaState.StatueIdle || _bossPrefab == null || _definition == null)
            return false;

        StartCoroutine(SummonRoutine());
        return true;
    }

    /// <summary>Shrine entry point: consumes the required offering from the inventory and starts the summon.
    /// Nothing is consumed unless the summon actually starts; a failed start refunds the offering.</summary>
    public bool TrySummonWithOffering()
    {
        if (State != ArenaState.StatueIdle)
            return false;

        InventoryManager inventory = InventoryManager.Instance;
        if (_summonItem != null)
        {
            if (inventory == null || !inventory.HasItemId(_summonItem.itemId, SummonItemCount))
                return false;

            // RemoveItem matches by instance, so use the exact asset the inventory slots hold (same itemId).
            ItemSO owned = FindOwnedItem(inventory, _summonItem.itemId) ?? _summonItem;
            if (!inventory.RemoveItem(owned, SummonItemCount))
                return false;
        }

        if (TrySummon())
            return true;

        if (_summonItem != null && inventory != null)
            inventory.AddItem(_summonItem, SummonItemCount);
        return false;
    }

    private static ItemSO FindOwnedItem(InventoryManager inventory, string itemId)
    {
        foreach (InventorySlot slot in inventory.Slots)
        {
            if (!slot.IsEmpty && slot.item.itemId == itemId)
                return slot.item;
        }

        return null;
    }

    /// <summary>Removes the boss (if any) and puts the statue back. Debug/tool use.</summary>
    public void ForceReset()
    {
        StopAllCoroutines();
        BossCameraDirector director = FindAnyObjectByType<BossCameraDirector>();
        if (director != null)
            director.EndRitual();
        GameObject ritual = GameObject.Find("SummonRitualFX");
        if (ritual != null)
            Destroy(ritual);
        if (_boss != null)
            Destroy(_boss.gameObject);
        _boss = null;
        ShowStatue(true);
        SetSealAlpha(_sealIdleAlpha);
        State = ArenaState.StatueIdle;
    }

    private IEnumerator SummonRoutine()
    {
        State = ArenaState.Summoning;
        if (_statueCollider != null)
            _statueCollider.enabled = false;
        SetStatueHighlightEnabled(false);

        StartCoroutine(FadeSeal(1f, 1.4f));
        SoundFXManager.PlaySfx(SfxIds.BossArenaLock);

        float vanishLength = 1f;
        if (_statueVanishFrames != null && _statueVanishFrames.Length > 0)
            vanishLength = _statueVanishFrames.Length / _vanishFrameRate;

        float elapsedAfterImpact = 0f;
        if (_ritualEmitters != null && _ritualEmitters.Length > 0 && _statueRenderer != null)
        {
            Vector3 target = _ritualTarget != null
                ? _ritualTarget.position
                : _statueRenderer.transform.position + Vector3.up * 2.4f;
            BossCameraDirector director = FindAnyObjectByType<BossCameraDirector>();
            if (director != null)
                director.BeginRitual(transform.position, _ritualCameraSize);
            yield return SummonRitualFX.Play(_ritualEmitters, target, _ritualChargeSeconds, _ritualBeamSeconds, PlayVanish, _ritualBeamColor);
            if (director != null)
                director.EndRitual();
            elapsedAfterImpact = 0.55f; // the explosion burst inside the ritual
        }
        else
        {
            SkillScreenFX.Shake(0.14f, 2.2f);
            PlayVanish();
        }

        yield return new WaitForSeconds(Mathf.Max(0f, vanishLength * 0.55f - elapsedAfterImpact));
        SpawnDust();
        SkillScreenFX.Flash(new Color(0.55f, 1f, 0.65f), 0.3f, 0.7f);
        yield return new WaitForSeconds(vanishLength * 0.45f);

        // The plinth that is left over fades away.
        yield return FadeStatue(0f, 0.6f);
        if (_statueRenderer != null)
            _statueRenderer.enabled = false; // the hover-outline material ignores the sprite's alpha, so hide the renderer for real
        SpawnBoss();
        StartCoroutine(FadeSeal(_sealFightAlpha, 3f));
    }

    private void PlayVanish()
    {
        if (_statueAnimator != null && _statueVanishFrames != null && _statueVanishFrames.Length > 0)
            _statueAnimator.Play(_statueVanishFrames, _vanishFrameRate, false);
    }

    private void SpawnBoss()
    {
        Vector3 position = _bossSpawnPoint != null ? _bossSpawnPoint.position : transform.position;
        GameObject instance = Instantiate(_bossPrefab, position, Quaternion.identity);
        _boss = instance.GetComponent<BossController>();
        _boss.Initialize(Instantiate(_definition));
        _boss.SetArenaBounds(_walkableBounds);
        _boss.SetArenaPolygon(_walkablePolygon);
        BossHealthBarUI.Create(_boss);
        BossOffscreenIndicator.Create(_boss);
        _boss.Died += OnBossDied;
        _boss.BeginEncounter();
        State = ArenaState.BossFight;
    }

    private void OnBossDied()
    {
        _boss = null;
        StartCoroutine(ResetRoutine());
    }

    private IEnumerator ResetRoutine()
    {
        State = ArenaState.Resetting;
        yield return new WaitForSeconds(_resetDelaySeconds);
        ShowStatue(false);
        yield return FadeStatue(1f, 1.2f);
        if (_statueCollider != null)
            _statueCollider.enabled = true;
        ShowStatue(true);
        StartCoroutine(FadeSeal(_sealIdleAlpha, 2f));
        State = ArenaState.StatueIdle;
    }

    private void ShowStatue(bool fullyVisible)
    {
        if (_statueRenderer == null)
            return;

        _statueRenderer.enabled = true;
        SetStatueHighlightEnabled(fullyVisible);
        Color color = _statueRenderer.color;
        color.a = fullyVisible ? 1f : 0f;
        _statueRenderer.color = color;
        if (_statueCollider != null)
            _statueCollider.enabled = fullyVisible;
        if (fullyVisible && _statueAnimator != null && _statueIdleFrames != null && _statueIdleFrames.Length > 0)
            _statueAnimator.Play(_statueIdleFrames, _idleFrameRate, true);
    }

    /// <summary>The shrine statue carries a HoverOutline (material swap). While the summon runs it must be off, or the outline material keeps
    /// drawing the statue at full alpha even after the fade (the statue "does not disappear").</summary>
    private void SetStatueHighlightEnabled(bool enabled)
    {
        if (_statueRenderer == null)
            return;

        var outline = _statueRenderer.GetComponent<HoverOutline>();
        if (outline == null)
            return;

        if (!enabled)
            outline.SetHighlighted(false);
        outline.enabled = enabled;
    }

    private IEnumerator FadeStatue(float targetAlpha, float seconds)
    {
        if (_statueRenderer == null)
            yield break;

        float start = _statueRenderer.color.a;
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            Color color = _statueRenderer.color;
            color.a = Mathf.Lerp(start, targetAlpha, t / seconds);
            _statueRenderer.color = color;
            yield return null;
        }

        Color end = _statueRenderer.color;
        end.a = targetAlpha;
        _statueRenderer.color = end;
    }

    private void SetSealAlpha(float alpha)
    {
        _sealAlpha = alpha;
        if (_sealGlow == null)
            return;

        Color color = _sealGlow.color;
        color.a = alpha;
        _sealGlow.color = color;
    }

    private IEnumerator FadeSeal(float target, float seconds)
    {
        float start = _sealAlpha;
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            SetSealAlpha(Mathf.Lerp(start, target, t / seconds));
            yield return null;
        }

        SetSealAlpha(target);
    }

    private void SpawnDust()
    {
        if (_dustFrames == null || _dustFrames.Length == 0 || _statueRenderer == null)
            return;

        var go = new GameObject("SummonDust");
        go.transform.position = _statueRenderer.transform.position + Vector3.up * 2.5f;
        go.transform.localScale = Vector3.one * 1.5f;
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 30;
        go.AddComponent<SkillFrameAnimator>().Play(_dustFrames, 8f, false);
        Destroy(go, _dustFrames.Length / 8f + 0.5f);
    }
}
