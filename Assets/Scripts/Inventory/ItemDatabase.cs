using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Scriptable Objects/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public ItemSO item;
        public int amount = 1;
    }

    public Entry[] items;
}
