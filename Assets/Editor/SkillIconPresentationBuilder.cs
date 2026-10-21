using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class SkillIconPresentationBuilder
{
    public const string VisualsPath = "Assets/Resources/UI/Skills/SkillIconVisuals.asset";

    public static void Configure(Image image, SkillIconView.SocketShape shape)
    {
        var visuals = AssetDatabase.LoadAssetAtPath<SkillIconVisualCatalog>(VisualsPath);
        if (visuals == null) throw new System.InvalidOperationException("Import skill foregrounds first.");
        string path = "Assets/Shaders/Materials/SkillIcon" + shape + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/SkillIconSocket.shader"));
            material.SetFloat("_Shape", shape == SkillIconView.SocketShape.Diamond ? 0f : 1f);
            AssetDatabase.CreateAsset(material, path);
        }
        image.material = material;
        image.preserveAspect = false;
        Transform existing = image.transform.Find("Foreground");
        Image foreground;
        if (existing != null) foreground = existing.GetComponent<Image>();
        else
        {
            var child = new GameObject("Foreground", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            child.transform.SetParent(image.transform, false);
            foreground = child.GetComponent<Image>();
        }
        foreground.raycastTarget = false;
        foreground.preserveAspect = true;
        var view = image.GetComponent<SkillIconView>() ?? image.gameObject.AddComponent<SkillIconView>();
        view.Configure(visuals, foreground, shape);
        EditorUtility.SetDirty(view);
    }

    [MenuItem("Tools/ProjectGame2D/UI/Upgrade Shared Skill Icon Layout")]
    public static void UpgradePanel()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new System.InvalidOperationException("Upgrade skill presentation in Edit Mode only.");
        const string path = "Assets/Prefabs/UI/SkillPanel.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.name != "Icon") continue;
                bool loadout = image.transform.parent.name.StartsWith("Loadout_");
                if (!loadout && !image.transform.parent.name.StartsWith("Skill_")) continue;
                image.rectTransform.anchoredPosition = new Vector2(0f, loadout ? 7f : 0f);
                image.rectTransform.sizeDelta = loadout ? new Vector2(80f, 50f) : new Vector2(112f, 76f);
                Configure(image, SkillIconView.SocketShape.Hexagon);
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
    }
}
