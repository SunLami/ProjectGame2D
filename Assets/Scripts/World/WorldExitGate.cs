using System.Collections;
using UnityEngine;

/// <summary>
/// Temporary world-exit boundary. Keeps the future destination authored while that scene is not
/// ready, returning the player to a safe point and presenting a clear placeholder message.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public sealed class WorldExitGate : MonoBehaviour
{
    [SerializeField] private string _destinationSceneName = "MapDuy";
    [SerializeField] private string _destinationSpawnId;
    [SerializeField] private string _unavailableMessage = "Comming Soon";
    [SerializeField, Min(0.1f)] private float _returnClearance = 1f;
    [SerializeField, Min(0.1f)] private float _walkSpeed = 2f;

    private BoxCollider2D _trigger;
    private bool _isReturningPlayer;

    public string DestinationSceneName => _destinationSceneName;

    private void Reset()
    {
        BoxCollider2D trigger = GetComponent<BoxCollider2D>();
        trigger.isTrigger = true;
    }

    private void Awake() => _trigger = GetComponent<BoxCollider2D>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Only the body collider can cross the seam. Combat hitboxes on child objects
        // can extend past the return threshold even while the player stands at the spawn.
        Player player = other.GetComponent<Player>();
        if (player == null || _isReturningPlayer || GameStateManager.Instance == null
            || GameStateManager.Instance.CurrentState != GameState.Playing)
            return;

        if (string.IsNullOrEmpty(_destinationSpawnId))
            StartCoroutine(WalkPlayerBack(player));
        else
            StartCoroutine(TravelAfterPhysics());
    }

    private IEnumerator TravelAfterPhysics()
    {
        // SceneFlow replaces the outgoing Player immediately. Unity forbids that inside a
        // physics callback, so start the scene transition on the next frame.
        _isReturningPlayer = true;
        yield return null;

        if (GameStateManager.Instance?.CurrentState == GameState.Playing
            && !SceneTravel.TryTravelThroughWorldGate(gameObject.scene, _destinationSceneName,
                _destinationSpawnId, out string reason))
            WorldMessagePopupUI.Show(reason);

        _isReturningPlayer = false;
    }

    private IEnumerator WalkPlayerBack(Player player)
    {
        _isReturningPlayer = true;
        GameStateManager.Instance.PushState(GameState.Cutscene);
        player.BeginScriptedWalk(Vector2.down);

        float targetY = _trigger.bounds.min.y - _returnClearance;
        Vector3 target = new(player.transform.position.x, targetY, player.transform.position.z);
        while (player != null && player.transform.position.y > targetY)
        {
            Vector3 next = Vector3.MoveTowards(
                player.transform.position, target, _walkSpeed * Time.deltaTime);
            player.WarpTo(next);
            yield return null;
        }

        if (player != null)
        {
            player.WarpTo(target);
            player.EndScriptedWalk();
        }

        if (GameStateManager.Instance != null
            && GameStateManager.Instance.CurrentState == GameState.Cutscene)
            GameStateManager.Instance.ReturnToPreviousState();

        _isReturningPlayer = false;
        WorldMessagePopupUI.Show(_unavailableMessage);
    }
}
