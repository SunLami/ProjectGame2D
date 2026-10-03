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

        if (_attackFxRenderer == null)
            return;

        // AttackFX (the melee slash effect) only ever shows while a Sword is actually equipped --
        // bare-handed shouldn't show a sword slash, even though the same Attack animation still plays.
        // It also stays forced off during a skill cast plus a short grace period after (see
        // PlayerSkillFX.cs), so a repeated click right as a cast ends can't flicker it on early.
        bool weaponEquipped = EquipmentManager.Instance != null
            && EquipmentManager.Instance.GetEquipped(EquipSlot.Weapon) != null;
        if (!weaponEquipped || Time.time < _attackFxRestoreTime)
            _attackFxRenderer.enabled = false;
    }

    private void CacheVisualReferences()
    {
        if (_weaponRenderer == null)
            _weaponRenderer = transform.Find("Weapon")?.GetComponent<SpriteRenderer>();
    }
}
