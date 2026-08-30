using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-900)]
public sealed class GameCursorManager : MonoBehaviour
{
    private const string ResourceRoot = "UI/Cursors/";
    private const float InteractionRange = 2.5f;
    private const int MaxRaycastHits = 16;

    private static readonly IReadOnlyDictionary<GameCursorType, string> ResourceNames =
        new Dictionary<GameCursorType, string>
        {
            [GameCursorType.Default] = "cursor_default",
            [GameCursorType.Attack] = "cursor_attack",
            [GameCursorType.Talk] = "cursor_talk",
            [GameCursorType.Blocked] = "cursor_blocked",
            [GameCursorType.Interact] = "cursor_interact",
            [GameCursorType.Mining] = "cursor_mining",
            [GameCursorType.Chopping] = "cursor_chopping",
            [GameCursorType.Gathering] = "cursor_gathering"
        };

    private static readonly IReadOnlyDictionary<GameCursorType, Vector2> Hotspots =
        new Dictionary<GameCursorType, Vector2>
        {
            [GameCursorType.Default] = new(17f, 16f),
            [GameCursorType.Attack] = new(15f, 15f),
            [GameCursorType.Talk] = new(16f, 22f),
            [GameCursorType.Blocked] = new(16f, 16f),
            [GameCursorType.Interact] = new(16f, 17f),
            [GameCursorType.Mining] = new(15f, 18f),
            [GameCursorType.Chopping] = new(15f, 25f),
            [GameCursorType.Gathering] = new(17f, 19f)
        };

    private readonly Dictionary<GameCursorType, Texture2D> _textures = new();
    private readonly List<Collider2D> _overlapHits = new(MaxRaycastHits);
    private GameCursorType _current = (GameCursorType)(-1);
    private Transform _player;
    private ResourceNodeInteractable _hoveredGatheringNode;
    private QuestNpcInteractionUI _hoveredQuestNpc;
    private ChestInteractable _hoveredChest;

    public static GameCursorManager Instance { get; private set; }
    public GameCursorType CurrentCursor => _current;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        new GameObject(nameof(GameCursorManager)).AddComponent<GameCursorManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadTextures();
        SceneManager.sceneLoaded += HandleSceneLoaded;
        Apply(GameCursorType.Default);
    }

    private void Update()
    {
        Apply(ResolveCursor());
        if (_current == GameCursorType.Gathering
            && _hoveredGatheringNode != null
            && Mouse.current != null
            && Mouse.current.leftButton.wasPressedThisFrame)
        {
            _hoveredGatheringNode.TryBeginGathering();
        }
        else if (_current == GameCursorType.Talk
            && _hoveredQuestNpc != null
            && Mouse.current != null
            && Mouse.current.leftButton.wasPressedThisFrame)
        {
            _hoveredQuestNpc.TryInteract();
        }
        else if (_current == GameCursorType.Interact
            && _hoveredChest != null
            && Mouse.current != null
            && Mouse.current.leftButton.wasPressedThisFrame)
        {
            _hoveredChest.TryBeginOpen();
        }
    }

    private GameCursorType ResolveCursor()
    {
        _hoveredGatheringNode = null;
        _hoveredQuestNpc = null;
        _hoveredChest = null;
        if (Mouse.current == null
            || GameStateManager.Instance == null
            || GameStateManager.Instance.CurrentState != GameState.Playing
            || EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return GameCursorType.Default;
        }

        Camera camera = Camera.main;
        if (camera == null)
            return GameCursorType.Default;

        Vector3 worldPosition = camera.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        _overlapHits.Clear();
        int hitCount = Physics2D.OverlapPoint(worldPosition, ContactFilter2D.noFilter, _overlapHits);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D collider = _overlapHits[i];
            if (!GameCursorTargetResolver.TryResolve(collider, out GameCursorTarget target))
                continue;

            if (!target.IsAvailable || target.RequiresRange && !IsPlayerInRange(target.RangeOrigin))
                return GameCursorType.Blocked;

            if (target.Cursor == GameCursorType.Gathering)
                _hoveredGatheringNode = collider.GetComponentInParent<ResourceNodeInteractable>(true);
            else if (target.Cursor == GameCursorType.Talk)
                _hoveredQuestNpc = collider.GetComponentInParent<QuestNpcInteractionUI>(true);
            else if (target.Cursor == GameCursorType.Interact
                && collider.GetComponentInParent<ChestInteractable>(true) != null)
                _hoveredChest = collider.GetComponentInParent<ChestInteractable>(true);

            return target.Cursor;
        }

        return GameCursorType.Default;
    }

    private bool IsPlayerInRange(Transform target)
    {
        if (_player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            _player = playerObject != null ? playerObject.transform : null;
        }

        return _player != null
            && target != null
            && ((Vector2)(_player.position - target.position)).sqrMagnitude <= InteractionRange * InteractionRange;
    }

    private void LoadTextures()
    {
        foreach ((GameCursorType type, string resourceName) in ResourceNames)
        {
            Texture2D texture = Resources.Load<Texture2D>(ResourceRoot + resourceName);
            if (texture == null)
                Debug.LogError($"Missing cursor texture at Resources/{ResourceRoot}{resourceName}.png");
            else
                _textures[type] = texture;
        }
    }

    private void Apply(GameCursorType type)
    {
        if (_current == type)
            return;

        if (!_textures.TryGetValue(type, out Texture2D texture))
        {
            if (type != GameCursorType.Default && _textures.TryGetValue(GameCursorType.Default, out texture))
                type = GameCursorType.Default;
            else
                return;
        }

        _current = type;
        Cursor.SetCursor(texture, Hotspots[type], CursorMode.Auto);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _player = null;
        Apply(GameCursorType.Default);
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        Instance = null;
    }
}
