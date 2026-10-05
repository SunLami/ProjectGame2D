using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>The table editor of an SfxBank: one clean block per sound (play, name, volume) with its clips under it. A search box
/// filters the rows; "Show advanced" reveals id, description, pitch jitter, interval, voices and loop.</summary>
[CustomEditor(typeof(SfxBank))]
public sealed class SfxBankInspector : Editor
{
    private string _search = "";
    private bool _advanced;
    private static AudioSource _fallbackPreview;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        using (new EditorGUILayout.HorizontalScope())
        {
            _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
            _advanced = GUILayout.Toggle(_advanced, "Show advanced", EditorStyles.miniButton, GUILayout.Width(110));
            if (GUILayout.Button("Stop", EditorStyles.miniButton, GUILayout.Width(44)))
                StopPreview();
        }

        SerializedProperty rows = serializedObject.FindProperty("rows");
        int missing = 0;
        for (int i = 0; i < rows.arraySize; i++)
        {
            SerializedProperty row = rows.GetArrayElementAtIndex(i);
            string label = row.FindPropertyRelative("label").stringValue;
            string id = row.FindPropertyRelative("id").stringValue;
            if (!string.IsNullOrEmpty(_search) && label.IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) < 0
                && id.IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            SerializedProperty clips = row.FindPropertyRelative("clips");
            if (clips.arraySize == 0)
                missing++;
            DrawRow(row, clips);
        }

        EditorGUILayout.Space();
        if (GUILayout.Button("+ Add sound row"))
        {
            rows.arraySize++;
            SerializedProperty added = rows.GetArrayElementAtIndex(rows.arraySize - 1);
            added.FindPropertyRelative("label").stringValue = "New sound";
            added.FindPropertyRelative("id").stringValue = "sfx.custom.new_sound_" + rows.arraySize;
            added.FindPropertyRelative("category").stringValue = "Custom";
            added.FindPropertyRelative("volume").floatValue = 1f;
            added.FindPropertyRelative("clips").arraySize = 0;
        }

        if (missing > 0)
            EditorGUILayout.HelpBox(missing + " sound(s) shown have no clip yet (silent in game).", MessageType.Warning);
        serializedObject.ApplyModifiedProperties();
    }

    private void DrawRow(SerializedProperty row, SerializedProperty clips)
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            SerializedProperty label = row.FindPropertyRelative("label");
            SerializedProperty volume = row.FindPropertyRelative("volume");
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("▶", GUILayout.Width(26)))
                    PlayRandom(clips, volume.floatValue);
                if (_advanced)
                    EditorGUILayout.PropertyField(label, GUIContent.none);
                else
                    EditorGUILayout.LabelField(label.stringValue, EditorStyles.boldLabel);
                GUILayout.Label("vol", GUILayout.Width(24));
                volume.floatValue = GUILayout.HorizontalSlider(volume.floatValue, 0f, 2f, GUILayout.Width(110));
                GUILayout.Label(volume.floatValue.ToString("0.00"), GUILayout.Width(34));
            }

            int remove = -1;
            for (int i = 0; i < clips.arraySize; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(30);
                    SerializedProperty element = clips.GetArrayElementAtIndex(i);
                    var clip = element.objectReferenceValue as AudioClip;
                    if (GUILayout.Button("▶", EditorStyles.miniButton, GUILayout.Width(22)) && clip != null)
                        Play(clip);
                    EditorGUILayout.PropertyField(element, GUIContent.none);
                    GUILayout.Label(clip != null ? clip.length.ToString("0.0") + "s" : "?", EditorStyles.miniLabel, GUILayout.Width(34));
                    if (GUILayout.Button("✕", EditorStyles.miniButton, GUILayout.Width(22)))
                        remove = i;
                }
            }

            if (remove >= 0)
            {
                clips.GetArrayElementAtIndex(remove).objectReferenceValue = null;
                clips.DeleteArrayElementAtIndex(remove);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(30);
                var added = (AudioClip)EditorGUILayout.ObjectField("Drop a clip here to add", null, typeof(AudioClip), false);
                if (added != null)
                {
                    clips.arraySize++;
                    clips.GetArrayElementAtIndex(clips.arraySize - 1).objectReferenceValue = added;
                }

                if (GUILayout.Button("Import file…", EditorStyles.miniButton, GUILayout.Width(80)))
                {
                    string path = EditorUtility.OpenFilePanel("Import SFX for " + label.stringValue, "", "wav,ogg,mp3");
                    if (!string.IsNullOrEmpty(path))
                    {
                        AudioClip clip = SfxDatabaseTools.ImportExternalFile(path, row.FindPropertyRelative("category").stringValue,
                            row.FindPropertyRelative("id").stringValue, row.FindPropertyRelative("loop").boolValue);
                        if (clip != null)
                        {
                            clips.arraySize++;
                            clips.GetArrayElementAtIndex(clips.arraySize - 1).objectReferenceValue = clip;
                        }
                    }
                }
            }

            if (_advanced)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(row.FindPropertyRelative("id"));
                EditorGUILayout.PropertyField(row.FindPropertyRelative("category"));
                EditorGUILayout.PropertyField(row.FindPropertyRelative("description"));
                EditorGUILayout.PropertyField(row.FindPropertyRelative("pitchJitter"));
                EditorGUILayout.PropertyField(row.FindPropertyRelative("minInterval"));
                EditorGUILayout.PropertyField(row.FindPropertyRelative("maxVoices"));
                EditorGUILayout.PropertyField(row.FindPropertyRelative("loop"));
                using (new EditorGUI.DisabledScope(!Application.isPlaying))
                {
                    if (GUILayout.Button("Play in game (Play Mode)"))
                        SoundFXManager.PlaySfx(row.FindPropertyRelative("id").stringValue);
                }

                EditorGUI.indentLevel--;
            }
        }
    }

    // ------------------------------------------------------------------------------------------------ audition
    private static void PlayRandom(SerializedProperty clips, float volume)
    {
        if (clips.arraySize == 0)
            return;
        var clip = clips.GetArrayElementAtIndex(Random.Range(0, clips.arraySize)).objectReferenceValue as AudioClip;
        if (clip != null)
            Play(clip, volume);
    }

    public static void Play(AudioClip clip, float volume = 1f)
    {
        StopPreview();
        MethodInfo method = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil")?.GetMethod(
            "PlayPreviewClip", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
        if (method != null)
        {
            method.Invoke(null, new object[] { clip, 0, false });
            return;
        }

        if (_fallbackPreview == null)
            _fallbackPreview = new GameObject("SfxPreview") { hideFlags = HideFlags.HideAndDontSave }.AddComponent<AudioSource>();
        _fallbackPreview.volume = Mathf.Clamp01(volume);
        _fallbackPreview.PlayOneShot(clip);
    }

    public static void StopPreview()
    {
        typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil")?.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public)?.Invoke(null, null);
        if (_fallbackPreview != null)
            _fallbackPreview.Stop();
    }
}
