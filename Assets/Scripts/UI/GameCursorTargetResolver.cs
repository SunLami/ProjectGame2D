using UnityEngine;

public readonly struct GameCursorTarget
{
    public GameCursorTarget(GameCursorType cursor, Transform rangeOrigin, bool requiresRange, bool isAvailable)
    {
        Cursor = cursor;
        RangeOrigin = rangeOrigin;
        RequiresRange = requiresRange;
        IsAvailable = isAvailable;
    }

    public GameCursorType Cursor { get; }
    public Transform RangeOrigin { get; }
    public bool RequiresRange { get; }
    public bool IsAvailable { get; }
}

public static class GameCursorTargetResolver
{
    public static bool TryResolve(Component hoveredComponent, out GameCursorTarget target)
    {
        target = default;
        if (hoveredComponent == null)
            return false;

        Enemy enemy = hoveredComponent.GetComponentInParent<Enemy>(true);
        if (enemy != null)
        {
            if (enemy.IsDead)
                return false;

            target = new GameCursorTarget(GameCursorType.Attack, enemy.transform, false, true);
            return true;
        }

        EnemyUniversal universalEnemy = hoveredComponent.GetComponentInParent<EnemyUniversal>(true);
        if (universalEnemy != null)
        {
            if (universalEnemy.IsDead)
                return false;

            target = new GameCursorTarget(GameCursorType.Attack, universalEnemy.transform, false, true);
            return true;
        }

        ResourceNodeInteractable resource = hoveredComponent.GetComponentInParent<ResourceNodeInteractable>(true);
        if (resource != null)
        {
            target = new GameCursorTarget(ToCursor(resource.HarvestType), resource.transform, true, resource.IsAvailable);
            return true;
        }

        ChestInteractable chest = hoveredComponent.GetComponentInParent<ChestInteractable>(true);
        if (chest != null)
        {
            target = new GameCursorTarget(
                GameCursorType.Interact,
                chest.transform,
                true,
                !chest.IsOpened && !chest.IsOpening);
            return true;
        }

        UniquePickupInteractable pickup = hoveredComponent.GetComponentInParent<UniquePickupInteractable>(true);
        if (pickup != null)
        {
            target = new GameCursorTarget(GameCursorType.Interact, pickup.transform, true, !pickup.IsCollected);
            return true;
        }

        QuestNpcInteractionUI questNpc = hoveredComponent.GetComponentInParent<QuestNpcInteractionUI>(true);
        TownElderCommerceInteractionUI commerceNpc = hoveredComponent.GetComponentInParent<TownElderCommerceInteractionUI>(true);
        Transform npcTransform = questNpc != null ? questNpc.transform : commerceNpc != null ? commerceNpc.transform : null;
        if (npcTransform != null)
        {
            target = new GameCursorTarget(GameCursorType.Talk, npcTransform, true, true);
            return true;
        }

        return false;
    }

    private static GameCursorType ToCursor(ResourceHarvestType harvestType) => harvestType switch
    {
        ResourceHarvestType.Mining => GameCursorType.Mining,
        ResourceHarvestType.Chopping => GameCursorType.Chopping,
        _ => GameCursorType.Gathering
    };
}
