using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class PlayerHUDApprovedPlayModeTests
{
    [UnityTest]
    public IEnumerator SourcePrefabBindsRealResourcesAndReopensWithoutStaleManaOrExperience()
    {
        PlayerStat stat = PlayerStat.Instance;
        GameObject owner = null;
        if (stat == null) { owner = new GameObject("HUDTestPlayer"); stat = owner.AddComponent<PlayerStat>(); }
        float health = stat.Health, mana = stat.Mana;
        int level = stat.Level, xp = stat.CurrentExperience;
        var canvasObject = new GameObject("HUDTestCanvas", typeof(RectTransform), typeof(Canvas));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var root = Object.Instantiate(Resources.Load<GameObject>("UI/Gameplay/HUD/PlayerHUD"), canvasObject.transform);
        try
        {
            yield return null;
            var hud = root.GetComponent<PlayerHUDController>();
            var exp = root.GetComponent<PlayerExperienceBarController>();
            hud.Bind(stat); exp.Bind(stat);
            stat.RestoreHealth(-1); stat.RestoreMana(stat.MaxMana);
            Assert.IsTrue(stat.TryConsumeMana(40));
            var images = root.GetComponentsInChildren<Image>(true);
            Assert.That(images.Single(i => i.name == "ManaFill").fillAmount, Is.EqualTo(.6f).Within(.001f));
            Assert.That(images.Single(i => i.name == "HealthFill").fillAmount, Is.EqualTo(1f).Within(.001f));
            stat.RestoreProgression(1, 40);
            Assert.That(images.Single(i => i.name == "ExperienceFill").fillAmount, Is.EqualTo(.4f).Within(.001f));
            Assert.AreEqual("EXP  40 / 100", root.GetComponentsInChildren<TMP_Text>(true).Single(t => t.name == "ExperienceText").text);
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.name == "Quantity") text.text = "12";
                text.ForceMeshUpdate();
                Assert.IsFalse(text.isTextOverflowing, text.name + " must fit its authored rectangle");
                Assert.Greater(text.textInfo.characterCount, 0, text.name + " must render glyphs");
            }
            root.SetActive(false);
            Assert.IsTrue(stat.TryConsumeMana(20));
            stat.RestoreProgression(1, 70);
            root.SetActive(true);
            yield return null;
            Assert.That(images.Single(i => i.name == "ManaFill").fillAmount, Is.EqualTo(.4f).Within(.001f));
            Assert.That(images.Single(i => i.name == "ExperienceFill").fillAmount, Is.EqualTo(.7f).Within(.001f));
            Assert.AreEqual(4, root.GetComponentsInChildren<QuickBarSlotUI>(true).Length);
        }
        finally
        {
            Object.DestroyImmediate(canvasObject);
            if (owner != null) Object.DestroyImmediate(owner);
            else
            {
                stat.RestoreProgression(level, xp); stat.RestoreHealth(health);
                typeof(PlayerStat).GetField("_mana", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(stat, mana);
                stat.RestoreMana(0);
            }
        }
    }
}
