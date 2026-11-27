using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Brings the player's elemental skill loadout bridge (Q/E/R/T) and the dash into
/// BossArena_Earth (D-093). The kits are driven by the DemoScene debug stand-in SkillTestHarness_DEBUG, so this
/// copies that object (all prefab references intact) into the arena scene, points it at the arena's Player and adds
/// the arena to the Player's dash scene list. Idempotent. Re-run after "Arena Earth - 2 Build Scene" rebuilds the
/// arena. Menu: Tools > Project Game > Boss > Arena Earth - Add Player Skill Kits.
/// </summary>
public static class BossArenaKitBuilder
{
    private const string ArenaScenePath = "Assets/Scenes/BossArena_Earth.unity";
    private const string DemoScenePath = "Assets/Scenes/DemoScene.unity";
    private const string SkillCatalogPath = "Assets/Resources/Skills/PlayerSkillCatalog.asset";
    private const string ArenaSceneName = "BossArena_Earth";

    [MenuItem("Tools/Project Game/Boss/Arena Earth - Add Player Skill Kits")]
    public static void Build()
    {
        var catalog = AssetDatabase.LoadAssetAtPath<SkillCatalog>(SkillCatalogPath);
        if (catalog == null)
            throw new System.InvalidOperationException("Missing player skill catalog: " + SkillCatalogPath);

        Scene arena = EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Single);
        Scene demo = EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Additive);

        SkillTestHarness source = demo.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<SkillTestHarness>(true)).FirstOrDefault();
        if (source == null)
        {
            EditorSceneManager.CloseScene(demo, true);
            throw new System.InvalidOperationException("No SkillTestHarness in DemoScene.");
        }

        // replace an earlier copy
        foreach (SkillTestHarness old in arena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SkillTestHarness>(true)).ToArray())
            Object.DestroyImmediate(old.gameObject);

        GameObject copy = Object.Instantiate(source.gameObject);
        copy.name = source.gameObject.name;
        SceneManager.MoveGameObjectToScene(copy, arena);
        EditorSceneManager.CloseScene(demo, true);

        Player player = arena.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Player>(true)).FirstOrDefault();
        if (player == null)
            throw new System.InvalidOperationException("No Player in " + ArenaSceneName);

        var harness = copy.GetComponent<SkillTestHarness>();
        var harnessSerialized = new SerializedObject(harness);
        harnessSerialized.FindProperty("_player").objectReferenceValue = player;
        harnessSerialized.FindProperty("_catalog").objectReferenceValue = catalog;
        harnessSerialized.ApplyModifiedPropertiesWithoutUndo();

        // the copy must not keep references into the (now closed) DemoScene
        var check = new SerializedObject(harness).GetIterator();
        check.NextVisible(true);
        while (check.NextVisible(false))
        {
            if (check.propertyType == SerializedPropertyType.ObjectReference && check.objectReferenceValue != null
                && !EditorUtility.IsPersistent(check.objectReferenceValue) && check.objectReferenceValue.name != player.name)
                Debug.LogWarning("BossArenaKitBuilder: scene reference left in harness: " + check.propertyPath);
        }

        // dash: enable it in the arena as well
        var playerSerialized = new SerializedObject(player);
        SerializedProperty scenes = playerSerialized.FindProperty("_dashScenes");
        bool present = false;
        for (int i = 0; i < scenes.arraySize; i++)
            present |= scenes.GetArrayElementAtIndex(i).stringValue == ArenaSceneName;
        if (!present)
        {
            scenes.arraySize++;
            scenes.GetArrayElementAtIndex(scenes.arraySize - 1).stringValue = ArenaSceneName;
            playerSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.MarkSceneDirty(arena);
        EditorSceneManager.SaveScene(arena);
        EditorSceneManager.OpenScene(DemoScenePath, OpenSceneMode.Single);
        Debug.Log("BossArenaKitBuilder: skill loadout (Q/E/R/T) and dash added to " + ArenaSceneName + ".");
    }
}
