using NUnit.Framework;
using UnityEngine;

public sealed class PlayerStaminaTests
{
    [Test]
    public void TickStamina_DrainsAndRegeneratesWithinBounds()
    {
        GameObject playerObject = new GameObject("PlayerStat_Stamina_Test");
        PlayerStat stat = playerObject.AddComponent<PlayerStat>();

        try
        {
            stat.TickStamina(true, 2f);
            Assert.AreEqual(50f, stat.Stamina, 0.001f);

            stat.TickStamina(true, 10f);
            Assert.AreEqual(0f, stat.Stamina, 0.001f);
            Assert.IsFalse(stat.HasStamina);

            stat.TickStamina(false, 1f);
            Assert.AreEqual(18f, stat.Stamina, 0.001f);
            Assert.IsTrue(stat.HasStamina);
        }
        finally
        {
            Object.DestroyImmediate(playerObject);
        }
    }

    [Test]
    public void TryConsumeAttackStamina_ConsumesOnceAndRejectsWhenInsufficient()
    {
        GameObject playerObject = new GameObject("PlayerStat_Attack_Stamina_Test");
        PlayerStat stat = playerObject.AddComponent<PlayerStat>();

        try
        {
            Assert.IsTrue(stat.TryConsumeAttackStamina());
            Assert.AreEqual(85f, stat.Stamina, 0.001f);

            for (int index = 0; index < 5; index++)
                Assert.IsTrue(stat.TryConsumeAttackStamina());

            Assert.AreEqual(10f, stat.Stamina, 0.001f);
            Assert.IsFalse(stat.TryConsumeAttackStamina());
            Assert.AreEqual(10f, stat.Stamina, 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(playerObject);
        }
    }
}
