// Resolves D-014: Obtain objectives support two distinct, explicitly-configured semantics instead
// of one implicit rule (see TutorialAndQuestProgression.md "Obtain co hai lua chon").
public enum ObtainObjectiveMode
{
    // Counter increases by every item picked up via InventoryItemAdded; never decreases when the
    // item is later consumed/equipped/sold.
    CountAcquired,

    // Not counter-based -- checked against live inventory possession (>= targetCount) whenever a
    // matching InventoryItemAdded event fires for the current objective, and again at turn-in.
    RequirePossession
}
