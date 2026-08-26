using NUnit.Framework;
using UnityEngine;

public sealed class GameCursorTargetResolverPlayModeTests
{
    [TestCase("cursor_default")]
    [TestCase("cursor_attack")]
    [TestCase("cursor_talk")]
    [TestCase("cursor_blocked")]
    [TestCase("cursor_interact")]
    [TestCase("cursor_mining")]
    [TestCase("cursor_chopping")]
    [TestCase("cursor_gathering")]
    public void CursorTexture_LoadsFromResourcesAndIsReadable(string resourceName)
    {
        Texture2D texture = Resources.Load<Texture2D>($"UI/Cursors/{resourceName}");

        Assert.IsNotNull(texture);
        Assert.AreEqual(64, texture.width);
        Assert.AreEqual(64, texture.height);
        Assert.IsTrue(texture.isReadable);
    }

    [TestCase(ResourceHarvestType.Mining, GameCursorType.Mining)]
    [TestCase(ResourceHarvestType.Chopping, GameCursorType.Chopping)]
    [TestCase(ResourceHarvestType.Gathering, GameCursorType.Gathering)]
    public void ResourceNode_MapsAuthoredHarvestType(ResourceHarvestType harvestType, GameCursorType expected)
    {
        var go = new GameObject("ResourceCursorFixture");
        try
        {
            ResourceNodeInteractable resource = go.AddComponent<ResourceNodeInteractable>();
            resource.ConfigureForTests("world.resource.test", "resource.test", "item.test", 1, 0f, null, harvestType);

            Assert.IsTrue(GameCursorTargetResolver.TryResolve(resource, out GameCursorTarget target));
            Assert.AreEqual(expected, target.Cursor);
            Assert.IsTrue(target.RequiresRange);
            Assert.IsTrue(target.IsAvailable);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Chest_MapsInteractAndBecomesUnavailableAfterRestore()
    {
        var go = new GameObject("ChestCursorFixture");
        try
        {
            ChestInteractable chest = go.AddComponent<ChestInteractable>();

            Assert.IsTrue(GameCursorTargetResolver.TryResolve(chest, out GameCursorTarget available));
            Assert.AreEqual(GameCursorType.Interact, available.Cursor);
            Assert.IsTrue(available.IsAvailable);

            chest.RestoreState(new WorldObjectState(true, 0));
            Assert.IsTrue(GameCursorTargetResolver.TryResolve(chest, out GameCursorTarget opened));
            Assert.IsFalse(opened.IsAvailable);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Enemy_MapsAttackWithoutRangeRequirement()
    {
        var go = new GameObject("EnemyCursorFixture");
        go.SetActive(false);
        try
        {
            Enemy enemy = go.AddComponent<Enemy>();

            Assert.IsTrue(GameCursorTargetResolver.TryResolve(enemy, out GameCursorTarget target));
            Assert.AreEqual(GameCursorType.Attack, target.Cursor);
            Assert.IsFalse(target.RequiresRange);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void QuestNpc_MapsTalk()
    {
        var go = new GameObject("NpcCursorFixture");
        go.SetActive(false);
        try
        {
            QuestNpcInteractionUI npc = go.AddComponent<QuestNpcInteractionUI>();

            Assert.IsTrue(GameCursorTargetResolver.TryResolve(npc, out GameCursorTarget target));
            Assert.AreEqual(GameCursorType.Talk, target.Cursor);
            Assert.IsTrue(target.RequiresRange);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
