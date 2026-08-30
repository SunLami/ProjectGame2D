/// <summary>Outcome of a write/delete operation against ISaveSlotRepository.</summary>
public readonly struct SaveOperationResult
{
    private SaveOperationResult(bool success, string errorMessage)
    {
        Success = success;
        ErrorMessage = errorMessage;
    }

    public bool Success { get; }
    public string ErrorMessage { get; }

    public static SaveOperationResult Ok() => new(true, null);
    public static SaveOperationResult Failure(string errorMessage) => new(false, errorMessage);
}
