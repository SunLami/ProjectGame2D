using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerStaminaTests
{
    [Test]
    public void TickStamina_DrainsAndRegeneratesWithinBounds()
    {
        GameObject playerObject = new GameObject("PlayerStat_Stamina_Test");
        PlayerStat stat = playerObject.AddComponent<PlayerStat>();
        SetPrivate(stat, "_stamina", stat.MaxStamina);

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
        SetPrivate(stat, "_stamina", stat.MaxStamina);

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

    [Test]
    public void PlayerHUD_StartRefreshesStaminaAfterPlayerAwakeOrdering()
    {
        GameObject playerObject = new GameObject("PlayerStat_HUD_Lifecycle_Test");
        PlayerStat stat = playerObject.AddComponent<PlayerStat>();
        GameObject hudObject = new GameObject("PlayerHUD_Lifecycle_Test");
        PlayerHUDController hud = hudObject.AddComponent<PlayerHUDController>();
        Image staminaFill = CreateImage("StaminaFill_Lifecycle_Test");

        try
        {
            SetPrivate(hud, "_staminaFill", staminaFill);
            SetPrivate(stat, "_stamina", 0f);
            hud.Bind(stat);
            Assert.AreEqual(0f, staminaFill.fillAmount, 0.001f);

            SetPrivate(stat, "_stamina", stat.MaxStamina);
            InvokePrivate(hud, "Start");

            Assert.AreEqual(1f, staminaFill.fillAmount, 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(staminaFill.gameObject);
            Object.DestroyImmediate(hudObject);
            Object.DestroyImmediate(playerObject);
        }
    }

    private static Image CreateImage(string name)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        return imageObject.GetComponent<Image>();
    }

    private static void SetPrivate<T>(object target, string fieldName, T value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field, $"Missing private field {fieldName}.");
        field.SetValue(target, value);
    }

    private static void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"Missing private method {methodName}.");
        method.Invoke(target, null);
    }
}
