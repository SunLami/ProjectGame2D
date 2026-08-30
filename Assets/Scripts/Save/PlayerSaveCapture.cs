using UnityEngine;

/// <summary>
/// The only sanctioned way to turn live PlayerStat/Transform state into a PlayerSaveData
/// snapshot. UI and other callers must go through this instead of constructing/mutating
/// GameSaveData fields directly.
///
/// This is capture only -- it does not decide when to save. Wiring it into "Save Game" or an
/// automatic Return-to-MainMenu save belongs to Phase 9 (D-017 dirty-session confirm); calling
/// it here would silently overwrite a save without the player's consent.
///
/// areaId/fallbackSpawnId are passed in rather than looked up because there is no world/area
/// service yet (Phase 8); callers currently carry these forward from the active session's
/// existing save data.
/// </summary>
public static class PlayerSaveCapture
{
    public static PlayerSaveData Capture(PlayerStat stat, Transform playerTransform, string areaId, string fallbackSpawnId)
    {
        return new PlayerSaveData
        {
            level = stat.Level,
            currentExperience = stat.CurrentExperience,
            health = stat.Health,
            location = new PlayerLocationSaveData
            {
                areaId = areaId,
                positionX = playerTransform.position.x,
                positionY = playerTransform.position.y,
                fallbackSpawnId = fallbackSpawnId
            }
        };
    }
}
