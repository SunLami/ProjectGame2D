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
                if (!_dataFromTiles.ContainsKey(tile))
                {
                    _dataFromTiles.Add(tile, tileData);
                }
            }
        }
    }

    public AudioClip GetCurrentTileAudioClip(Vector2 worldPosition)
    {
        Vector3Int gridPosition = _tilemap.WorldToCell(worldPosition);
        TileBase tile = _tilemap.GetTile(gridPosition);

        if (tile == null) return null;

        int index;
        AudioClip currentTileAudioClip;

        if (_player.IsRunning)
        {
            index = Random.Range(0, _dataFromTiles[tile].runAudioClip.Length);
            currentTileAudioClip = _dataFromTiles[tile].runAudioClip[index];
            return currentTileAudioClip;
        }

        index = Random.Range(0, _dataFromTiles[tile].walkAudioClip.Length);
        currentTileAudioClip = _dataFromTiles[tile].walkAudioClip[index];

        return currentTileAudioClip;
    }
}
