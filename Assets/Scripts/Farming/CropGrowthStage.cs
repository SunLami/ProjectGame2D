using System;
using UnityEngine;

[Serializable]
public sealed class CropGrowthStage
{
    public Sprite sprite;
    [Min(0.1f)] public float durationSeconds = 10f;
}
