using NUnit.Framework;

public sealed class ResourcesItemResolverTests
{
    [Test]
    public void TryResolve_KnownRealItemId_ReturnsDefinition()
    {
        IItemResolver resolver = new ResourcesItemResolver();

        Assert.IsTrue(resolver.TryResolve("sword_lvl1", out ItemSO item));
        Assert.IsNotNull(item);
        Assert.AreEqual("sword_lvl1", item.itemId);
    }

    [Test]
    public void TryResolve_UnknownItemId_ReturnsFalse()
    {
        IItemResolver resolver = new ResourcesItemResolver();

        Assert.IsFalse(resolver.TryResolve("item.does.not.exist", out ItemSO item));
        Assert.IsNull(item);
    }

    [Test]
    public void TryResolve_EmptyOrNullId_ReturnsFalse()
    {
        IItemResolver resolver = new ResourcesItemResolver();

        Assert.IsFalse(resolver.TryResolve("", out _));
        Assert.IsFalse(resolver.TryResolve(null, out _));
    }
}
