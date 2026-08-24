using System;

/// <summary>
/// Saved player location. A NaN position means "no saved position yet" — restore should resolve
/// fallbackSpawnId via SpawnRegistry instead of trusting positionX/positionY.
/// </summary>
[Serializable]
public sealed class PlayerLocationSaveData
{
    public string sceneId;
    public string areaId;
    public float positionX = float.NaN;
    public float positionY = float.NaN;
    public string fallbackSpawnId;

    public bool HasSavedPosition => !float.IsNaN(positionX) && !float.IsNaN(positionY);
}
