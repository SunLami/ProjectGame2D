using System;

/// <summary>Health &lt; 0 means "use current max health" (fresh New Game character).</summary>
[Serializable]
public sealed class PlayerSaveData
{
    public int level = 1;
    public int currentExperience;
    public float health = -1f;
    public PlayerLocationSaveData location = new();
}
