using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Tools > SFX > SFX Manager: a thin launcher around the real assets. Left: the list of SFX banks (or scene profiles);
/// right: that asset's own inspector (the table of sounds, or the scene settings). Everything it edits is a normal
/// ScriptableObject, so you can also just select the asset in the Project window.</summary>
public sealed class SfxManagerWindow : EditorWindow
{
    private enum Tab { Sounds, Scenes }

    private Tab _tab;
    private List<SfxBank> _banks = new List<SfxBank>();
    private List<SfxSceneProfile> _scenes = new List<SfxSceneProfile>();
    private Object _selected;
    private Editor _editor;
    private Vector2 _listScroll, _detailScroll;

    [MenuItem("Tools/SFX/SFX Manager")]
    public static void Open()
    {
        var window = GetWindow<SfxManagerWindow>("SFX Manager");
        window.minSize = new Vector2(620, 420);
        window.Show();
    }

    private void OnEnable() => Reload();

    private void OnDisable()
    {
        SfxBankInspector.StopPreview();
        if (_editor != null)
            DestroyImmediate(_editor);
    }

    private void OnFocus() => Reload();

    private void Reload()
    {
        _banks = SfxDatabaseTools.AllBanks();
        _scenes = SfxDatabaseTools.AllScenes();
    }

    private void Select(Object asset)
    {
        if (_selected == asset)
            return;
        _selected = asset;
        if (_editor != null)
            DestroyImmediate(_editor);
        _editor = asset != null ? Editor.CreateEditor(asset) : null;
        _detailScroll = Vector2.zero;
    }

    private void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            Tab next = (Tab)GUILayout.Toolbar((int)_tab, new[] { "Sounds", "Scenes" }, EditorStyles.toolbarButton, GUILayout.Width(160));
            if (next != _tab)
            {
                _tab = next;
                Select(null);
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Add missing sounds from catalog", EditorStyles.toolbarButton))
            {
                int n = SfxDatabaseTools.SyncFromCatalog();
                Reload();
                ShowNotification(new GUIContent(n + " new sound row(s) added"));
            }

            if (GUILayout.Button("Stop", EditorStyles.toolbarButton))
                SfxBankInspector.StopPreview();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(200)))
            {
                _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
                if (_tab == Tab.Sounds)
                    DrawBankList();
                else
                    DrawSceneList();
                EditorGUILayout.EndScrollView();
            }

            using (new EditorGUILayout.VerticalScope())
            {
                if (_editor == null)
                    EditorGUILayout.HelpBox(_tab == Tab.Sounds
                        ? "Pick a bank on the left. Each bank is one asset with a table of sounds: press play to listen, drop a clip to replace or add one, move the slider for volume."
                        : "Pick a scene on the left to set its ambience, footsteps and the volume or mute of each SFX type in that scene.", MessageType.Info);
                else
                {
                    _detailScroll = EditorGUILayout.BeginScrollView(_detailScroll);
                    EditorGUILayout.ObjectField("Asset", _selected, _selected.GetType(), false);
                    _editor.OnInspectorGUI();
                    EditorGUILayout.EndScrollView();
                }
            }
        }
    }

    private void DrawBankList()
    {
        foreach (SfxBank bank in _banks)
        {
            int silent = bank.rows.Count(r => r.clips == null || r.clips.Count == 0 || r.clips.Any(c => c == null));
            string text = bank.title + "  (" + bank.rows.Count + (silent > 0 ? ", " + silent + " silent" : "") + ")";
            if (GUILayout.Toggle(_selected == bank, text, "Button"))
                Select(bank);
        }
    }

    private void DrawSceneList()
    {
        foreach (SfxSceneProfile profile in _scenes)
        {
            if (GUILayout.Toggle(_selected == profile, profile.sceneName, "Button"))
                Select(profile);
        }

        EditorGUILayout.Space();
        GUILayout.Label("Create profile for", EditorStyles.miniBoldLabel);
        foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
        {
            string name = Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid));
            if (_scenes.All(s => s.sceneName != name) && GUILayout.Button("+ " + name))
            {
                SfxSceneProfile created = SfxDatabaseTools.CreateSceneProfile(name);
                Reload();
                Select(created);
            }
        }

        EditorGUILayout.HelpBox("A new scene also needs the SceneAmbience object: run Tools > SFX > Install Scene Ambience once.", MessageType.None);
    }
}
