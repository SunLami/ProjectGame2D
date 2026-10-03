using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Turns the four corner GuardianStatues of BossArena_Earth into the summon ritual emitters (D-091):
/// each statue gets the sprite that faces the summoning statue (south-east / north-east view, mirrored on
/// the east side), a solid collider that covers the whole plinth (the player must not stand on it), and an
/// "EyePoint" child that fires the laser. Wires the arena's ritual fields. Idempotent; BossArenaEarthBuilder
/// calls <see cref="Apply"/> after a rebuild. Menu: Tools > Project Game > Boss > Ritual - Build.
/// </summary>
public static class BossRitualBuilder
{
    private const string ArenaScenePath = "Assets/Scenes/BossArena_Earth.unity";
    private const string FaceSouthEastPath = "Assets/Art/BossArena/Earth/Objects/Guardian/GuardianStatue_FaceSE.png";
    private const string FaceNorthEastPath = "Assets/Art/BossArena/Earth/Objects/Guardian/GuardianStatue_FaceNE.png";

    [MenuItem("Tools/Project Game/Boss/Ritual - Build")]
    public static void BuildAll()
    {
        Scene scene = EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Single);
        Apply(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorSceneManager.OpenScene("Assets/Scenes/DemoScene.unity", OpenSceneMode.Single);
        Debug.Log("BossRitualBuilder: guardian statues turned and wired for the summon ritual.");
    }

    public static void Apply(Scene scene)
    {
        BossArenaController arena = scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<BossArenaController>(true)).FirstOrDefault();
        if (arena == null)
            throw new System.InvalidOperationException("No BossArenaController in " + scene.name);

        Sprite faceSouthEast = AssetDatabase.LoadAssetAtPath<Sprite>(FaceSouthEastPath);
        Sprite faceNorthEast = AssetDatabase.LoadAssetAtPath<Sprite>(FaceNorthEastPath);
        if (faceSouthEast == null || faceNorthEast == null)
            throw new System.InvalidOperationException("Guardian statue sprites are not imported (run Arena Earth - 1 Import Sprites).");

        var statues = scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<SpriteRenderer>(true))
            .Where(r => r.name == "GuardianStatue").ToArray();

        var eyes = new System.Collections.Generic.List<Transform>();
        foreach (SpriteRenderer renderer in statues)
        {
            GameObject statue = renderer.gameObject;
            bool east = statue.transform.position.x > 0f;
            bool north = statue.transform.position.y > 0f;

            var loop = statue.GetComponent<AmbientLoop>();
            if (loop != null)
                Object.DestroyImmediate(loop);

            renderer.sprite = north ? faceSouthEast : faceNorthEast; // north corners look down into the arena, south corners look up
            renderer.flipX = east;                                   // east corners look west

            // the collider covers the plinth and the body base: nobody can step onto the statue
            foreach (BoxCollider2D old in statue.GetComponents<BoxCollider2D>())
                Object.DestroyImmediate(old);
            var body = statue.AddComponent<BoxCollider2D>();
            body.size = new Vector2(2.1f, 1.5f);
            body.offset = new Vector2(0f, 0.6f);

            Transform existing = statue.transform.Find("EyePoint");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);
            var eye = new GameObject("EyePoint").transform;
            eye.SetParent(statue.transform, false);
            float side = east ? -1f : 1f;
            eye.localPosition = north ? new Vector3(0.12f * side, 2.1f, 0f) : new Vector3(0.28f * side, 2.15f, 0f);
            eyes.Add(eye);
        }

        var serialized = new SerializedObject(arena);
        SerializedProperty emitters = serialized.FindProperty("_ritualEmitters");
        emitters.arraySize = eyes.Count;
        for (int i = 0; i < eyes.Count; i++)
            emitters.GetArrayElementAtIndex(i).objectReferenceValue = eyes[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
