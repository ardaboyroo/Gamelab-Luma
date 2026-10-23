using UnityEngine;

/// Swaps the wall and floor materials. Put on any GameObject and drag the room's renderers in.
public class RoomSurfaces : MonoBehaviour
{
    public Renderer[] wallRenderers;
    public Renderer[] floorRenderers;

    public void Apply(SurfaceData s)
    {
        if (s == null || s.material == null) return;
        var targets = s.kind == SurfaceKind.Wallpaper ? wallRenderers : floorRenderers;
        if (targets == null) return;
        foreach (var r in targets) if (r) r.sharedMaterial = s.material;
    }
}
