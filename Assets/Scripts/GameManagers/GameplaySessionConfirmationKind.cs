/// <summary>Which 3-way confirm popup (Save and X / X Without Saving / Cancel) the UI must show,
/// fired by GameplaySessionController.OnConfirmationRequired when the active session is dirty.</summary>
public enum GameplaySessionConfirmationKind
{
    ReturnToMainMenu,
    Quit
}
