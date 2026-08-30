using NUnit.Framework;
using UnityEngine;

public sealed class PlayerSaveCapturePlayModeTests
{
    [Test]
    public void Capture_ReadsLiveStatAndTransform()
    {
        GameObject go = new("CaptureFixture");
        try
        {
            PlayerStat stat = go.AddComponent<PlayerStat>();
            stat.RestoreProgression(level: 4, currentExperience: 33, health: 12f);
            go.transform.position = new Vector3(3.5f, -2.25f, 0f);

            PlayerSaveData snapshot = PlayerSaveCapture.Capture(stat, go.transform, "area.town", "spawn.town.gate");

            Assert.AreEqual(4, snapshot.level);
            Assert.AreEqual(33, snapshot.currentExperience);
            Assert.AreEqual(12f, snapshot.health, 0.001f);
            Assert.AreEqual("area.town", snapshot.location.areaId);
            Assert.AreEqual("spawn.town.gate", snapshot.location.fallbackSpawnId);
            Assert.AreEqual(3.5f, snapshot.location.positionX, 0.001f);
            Assert.AreEqual(-2.25f, snapshot.location.positionY, 0.001f);
            Assert.IsTrue(snapshot.location.HasSavedPosition);
        }
        finally
        {
            Object.Destroy(go);
        }
    }
}
