using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class PlayerExperienceBarControllerPlayModeTests
{
    [UnityTest]
    public IEnumerator Bind_AndExperienceChange_UpdateFillAndText()
    {
        GameObject statObject = new("ExperienceBarPlayerStat");
        GameObject uiObject = new("ExperienceBarUI", typeof(RectTransform));

        try
        {
            PlayerStat stat = statObject.AddComponent<PlayerStat>();
            Image fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
                .GetComponent<Image>();
            fill.transform.SetParent(uiObject.transform, false);
            TMP_Text text = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI))
                .GetComponent<TMP_Text>();
            text.transform.SetParent(uiObject.transform, false);

            PlayerExperienceBarController controller = uiObject.AddComponent<PlayerExperienceBarController>();
            SetPrivate(controller, "_experienceFill", fill);
            SetPrivate(controller, "_experienceText", text);
            controller.Bind(stat);

            Assert.AreEqual(0f, fill.fillAmount, 0.0001f);
            Assert.AreEqual("0 / 100", text.text);

            stat.AddExperience(40);

            Assert.AreEqual(0.4f, fill.fillAmount, 0.0001f);
            Assert.AreEqual("40 / 100", text.text);
        }
        finally
        {
            Object.Destroy(uiObject);
            Object.Destroy(statObject);
        }

        yield return null;
    }

    private static void SetPrivate(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field, $"Missing field {fieldName}");
        field.SetValue(target, value);
    }
}
