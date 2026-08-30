using System;

// Plain serializable tutorial progress snapshot. currentStepId is null/empty when not started or
// when the definition has no matching step (restore falls back to the first step in that case).
[Serializable]
public sealed class TutorialSaveData
{
    public string currentStepId;
    public bool completed;
}
