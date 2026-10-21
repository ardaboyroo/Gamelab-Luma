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
    Renderer[,] quads;
    Material freeMat, occMat, okMat, badMat, selMat;
    Texture2D lineTex, fillTex;
    GameObject visualsRoot;
    readonly List<Vector2Int> previewCells = new List<Vector2Int>();

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
        if (lineTex) Destroy(lineTex);
        if (fillTex) Destroy(fillTex);
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
                quads[c.x, c.y].sharedMaterial = ok ? okMat : badMat;
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
                quads[c.x, c.y].sharedMaterial = selMat;
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
        if (quads == null || !InBounds(c)) return;
        quads[c.x, c.y].sharedMaterial = cells[c.x, c.y] != null ? occMat : freeMat;
    }

    /// White square outline; the inside is see-through (fillAlpha 0) or lightly filled.
    /// The material colour tints it, so one texture serves every cell colour.
    Texture2D MakeCellTexture(float fillAlpha)
    {
        const int n = 64, border = 2;
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                bool edge = x < border || y < border || x >= n - border || y >= n - border;
                px[y * n + x] = new Color(1, 1, 1, edge ? 1f : fillAlpha);
            }
        t.SetPixels(px);
        t.wrapMode = TextureWrapMode.Clamp;
        t.filterMode = FilterMode.Bilinear;
        t.Apply();
        return t;
    }

    Material MakeMat(Color c, Texture2D tex)
    {
        var m = new Material(Shader.Find("Sprites/Default"));
        m.mainTexture = tex;
        m.color = c;
        return m;
    }

    void BuildVisuals()
    {
        lineTex = MakeCellTexture(0f);      // free cells: only the dark outline
        fillTex = MakeCellTexture(0.4f);    // occupied / preview cells: outline + tinted fill

        freeMat = MakeMat(gridColor, lineTex);
        occMat = MakeMat(blockedColor, fillTex);
        okMat = MakeMat(previewValid, fillTex);
        badMat = MakeMat(previewInvalid, fillTex);
        selMat = MakeMat(selectedColor, fillTex);

        quads = new Renderer[width, height];
        visualsRoot = new GameObject("GridCells");   // not parented, so this object's scale can't distort it

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                Destroy(q.GetComponent<Collider>());
                q.name = $"Cell {x},{y}";
                q.transform.SetParent(visualsRoot.transform, true);
                q.transform.position = CellCenter(new Vector2Int(x, y)) + Vector3.up * 0.02f;
                q.transform.rotation = Quaternion.Euler(90, 0, 0);
                q.transform.localScale = Vector3.one * cellSize;   // outline is part of the texture
                quads[x, y] = q.GetComponent<Renderer>();
                Refresh(new Vector2Int(x, y));
            }
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
