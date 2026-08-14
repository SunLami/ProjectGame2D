using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(EnemyUniversal.AttackProfile))]
public sealed class EnemyUniversalAttackProfileDrawer : PropertyDrawer
{
    private const float Spacing = 2f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (!IsValid(property)) return;

        EditorGUI.BeginProperty(position, label, property);
        Rect line = new(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, GetLabel(property, label), true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            foreach (SerializedProperty child in GetVisibleProperties(property))
            {
                line.y += line.height + Spacing;
                line.height = EditorGUI.GetPropertyHeight(child, true);
                EditorGUI.PropertyField(line, child, true);
            }
            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!IsValid(property)) return EditorGUIUtility.singleLineHeight;

        float height = EditorGUIUtility.singleLineHeight;
        if (!property.isExpanded) return height;

        foreach (SerializedProperty child in GetVisibleProperties(property))
            height += Spacing + EditorGUI.GetPropertyHeight(child, true);
        return height;
    }

    public override bool CanCacheInspectorGUI(SerializedProperty property) => false;

    private static bool IsValid(SerializedProperty property) =>
        property != null && property.serializedObject != null
        && property.serializedObject.targetObject != null;

    private static GUIContent GetLabel(SerializedProperty property, GUIContent fallback)
    {
        SerializedProperty name = property.FindPropertyRelative("name");
        return name == null || string.IsNullOrWhiteSpace(name.stringValue)
            ? fallback
            : new GUIContent(name.stringValue);
    }

    private static IEnumerable<SerializedProperty> GetVisibleProperties(SerializedProperty property)
    {
        List<SerializedProperty> fields = new();
        Add(fields, property, "name");

        SerializedProperty type = property.FindPropertyRelative("type");
        if (type == null) return fields;
        fields.Add(type);

        Add(fields, property, "activationRange");
        Add(fields, property, "damage");
        Add(fields, property, "knockbackForce");
        Add(fields, property, "cooldown");
        Add(fields, property, "animatorTrigger");

        switch ((EnemyUniversal.AttackType)type.enumValueIndex)
        {
            case EnemyUniversal.AttackType.Melee:
                Add(fields, property, "meleeHitboxDown");
                Add(fields, property, "meleeHitboxLeft");
                Add(fields, property, "meleeHitboxRight");
                Add(fields, property, "meleeHitboxUp");
                SerializedProperty launch = property.FindPropertyRelative("launchForward");
                if (launch != null)
                {
                    fields.Add(launch);
                    if (launch.boolValue)
                    {
                        Add(fields, property, "launchDelay");
                        Add(fields, property, "launchDistance");
                        Add(fields, property, "launchDuration");
                    }
                }
                break;

            case EnemyUniversal.AttackType.Area:
                SerializedProperty areaMode = property.FindPropertyRelative("areaMode");
                if (areaMode != null)
                {
                    fields.Add(areaMode);
                    if ((EnemyUniversal.AreaMode)areaMode.enumValueIndex == EnemyUniversal.AreaMode.Radius)
                    {
                        Add(fields, property, "areaRadius");
                        Add(fields, property, "areaTargetLayers");
                    }
                    else
                    {
                        Add(fields, property, "hitboxes");
                    }
                }
                break;

            case EnemyUniversal.AttackType.Projectile:
                Add(fields, property, "projectilePrefab");
                Add(fields, property, "projectileOrigin");
                Add(fields, property, "projectileSpawnOffset");
                Add(fields, property, "rotateProjectileOffsetWithDirection");
                Add(fields, property, "projectileSpeed");
                break;
        }

        return fields;
    }

    private static void Add(
        ICollection<SerializedProperty> fields,
        SerializedProperty property,
        string relativeName)
    {
        SerializedProperty child = property.FindPropertyRelative(relativeName);
        if (child != null) fields.Add(child);
    }
}
