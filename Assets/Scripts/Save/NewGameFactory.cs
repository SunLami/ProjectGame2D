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
            },
            // Empty on creation -- the restore-ready path seeds the starting loadout live (once,
            // for this NewGame session) and re-captures it into the just-written initial save
            // rather than baking a loadout into this factory.
            inventory = new InventorySaveData(),
            equipment = new EquipmentSaveData(),
            // null currentStepId + completed=false means "start at the first step" once restored
            // against whatever TutorialDefinition is wired in the gameplay scene.
            tutorial = new TutorialSaveData(),
            // Empty -- nothing accepted yet. Locked/Available are derived from prerequisites at
            // restore, not baked into this default snapshot.
            quests = new QuestSaveData(),
            // Empty -- every persistent world object starts at its scene-authored default state.
            world = new WorldSaveData()
        };
    }
}
