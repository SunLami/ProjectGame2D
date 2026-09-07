using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class QuestTrackerPreviewMenu
{
    private const string MenuPath = "Tools/ProjectGame2D/UI/Preview Quest Tracker (Play Mode)";

    [MenuItem(MenuPath)]
    public static void Preview()
    {
        QuestTrackerUI tracker = Object.FindAnyObjectByType<QuestTrackerUI>(FindObjectsInactive.Include);
        if (tracker == null || !tracker.IsInitialized)
        {
            Debug.LogError("QuestTracker preview requires Play Mode with GameplayUIRoot loaded.");
            return;
        }

        var quests = new List<TrackedQuestView>
        {
            View("preview.main.01", "The Crown's Missing Heir", QuestCategory.Main),
            View("preview.main.02", "Shadows Over Orynthals", QuestCategory.Main),
            View("preview.side.01", "Potion Supply", QuestCategory.Side),
            View("preview.side.02", "Wayward Scouts", QuestCategory.Side),
            View("preview.side.03", "The Old Forest Shrine", QuestCategory.Side),
            View("preview.side.04", "A Blacksmith's Favor", QuestCategory.Side),
            View("preview.daily.01", "Gather Medicinal Leaves", QuestCategory.Daily),
            View("preview.daily.02", "Clear the Slime Nest", QuestCategory.Daily),
            View("preview.daily.03", "Deliver Town Supplies", QuestCategory.Daily)
        };
        quests.Sort(QuestTrackerOrdering.Compare);

        tracker.gameObject.SetActive(true);
        tracker.SetQuests(quests);
        Debug.Log("QuestTracker preview loaded. Exit Play Mode to restore live quest presentation.");
    }

    [MenuItem(MenuPath, true)]
    private static bool ValidatePreview() => EditorApplication.isPlaying;

    private static TrackedQuestView View(string id, string title, QuestCategory category) =>
        new(id, title, "Track the current objective and return when it is complete. 0 / 5", category);
}
