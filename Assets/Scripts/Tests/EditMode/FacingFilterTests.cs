using NUnit.Framework;
using UnityEngine;

public sealed class FacingFilterTests
{
    [Test]
    public void NearDiagonalJitter_DoesNotFlipFacing()
    {
        FacingFilter filter = new(Vector2.right);
        float t = 0f;
        for (int i = 0; i < 20; i++, t += 0.05f)
        {
            Vector2 wobble = i % 2 == 0 ? new Vector2(1f, 1.1f) : new Vector2(1.1f, 1f);
            Assert.AreEqual(Vector2.right, filter.Update(wobble, t));
        }
    }

    [Test]
    public void SustainedTurn_SwitchesAfterHoldTime()
    {
        FacingFilter filter = new(Vector2.right);
        Assert.AreEqual(Vector2.right, filter.Update(Vector2.up, 0f));
        Assert.AreEqual(Vector2.right, filter.Update(Vector2.up, FacingFilter.HoldTime * 0.5f));
        Assert.AreEqual(Vector2.up, filter.Update(Vector2.up, FacingFilter.HoldTime + 0.01f));
    }

    [Test]
    public void Immediate_TurnsAtOnce()
    {
        FacingFilter filter = new(Vector2.right);
        Assert.AreEqual(Vector2.left, filter.Update(Vector2.left, 0f, immediate: true));
    }

    [Test]
    public void ZeroDirection_KeepsCurrent()
    {
        FacingFilter filter = new(Vector2.down);
        Assert.AreEqual(Vector2.down, filter.Update(Vector2.zero, 1f));
    }
}
