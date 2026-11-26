using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

// Scene service (ServiceOwnershipLifecycle.md), not an application service: intentionally NOT
// DontDestroyOnLoad. It must live and die with the scene that owns _tilemap/_player, so a scene
// reload (e.g. Return Main Menu -> re-enter gameplay) always rebinds fresh scene references
// instead of a persisted instance surviving with a Tilemap/Player from an already-unloaded scene.
// _player/_tilemap are bound via the Inspector -- no Find/FindAnyObjectByType at runtime.
public class MapManager : MonoBehaviour
{
    public static MapManager Instance;
    [SerializeField] private Player _player;
    [SerializeField] private Tilemap _tilemap;
    [SerializeField] private List<TileDataSO> _tileDataList;
    private Dictionary<TileBase, TileDataSO> _dataFromTiles;

    private void Awake()
    {
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        _tileDataList = Resources.LoadAll<TileDataSO>("TileDatas").ToList();
        InitializeDictionary();
    }

    private void InitializeDictionary()
    {
        _dataFromTiles = new Dictionary<TileBase, TileDataSO>();

        foreach (var tileData in _tileDataList)
        {
            foreach (var tile in tileData.tiles)
            {
                if (tile != null && !_dataFromTiles.ContainsKey(tile))
                {
                    _dataFromTiles.Add(tile, tileData);
                }
            }
        }
    }

    public AudioClip GetCurrentTileAudioClip(Vector2 worldPosition)
    {
        if (_tilemap == null || _dataFromTiles == null)
            return null;

        TileBase tile = _tilemap.GetTile(_tilemap.WorldToCell(worldPosition));
        if (tile == null || !_dataFromTiles.TryGetValue(tile, out TileDataSO data))
            return null;

        AudioClip[] clips = _player != null && _player.IsRunning && data.runAudioClip?.Length > 0
            ? data.runAudioClip
            : data.walkAudioClip;
        return clips?.Length > 0 ? clips[Random.Range(0, clips.Length)] : null;
    }
}
