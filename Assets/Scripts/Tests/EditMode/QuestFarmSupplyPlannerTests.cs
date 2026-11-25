using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

public sealed class QuestFarmSupplyPlannerTests
{
    private static QuestObjectiveDefinition Objective(QuestObjectiveType type, string targetId, int count)
    {
        var objective = new QuestObjectiveDefinition();
        Set(objective, "_type", type);
        Set(objective, "_targetId", targetId);
        Set(objective, "_targetCount", count);
        return objective;
    }

    private static void Set(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

    // Mirrors Quest_Act1BackToFields: harvest 15 Carrot/Grape/Lemon, then replant 5 of each.
    private static List<QuestObjectiveDefinition> BackToFields() => new()
    {
        Objective(QuestObjectiveType.Gather, "crop.carrot", 15),
        Objective(QuestObjectiveType.Gather, "crop.grape", 15),
        Objective(QuestObjectiveType.Gather, "crop.lemon", 15),
        Objective(QuestObjectiveType.Replant, "crop.carrot", 5),
        Objective(QuestObjectiveType.Replant, "crop.grape", 5),
        Objective(QuestObjectiveType.Replant, "crop.lemon", 5),
    };

    [Test]
    public void RemainingNeeds_AggregatesHarvestAndReplantFromCurrentObjective()
    {
        List<QuestFarmSupplyPlanner.CropNeed> needs =
            QuestFarmSupplyPlanner.RemainingNeeds(BackToFields(), 1, new[] { 15, 6, 0, 0, 0, 0 });

        Assert.AreEqual(3, needs.Count);
        Assert.AreEqual("crop.carrot", needs[2].CropId, "Carrot harvest is done; only its replant remains.");
        Assert.AreEqual(0, needs[2].HarvestRemaining);
        Assert.AreEqual(5, needs[2].ReplantRemaining);
        Assert.AreEqual("crop.grape", needs[0].CropId);
        Assert.AreEqual(9, needs[0].HarvestRemaining);
        Assert.AreEqual(5, needs[0].ReplantRemaining);
    }

    [Test]
    public void PlanCrop_QuestStart_PlacesFiveRipePlotsAndFiveSeeds()
    {
        var need = new QuestFarmSupplyPlanner.CropNeed("crop.carrot", 15, 5);
        QuestFarmSupplyPlanner.CropSupply plan = QuestFarmSupplyPlanner.PlanCrop(need, 3, 0, 0, 21, allowMature: true);

        Assert.AreEqual(5, plan.MaturePlots);
        Assert.AreEqual(5, plan.Seeds);
    }

    [Test]
    public void PlanCrop_SecondPassWithSupplyAlreadyInPlace_AddsNothing()
    {
        var need = new QuestFarmSupplyPlanner.CropNeed("crop.carrot", 15, 5);
        QuestFarmSupplyPlanner.CropSupply plan = QuestFarmSupplyPlanner.PlanCrop(need, 3, 5, 5, 6, allowMature: true);

        Assert.AreEqual(0, plan.MaturePlots);
        Assert.AreEqual(0, plan.Seeds, "Re-running (save/load, every event) must be idempotent.");
    }

    [Test]
    public void PlanCrop_WrongOrderHarvestMidQuest_RecoversWithSeedsNotRipeCrops()
    {
        // Player harvested all 5 grape plots while the carrot step was current: none counted.
        var need = new QuestFarmSupplyPlanner.CropNeed("crop.grape", 15, 5);
        QuestFarmSupplyPlanner.CropSupply plan = QuestFarmSupplyPlanner.PlanCrop(need, 3, 0, 5, 10, allowMature: false);

        Assert.AreEqual(0, plan.MaturePlots);
        Assert.AreEqual(5, plan.Seeds, "5 more seeds regrow the 15 grapes; the 5 held stay for the replant step.");
    }

    [Test]
    public void PlanCrop_SavedBeforeSupplyExisted_RestoresRipeCropsOnLoad()
    {
        var need = new QuestFarmSupplyPlanner.CropNeed("crop.lemon", 15, 5);
        QuestFarmSupplyPlanner.CropSupply plan = QuestFarmSupplyPlanner.PlanCrop(need, 3, 0, 0, 21, allowMature: true);

        Assert.AreEqual(5, plan.MaturePlots);
        Assert.AreEqual(5, plan.Seeds);
    }

    [Test]
    public void PlanCrop_NotEnoughEmptyPlots_FallsBackToSeeds()
    {
        var need = new QuestFarmSupplyPlanner.CropNeed("crop.lemon", 15, 5);
        QuestFarmSupplyPlanner.CropSupply plan = QuestFarmSupplyPlanner.PlanCrop(need, 3, 0, 0, 2, allowMature: true);

        Assert.AreEqual(2, plan.MaturePlots);
        Assert.AreEqual(8, plan.Seeds);
    }

    [Test]
    public void PlanCrop_ReplantOnly_CountsHeldSeeds()
    {
        var need = new QuestFarmSupplyPlanner.CropNeed("crop.carrot", 0, 5);

        Assert.AreEqual(2, QuestFarmSupplyPlanner.PlanCrop(need, 3, 0, 3, 21, allowMature: true).Seeds);
        Assert.AreEqual(0, QuestFarmSupplyPlanner.PlanCrop(need, 3, 0, 9, 21, allowMature: true).Seeds);
    }

    [Test]
    public void PlanCrop_QuestStart_PlacesRipePlotsEvenWhenPlayerCarriesSeeds()
    {
        var need = new QuestFarmSupplyPlanner.CropNeed("crop.carrot", 15, 5);

        QuestFarmSupplyPlanner.CropSupply start = QuestFarmSupplyPlanner.PlanCrop(need, 3, 0, 24, 21, true, questJustStarted: true);
        Assert.AreEqual(5, start.MaturePlots, "The authored ripe field appears regardless of carried seeds.");
        Assert.AreEqual(0, start.Seeds);

        QuestFarmSupplyPlanner.CropSupply restore = QuestFarmSupplyPlanner.PlanCrop(need, 3, 0, 24, 21, true);
        Assert.AreEqual(0, restore.MaturePlots, "On restore, carried seeds already cover the need.");
    }
}
