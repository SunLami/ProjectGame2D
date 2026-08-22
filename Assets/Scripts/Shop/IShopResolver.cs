using System.Collections.Generic;

/// <summary>Resolves a stable shopId to its ShopDefinition, mirroring IItemResolver (D-020) and
/// IQuestResolver.</summary>
public interface IShopResolver
{
    bool TryResolve(string shopId, out ShopDefinition definition);
    IReadOnlyList<ShopDefinition> AllShops { get; }
}
