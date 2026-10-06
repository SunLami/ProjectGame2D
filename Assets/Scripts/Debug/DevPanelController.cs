using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>QA/demo-only tool panel gated by DevModeSettings.IsDevModeEnabled (see that asset's
/// summary for the build/ship contract). Calls straight into existing manager APIs
/// (PlayerStat/InventoryManager/Player/SpawnRegistry) -- it owns no gameplay/save logic of its own.
/// The fixed layout (buttons, fields, panel size/position) is hand-authored on the
/// Assets/Prefabs/UI/Debug/DevPanel.prefab hierarchy -- built once by DevPanelPrefabBuilder
/// (Tools/ProjectGame2D/UI/Build Dev Panel) and freely re-editable afterward in Prefab Mode, same
/// contract as D-046's Shop/Crafting authoring prefabs. Only the Teleport list is still generated
/// at runtime (RefreshTeleportButtons below), because its content -- SpawnRegistry entries and
/// scene NPCs -- varies per scene/session and can't be author-fixed into the prefab.</summary>
public sealed class DevPanelController : MonoBehaviour
{
    private const float RowHeight = 34f;

    [SerializeField] private DevModeSettings _devModeSettings;

    [Header("Shell")]
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private TMP_Text _feedbackText;

    [Header("Progression")]
    [SerializeField] private TMP_InputField _setLevelField;

    [Header("Spawn Item")]
    [SerializeField] private TMP_InputField _itemIdField;
    [SerializeField] private TMP_InputField _amountField;

    [Header("Combat")]
    [SerializeField] private Toggle _godModeToggle;

    [Header("Inventory")]
    [SerializeField] private Toggle _seedStartingItemsToggle;

    [Header("Teleport")]
    [SerializeField] private RectTransform _teleportContainer;

    private Dictionary<string, ItemSO> _itemLookup;
    private SpawnRegistry _spawnRegistry;
    private InventorySeeder _inventorySeeder;

    private void Awake()
    {
        if (_devModeSettings == null || !_devModeSettings.IsDevModeEnabled)
        {
            gameObject.SetActive(false);
            return;
        }

        _godModeToggle.onValueChanged.AddListener(OnGodModeChanged);
        _seedStartingItemsToggle.onValueChanged.AddListener(OnSeedStartingItemsChanged);
        _panelRoot.SetActive(false);
    }

    // -------------------------------------------------------------------------------------- Shell

    // Public: wired as a persistent Button.onClick listener by DevPanelPrefabBuilder (both the
    // toggle button under the Minimap and the panel's own Close button call this).
    public void TogglePanel()
    {
        bool willOpen = !_panelRoot.activeSelf;
        _panelRoot.SetActive(willOpen);
        if (willOpen)
        {
            RefreshTeleportButtons();
            RefreshSeedStartingItemsToggle();
        }
    }

    public void ClosePanel() => _panelRoot.SetActive(false);

    private void SetFeedback(string message)
    {
        if (_feedbackText != null)
            _feedbackText.text = message;
    }

    // -------------------------------------------------------------------------------- Progression

    public void OnLevelUpClicked()
    {
        PlayerStat stat = PlayerStat.Instance;
        if (stat == null) { SetFeedback("PlayerStat not found in scene."); return; }

        int remaining = Mathf.Max(1, stat.ExperienceToNextLevel - stat.CurrentExperience);
        stat.AddExperience(remaining);
        SetFeedback($"Leveled up to {stat.Level}.");
    }

    public void OnSetLevelClicked()
    {
        PlayerStat stat = PlayerStat.Instance;
        if (stat == null) { SetFeedback("PlayerStat not found in scene."); return; }
        if (!int.TryParse(_setLevelField.text, out int level) || level < 1)
        {
            SetFeedback("Invalid level.");
            return;
        }

        stat.RestoreProgression(level, 0, -1f);
        SetFeedback($"Level set to {level}.");
    }

    public void OnFullHealClicked()
    {
        PlayerStat stat = PlayerStat.Instance;
        if (stat == null) { SetFeedback("PlayerStat not found in scene."); return; }
        stat.RestoreHealth(-1f);
        SetFeedback("Health fully restored.");
    }

    public void OnFullStaminaClicked()
    {
        PlayerStat stat = PlayerStat.Instance;
        if (stat == null) { SetFeedback("PlayerStat not found in scene."); return; }
        stat.RestoreStamina(stat.MaxStamina);
        SetFeedback("Stamina fully restored.");
    }

    // -------------------------------------------------------------------------------- Spawn item

    public void OnSpawnItemClicked()
    {
        if (InventoryManager.Instance == null) { SetFeedback("InventoryManager not found in scene."); return; }

        string itemId = _itemIdField.text?.Trim();
        if (string.IsNullOrEmpty(itemId)) { SetFeedback("Enter an itemId first."); return; }

        if (!int.TryParse(_amountField.text, out int amount) || amount < 1)
            amount = 1;

        _itemLookup ??= ItemLookup.BuildFromResources();
        if (!_itemLookup.TryGetValue(itemId, out ItemSO item))
        {
            SetFeedback($"itemId \"{itemId}\" not found.");
            return;
        }

        if (item is FishDefinitionSO fishDefinition)
        {
            int weight = fishDefinition.RollWeightGrams();
            bool added = InventoryManager.Instance.TryAddFish(fishDefinition, weight);
            SetFeedback(added
                ? $"Added fish {itemId} ({weight}g)."
                : "Inventory full, fish not added.");
            return;
        }

        bool success = InventoryManager.Instance.AddItem(item, amount);
        SetFeedback(success
            ? $"Added {amount}x {itemId}."
            : "Inventory full or item could not be added.");
    }

    // ---------------------------------------------------------------------------------- Currency

    public void OnGold100Clicked() => GiveGold(100);
    public void OnGold1000Clicked() => GiveGold(1000);
    public void OnGold9999Clicked() => GiveGold(9999);

    private void GiveGold(int amount)
    {
        if (InventoryManager.Instance == null) { SetFeedback("InventoryManager not found in scene."); return; }
        InventoryManager.Instance.AddGold(amount);
        SetFeedback($"Added {amount} Gold.");
    }

    // ------------------------------------------------------------------------------------ Combat

    private void OnGodModeChanged(bool enabled)
    {
        PlayerStat stat = PlayerStat.Instance;
        if (stat == null) { SetFeedback("PlayerStat not found in scene."); return; }
        stat.IsInvulnerable = enabled;
        SetFeedback(enabled ? "God Mode: ON." : "God Mode: OFF.");
    }

    // --------------------------------------------------------------------------------- Inventory

    // InventorySeeder lives in the persistent Bootstrap scene, so it's found once and reused across
    // gameplay scene reloads just like SpawnRegistry above. The toggle only affects the *next* New
    // Game session -- seeding for the current session (if any) has already run in
    // PlayerSpawnReadinessSource.Start() before this panel could be opened.
    private void RefreshSeedStartingItemsToggle()
    {
        _inventorySeeder ??= FindAnyObjectByType<InventorySeeder>();
        if (_inventorySeeder == null) return;

        _seedStartingItemsToggle.SetIsOnWithoutNotify(_inventorySeeder.SeedingEnabled);
    }

    private void OnSeedStartingItemsChanged(bool enabled)
    {
        _inventorySeeder ??= FindAnyObjectByType<InventorySeeder>();
        if (_inventorySeeder == null) { SetFeedback("InventorySeeder not found in scene."); return; }

        _inventorySeeder.SeedingEnabled = enabled;
        SetFeedback(enabled
            ? "Seed starting items: ON (applies to the next New Game)."
            : "Seed starting items: OFF (applies to the next New Game).");
    }

    // ---------------------------------------------------------------------------------- Teleport

    // Landing exactly on an NPC's position would overlap its (usually trigger) collider; nudge the
    // player down one short step so they land facing/next to the NPC instead of inside it.
    private static readonly Vector3 NpcApproachOffset = new(0f, -1.2f, 0f);

    // Display-only flavor text for the Teleport NPC list -- keyed by the stable npcId (not the
    // GameObject name) so it survives a scene-object rename. Not used anywhere else in the game;
    // purely a memory aid for whoever is demoing with this panel.
    private static readonly Dictionary<string, string> NpcRoles = new()
    {
        ["npc.town.elder"] = "Village Elder",
        ["npc.town.guildmaster"] = "Wayfarer Guildmaster",
        ["npc.dunstan"] = "Fisher",
        ["npc.town.artificer"] = "Artificer",
        ["npc.town.gastronome"] = "Gastronome",
        ["npc.town.mixologist"] = "Mixology",
        ["npc.town.hermetist"] = "Hermetist",
        ["npc.leofrun"] = "Yrthling",
        ["npc.town.chapman"] = "Chapman",
    };

    private void RefreshTeleportButtons()
    {
        for (int i = _teleportContainer.childCount - 1; i >= 0; i--)
            Destroy(_teleportContainer.GetChild(i).gameObject);

        _spawnRegistry = FindAnyObjectByType<SpawnRegistry>();
        string[] spawnIds = _spawnRegistry != null ? _spawnRegistry.GetSpawnIds() : System.Array.Empty<string>();

        if (spawnIds.Length == 0)
        {
            CreateLabel(_teleportContainer, "(No SpawnRegistry / spawn points in this scene.)", 12, FontStyles.Italic);
        }
        else
        {
            foreach (string spawnId in spawnIds)
            {
                string capturedId = spawnId;
                Button button = CreateButton(_teleportContainer, $"Teleport_{spawnId}", $"Go: {spawnId}", new Color(0.08f, 0.25f, 0.45f, 1f));
                button.GetComponent<LayoutElement>().preferredHeight = RowHeight;
                button.onClick.AddListener(() => OnTeleportToSpawnClicked(capturedId));
            }
        }

        List<(string Name, string NpcId, Vector3 Position)> npcs = CollectNpcs();
        if (npcs.Count > 0)
        {
            TMP_Text npcHeader = CreateLabel(_teleportContainer, "-- NPCs --", 11, FontStyles.Bold);
            npcHeader.color = new Color(1f, 0.82f, 0.38f, 1f);

            foreach ((string name, string npcId, Vector3 position) in npcs)
            {
                string capturedName = name;
                Vector3 capturedPosition = position + NpcApproachOffset;
                string label = NpcRoles.TryGetValue(npcId, out string role) ? $"NPC: {name} ({role})" : $"NPC: {name}";
                Button button = CreateButton(_teleportContainer, $"TeleportNpc_{name}", label, new Color(0.1f, 0.4f, 0.3f, 1f));
                button.GetComponent<LayoutElement>().preferredHeight = RowHeight;
                button.onClick.AddListener(() => TeleportPlayerTo(capturedPosition, capturedName));
            }
        }
    }

    // Every scene NPC the player can talk to is either quest-facing (QuestNpcInteractionUI) or
    // commerce-facing (TraderNpcInteractionUI) -- combining both covers the full cast without
    // hardcoding names here, so new NPCs show up automatically as they're authored (just without
    // a role suffix until NpcRoles above gets an entry for their npcId).
    private static List<(string Name, string NpcId, Vector3 Position)> CollectNpcs()
    {
        var result = new List<(string, string, Vector3)>();

        foreach (QuestNpcInteractionUI npc in FindObjectsByType<QuestNpcInteractionUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (!string.IsNullOrEmpty(npc.NpcId))
                result.Add((npc.gameObject.name, npc.NpcId, npc.transform.position));

        foreach (TraderNpcInteractionUI npc in FindObjectsByType<TraderNpcInteractionUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (!string.IsNullOrEmpty(npc.NpcId))
                result.Add((npc.gameObject.name, npc.NpcId, npc.transform.position));

        result.Sort((a, b) => string.CompareOrdinal(a.Item1, b.Item1));
        return result;
    }

    private void OnTeleportToSpawnClicked(string spawnId)
    {
        if (_spawnRegistry == null || !_spawnRegistry.TryGetSpawn(spawnId, out Vector3 position))
        {
            SetFeedback($"Spawn point \"{spawnId}\" not found.");
            return;
        }

        TeleportPlayerTo(position, spawnId);
    }

    private void TeleportPlayerTo(Vector3 position, string label)
    {
        Player player = FindAnyObjectByType<Player>();
        if (player == null) { SetFeedback("Player not found in scene."); return; }

        player.WarpTo(position);
        SetFeedback($"Teleported to {label}.");
    }

    // Minimal factories kept only for the Teleport list above, whose row count/labels are
    // inherently dynamic (SpawnRegistry entries + scene NPCs vary per scene) and so can't be
    // hand-authored into the prefab like the rest of the panel.
    private static TMP_Text CreateLabel(Transform parent, string text, int fontSize, FontStyles style = FontStyles.Normal)
    {
        GameObject go = new("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.color = Color.white;
        label.raycastTarget = false;
        return label;
    }

    private static Button CreateButton(Transform parent, string name, string label, Color color)
    {
        GameObject go = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        Image image = go.GetComponent<Image>();
        image.color = color;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;

        TMP_Text text = CreateLabel(go.transform, label, 12, FontStyles.Normal);
        text.alignment = TextAlignmentOptions.Center;
        RectTransform textRect = (RectTransform)text.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        go.AddComponent<LayoutElement>().preferredWidth = 110f;
        return button;
    }
}
