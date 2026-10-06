using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Builds the Wind owl boss prefab + BossDefinition asset (D-111): imports the owl clip sheets
/// (Assets/Art/BossArena/Wind/Boss/Anim, 17 frames of 384 px each, rendered by Tools/owl_render.py from the keyframes of Tools/owl_recipe.py), fills a
/// <see cref="BossClipPlayer"/> (the generic Recovery clip is the Perch pose and Resonance is the Screech, so the existing boss
/// loop plays them) and wires <see cref="BossController"/>. Idempotent. Menu: Tools > Project Game > Boss > Build Wind Owl Prefab.</summary>
public static class OwlBossBuilder
{
    public const string Folder = "Assets/Bosses/WindOwl";
    public const string PrefabPath = Folder + "/WindOwlBoss.prefab";
    public const string DefinitionPath = Folder + "/WindOwlBoss.asset";
    private const string AnimFolder = "Assets/Art/BossArena/Wind/Boss/Anim/";
    private const float Ppu = 32f;

    private static readonly string[] Sheets =
    {
        "Owl_Idle_17f", "Owl_Move_17f", "Owl_Volley_17f", "Owl_TakeOff_17f", "Owl_Dive_17f", "Owl_Perch_17f", "Owl_Flap_17f", "Owl_Sweep_17f",
        "Owl_Slash_17f", "Owl_Circle_17f", "Owl_Screech_17f", "Owl_Death_17f",
    };

