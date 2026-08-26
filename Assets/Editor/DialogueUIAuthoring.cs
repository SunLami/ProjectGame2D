#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class DialogueUIAuthoring
{
    private const string PrefabPath = "Assets/Prefabs/UI/DialogueUI.prefab";
    private const string ScenePath = "Assets/Scenes/DemoScene.unity";
    private const string DemoDialoguePath = "Assets/Resources/Dialogue/TownElderGreeting.asset";

    [MenuItem("Tools/Project Game 2D/Build Dialogue UI")]
    public static void Build()
    {
        EnsureFolder("Assets/Prefabs/UI");
        Sprite frame = LoadSprite("Assets/Resources/UI/Dialogue/LightFantasy/dialogue_frame_hd.png");
        Sprite nameplate = LoadSprite("Assets/Resources/UI/Dialogue/LightFantasy/dialogue_nameplate_hd.png");
        Sprite choice = LoadSprite("Assets/Resources/UI/Dialogue/LightFantasy/dialogue_choice_button_hd.png");
        Sprite indicator = LoadSprite("Assets/Resources/UI/Dialogue/LightFantasy/dialogue_continue_indicator_hd.png");
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/DigitalDisco SDF v3.asset");
        DialogueDefinition demoDialogue = CreateDemoDialogue();
        BindDemoNpcPrefab(demoDialogue);

        GameObject root = new("DialogueUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(DialogueUI));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 220;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800f, 450f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject overlay = UI("DialogueOverlay", root.transform);
        Stretch(overlay.GetComponent<RectTransform>());
        Image blocker = overlay.AddComponent<Image>();
        blocker.color = new Color(0f, 0f, 0f, 0.42f);

        GameObject panel = UI("Panel", overlay.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 42f);
        panelRect.sizeDelta = new Vector2(700f, 190f);

        Image portraitBacking = Image("PortraitBacking", panel.transform, null, new Color32(22, 42, 62, 255));
        SetRect(portraitBacking.rectTransform, new Vector2(-234f, 0f), new Vector2(154f, 154f));
        Image portrait = Image("Portrait", panel.transform, null, Color.white);
        SetRect(portrait.rectTransform, new Vector2(-234f, 0f), new Vector2(140f, 140f));
        portrait.preserveAspect = true;
        portrait.enabled = false;

        Image textBacking = Image("TextBacking", panel.transform, null, new Color32(244, 220, 166, 255));
        SetRect(textBacking.rectTransform, new Vector2(96f, -1f), new Vector2(448f, 164f));
        textBacking.raycastTarget = false;

        Image frameImage = Image("Frame", panel.transform, frame, Color.white);
        SetRect(frameImage.rectTransform, Vector2.zero, new Vector2(700f, 394f));
        frameImage.preserveAspect = true;
        frameImage.raycastTarget = false;

        Image nameplateImage = Image("Nameplate", panel.transform, nameplate, Color.white);
        SetRect(nameplateImage.rectTransform, new Vector2(-48f, 80f), new Vector2(230f, 100f));
        nameplateImage.preserveAspect = true;
        TMP_Text speaker = Text("SpeakerName", nameplateImage.transform, font, 16f, TextAlignmentOptions.Center, new Color32(78, 43, 21, 255));
        Stretch(speaker.rectTransform, 34f, 34f, 10f, 10f);
        speaker.textWrappingMode = TextWrappingModes.NoWrap;
        speaker.overflowMode = TextOverflowModes.Ellipsis;
        speaker.text = "VILLAGE ELDER";

        TMP_Text body = Text("BodyText", panel.transform, font, 15f, TextAlignmentOptions.TopLeft, new Color32(56, 40, 27, 255));
        RectTransform bodyRect = body.rectTransform;
        bodyRect.anchorMin = bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
        bodyRect.pivot = new Vector2(0.5f, 0.5f);
        bodyRect.anchoredPosition = new Vector2(98f, 45f);
        bodyRect.sizeDelta = new Vector2(360f, 40f);
        body.textWrappingMode = TextWrappingModes.Normal;
        body.overflowMode = TextOverflowModes.Ellipsis;
        body.lineSpacing = 1f;
        body.text = "Welcome, traveler. Our village has work for capable hands.";

        Image continueImage = Image("ContinueIndicator", panel.transform, indicator, Color.white);
        SetRect(continueImage.rectTransform, new Vector2(285f, -55f), new Vector2(30f, 30f));
        continueImage.raycastTarget = false;

        GameObject choiceRoot = UI("ChoiceRoot", panel.transform);
        RectTransform choiceRootRect = choiceRoot.GetComponent<RectTransform>();
        choiceRootRect.anchorMin = choiceRootRect.anchorMax = new Vector2(0.5f, 0.5f);
        choiceRootRect.pivot = new Vector2(0.5f, 0.5f);
        choiceRootRect.anchoredPosition = new Vector2(98f, -28f);
        choiceRootRect.sizeDelta = new Vector2(280f, 104f);
        VerticalLayoutGroup layout = choiceRoot.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 1f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        GameObject templateObject = UI("ChoiceTemplate", choiceRoot.transform);
        LayoutElement element = templateObject.AddComponent<LayoutElement>();
        element.preferredHeight = 20f;
        Image choiceImage = templateObject.AddComponent<Image>();
        choiceImage.sprite = choice;
        Button choiceButton = templateObject.AddComponent<Button>();
        ColorBlock colors = choiceButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(210, 232, 255, 255);
        colors.pressedColor = new Color32(202, 176, 126, 255);
        colors.disabledColor = new Color32(130, 125, 116, 170);
        colors.fadeDuration = 0.08f;
        choiceButton.colors = colors;
        TMP_Text choiceLabel = Text("Label", templateObject.transform, font, 11f, TextAlignmentOptions.Center, new Color32(58, 40, 25, 255));
        Stretch(choiceLabel.rectTransform, 18f, 18f, 1f, 1f);
        choiceLabel.enableAutoSizing = true;
        choiceLabel.fontSizeMin = 8f;
        choiceLabel.fontSizeMax = 11f;
        choiceLabel.textWrappingMode = TextWrappingModes.NoWrap;
        choiceLabel.overflowMode = TextOverflowModes.Ellipsis;

        DialogueUI dialogue = root.GetComponent<DialogueUI>();
        SerializedObject data = new(dialogue);
        data.FindProperty("_root").objectReferenceValue = overlay;
        data.FindProperty("_portrait").objectReferenceValue = portrait;
        data.FindProperty("_speakerName").objectReferenceValue = speaker;
        data.FindProperty("_bodyText").objectReferenceValue = body;
        data.FindProperty("_bodyTextWithChoicesPosition").vector2Value = new Vector2(98f, 45f);
        data.FindProperty("_bodyTextWithChoicesSize").vector2Value = new Vector2(360f, 40f);
        data.FindProperty("_bodyTextWithoutChoicesPosition").vector2Value = new Vector2(98f, -7f);
        data.FindProperty("_bodyTextWithoutChoicesSize").vector2Value = new Vector2(360f, 96f);
        data.FindProperty("_continueIndicator").objectReferenceValue = continueImage.gameObject;
        data.FindProperty("_choiceRoot").objectReferenceValue = choiceRoot.transform;
        data.FindProperty("_choiceTemplate").objectReferenceValue = choiceButton;
        data.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        InstallInDemoScene(prefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Dialogue UI prefab built and installed in DemoScene.");
    }

    private static void InstallInDemoScene(GameObject prefab)
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject existingDialogue = null;
        foreach (GameObject existing in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
        {
            if (existing != null && existing.scene == scene && existing.name == "DialogueUI")
            {
                existingDialogue = existing;
                break;
            }
        }
        if (existingDialogue != null)
            Object.DestroyImmediate(existingDialogue);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.name = "DialogueUI";
        GameObject gameplayCanvas = GameObject.Find("_UI/UICanvas");
        if (gameplayCanvas == null)
            throw new MissingReferenceException("DemoScene requires _UI/UICanvas for Dialogue HUD suppression.");
        if (gameplayCanvas.GetComponent<DialogueHudGroup>() == null)
            gameplayCanvas.AddComponent<DialogueHudGroup>();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static DialogueDefinition CreateDemoDialogue()
    {
        EnsureFolder("Assets/Resources/Dialogue");
        DialogueDefinition definition = AssetDatabase.LoadAssetAtPath<DialogueDefinition>(DemoDialoguePath);
        if (definition == null)
        {
            definition = ScriptableObject.CreateInstance<DialogueDefinition>();
            AssetDatabase.CreateAsset(definition, DemoDialoguePath);
        }

        if (definition.Nodes.Count > 0)
            return definition;

        SerializedObject data = new(definition);
        data.FindProperty("_dialogueId").stringValue = "dialogue.town.elder.greeting";
        data.FindProperty("_initialNodeId").stringValue = "greeting";
        SerializedProperty nodes = data.FindProperty("_nodes");
        nodes.arraySize = 2;
        ConfigureNode(nodes.GetArrayElementAtIndex(0), "greeting", "Village Elder",
            "Welcome, traveler. Our village has work for capable hands. Come, let us speak of what lies ahead.",
            "quest", string.Empty);
        ConfigureNode(nodes.GetArrayElementAtIndex(1), "quest", "Village Elder",
            "Help the people, learn the land, and return when your task is complete.",
            string.Empty, "conversation.completed");
        data.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(definition);
        return definition;
    }

    private static void BindDemoNpcPrefab(DialogueDefinition dialogue)
    {
        const string npcPrefabPath = "Assets/Prefabs/Quest/TownElderNPC.prefab";
        GameObject contents = PrefabUtility.LoadPrefabContents(npcPrefabPath);
        try
        {
            QuestNpcInteractionUI npc = contents.GetComponentInChildren<QuestNpcInteractionUI>(true);
            if (npc == null)
                throw new MissingComponentException($"Town Elder prefab has no {nameof(QuestNpcInteractionUI)}.");
            SerializedObject data = new(npc);
            data.FindProperty("_dialogue").objectReferenceValue = dialogue;
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(contents, npcPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    private static void ConfigureNode(SerializedProperty node, string nodeId, string speaker, string text, string nextNodeId, string outcomeId)
    {
        node.FindPropertyRelative("_nodeId").stringValue = nodeId;
        node.FindPropertyRelative("_speakerName").stringValue = speaker;
        node.FindPropertyRelative("_portrait").objectReferenceValue = null;
        node.FindPropertyRelative("_text").stringValue = text;
        node.FindPropertyRelative("_nextNodeId").stringValue = nextNodeId;
        node.FindPropertyRelative("_outcomeId").stringValue = outcomeId;
        node.FindPropertyRelative("_choices").arraySize = 0;
    }

    private static GameObject UI(string name, Transform parent)
    {
        GameObject value = new(name, typeof(RectTransform));
        value.transform.SetParent(parent, false);
        return value;
    }

    private static Image Image(string name, Transform parent, Sprite sprite, Color color)
    {
        GameObject value = UI(name, parent);
        Image image = value.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        return image;
    }

    private static TMP_Text Text(string name, Transform parent, TMP_FontAsset font, float size, TextAlignmentOptions alignment, Color color)
    {
        GameObject value = UI(name, parent);
        TextMeshProUGUI text = value.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect, float left = 0f, float right = 0f, float top = 0f, float bottom = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            throw new MissingReferenceException($"Dialogue UI sprite is missing or not imported as Sprite: {path}");
        return sprite;
    }

    private static void EnsureFolder(string path)
    {
        string current = "Assets";
        foreach (string part in path.Substring("Assets/".Length).Split('/'))
        {
            string next = $"{current}/{part}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, part);
            current = next;
        }
    }
}
#endif
