using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class ShopCatalogTests
{
    private static ShopDefinition MakeDefinition(string shopId)
    {
        var definition = ScriptableObject.CreateInstance<ShopDefinition>();
        SetPrivate(definition, "_shopId", shopId);
        return definition;
    }

    private static ShopCatalog MakeCatalog(params ShopDefinition[] shops)
    {
        var catalog = ScriptableObject.CreateInstance<ShopCatalog>();
        SetPrivate(catalog, "_shops", shops);
        return catalog;
    }

    private static void SetPrivate(object target, string field, object value) =>
        target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

    [Test]
    public void TryResolve_FindsShopByStableId()
    {
        ShopDefinition general = MakeDefinition("shop.town.general");
        ShopCatalog catalog = MakeCatalog(general);
        try
        {
            Assert.IsTrue(catalog.TryResolve("shop.town.general", out ShopDefinition resolved));
            Assert.AreEqual(general, resolved);
            Assert.AreEqual(1, catalog.AllShops.Count);
        }
        finally
        {
            Object.DestroyImmediate(catalog);
            Object.DestroyImmediate(general);
        }
    }

    [Test]
    public void TryResolve_UnknownOrEmptyId_ReturnsFalse()
    {
        ShopCatalog catalog = MakeCatalog(MakeDefinition("shop.town.general"));
        try
        {
            Assert.IsFalse(catalog.TryResolve("shop.unknown", out ShopDefinition resolved));
            Assert.IsNull(resolved);
            Assert.IsFalse(catalog.TryResolve(null, out _));
        }
        finally
        {
            Object.DestroyImmediate(catalog);
        }
    }
}
