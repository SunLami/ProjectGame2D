using UnityEngine;
using UnityEngine.Tilemaps;
public enum FloorType
{
    Grass,
    Rock,
}

[CreateAssetMenu(fileName = "NewTileData", menuName = "Scriptable Objects/Create TileData")]
public class TileDataSO : ScriptableObject
{
    public TileBase[] tiles;
    public AudioClip[] audioClip;
    public FloorType floorType;
}
