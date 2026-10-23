using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class SkillProgressionTests
{
    private SkillCatalog Catalog => AssetDatabase.LoadAssetAtPath<SkillCatalog>("Assets/Resources/Skills/PlayerSkillCatalog.asset");
    private const string Water = "skill.water.spiral_bolt";
    private const string Earth = "skill.earth.rock_bolt";

    [Test]
    public void PointBudget_CapsAwardsAndLegacyRestoreAtUnlearnedCost()
    {
        var state = new SkillProgressionState(Catalog);
        Assert.AreEqual(42, Catalog.RemainingLearningCost(_ => false));
        state.AwardLevel(101);
        Assert.AreEqual(42, state.Points);
        Assert.IsTrue(state.Learn(Catalog, Water));
        state.AwardLevel(102);
        Assert.AreEqual(40, state.Points);
        var saved = state.Capture();
        saved.points = 999;
        state.Restore(saved, 102);
        Assert.AreEqual(40, state.Points);
        Assert.AreEqual(999, saved.points, "Restore must not mutate the source snapshot.");
        var definitions = new SerializedObject(Catalog).FindProperty("_definitions");
        for (int i = 0; i < definitions.arraySize; i++)
        {
            var id = definitions.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue;
            if (!state.IsLearned(id)) Assert.IsTrue(state.Learn(Catalog, id));
        }
        Assert.AreEqual(0, state.Points);
        state.AwardLevel(200);
        Assert.AreEqual(0, state.Points);
        state.Restore(state.Capture(), 200);
        Assert.AreEqual(0, state.Points);
    }

    [Test]
    public void LevelAwards_ArePerLevelAndIdempotent()
    {
        var state=new SkillProgressionState(Catalog);state.AwardLevel(5);state.AwardLevel(5);
        Assert.AreEqual(4,state.Points);state.AwardLevel(6);Assert.AreEqual(5,state.Points);
        var saved=state.Capture();state.Restore(saved,6);state.AwardLevel(6);Assert.AreEqual(5,state.Points);
    }
    [Test]
    public void Learn_IsAtomicAndRejectsRepeatedUnknownAndInsufficientRequests()
    {
        Catalog.Validate();var state=new SkillProgressionState(Catalog);
        Assert.IsFalse(state.Learn(Catalog,Water));state.AwardLevel(4);
        Assert.IsTrue(state.Learn(Catalog,Water));Assert.AreEqual(1,state.Points);
        Assert.IsFalse(state.Learn(Catalog,Water));Assert.IsFalse(state.Learn(Catalog,Earth));
        Assert.IsFalse(state.Learn(Catalog,"missing"));Assert.AreEqual(1,state.Points);
    }
    [Test]
    public void Equip_MovesAndReplacesWithoutUnlearning()
    {
        var state=new SkillProgressionState(Catalog);Assert.IsFalse(state.Equip(Catalog,0,Water));
        state.AwardLevel(8);state.Learn(Catalog,Water);state.Learn(Catalog,Earth);
        Assert.IsTrue(state.Equip(Catalog,0,Water));Assert.IsTrue(state.Equip(Catalog,2,Water));
        Assert.IsNull(state.Equipped(0));Assert.AreEqual(Water,state.Equipped(2));
        state.Equip(Catalog,2,Earth);Assert.IsTrue(state.IsLearned(Water));
        Assert.AreEqual(Earth,state.Equipped(2));Assert.IsFalse(state.Equip(Catalog,4,Water));
    }
    [Test]
    public void Save_RoundTripsCopiesAndResetsAcrossCharacters()
    {
        var state=new SkillProgressionState(Catalog);state.AwardLevel(8);state.Learn(Catalog,Water);state.Equip(Catalog,0,Water);
        var dto=JsonUtility.FromJson<SkillProgressionSaveData>(JsonUtility.ToJson(state.Capture()));
        var restored=new SkillProgressionState(Catalog);restored.Restore(dto,8);
        Assert.AreEqual(5,restored.Points);Assert.AreEqual(Water,restored.Equipped(0));
        dto.learnedIds.Clear();dto.loadout[0]=null;Assert.IsTrue(restored.IsLearned(Water));
        restored.Restore(new SkillProgressionSaveData(),1);Assert.AreEqual(0,restored.Points);
        Assert.IsFalse(restored.IsLearned(Water));Assert.IsNull(restored.Equipped(0));
    }
    [Test]
    public void Migration_BackfillsExactlyOnceAndNeverMutatesInventory()
    {
        var save=new GameSaveData{saveVersion=12,player=new PlayerSaveData{level=7},inventory=new InventorySaveData()};
        var inventory=save.inventory;SaveMigration.Migrate(save);Assert.AreEqual(13,save.saveVersion);
        Assert.AreEqual(6,save.player.skills.points);save.player.skills.points=2;SaveMigration.Migrate(save);
        Assert.AreEqual(2,save.player.skills.points);Assert.AreSame(inventory,save.inventory);
        Assert.AreEqual(0,NewGameFactory.CreateDefault().player.skills.points);
    }
    [Test]
    public void Restore_NormalizesInvalidDataAndRetainsMissingContentForRecovery()
    {
        var dto=new SkillProgressionSaveData{points=-5,learnedIds=new(){Water,Water,"missing",""},loadout=new[]{Water,Water,"unlearned","missing","extra"}};
        var state=new SkillProgressionState(Catalog);state.Restore(dto,5);Assert.AreEqual(0,state.Points);
        Assert.AreEqual(Water,state.Equipped(0));Assert.IsNull(state.Equipped(1));Assert.IsNull(state.Equipped(2));
        Assert.AreEqual("missing",state.Equipped(3));Assert.IsTrue(state.IsLearned("missing"));
        Assert.IsFalse(state.Equip(Catalog,1,"missing"));state.AwardLevel(5);Assert.AreEqual(0,state.Points);
    }
    [Test]
    public void LegacyJson_MissingSkillsStillBackfillsDespiteUnityDefaultObjects()
    {
        var save=JsonUtility.FromJson<GameSaveData>("{\"saveVersion\":12,\"saveId\":\"legacy\",\"player\":{\"level\":9}}");
        SaveMigration.Migrate(save);Assert.AreEqual(8,save.player.skills.points);
        save.player.skills.points=3;SaveMigration.Migrate(save);Assert.AreEqual(3,save.player.skills.points);
    }
}
