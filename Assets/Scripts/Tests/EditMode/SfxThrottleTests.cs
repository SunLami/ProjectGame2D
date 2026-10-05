using NUnit.Framework;

public sealed class SfxThrottleTests
{
    [Test]
    public void NoLimits_AlwaysAllows()
    {
        var throttle = new SfxThrottle();
        for (int i = 0; i < 50; i++)
            Assert.IsTrue(throttle.TryAcquire("sfx.a", 1f, 0f, 0, 1f));
    }

    [Test]
    public void MinInterval_BlocksUntilItHasPassed()
    {
        var throttle = new SfxThrottle();
        Assert.IsTrue(throttle.TryAcquire("sfx.a", 10f, 0.25f, 0, 1f));
        Assert.IsFalse(throttle.TryAcquire("sfx.a", 10.1f, 0.25f, 0, 1f));
        Assert.IsTrue(throttle.TryAcquire("sfx.a", 10.3f, 0.25f, 0, 1f));
    }

    [Test]
    public void MinInterval_IsPerId()
    {
        var throttle = new SfxThrottle();
        Assert.IsTrue(throttle.TryAcquire("sfx.a", 5f, 1f, 0, 1f));
        Assert.IsTrue(throttle.TryAcquire("sfx.b", 5f, 1f, 0, 1f));
    }

    [Test]
    public void MaxVoices_CapsOverlappingInstancesAndFreesThemWhenTheyEnd()
    {
        var throttle = new SfxThrottle();
        Assert.IsTrue(throttle.TryAcquire("sfx.rain", 0f, 0f, 2, 1f));
        Assert.IsTrue(throttle.TryAcquire("sfx.rain", 0.2f, 0f, 2, 1f));
        Assert.IsFalse(throttle.TryAcquire("sfx.rain", 0.4f, 0f, 2, 1f));
        Assert.AreEqual(2, throttle.ActiveVoices("sfx.rain", 0.5f));
        Assert.IsTrue(throttle.TryAcquire("sfx.rain", 1.05f, 0f, 2, 1f)); // the first one ended at t=1
    }

    [Test]
    public void DeniedRequests_DoNotConsumeAVoiceOrResetTheInterval()
    {
        var throttle = new SfxThrottle();
        Assert.IsTrue(throttle.TryAcquire("sfx.a", 0f, 0.5f, 1, 2f));
        Assert.IsFalse(throttle.TryAcquire("sfx.a", 0.2f, 0.5f, 1, 2f));
        Assert.IsFalse(throttle.TryAcquire("sfx.a", 0.4f, 0.5f, 1, 2f)); // still inside the first one's interval from t=0
        Assert.AreEqual(1, throttle.ActiveVoices("sfx.a", 0.5f));
    }

    [Test]
    public void Clear_ForgetsEverything()
    {
        var throttle = new SfxThrottle();
        throttle.TryAcquire("sfx.a", 0f, 5f, 1, 5f);
        throttle.Clear();
        Assert.IsTrue(throttle.TryAcquire("sfx.a", 0.1f, 5f, 1, 5f));
    }
}
