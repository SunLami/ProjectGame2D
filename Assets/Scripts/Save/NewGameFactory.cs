using System;

/// <summary>Builds the default GameSaveData for a brand-new character. No scene/UI dependency.</summary>
public static class NewGameFactory
{
    public const string TutorialAreaId = "area.tutorial";
    public const string TutorialStartSpawnId = "spawn.tutorial.start";

    public static GameSaveData CreateDefault()
    {
        return new GameSaveData
        {
            saveId = Guid.NewGuid().ToString("N"),
            totalPlayTimeSeconds = 0,
            player = new PlayerSaveData
            {
                level = 1,
                currentExperience = 0,
                health = -1f,
                location = new PlayerLocationSaveData
                {
                    sceneId = null,
                    areaId = TutorialAreaId,
                    positionX = float.NaN,
                    positionY = float.NaN,
                    fallbackSpawnId = TutorialStartSpawnId
                }
            }
        };
    }
}