    [MenuItem("Tools/Project Game/Boss/Build Wind Owl Prefab")]
    public static void Build()
    {
        Directory.CreateDirectory(Folder);
        foreach (string sheet in Sheets)
            CrabBossBuilder.ConfigureSheet(AnimFolder + sheet + ".png", 17, Ppu);
        // the hover loop is Pixellab animate_image output (D-081): drawn at 176 px inside a 256 px frame, so its PPU is scaled to keep the owl the same size
        CrabBossBuilder.ConfigureSheet(AnimFolder + "Owl_IdleAI_17f.png", 17, Ppu * 176f / 256f);
        CrabBossBuilder.ConfigureSheet(AnimFolder + "Owl_VolleyAI_9f.png", 9, Ppu * 176f / 256f);   // Pixellab animate_image, 9 frames
        CrabBossBuilder.ConfigureSheet(AnimFolder + "Owl_TakeOffAI_9f.png", 9, Ppu * 176f / 256f);
        CrabBossBuilder.ConfigureSheet(AnimFolder + "Owl_DiveAI_9f.png", 9, Ppu * 176f / 256f);
        CrabBossBuilder.ConfigureSheet(AnimFolder + "Owl_SweepAI_9f.png", 9, Ppu * 176f / 256f);
        CrabBossBuilder.ConfigureSheet(AnimFolder + "Owl_SlashAI_9f.png", 9, Ppu * 176f / 256f);

        var definition = AssetDatabase.LoadAssetAtPath<BossDefinition>(DefinitionPath);
        if (definition == null)
        {
            definition = BossDefinition.CreateWindOwlDefaults();
            AssetDatabase.CreateAsset(definition, DefinitionPath);
        }

        definition.phases = BossDefinition.CreateWindOwlDefaults().phases; // the combo lists live in code; the numbers stay in the asset
        definition.impactVfx = "Wind/WindImpact_Burst";
        definition.phaseVfx = "Wind/WindRing_Flow";
        EditorUtility.SetDirty(definition);

        var root = new GameObject("WindOwlBoss");
        root.transform.localScale = Vector3.one * 0.85f;
        var body = new GameObject("Body");
        body.transform.SetParent(root.transform, false);
        var renderer = body.AddComponent<SpriteRenderer>();
        renderer.sprite = Frames("Owl_IdleAI_17f").FirstOrDefault();
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 5;

        var player = body.AddComponent<BossClipPlayer>();
        FillClips(player, new[]
        {
            Clip(BossClipId.Idle, "Owl_IdleAI_17f", 14f, true, -1),     // AI wing-beat hover loop
            Clip(BossClipId.Move, "Owl_IdleAI_17f", 24f, true, -1),
            Clip(BossClipId.Volley, "Owl_VolleyAI_9f", 14f, false, 4),     // wings back (held for the telegraph), then the fling
            Clip(BossClipId.TakeOff, "Owl_TakeOffAI_9f", 12f, false, -1),  // crouch, beat the wings, fade upward
            Clip(BossClipId.Dive, "Owl_DiveAI_9f", 14f, false, -1),        // wings tucked, then a flare on landing
            Clip(BossClipId.Recovery, "Owl_Perch_17f", 16f, false, 12),     // perched and winded (the vulnerable window)
            Clip(BossClipId.Flap, "Owl_IdleAI_17f", 30f, false, -1),        // alternating wing beats: cyclones
            Clip(BossClipId.Sweep, "Owl_SweepAI_9f", 12f, false, 3),       // bank to one side, then sweep: gale wall
            Clip(BossClipId.Slash, "Owl_SlashAI_9f", 14f, false, 3),       // wings up, then the crossing cuts: crescent blades
            Clip(BossClipId.Circle, "Owl_Circle_17f", 20f, true, -1),     // flying circles: sky storm
            Clip(BossClipId.Resonance, "Owl_Screech_17f", 20f, false, 10), // head back, wings up and shaking: phase change
            Clip(BossClipId.Death, "Owl_Death_17f", 16f, false, -1),
        });

        var shadow = new GameObject("Shadow");
        shadow.transform.SetParent(root.transform, false);
        shadow.transform.localPosition = new Vector3(0f, -3.4f, 0f);
        shadow.transform.localScale = new Vector3(6.5f, 1.7f, 1f);
        var shadowRenderer = shadow.AddComponent<SpriteRenderer>();
        shadowRenderer.sprite = BossTelegraph.CircleSprite;
        shadowRenderer.color = new Color(0f, 0f, 0.05f, 0.3f);
        shadowRenderer.sortingLayerName = "Default";
        shadowRenderer.sortingOrder = -50;

        var rigidbody = root.AddComponent<Rigidbody2D>();
        rigidbody.bodyType = RigidbodyType2D.Kinematic;
        var hurtbox = root.AddComponent<CircleCollider2D>();
        hurtbox.isTrigger = true;
        hurtbox.radius = 2.7f;
        hurtbox.offset = new Vector2(0f, 0.2f);
        var solidHurtbox = root.AddComponent<CircleCollider2D>(); // found by overlap-based player skills, pushes nobody
        solidHurtbox.isTrigger = false;
        solidHurtbox.radius = hurtbox.radius;
        solidHurtbox.offset = hurtbox.offset;
        solidHurtbox.excludeLayers = ~0;
        var bodyBlock = root.AddComponent<CircleCollider2D>(); // really collides: the player cannot walk through the owl
        bodyBlock.isTrigger = false;
        bodyBlock.radius = 1.8f;
        bodyBlock.offset = new Vector2(0f, 0.2f);

        var controller = root.AddComponent<BossController>();
        var serialized = new SerializedObject(controller);
        serialized.FindProperty("_definition").objectReferenceValue = definition;
        serialized.FindProperty("_body").objectReferenceValue = renderer;
        serialized.FindProperty("_visualRoot").objectReferenceValue = body.transform;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = enemyLayer;
        }

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        Debug.Log("OwlBossBuilder: built " + PrefabPath);
    }

    private static Sprite[] Frames(string sheet)
    {
        return AssetDatabase.LoadAllAssetsAtPath(AnimFolder + sheet + ".png").OfType<Sprite>()
            .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1))).ToArray();
    }

    private readonly struct ClipSpec
    {
        public readonly BossClipId Id;
        public readonly Sprite[] Frames;
        public readonly float Fps;
        public readonly bool Loop;
        public readonly int Hold;

        public ClipSpec(BossClipId id, Sprite[] frames, float fps, bool loop, int hold)
        {
            Id = id;
            Frames = frames;
            Fps = fps;
            Loop = loop;
            Hold = hold;
        }
    }

    private static ClipSpec Clip(BossClipId id, string sheet, float fps, bool loop, int hold) => new ClipSpec(id, Frames(sheet), fps, loop, hold);

    private static void FillClips(BossClipPlayer player, ClipSpec[] specs)
    {
        var serialized = new SerializedObject(player);
        serialized.FindProperty("_renderer").objectReferenceValue = player.GetComponent<SpriteRenderer>();
        SerializedProperty list = serialized.FindProperty("_clips");
        list.arraySize = specs.Length;
        for (int i = 0; i < specs.Length; i++)
        {
            SerializedProperty element = list.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("id").enumValueIndex = (int)specs[i].Id;
            element.FindPropertyRelative("fps").floatValue = specs[i].Fps;
            element.FindPropertyRelative("loop").boolValue = specs[i].Loop;
            element.FindPropertyRelative("holdFrame").intValue = specs[i].Hold;
            SerializedProperty frames = element.FindPropertyRelative("frames");
            frames.arraySize = specs[i].Frames.Length;
            for (int f = 0; f < specs[i].Frames.Length; f++)
                frames.GetArrayElementAtIndex(f).objectReferenceValue = specs[i].Frames[f];
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
