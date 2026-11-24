using System.Collections.Generic;
using UnityEngine;

public class PlacedFurniture
{
    public FurnitureData data;
    public Vector2Int origin;   // bottom-left cell of the footprint
    public int rotation;        // 0-3 quarter turns
    public GameObject instance;
}

/// Pure grid logic + cell visuals. Put on an empty GameObject at the floor's bottom-left corner.
public class GridManager : MonoBehaviour
{
    [Header("Grid")]
    public int width = 10;
    public int height = 10;
    public float cellSize = 1f;

    [Header("Cell visuals")]
    [Tooltip("Colour of the grid lines on free cells. Dark = easy to see on a light floor.")]
    public Color gridColor = new Color(0.23f, 0.14f, 0.09f, 0.7f);
    public Color blockedColor = new Color(0.9f, 0.25f, 0.2f, 0.9f);
    public Color previewValid = new Color(0.3f, 0.85f, 0.35f, 1f);
    public Color previewInvalid = new Color(0.95f, 0.25f, 0.2f, 1f);
    public Color selectedColor = new Color(1f, 0.85f, 0.25f, 1f);

    PlacedFurniture[,] cells;
    readonly List<Vector2Int> previewCells = new List<Vector2Int>();

    // All cells are drawn as ONE mesh (one draw call instead of one per cell).
    // Each cell is a quad with its own vertex colour; the UVs pick the 'outline only' or the
    // 'outline + light fill' half of a small generated texture.
    GameObject visualsRoot;
    Mesh gridMesh;
    Material gridMat;
    Texture2D cellTex;
    Color[] vColors;
    Vector2[] vUVs;
    bool meshDirty;

    public Vector3 GridCenter => transform.position + new Vector3(width, 0, height) * cellSize * 0.5f;

    void Awake()
    {
        cells = new PlacedFurniture[width, height];
        BuildVisuals();
        SetCellsVisible(false);
    }

    void OnDestroy()
    {
        if (visualsRoot) Destroy(visualsRoot);
        if (gridMesh) Destroy(gridMesh);
        if (gridMat) Destroy(gridMat);
        if (cellTex) Destroy(cellTex);
    }

    // ---------- coordinates ----------
    public bool InBounds(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < width && c.y < height;

    public Vector2Int WorldToCell(Vector3 world)
    {
        Vector3 l = world - transform.position;
        return new Vector2Int(Mathf.FloorToInt(l.x / cellSize), Mathf.FloorToInt(l.z / cellSize));
    }

    public Vector3 CellToWorld(Vector2Int c) => transform.position + new Vector3(c.x, 0, c.y) * cellSize;

    public Vector3 CellCenter(Vector2Int c) => CellToWorld(c) + new Vector3(cellSize, 0, cellSize) * 0.5f;

    public Vector3 FootprintCenter(Vector2Int origin, Vector2Int size) =>
        CellToWorld(origin) + new Vector3(size.x, 0, size.y) * cellSize * 0.5f;

    public static Vector2Int RotatedSize(Vector2Int s, int rot) =>
        rot % 2 == 0 ? s : new Vector2Int(s.y, s.x);

    // ---------- placement ----------
    public bool CanPlace(Vector2Int origin, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
            {
                var c = origin + new Vector2Int(x, y);
                if (!InBounds(c) || cells[c.x, c.y] != null) return false;
            }
        return true;
    }

    public PlacedFurniture Place(FurnitureData data, Vector2Int origin, int rotation)
    {
        Vector2Int size = RotatedSize(data.size, rotation);
        if (!CanPlace(origin, size)) return null;

        var go = Instantiate(data.prefab, FootprintCenter(origin, size), Quaternion.Euler(0, rotation * 90f, 0));
        var p = new PlacedFurniture { data = data, origin = origin, rotation = rotation, instance = go };
        go.AddComponent<FurnitureHandle>().placed = p;   // lets a tap on its collider find this entry

        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
            {
                var c = origin + new Vector2Int(x, y);
                cells[c.x, c.y] = p;
                Refresh(c);
            }
        return p;
    }

    public void Remove(PlacedFurniture p)
    {
        Vector2Int size = RotatedSize(p.data.size, p.rotation);
        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
            {
                var c = p.origin + new Vector2Int(x, y);
                cells[c.x, c.y] = null;
                Refresh(c);
            }
        Destroy(p.instance);
    }

    public PlacedFurniture GetAt(Vector2Int c) => InBounds(c) ? cells[c.x, c.y] : null;

    // ---------- visuals ----------
    public void SetCellsVisible(bool visible) { if (visualsRoot) visualsRoot.SetActive(visible); }

    public void ShowPreview(Vector2Int origin, Vector2Int size, bool ok)
    {
        ClearPreview();
        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
            {
                var c = origin + new Vector2Int(x, y);
                if (!InBounds(c)) continue;
                SetCell(c, ok ? previewValid : previewInvalid, true);
                previewCells.Add(c);
            }
    }

    /// Marks the cells of an already placed piece as selected.
    public void ShowHighlight(PlacedFurniture p)
    {
        ClearPreview();
        Vector2Int size = RotatedSize(p.data.size, p.rotation);
        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
            {
                var c = p.origin + new Vector2Int(x, y);
                if (!InBounds(c)) continue;
                SetCell(c, selectedColor, true);
                previewCells.Add(c);
            }
    }

    public void ClearPreview()
    {
        foreach (var c in previewCells) Refresh(c);
        previewCells.Clear();
    }

    void Refresh(Vector2Int c)
    {
        if (vColors == null || !InBounds(c)) return;
        bool occupied = cells[c.x, c.y] != null;
        SetCell(c, occupied ? blockedColor : gridColor, occupied);
    }

    // Colours one cell's quad. filled = outline + light fill, otherwise only the outline.
    void SetCell(Vector2Int c, Color color, bool filled)
    {
        int i = (c.x * height + c.y) * 4;
        // the texture is two 64x64 halves side by side; stay half a texel inside each half
        const float t = 0.5f / 128f;
        float u0 = filled ? 0.5f + t : t, u1 = filled ? 1f - t : 0.5f - t;
        vUVs[i] = new Vector2(u0, 0f); vUVs[i + 1] = new Vector2(u1, 0f);
        vUVs[i + 2] = new Vector2(u0, 1f); vUVs[i + 3] = new Vector2(u1, 1f);
        vColors[i] = vColors[i + 1] = vColors[i + 2] = vColors[i + 3] = color;
        meshDirty = true;
    }

    void LateUpdate()
    {
        if (!meshDirty || gridMesh == null) return;
        gridMesh.SetColors(vColors);
        gridMesh.SetUVs(0, vUVs);
        meshDirty = false;
    }

    /// 128x64 texture: left half = white square outline (see-through inside),
    /// right half = the same outline with a light fill. Vertex colours tint it.
    Texture2D MakeCellTexture()
    {
        const int n = 64, border = 2;
        var t = new Texture2D(n * 2, n, TextureFormat.RGBA32, false);
        var px = new Color[n * 2 * n];
        for (int half = 0; half < 2; half++)
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    bool edge = x < border || y < border || x >= n - border || y >= n - border;
                    px[y * n * 2 + half * n + x] = new Color(1, 1, 1, edge ? 1f : (half == 0 ? 0f : 0.4f));
                }
        t.SetPixels(px);
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear;
        t.Apply(false, true);   // no CPU copy needed after upload
        return t;
    }

