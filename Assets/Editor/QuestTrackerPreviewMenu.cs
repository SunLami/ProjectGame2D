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
            View("preview.main.01", "The Crown's Missing Heir", QuestCategory.Main, QuestObjectiveType.Kill, "Defeat forest wolves", 3, 5),
            View("preview.side.01", "Potion Supply", QuestCategory.Side, QuestObjectiveType.Gather, "Collect medicinal herbs", 8, 10),
            View("preview.daily.01", "Village Check-In", QuestCategory.Daily, QuestObjectiveType.Talk, "Talk to Elder Rowan", 0, 1)
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

    private static TrackedQuestView View(string id, string title, QuestCategory category,
        QuestObjectiveType type, string objective, int current, int target) =>
        new(id, title, objective, category, type, current, target, false);
}
