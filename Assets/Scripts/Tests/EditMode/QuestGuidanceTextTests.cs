using NUnit.Framework;
using UnityEditor;

/// <summary>Guards the quest guidance copy that TutorialOverlayRoot/QuestTracker render with the
/// DigitalDisco pixel font: the tutorial banner text box fits three lines (~115 characters, measured
/// 2026-11-25 with TMP GetPreferredValues at 205x38), and the font has no U+00B7 middle dot.</summary>
public sealed class QuestGuidanceTextTests
{
    private const int MaxControlHintLength = 115;

    [Test]
    public void QuestControlHints_FitTheTutorialBanner_AndUseSupportedGlyphs()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:QuestDefinition"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            QuestDefinition quest = AssetDatabase.LoadAssetAtPath<QuestDefinition>(path);
            Assert.IsFalse(quest.DisplayName != null && quest.DisplayName.Contains("·"), $"{path}: title uses a glyph missing from DigitalDisco.");
            foreach (QuestObjectiveDefinition objective in quest.Objectives)
            {
                string hint = objective.ControlHint;
                if (string.IsNullOrEmpty(hint))
                    continue;
                Assert.LessOrEqual(hint.Length, MaxControlHintLength, $"{path}: control hint overflows the tutorial banner: \"{hint}\"");
                Assert.IsFalse(hint.Contains("·"), $"{path}: hint uses a glyph missing from DigitalDisco.");
            }
        }
    }
}
