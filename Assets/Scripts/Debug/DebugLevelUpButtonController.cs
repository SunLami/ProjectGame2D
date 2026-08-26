using UnityEngine;

public sealed class DebugLevelUpButtonController : MonoBehaviour
{
    public void LevelUpOne()
    {
        PlayerStat playerStat = PlayerStat.Instance != null
            ? PlayerStat.Instance
            : FindAnyObjectByType<PlayerStat>();
        if (playerStat == null)
            return;

        int remainingExperience = Mathf.Max(1,
            playerStat.ExperienceToNextLevel - playerStat.CurrentExperience);
        playerStat.AddExperience(remainingExperience);
    }
}
