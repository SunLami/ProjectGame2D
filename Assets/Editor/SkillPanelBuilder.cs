using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SkillPanelBuilder
{
    public const string PrefabPath = "Assets/Prefabs/UI/SkillPanel.prefab";
    public const string CatalogPath = "Assets/Resources/Skills/PlayerSkillCatalog.asset";
    private static readonly string[] SkillIds = { "skill.water.spiral_bolt", "skill.water.vortex", "skill.water.spiral_beam", "skill.water.threefold_tsunami", "skill.earth.rock_bolt", "skill.earth.rock_arena", "skill.earth.spike_field", "skill.earth.meteor_storm", "skill.wind.returning_blade", "skill.wind.whirlwind", "skill.wind.gale_fan", "skill.wind.roc" };
    private const string Art = "Assets/Resources/UI/Skills/Panel/V1/";
    private const string Icons = "Assets/Resources/UI/Skills/Icons/V2/";
    private static readonly string[] Files = { "water_01_spiral_bolt", "water_02_vortex", "water_03_spiral_beam", "water_04_threefold_tsunami", "earth_01_rock_bolt", "earth_02_rock_arena", "earth_03_spike_field", "earth_04_meteor_storm", "wind_01_returning_blade", "wind_02_whirlwind", "wind_03_gale_fan", "wind_04_wind_roc" };
    private static readonly string[] Names = { "Spiral Water Bolt", "Water Vortex", "Spiral Water Beam", "Threefold Tsunami", "Rock Bolt", "Rock Arena", "Rock Spike Field", "Meteor Storm", "Returning Wind Blade", "Whirlwind", "Gale Fan", "Wind Roc" };
    private static readonly string[] Descriptions = {
        "Launch a spiraling water projectile.", "Create a damaging water vortex that slows enemies and pulls them toward its dark center.",
        "Fire a sustained spiral water beam while rooted in place.", "Unleash three increasingly powerful waves, culminating in a water dragon and final water blast.",
        "Launch an angular boulder.", "Raise a pillar arena that traps, slows and weakens enemies, with stunning spikes inside.",
        "Erupt a forward fan of damaging stone spikes that stun and root enemies.", "Call down flaming rocky meteors with explosive impacts.",
        "Throw a crescent wind blade that damages enemies on its outward and return paths.", "Summon a moving whirlwind that lifts enemies, then drops and stuns them.",
        "Release three pushing gusts in a forward fan. Wall collisions deal bonus damage and stun.", "Summon a diving wind roc that carries and throws enemies into a shockwave, leaving a slowing aftermath." };
    private static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/DigitalDisco SDF v3.asset");

    [MenuItem("Tools/ProjectGame2D/UI/Build Skill Panel")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
        var root = new GameObject("SkillPanel", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(SkillPanelUI));
        try
        {
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            root.GetComponent<Canvas>().sortingOrder = 120;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var window = Rect(root.transform, "Window", 0, 0, 1920, 1080);
            window.anchorMin = Vector2.zero; window.anchorMax = Vector2.one; window.sizeDelta = Vector2.zero;
            var dim = window.gameObject.AddComponent<Image>(); dim.color = new Color(0,0,0,.62f);
            var composition = Rect(window, "Composition", 0, 0, 1720, 900);
            // Center the board and content; hover tooltip positioning clamps itself to the Canvas.
            var board = Picture(composition, "Board", "skill_panel_board_v1", 0, 0, 1300, 790);
            var content = Rect(composition, "Content", 0, 0, 1180, 710);
            Label(content, "Title", "SKILLS LIBRARY", -330, 290, 430, 48, 30);
            Picture(content, "PointsPanel", "skill_points_panel_v1", 360, 288, 300, 74);
            Label(content, "PointsText", "Skill Points: --", 360, 288, 250, 40, 22);
            var close = Picture(content, "Close", "skill_close_button_v1", 550, 303, 44, 44).gameObject.AddComponent<Button>();
            close.targetGraphic = close.GetComponent<Image>(); close.targetGraphic.raycastTarget = true;
            var socket = Crop(Art + "skill_hex_socket_v1.png");
            var badge = Crop(Art + "skill_cost_badge_v1.png");
            var buttons = new Button[12];
            for (int i = 0; i < 12; i++)
            {
                float x = -475 + i % 6 * 190; float y = 160 - i / 6 * 180;
                var slot = SpriteImage(content, "Skill_" + Files[i], socket, x, y, 140, 105);
                buttons[i] = slot.gameObject.AddComponent<Button>(); buttons[i].targetGraphic = slot; slot.raycastTarget = true;
                var icon = SpriteImage(slot.transform, "Icon", AssetDatabase.LoadAssetAtPath<Sprite>(Icons + Files[i] + ".png"), 0, 0, 112, 76);
                SkillIconPresentationBuilder.Configure(icon, SkillIconView.SocketShape.Hexagon);
                icon.color = new Color(.68f,.68f,.68f,1);
                SpriteImage(slot.transform, "CostBadge", badge, 0, -53, 38, 38);
                Label(slot.transform, "Cost", (i % 4 + 2).ToString(), 0, -53, 30, 28, 20);
                Label(slot.transform, "Name", Names[i], 0, -97, 174, 48, 18);
            }
            Label(content, "LoadoutTitle", "SKILL LOADOUT", -410, -190, 290, 36, 22);
            for (int i = 0; i < 4; i++)
            {
                var slot = SpriteImage(content, "Loadout_" + (i+1), socket, -345 + i * 230, -245, 108, 80);
                Label(slot.transform, "SlotLabel", SkillPanelUI.LoadoutKey(i), 0, -26, 80, 20, 16);
            }
            var tooltip = Rect(composition, "Tooltip", 640, 0, 380, 620);
            Picture(tooltip, "Background", "skill_tooltip_panel_v1", 0, 0, 380, 620);
            var title = Label(tooltip, "SkillTitle", Names[0], 0, 220, 270, 80, 26);
            var description = Label(tooltip, "Description", Descriptions[0], 0, 65, 270, 180, 20);
            description.alignment = TextAlignmentOptions.TopLeft;
            var cost = Label(tooltip, "LearningCost", "Skill Point Cost: 2", 0, -80, 270, 60, 18);
            var learn = Picture(tooltip, "Learn", "skill_learn_button_v1", 0, -145, 255, 58).gameObject.AddComponent<Button>();
            learn.targetGraphic = learn.GetComponent<Image>(); learn.interactable = false;
            Label(learn.transform, "Label", "Learn", 0, 0, 200, 35, 22);
            Label(tooltip, "Status", "Skill learning is not connected yet.", 0, -220, 270, 62, 17);
            var so = new SerializedObject(root.GetComponent<SkillPanelUI>());
            Set(so, "_window", window.gameObject); Set(so, "_tooltip", tooltip.gameObject);
            Set(so, "_title", title); Set(so, "_description", description); Set(so, "_cost", cost); Set(so, "_close", close);
            var entries = so.FindProperty("_entries"); entries.arraySize = 12;
            for(int i=0;i<12;i++)
            {
                var entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("id").stringValue = "skill." + Files[i];
                entry.FindPropertyRelative("displayName").stringValue = Names[i];
                entry.FindPropertyRelative("description").stringValue = Descriptions[i];
                entry.FindPropertyRelative("learningCost").intValue = i%4+2;
                entry.FindPropertyRelative("button").objectReferenceValue = buttons[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo(); tooltip.gameObject.SetActive(false); window.gameObject.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
        AssetDatabase.SaveAssets();
        UpgradeProgression();
    }

    [MenuItem("Tools/ProjectGame2D/UI/Connect Skill Progression")]
    public static void UpgradeProgression()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
        var catalog = AssetDatabase.LoadAssetAtPath<SkillCatalog>(CatalogPath);
        if (catalog == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Skills")) AssetDatabase.CreateFolder("Assets/Resources", "Skills");
            catalog = ScriptableObject.CreateInstance<SkillCatalog>(); AssetDatabase.CreateAsset(catalog,CatalogPath);
            var data = new SerializedObject(catalog); var defs = data.FindProperty("_definitions"); defs.arraySize=12;
            for(int i=0;i<12;i++)
            {
                var d=defs.GetArrayElementAtIndex(i);
                d.FindPropertyRelative("id").stringValue=SkillIds[i];
                d.FindPropertyRelative("displayName").stringValue=Names[i];
                d.FindPropertyRelative("description").stringValue=Descriptions[i];
                d.FindPropertyRelative("learningCost").intValue=i%4+2;
                d.FindPropertyRelative("castKind").intValue=i+1;
                d.FindPropertyRelative("manaCost").floatValue=new float[] {8f,16f,24f,35f}[i%4];
                d.FindPropertyRelative("cooldownSeconds").floatValue=new float[] {2f,6f,10f,18f}[i%4];
                d.FindPropertyRelative("icon").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(Icons+Files[i]+".png");
            }
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(catalog);
        }
        catalog.Validate();
        var root=PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var ui=root.GetComponent<SkillPanelUI>(); var so=new SerializedObject(ui);
            var content=root.transform.Find("Window/Composition/Content"); var tooltip=root.transform.Find("Window/Composition/Tooltip");
            Set(so,"_catalog",catalog);Set(so,"_pointsText",content.Find("PointsText").GetComponent<TMP_Text>());
            var learn=tooltip.Find("Learn").GetComponent<Button>(); learn.GetComponent<Image>().raycastTarget=true;
            Set(so,"_learn",learn);Set(so,"_learnLabel",learn.transform.Find("Label").GetComponent<TMP_Text>());
            Set(so,"_status",tooltip.Find("Status").GetComponent<TMP_Text>());
            content.Find("PointsText").GetComponent<TMP_Text>().text="Skill Points: 0";
            tooltip.Find("Status").GetComponent<TMP_Text>().text="Select a skill to learn.";
            var entries=so.FindProperty("_entries");
            for(int i=0;i<12;i++)
            {
                var e=entries.GetArrayElementAtIndex(i); e.FindPropertyRelative("id").stringValue=SkillIds[i];
                var button=(Button)e.FindPropertyRelative("button").objectReferenceValue;
                if(button.GetComponent<SkillIconDragUI>()==null) button.gameObject.AddComponent<SkillIconDragUI>();
            }
            var icons=so.FindProperty("_loadoutIcons");icons.arraySize=4;
            for(int i=0;i<4;i++)
            {
                var slot=content.Find("Loadout_"+(i+1));slot.GetComponent<Image>().raycastTarget=true;
                if(slot.GetComponent<SkillIconDragUI>()==null)slot.gameObject.AddComponent<SkillIconDragUI>();
                if(slot.GetComponent<SkillLoadoutDropUI>()==null)slot.gameObject.AddComponent<SkillLoadoutDropUI>();
                var icon=slot.Find("Icon");
                if(icon==null)icon=SpriteImage(slot,"Icon",null,0,10,56,40).transform;
                ((RectTransform)icon).anchoredPosition=new Vector2(0,7);
                ((RectTransform)icon).sizeDelta=new Vector2(80,50);
                var keyLabel=slot.Find("SlotLabel").GetComponent<TMP_Text>();
                keyLabel.text=SkillPanelUI.LoadoutKey(i);keyLabel.fontSize=16;
                keyLabel.rectTransform.anchoredPosition=new Vector2(0,-26);
                keyLabel.rectTransform.sizeDelta=new Vector2(80,20);
                icon.GetComponent<Image>().enabled=false;
                icons.GetArrayElementAtIndex(i).objectReferenceValue=icon.GetComponent<Image>();
                SkillIconPresentationBuilder.Configure(icon.GetComponent<Image>(), SkillIconView.SocketShape.Hexagon);
            }
            so.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/ProjectGame2D/UI/Install Skill Panel In Active Scene")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
        if (UnityEngine.Object.FindAnyObjectByType<SkillPanelUI>(FindObjectsInactive.Include) != null)
            throw new InvalidOperationException("SkillPanel already installed; preserve the existing instance.");
        PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), EditorSceneManager.GetActiveScene());
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    [MenuItem("Tools/ProjectGame2D/UI/Connect DemoScene Loadout Casting")]
    public static void ConnectDemoSceneLoadoutCasting()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required; do not interrupt owner's manual test.");
        var scene=EditorSceneManager.GetActiveScene();
        if(scene.path!="Assets/Scenes/DemoScene.unity") throw new InvalidOperationException("Open DemoScene before connecting its cast bridge.");
        UpgradeProgression();
        var catalog=AssetDatabase.LoadAssetAtPath<SkillCatalog>(CatalogPath);
        var harnesses=UnityEngine.Object.FindObjectsByType<SkillTestHarness>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .Where(h=>h.gameObject.scene==scene).ToArray();
        if(harnesses.Length!=1)throw new InvalidOperationException("Expected exactly one DemoScene cast bridge.");
        var binding=new SerializedObject(harnesses[0]);Set(binding,"_catalog",catalog);binding.ApplyModifiedPropertiesWithoutUndo();
        foreach(var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(button.gameObject.scene!=scene)continue;
            for(int i=0;i<button.onClick.GetPersistentEventCount();i++)
                if(button.onClick.GetPersistentTarget(i)==harnesses[0] && button.onClick.GetPersistentMethodName(i)=="CastTestProjectile")
                { Undo.DestroyObjectImmediate(button.gameObject);break; }
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }

    // Crop only Sprite metadata, never raster pixels. Keeps AI safety padding out of layout measurements.
    private static Sprite Crop(string path)
    {
        var texture = new Texture2D(2,2); texture.LoadImage(System.IO.File.ReadAllBytes(path));
        var pixels = texture.GetPixels32(); int left=texture.width, right=0, bottom=texture.height, top=0;
        for(int y=0;y<texture.height;y++) for(int x=0;x<texture.width;x++)
            if(pixels[y*texture.width+x].a > 128) { left=Math.Min(left,x); right=Math.Max(right,x); bottom=Math.Min(bottom,y); top=Math.Max(top,y); }
        UnityEngine.Object.DestroyImmediate(texture);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.spriteImportMode = SpriteImportMode.Multiple;
#pragma warning disable CS0618
        importer.spritesheet = new[] { new SpriteMetaData { name = "UI", rect = new Rect(left,bottom,right-left+1,top-bottom+1), alignment=0, pivot=new Vector2(.5f,.5f) } };
#pragma warning restore CS0618
        importer.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Single();
    }
    private static RectTransform Rect(Transform parent, string name, float x,float y,float w,float h)
    {
        var go = new GameObject(name, typeof(RectTransform)); var r = go.GetComponent<RectTransform>();
        r.SetParent(parent,false); r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);
        r.anchoredPosition=new Vector2(x,y); r.sizeDelta=new Vector2(w,h); return r;
    }
    private static Image SpriteImage(Transform parent,string name,Sprite sprite,float x,float y,float w,float h)
    {
        var r=Rect(parent,name,x,y,w,h); var image=r.gameObject.AddComponent<Image>();
        image.sprite=sprite; image.preserveAspect=true; image.raycastTarget=false; return image;
    }
    private static Image Picture(Transform parent,string name,string file,float x,float y,float w,float h) => SpriteImage(parent,name,Crop(Art+file+".png"),x,y,w,h);
    private static TMP_Text Label(Transform parent,string name,string value,float x,float y,float w,float h,float size)
    {
        var r=Rect(parent,name,x,y,w,h); var t=r.gameObject.AddComponent<TextMeshProUGUI>();
        t.font=Font; t.text=value; t.fontSize=size; t.color=new Color(1,.91f,.70f);
        t.alignment=TextAlignmentOptions.Center; t.raycastTarget=false;
        t.textWrappingMode=TextWrappingModes.Normal; return t;
    }
    private static void Set(SerializedObject so,string name,UnityEngine.Object value) => so.FindProperty(name).objectReferenceValue=value;
}
