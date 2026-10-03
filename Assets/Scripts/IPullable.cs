using UnityEngine;

public interface IPullable
{
    void ApplyPull(Vector2 targetPosition, float speed, float duration);
}
