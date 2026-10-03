using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>DemoScene test spawner for boss encounters (D-086): F6 summons the boss near the player,
/// F7 removes it. Feature code does not depend on this component; the real flow will be started by
/// the summoning statue/shrine.</summary>
public sealed class BossEncounterTestHarness : MonoBehaviour
{
    [SerializeField] private GameObject _bossPrefab;
    [SerializeField] private BossDefinition _definition;
    [SerializeField] private Vector2 _spawnOffset = new Vector2(0f, 10f);

    private BossController _boss;

    public BossController Boss => _boss;

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.f6Key.wasPressedThisFrame)
            Spawn();
        else if (keyboard.f7Key.wasPressedThisFrame)
            Despawn();
    }

    public BossController Spawn()
    {
        if (_boss != null || _bossPrefab == null || _definition == null)
            return _boss;

        Player player = FindAnyObjectByType<Player>();
        Vector2 origin = player != null ? (Vector2)player.transform.position : Vector2.zero;
        GameObject instance = Instantiate(_bossPrefab, origin + _spawnOffset, Quaternion.identity);
        _boss = instance.GetComponent<BossController>();
        _boss.Initialize(Instantiate(_definition));
        BossHealthBarUI.Create(_boss);
        _boss.BeginEncounter();
        return _boss;
    }

    public void Despawn()
    {
        if (_boss != null)
            Destroy(_boss.gameObject);
        _boss = null;
    }
}
