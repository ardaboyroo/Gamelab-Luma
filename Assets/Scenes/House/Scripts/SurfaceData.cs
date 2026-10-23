using UnityEngine;

public enum SurfaceKind { Wallpaper, Floor }

/// One asset per wallpaper / floor. Create via: Assets > Create > Housing > Surface
[CreateAssetMenu(menuName = "Housing/Surface", fileName = "NewSurface")]
public class SurfaceData : ScriptableObject
{
    public string displayName;
    public Sprite icon;                 // optional - without one the inventory shows the material's colour
    public SurfaceKind kind;
    public Material material;           // set the tiling on the material itself
}
