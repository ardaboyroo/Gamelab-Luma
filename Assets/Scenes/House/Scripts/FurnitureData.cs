using UnityEngine;

/// One asset per furniture type. Create via: Assets > Create > Housing > Furniture
[CreateAssetMenu(menuName = "Housing/Furniture", fileName = "NewFurniture")]
public class FurnitureData : ScriptableObject
{
    public string displayName;
    public Sprite icon;                 // shown in the inventory bar
    public GameObject prefab;           // pivot must be at the CENTER of the footprint, at floor level
    public Vector2Int size = Vector2Int.one;   // footprint in tiles, bed = (2, 3)
}