    void BuildVisuals()
    {
        cellTex = MakeCellTexture();
        gridMat = new Material(Shader.Find("Sprites/Default")) { mainTexture = cellTex, color = Color.white };

        int count = width * height;
        var verts = new Vector3[count * 4];
        var tris = new int[count * 6];
        vColors = new Color[count * 4];
        vUVs = new Vector2[count * 4];
        Vector3 lift = Vector3.up * 0.02f;
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                int i = (x * height + y) * 4, k = (x * height + y) * 6;
                Vector3 o = CellToWorld(new Vector2Int(x, y)) + lift;
                verts[i] = o;
                verts[i + 1] = o + new Vector3(cellSize, 0, 0);
                verts[i + 2] = o + new Vector3(0, 0, cellSize);
                verts[i + 3] = o + new Vector3(cellSize, 0, cellSize);
                tris[k] = i; tris[k + 1] = i + 2; tris[k + 2] = i + 1;
                tris[k + 3] = i + 1; tris[k + 4] = i + 2; tris[k + 5] = i + 3;
            }

        gridMesh = new Mesh { name = "Grid cells" };
        gridMesh.MarkDynamic();
        gridMesh.SetVertices(verts);
        gridMesh.SetTriangles(tris, 0);
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                Refresh(new Vector2Int(x, y));
        gridMesh.SetColors(vColors);
        gridMesh.SetUVs(0, vUVs);
        gridMesh.RecalculateBounds();
        meshDirty = false;

        // world-space mesh on an unparented object, so this object's scale can't distort it
        visualsRoot = new GameObject("GridCells");
        visualsRoot.AddComponent<MeshFilter>().sharedMesh = gridMesh;
        var mr = visualsRoot.AddComponent<MeshRenderer>();
        mr.sharedMaterial = gridMat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Vector3 o = transform.position + Vector3.up * 0.01f;
        for (int x = 0; x <= width; x++)
            Gizmos.DrawLine(o + new Vector3(x * cellSize, 0, 0), o + new Vector3(x * cellSize, 0, height * cellSize));
        for (int y = 0; y <= height; y++)
            Gizmos.DrawLine(o + new Vector3(0, 0, y * cellSize), o + new Vector3(width * cellSize, 0, y * cellSize));
        Gizmos.color = Color.cyan;
        Gizmos.DrawCube(CellCenter(Vector2Int.zero) + Vector3.up * 0.02f, new Vector3(cellSize, 0.02f, cellSize) * 0.6f);
    }
}
