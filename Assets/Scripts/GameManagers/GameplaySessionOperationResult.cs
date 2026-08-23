/// <summary>Specific reason a GameplaySessionController operation did not succeed, paired with a
/// user-facing message on OnOperationFailed.</summary>
public enum GameplaySessionOperationResult
{
    Success,
    NoActiveSession,
    AlreadyBusy,
    SlotNotValid,
    ReadFailed,
    WriteFailed,
    TransitionFailed,
    InvalidSlot
}
