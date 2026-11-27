using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class BossTeleportPanelTests
{
    [UnityTest]
    public IEnumerator ShowsArtworkAndRebuildsForTheReturnDestination()
    {
        var host = new GameObject("Boss teleport test");
        var manager = GameStateManager.Instance;
        var previous = manager.Current;
        manager.ReplaceState(GameState.Playing);
        try
        {
            var ui = host.AddComponent<BossTeleportSelectUI>();
            var earth = new TeleportDestination { id = "boss.earth", displayName = "Runic Golem" };
            var water = new TeleportDestination { id = "boss.water", displayName = "Wryshell Warden" };
            var wind = new TeleportDestination { id = "boss.wind", displayName = "Galewing Sentinent" };
            var returnIcon = Resources.Load<Sprite>("UI/BossTeleport/ruined_temple_portrait_v1");
            Assert.NotNull(returnIcon);
            var temple = new TeleportDestination { id = "return.mapduy", displayName = "Ruined Temple", icon = returnIcon };

            Assert.IsTrue(ui.Open(new[] { earth, water, wind }, _ => Assert.Fail("Cancel must not confirm")));
            yield return null;

            var frame = host.transform.Find("BossTeleportSelectCanvas/PanelFrame");
            var panel = frame.Find("Panel");
            var cards = panel.Find("Cards");
            var confirm = panel.Find("Confirm").GetComponent<Button>();
            Assert.AreEqual("boss_teleport_board_v1", frame.GetComponent<Image>().sprite.name);
            Assert.AreEqual(3, cards.childCount);
            AssertButtonsInside((RectTransform)frame, panel);
            Assert.IsFalse(confirm.interactable);
            Assert.AreEqual("boss_destination_card_v1", cards.GetChild(0).GetComponent<Image>().sprite.name);
            panel.Find("Cancel").GetComponent<Button>().onClick.Invoke();
            Assert.IsFalse(ui.IsOpen);

            TeleportDestination chosen = null;
            Assert.IsTrue(ui.Open(new[] { temple }, destination => chosen = destination, "LEAVE THE ARENA"));
            yield return null;

            Assert.AreEqual(1, cards.childCount);
            AssertButtonsInside((RectTransform)frame, panel);
            Assert.AreEqual(620f, ((RectTransform)frame).rect.width);
            var card = cards.GetChild(0);
            Assert.AreSame(returnIcon, card.Find("Content/IconBack/Icon").GetComponent<Image>().sprite);
            card.GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(card.GetComponent<Outline>().enabled);
            Assert.IsTrue(confirm.interactable);
            confirm.onClick.Invoke();
            Assert.AreSame(temple, chosen);
            Assert.IsFalse(ui.IsOpen);
            Assert.AreEqual(GameState.Playing, manager.CurrentState);
        }
        finally
        {
            host.GetComponent<BossTeleportSelectUI>()?.Close();
            manager.ReplaceState(previous.State, previous.MenuPage);
            Object.Destroy(host);
        }
    }

    private static void AssertButtonsInside(RectTransform frame, Transform panel)
    {
        foreach (string name in new[] { "Close", "Cancel", "Confirm" })
        {
            Bounds button = RectTransformUtility.CalculateRelativeRectTransformBounds(frame, panel.Find(name));
            Assert.GreaterOrEqual(button.min.x, frame.rect.xMin + 40f, name);
            Assert.LessOrEqual(button.max.x, frame.rect.xMax - 40f, name);
            Assert.GreaterOrEqual(button.min.y, frame.rect.yMin + 50f, name);
            Assert.LessOrEqual(button.max.y, frame.rect.yMax - 50f, name);
        }
    }
}
