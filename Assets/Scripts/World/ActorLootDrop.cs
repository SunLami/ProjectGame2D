using System.Collections.Generic;
using UnityEngine;

/// <summary>Generic death-to-loot adapter for existing enemies and wildlife; one roll per death.</summary>
public sealed class ActorLootDrop : MonoBehaviour
{
    [SerializeField] private ResourceLootEntry[] _loot;
    private bool _dropped;
    private EnemyUniversal _enemy;
    private Enemy _legacyEnemy;
    private WildlifeActor _animal;
    private void OnEnable()
    {
        _enemy = GetComponent<EnemyUniversal>();
        _legacyEnemy = GetComponent<Enemy>();
        _animal = GetComponent<WildlifeActor>();
        if (_enemy != null) _enemy.Died += Drop;
        if (_legacyEnemy != null) _legacyEnemy.Died += Drop;
        if (_animal != null) _animal.Died += Drop;
    }
    private void OnDisable()
    {
        if (_enemy != null) _enemy.Died -= Drop;
        if (_legacyEnemy != null) _legacyEnemy.Died -= Drop;
        if (_animal != null) _animal.Died -= Drop;
    }
    private void Drop()
    {
        if (_dropped) return;
        _dropped = true;
        ResourceLootEntry[] entries = _animal != null && _animal.Definition != null ? _animal.Definition.Loot : _loot;
        var grants = new List<InventoryItemGrant>();
        if (entries != null)
            foreach (ResourceLootEntry entry in entries)
                if (entry != null && entry.TryRoll(out InventoryItemGrant grant)) grants.Add(grant);
        if (grants.Count == 0) return;
        var root = new GameObject("DroppedLoot");
        root.transform.position = transform.position;
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, gameObject.scene);

        for (int i = 0; i < grants.Count; i++)
        {
            var icon = new GameObject(grants[i].Item.itemName);
            icon.transform.SetParent(root.transform, false);
            icon.transform.localPosition = Vector3.right * ((i - (grants.Count - 1) * 0.5f) * 0.5f);
            var view = icon.AddComponent<SpriteRenderer>();
            view.sprite = grants[i].Item.icon;
            view.sortingOrder = 30;
        }

        root.AddComponent<DroppedLootPickup>().Initialize(grants, _animal != null ? 0.45f : 0f);
        SoundFXManager.PlaySfxAt(SfxIds.WorldLootDrop, root.transform.position);
    }
}
