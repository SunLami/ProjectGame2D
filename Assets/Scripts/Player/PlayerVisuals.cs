using UnityEngine;

public partial class Player
{
    [Header("Weapon Rendering")]
    [SerializeField] private SpriteRenderer _weaponRenderer;
    [SerializeField] private int _weaponSortingOrderRight = 1;
    [SerializeField] private int _weaponSortingOrderLeft = -1;
    [SerializeField] private int _weaponSortingOrderVertical;

    private void LateUpdate()
    {
        if (_weaponRenderer == null)
            return;

        int targetOrder = _facingDirection.x > 0f
            ? _weaponSortingOrderRight
            : _facingDirection.x < 0f
                ? _weaponSortingOrderLeft
                : _weaponSortingOrderVertical;

        if (_weaponRenderer.sortingOrder != targetOrder)
            _weaponRenderer.sortingOrder = targetOrder;
    }

    private void CacheVisualReferences()
    {
        if (_weaponRenderer == null)
            _weaponRenderer = transform.Find("Weapon")?.GetComponent<SpriteRenderer>();
    }
}
