using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UnifiedGameplayHudControllerPlayModeTests
{
    [Test]
    public void Bind_UpdatesHealthStaminaExperienceAndLevel()
    {
        GameObject statObject = new("UnifiedHudStat");
        GameObject hudObject = new("UnifiedHud");
        try
        {
            PlayerStat stat = statObject.AddComponent<PlayerStat>();
            UnifiedGameplayHudController controller = hudObject.AddComponent<UnifiedGameplayHudController>();
            Image health = CreateImage("Health", hudObject.transform);
            Image stamina = CreateImage("Stamina", hudObject.transform);
            Image experience = CreateImage("Experience", hudObject.transform);
            TMP_Text experienceText = CreateText("ExperienceText", hudObject.transform);
            TMP_Text levelText = CreateText("LevelText", hudObject.transform);
            SetPrivate(controller, "_healthFill", health);
            SetPrivate(controller, "_staminaFill", stamina);
            SetPrivate(controller, "_experienceFill", experience);
            SetPrivate(controller, "_experienceText", experienceText);
            SetPrivate(controller, "_levelText", levelText);

            controller.Bind(stat);
            stat.AddExperience(40);

            Assert.AreEqual(1f, health.fillAmount, 0.0001f);
            Assert.AreEqual(1f, stamina.fillAmount, 0.0001f);
            Assert.AreEqual(0.4f, experience.fillAmount, 0.0001f);
            Assert.AreEqual("40 / 100", experienceText.text);
            Assert.AreEqual("LV. 1", levelText.text);
        }
        finally
        {
            Object.DestroyImmediate(hudObject);
            Object.DestroyImmediate(statObject);
        }
    }

    private static Image CreateImage(string name, Transform parent)
    {
        Image image = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(parent, false);
        return image;
    }

    private static TMP_Text CreateText(string name, Transform parent)
    {
        TMP_Text text = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
        text.transform.SetParent(parent, false);
        return text;
    }

    private static void SetPrivate(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(target, value);
    }
}
