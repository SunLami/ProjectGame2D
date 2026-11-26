using UnityEngine;

public static class EncounterLevel
{
    public static int Roll(Vector2Int range)
    {
        int minimum = Mathf.Max(1, range.x);
        int maximum = Mathf.Max(minimum, range.y);
        return minimum == maximum ? minimum : Random.Range(minimum, maximum + 1);
    }

    public static float HealthMultiplier(int level) => 1f + 0.10f * (level - 1);
    public static float DamageMultiplier(int level) => 1f + 0.05f * (level - 1);
}
