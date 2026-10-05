using NUnit.Framework;
using UnityEngine;

public sealed class ArenaPolygonTests
{
    // a plus sign: 2 wide arms, 6 long
    private static readonly Vector2[] Plus =
    {
        new Vector2(-1f, 3f), new Vector2(1f, 3f), new Vector2(1f, 1f), new Vector2(3f, 1f), new Vector2(3f, -1f), new Vector2(1f, -1f),
        new Vector2(1f, -3f), new Vector2(-1f, -3f), new Vector2(-1f, -1f), new Vector2(-3f, -1f), new Vector2(-3f, 1f), new Vector2(-1f, 1f),
    };

    [Test]
    public void Contains_CentreAndArmsAreInside_NotchesAreOutside()
    {
        Assert.IsTrue(ArenaPolygon.Contains(Plus, Vector2.zero));
        Assert.IsTrue(ArenaPolygon.Contains(Plus, new Vector2(2.5f, 0f)));
        Assert.IsTrue(ArenaPolygon.Contains(Plus, new Vector2(0f, -2.5f)));
        Assert.IsFalse(ArenaPolygon.Contains(Plus, new Vector2(2f, 2f)));
        Assert.IsFalse(ArenaPolygon.Contains(Plus, new Vector2(5f, 0f)));
    }

    [Test]
    public void Constrain_InsidePointFarFromEdgesIsUnchanged()
    {
        Assert.AreEqual(Vector2.zero, ArenaPolygon.Constrain(Plus, Vector2.zero, 0.4f));
    }

    [Test]
    public void Constrain_OutsidePointEndsInsideByAtLeastTheMargin()
    {
        foreach (Vector2 outside in new[] { new Vector2(2f, 2f), new Vector2(6f, 0f), new Vector2(0f, 5f), new Vector2(-2.5f, -2.5f) })
        {
            Vector2 result = ArenaPolygon.Constrain(Plus, outside, 0.3f);
            Assert.IsTrue(ArenaPolygon.Contains(Plus, result), outside + " -> " + result);
        }
    }

    [Test]
    public void Constrain_PointCloserToEdgeThanMarginIsPushedInward()
    {
        Vector2 result = ArenaPolygon.Constrain(Plus, new Vector2(0.9f, 2f), 0.5f);
        Assert.IsTrue(ArenaPolygon.Contains(Plus, result));
        Assert.LessOrEqual(result.x, 0.5f + 0.001f);
    }

    [Test]
    public void Constrain_WorksForBothWindingOrders()
    {
        var reversed = (Vector2[])Plus.Clone();
        System.Array.Reverse(reversed);
        Vector2 result = ArenaPolygon.Constrain(reversed, new Vector2(6f, 0f), 0.3f);
        Assert.IsTrue(ArenaPolygon.Contains(reversed, result));
    }
}
