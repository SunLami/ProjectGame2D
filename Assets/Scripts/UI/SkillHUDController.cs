using UnityEngine;

// Bottom-right HUD mirroring PlayerHUD's bottom-left: 4 Q/E/R/T loadout slots (icon resolved via
// SkillCatalog from PlayerStat.Skills.Equipped, radial cooldown read from SkillTestHarness — the
// same bridge that gates casting, so the UI can never show a different cooldown than the one
// actually enforced) plus a 5th fixed Shield slot highlighted while Player.IsBlocking.
public sealed class SkillHUDController : MonoBehaviour
{
    [SerializeField] private SkillCatalog _catalog;
    // Scene objects, not assets -- SkillHUD itself lives inside GameplayUIRoot.prefab (shared with
    // PlayerHUD), so these can't be baked as prefab-serialized references (would resolve to null the
    // same way InventorySeeder's cross-scene wiring did -- see this session's inventory-seed fix).
    // Resolved lazily at runtime instead, same fallback pattern as TryBindPlayerStat below.
    [SerializeField] private SkillTestHarness _castBridge;
    [SerializeField] private Player _player;
    [SerializeField] private SkillHUDSlotUI[] _loadoutSlots = new SkillHUDSlotUI[4];
    [SerializeField] private SkillHUDSlotUI _shieldSlot;

    private PlayerStat _playerStat;
    private readonly string[] _lastEquippedId = new string[4];

    private void OnEnable()
    {
        TryBindPlayerStat();
        if (_shieldSlot != null)
            _shieldSlot.SetCooldownFraction01(0f);
    }

    private void Start()
    {
        TryBindPlayerStat();
        RefreshAllIcons();
    }

    private void OnDisable()
    {
        UnbindPlayerStat();
    }

    private void Update()
    {
        if (_playerStat == null)
        {
            TryBindPlayerStat();
            if (_playerStat == null)
                return;
        }

        if (_castBridge == null)
            _castBridge = FindAnyObjectByType<SkillTestHarness>();
        if (_player == null)
            _player = FindAnyObjectByType<Player>();

        if (_castBridge != null)
        {
            for (int i = 0; i < _loadoutSlots.Length; i++)
            {
                if (_loadoutSlots[i] == null)
                    continue;

                string equippedId = _playerStat.Skills.Equipped(i);
                if (equippedId != _lastEquippedId[i])
                    RefreshIcon(i);

                _loadoutSlots[i].SetCooldownFraction01(_castBridge.GetCooldownFraction01(i));
            }
        }

        if (_shieldSlot != null && _player != null)
            _shieldSlot.SetActiveHighlight(_player.IsBlocking);
    }

    private void TryBindPlayerStat()
    {
        PlayerStat stat = PlayerStat.Instance != null ? PlayerStat.Instance : FindAnyObjectByType<PlayerStat>();
        if (stat == _playerStat)
            return;

        UnbindPlayerStat();
        _playerStat = stat;
        if (_playerStat == null)
            return;

        _playerStat.Skills.Changed += RefreshAllIcons;
        RefreshAllIcons();
    }

    private void UnbindPlayerStat()
    {
        if (_playerStat == null)
            return;

        _playerStat.Skills.Changed -= RefreshAllIcons;
        _playerStat = null;
    }

    private void RefreshAllIcons()
    {
        for (int i = 0; i < _loadoutSlots.Length; i++)
            RefreshIcon(i);
    }

    private void RefreshIcon(int slot)
    {
        if (_playerStat == null || _loadoutSlots[slot] == null)
            return;

        string id = _playerStat.Skills.Equipped(slot);
        _lastEquippedId[slot] = id;

        Sprite icon = null;
        if (_playerStat.Skills.IsLearned(id) && _catalog != null && _catalog.TryResolve(id, out var definition))
            icon = definition.icon;

        _loadoutSlots[slot].SetIcon(icon);
    }
}
