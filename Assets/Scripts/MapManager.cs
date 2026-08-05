using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using UnityEngine;
using UnityEngine.Tilemaps;
using static SoundFXLibrary;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance;
    [SerializeField] private Player _player;
    [SerializeField] private Tilemap _tilemap;
    [SerializeField] private List<TileDataSO> _tileDataList;
    private Dictionary<TileBase, TileDataSO> _dataFromTiles;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            _player = FindAnyObjectByType<Player>();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
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
