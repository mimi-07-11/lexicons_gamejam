#if UNITY_EDITOR
// Put this file in Assets/Editor/MazePopulator.cs (and delete AutoHedges.cs, this replaces it).
//
//   Tools > Maze > Populate Negative Space              (same random layout every time)
//   Tools > Maze > Populate Negative Space (new layout) (a different random layout)
//
// Only the ROADS are walkable. Everything else gets filled in:
//   * hedges on every cell touching a road (so each road becomes a sealed corridor)
//   * a brick wall ring around the whole map
//   * random houses / apartments in the empty space (the Scenery object, in the Hierarchy)
//   * random garden-wall pieces, trees, bushes and lamps
//   * extra empty space (EdgeMargin) around the maze so bigger buildings fit
//
// It clears and rebuilds: the Walls tilemap, the Props tilemap and the Scenery object.
// It never touches Roads, Landmarks or DarkOverlay. Landmark cells are kept free.
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class MazePopulator
{
    // ------------------------ tweak these ------------------------
    const int EdgeMargin = 6;            // extra empty space around the roads (cells)
    const float BuildingDensity = 0.02f;  // 0 = no buildings, 1 = pack them in
    const float TreeChance = 0.1f;      // chance that a leftover cell gets a tree or bush
    const float LampChance = 0.03f;      // chance that a leftover cell gets a lamp
    const int WallSegments = 8;          // random garden-wall pieces
    // --------------------------------------------------------------

    static int seed = 1234;
    static readonly Dictionary<string, TileBase> cache = new Dictionary<string, TileBase>();

    struct B
    {
        public string name; public int w, h; public float p;
        public B(string n, int w, int h, float p) { name = n; this.w = w; this.h = h; this.p = p; }
    }

    // biggest first, so they get the best spots; w x h are in cells
    static readonly B[] Catalog =
    {
        new B("apartment_b_courtyard", 2, 3, 0.10f),
        new B("apartment_c_L", 3, 2, 0.10f),
        new B("apartment_a", 2, 2, 0.12f),
        new B("house_long_b", 4, 1, 0.12f),
        new B("house_long_a", 3, 1, 0.15f),
        new B("house_long_c_shops", 3, 1, 0.15f),
        new B("house_medium", 2, 1, 0.20f),
        new B("house_small_a", 1, 1, 0.25f),
        new B("house_small_b", 1, 1, 0.25f),
        new B("house_small_c", 1, 1, 0.25f),
    };

    [MenuItem("Tools/Maze/Populate Negative Space")]
    public static void RunSame() { Run(seed); }

    [MenuItem("Tools/Maze/Populate Negative Space (new layout)")]
    public static void RunNew() { seed = new System.Random().Next(1, 999999); Run(seed); }

    static void Run(int sd)
    {
        var rng = new System.Random(sd);
        Tilemap ground = FindMap("Ground"), roads = FindMap("Roads"), walls = FindMap("Walls"), props = FindMap("Props");
        if (!ground || !roads || !walls || !props) return;
        Tilemap landmarks = FindMapOptional("Landmarks");
        cache.Clear();

        var old = GameObject.Find("Scenery");
        if (old != null) UnityEngine.Object.DestroyImmediate(old);

        roads.CompressBounds();
        ground.CompressBounds();

        // 1. roads = the only walkable cells
        var road = new HashSet<Vector3Int>();
        foreach (var p in roads.cellBounds.allPositionsWithin)
            if (roads.HasTile(p)) road.Add(p);
        if (road.Count == 0)
        {
            Debug.LogError("No road tiles found on the Roads tilemap.");
            return;
        }

        // 2. cells covered by landmarks (painted on the Landmarks tilemap, or placed as sprites)
        var building = new HashSet<Vector3Int>();
        if (landmarks != null) AddTileBuildings(landmarks, building);
        foreach (var sr in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            if (sr.sprite != null && (sr.sprite.name.StartsWith("landmark_")))
                AddBounds(walls, sr.bounds, building);

        // 3. hedges on all 8 neighbours of every road cell
        var hedge = new HashSet<Vector3Int>();
        foreach (var r in road)
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    var n = new Vector3Int(r.x + dx, r.y + dy, 0);
                    if (road.Contains(n) || building.Contains(n)) continue;
                    hedge.Add(n);
                }

        // 4. the full area: roads + ground + landmarks + margin
        int xmin = int.MaxValue, ymin = int.MaxValue, xmax = int.MinValue, ymax = int.MinValue;
        foreach (var c in road) Grow(c, ref xmin, ref ymin, ref xmax, ref ymax);
        foreach (var c in building) Grow(c, ref xmin, ref ymin, ref xmax, ref ymax);
        Grow(new Vector3Int(ground.cellBounds.xMin, ground.cellBounds.yMin, 0), ref xmin, ref ymin, ref xmax, ref ymax);
        Grow(new Vector3Int(ground.cellBounds.xMax - 1, ground.cellBounds.yMax - 1, 0), ref xmin, ref ymin, ref xmax, ref ymax);
        xmin -= EdgeMargin; ymin -= EdgeMargin; xmax += EdgeMargin; ymax += EdgeMargin;

        var lit = GetTile("lit_path");
        for (int x = xmin; x <= xmax; x++)
            for (int y = ymin; y <= ymax; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                if (!ground.HasTile(cell)) ground.SetTile(cell, lit);
            }

        // 5. outer wall ring, one cell outside the area
        var ring = new HashSet<Vector3Int>();
        for (int x = xmin - 1; x <= xmax + 1; x++)
            for (int y = ymin - 1; y <= ymax + 1; y++)
                if (x == xmin - 1 || x == xmax + 1 || y == ymin - 1 || y == ymax + 1)
                    ring.Add(new Vector3Int(x, y, 0));

        // 6. negative space = everything that is not road, hedge or landmark
        var free = new HashSet<Vector3Int>();
        for (int x = xmin; x <= xmax; x++)
            for (int y = ymin; y <= ymax; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                if (road.Contains(cell) || hedge.Contains(cell) || building.Contains(cell)) continue;
                free.Add(cell);
            }
        var cells = new List<Vector3Int>(free);
        Shuffle(cells, rng);
        var used = new HashSet<Vector3Int>();

        // 7. random buildings
        var parent = new GameObject("Scenery").transform;
        var pr = props.GetComponent<TilemapRenderer>();
        int layerId = pr != null ? pr.sortingLayerID : 0;
        int order = pr != null ? pr.sortingOrder + 1 : 4;
        int placed = 0;
        foreach (var b in Catalog)
        {
            var sprite = LoadSprite(b.name);
            if (sprite == null) { Debug.LogWarning("Sprite not found: " + b.name); continue; }
            foreach (var a in cells)
            {
                if (rng.NextDouble() > b.p * BuildingDensity) continue;
                if (!Fits(a, b.w, b.h, free, used)) continue;
                var go = new GameObject(b.name);
                go.transform.SetParent(parent);
                go.transform.position = walls.GetCellCenterWorld(a) + new Vector3((b.w - 1) / 2f, (b.h - 1) / 2f, 0);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingLayerID = layerId;
                sr.sortingOrder = order;
                for (int dx = 0; dx < b.w; dx++)
                    for (int dy = 0; dy < b.h; dy++)
                        used.Add(a + new Vector3Int(dx, dy, 0));
                placed++;
            }
        }

        // 8. random garden-wall pieces
        var wallSeg = new HashSet<Vector3Int>();
        for (int i = 0; i < WallSegments; i++)
        {
            for (int attempt = 0; attempt < 30; attempt++)
            {
                var a = cells[rng.Next(cells.Count)];
                bool horizontal = rng.Next(2) == 0;
                int len = rng.Next(2, 5);
                var seg = new List<Vector3Int>();
                bool ok = true;
                for (int k = 0; k < len; k++)
                {
                    var c = a + (horizontal ? new Vector3Int(k, 0, 0) : new Vector3Int(0, k, 0));
                    if (!free.Contains(c) || used.Contains(c)) { ok = false; break; }
                    seg.Add(c);
                }
                if (!ok) continue;
                foreach (var c in seg) { wallSeg.Add(c); used.Add(c); }
                break;
            }
        }

        // 9. paint hedges + walls
        walls.ClearAllTiles();
        var solid = new HashSet<Vector3Int>(hedge);
        solid.UnionWith(ring);
        solid.UnionWith(wallSeg);
        foreach (var cell in solid)
        {
            string prefix = (ring.Contains(cell) || wallSeg.Contains(cell)) ? "wall_" : "hedge_";
            walls.SetTile(cell, GetTile(prefix + Shape(solid, cell)));
        }

        // 10. trees, bushes, lamps in whatever is left
        props.ClearAllTiles();
        foreach (var c in cells)
        {
            if (used.Contains(c)) continue;
            double r = rng.NextDouble();
            if (r < LampChance) props.SetTile(c, GetTile("street_lamp"));
            else if (r < LampChance + TreeChance)
            {
                int k = rng.Next(3);
                props.SetTile(c, GetTile(k == 0 ? "small_tree_a" : k == 1 ? "small_tree_b" : "bush"));
            }
        }

        // 11. collision: hedges + walls, and the landmark tiles
        var go2 = walls.gameObject;
        var comp = go2.GetComponent<CompositeCollider2D>();
        if (comp == null) comp = go2.AddComponent<CompositeCollider2D>();   // also adds a Rigidbody2D
        var rb = go2.GetComponent<Rigidbody2D>();
        if (rb != null) rb.bodyType = RigidbodyType2D.Static;
        var tc = go2.GetComponent<TilemapCollider2D>();
        if (tc == null) tc = go2.AddComponent<TilemapCollider2D>();
        tc.compositeOperation = Collider2D.CompositeOperation.Merge;
        if (landmarks != null && landmarks.GetComponent<TilemapCollider2D>() == null)
            landmarks.gameObject.AddComponent<TilemapCollider2D>();

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Populated (seed " + sd + "): " + road.Count + " road cells, " + hedge.Count + " hedge cells, "
                  + ring.Count + " ring wall cells, " + wallSeg.Count + " garden-wall cells, " + placed
                  + " buildings, " + building.Count + " landmark cells kept free.");
    }

    // ---------------------------- helpers ----------------------------
    static void Grow(Vector3Int c, ref int xmin, ref int ymin, ref int xmax, ref int ymax)
    {
        if (c.x < xmin) xmin = c.x; if (c.y < ymin) ymin = c.y;
        if (c.x > xmax) xmax = c.x; if (c.y > ymax) ymax = c.y;
    }

    static bool Fits(Vector3Int a, int w, int h, HashSet<Vector3Int> free, HashSet<Vector3Int> used)
    {
        for (int dx = 0; dx < w; dx++)
            for (int dy = 0; dy < h; dy++)
            {
                var c = a + new Vector3Int(dx, dy, 0);
                if (!free.Contains(c) || used.Contains(c)) return false;
            }
        return true;
    }

    static void Shuffle<T>(List<T> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            T t = list[i]; list[i] = list[j]; list[j] = t;
        }
    }

    static void AddTileBuildings(Tilemap tm, HashSet<Vector3Int> set)
    {
        foreach (var p in tm.cellBounds.allPositionsWithin)
        {
            var sp = tm.GetSprite(p);
            if (sp == null) continue;
            Vector3 c = tm.GetCellCenterWorld(p);
            Vector3 size = sp.bounds.size;
            AddBounds(tm, new Bounds(c, new Vector3(size.x, size.y, 1f)), set);
        }
    }

    static void AddBounds(Tilemap tm, Bounds b, HashSet<Vector3Int> set)
    {
        b.Expand(-0.2f);   // so a sprite that exactly fits N cells does not claim N+1
        Vector3Int min = tm.WorldToCell(b.min), max = tm.WorldToCell(b.max);
        for (int x = min.x; x <= max.x; x++)
            for (int y = min.y; y <= max.y; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                Vector3 c = tm.GetCellCenterWorld(cell);
                c.z = b.center.z;
                if (b.Contains(c)) set.Add(cell);
            }
    }

    static Tilemap FindMapOptional(string name)
    {
        foreach (var t in UnityEngine.Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            if (t.name == name) return t;
        return null;
    }

    static Tilemap FindMap(string name)
    {
        var t = FindMapOptional(name);
        if (t == null) Debug.LogError("Tilemap '" + name + "' not found. Create it under Grid first.");
        return t;
    }

    static Sprite LoadSprite(string name)
    {
        foreach (var guid in AssetDatabase.FindAssets(name + " t:Sprite"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == name) return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        return null;
    }

    static TileBase GetTile(string name)
    {
        TileBase t;
        if (!cache.TryGetValue(name, out t))
        {
            foreach (var guid in AssetDatabase.FindAssets(name + " t:TileBase"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == name)
                {
                    t = AssetDatabase.LoadAssetAtPath<TileBase>(path);
                    break;
                }
            }
            if (t == null) Debug.LogWarning("Tile asset not found: " + name);
            var tile = t as Tile;
            if (tile != null && (name.StartsWith("hedge_") || name.StartsWith("wall_")) && tile.colliderType != Tile.ColliderType.Grid)
            {
                tile.colliderType = Tile.ColliderType.Grid;   // the whole cell blocks the player
                EditorUtility.SetDirty(tile);
            }
            cache[name] = t;
        }
        return t;
    }

    static string Shape(HashSet<Vector3Int> s, Vector3Int c)
    {
        string k = "";
        if (s.Contains(c + Vector3Int.up)) k += "N";
        if (s.Contains(c + Vector3Int.right)) k += "E";
        if (s.Contains(c + Vector3Int.down)) k += "S";
        if (s.Contains(c + Vector3Int.left)) k += "W";
        switch (k)
        {
            case "NS": return "ns";
            case "EW": return "ew";
            case "NE": return "corner_ne";
            case "ES": return "corner_se";
            case "SW": return "corner_sw";
            case "NW": return "corner_nw";
            case "NES": return "t_nes";
            case "ESW": return "t_esw";
            case "NSW": return "t_nsw";
            case "NEW": return "t_new";
            case "NESW": return "cross";
            case "E": return "end_e";
            case "S": return "end_s";
            case "W": return "end_w";
            default: return "end_n";
        }
    }
}
#endif