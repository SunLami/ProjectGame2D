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

    [TestCase("fish.river.Fish1", "fish.river.azure_minnow", "Azure Minnow")]
    [TestCase("fish.river.Fish2", "fish.river.mossfin_perch", "Mossfin Perch")]
    [TestCase("fish.river.Fish3", "fish.river.sunscale_carp", "Sunscale Carp")]
    [TestCase("fish.river.Fish4", "fish.river.rosefin_bream", "Rosefin Bream")]
    [TestCase("fish.river.Fish5", "fish.river.silverstream_darter", "Silverstream Darter")]
    [TestCase("fish.river.Fish6", "fish.river.amethyst_bass", "Amethyst Bass")]
    [TestCase("fish.river.Fish7", "fish.river.green_pike", "Green Pike")]
    [TestCase("fish.river.Fish8", "fish.river.pearlstripe_koi", "Pearlstripe Koi")]
    [TestCase("fish.river.Fish9", "fish.river.shadowfin_bass", "Shadowfin Bass")]
    [TestCase("fish.river.Fish10", "fish.river.ember_koi", "Ember Koi")]
    public void TryResolve_LegacyFishId_ReturnsRenamedDefinition(
        string legacyId,
        string canonicalId,
        string displayName)
    {
        IItemResolver resolver = new ResourcesItemResolver();

        Assert.IsTrue(resolver.TryResolve(legacyId, out ItemSO item));
        Assert.AreEqual(canonicalId, item.itemId);
        Assert.AreEqual(displayName, item.itemName);
    }
}
