/// <summary>Resolves a stable itemId to its ItemSO definition. Domain/save code depends on this
/// abstraction, not on how definitions are actually loaded (D-020).</summary>
public interface IItemResolver
{
    bool TryResolve(string itemId, out ItemSO item);
}
