using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class SkillPanelPresentationTests
{
    private GameObject Prefab => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/SkillPanel.prefab");

    [Test]
    public void Prefab_HasTwelveIndependentSkillButtonsAndApprovedCosts()
    {
        Assert.IsNotNull(Prefab);
        var ui = Prefab.GetComponent<SkillPanelUI>();
        Assert.IsNotNull(ui);
        var data = new SerializedObject(ui);
        var entries = data.FindProperty("_entries");
        Assert.AreEqual(12, entries.arraySize);
        for (int i = 0; i < 12; i++)
        {
            var entry = entries.GetArrayElementAtIndex(i);
            Assert.AreEqual(i % 4 + 2, entry.FindPropertyRelative("learningCost").intValue);
            Assert.IsNotNull(entry.FindPropertyRelative("button").objectReferenceValue);
            Assert.IsNotEmpty(entry.FindPropertyRelative("description").stringValue);
        }
        foreach (string field in new[] { "_window", "_tooltip", "_title", "_description", "_cost", "_close" })
            Assert.IsNotNull(data.FindProperty(field).objectReferenceValue, field);
    }

    [Test]
    public void Prefab_HasFourLoadoutSocketsAndNoMissingScripts()
    {
        Assert.IsNotNull(Prefab);
        var content = Prefab.transform.Find("Window/Composition/Content");
        for (int i = 1; i <= 4; i++) Assert.IsNotNull(content.Find("Loadout_" + i));
        Assert.IsNull(content.Find("Loadout_5"));
        foreach (var component in Prefab.GetComponentsInChildren<Component>(true)) Assert.IsNotNull(component);
    }

    [Test]
    public void SkillMenuPage_PreservesExistingSerializedOrdinals()
    {
        Assert.AreEqual(1, (int)GameplayMenuPage.Inventory);
        Assert.AreEqual(8, (int)GameplayMenuPage.Settings);
        Assert.AreEqual(9, (int)GameplayMenuPage.Skills);
    }
}
