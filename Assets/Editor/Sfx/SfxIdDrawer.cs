using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Dropdown of every sound id (grouped by bank) for fields marked [SfxId].</summary>
[CustomPropertyDrawer(typeof(SfxIdAttribute))]
public sealed class SfxIdDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        Rect fieldRect = EditorGUI.PrefixLabel(position, label);
        string current = property.stringValue;
        string shown = string.IsNullOrEmpty(current) ? "(none)" : Describe(current);
        if (!EditorGUI.DropdownButton(fieldRect, new GUIContent(shown, current), FocusType.Keyboard))
            return;

        var menu = new GenericMenu();
        menu.AddItem(new GUIContent("(none)"), string.IsNullOrEmpty(current), () => Set(property, ""));
        foreach (SfxBank bank in SfxDatabaseTools.AllBanks())
        {
            foreach (SfxBank.Row row in bank.rows.OrderBy(r => r.label))
            {
                string id = row.id;
                menu.AddItem(new GUIContent(bank.title + "/" + row.label), id == current, () => Set(property, id));
            }
        }

        menu.DropDown(fieldRect);
    }

    private static string Describe(string id)
    {
        SfxBank.Row row = SfxDatabaseTools.FindRow(id);
        return row != null ? row.label + "  (" + id + ")" : id + "  (missing)";
    }

    private static void Set(SerializedProperty property, string id)
    {
        property.serializedObject.Update();
        property.stringValue = id;
        property.serializedObject.ApplyModifiedProperties();
    }
}
