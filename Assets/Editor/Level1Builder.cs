#if UNITY_EDITOR
// Put this file in Assets/Editor/Level1Builder.cs.   Tools > Maze > Build Level 1
// Run it INSIDE your Level 1 scene (a duplicate of the Level 2 scene). It clears and rebuilds:
//   Ground, Roads, Walls, Props, DarkOverlay, Landmarks, Scenery and the "Level1Objects" group.
// Afterwards run  Tools > Maze > Populate Negative Space  to add hedges, walls and scenery around the roads.
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class Level1Builder
{
    // R = road
    const string RoadLayer = @"
..............................
..............................
...............R..............
...............R..............
............RRRRRRRRRRRRR.....
............R...........R.....
............R...........RRR...
............RRR.........R.....
............R...........RRRR..
..RRRRRRRRRRR.................
..R.........R.................
..R.......RRR.................
..R...........................
..............................
";

    // F = dark overlay (two dark areas)
    const string FogLayer = @"
..............FFF.............
.............FFFFF............
...........FFFFFFF...FFFFF....
..........FFFFFFFF...FFFFFF...
..........FFFFFFFF...FFFFFFF..
..........FFFFFFFF...FFFFFFFF.
..........FFFFFFFF...FFFFFFFF.
........FFFFFFFFF.....FFFFFFFF
........FFFFFFFFF.....FFFFFFFF
........FFFFFFFF......FFFFFFFF
........FFFFFFF........FFFFFF.
........FFFFFFF...............
........FFFFFFF...............
.........FFFFF................
";

    // S player start   Q twist portal   r return point after the 3D fight   N clue NPC   X weapon   B bank (landmark tile)
    const string ObjectLayer = @"
..............................
...................B..........
...............N..............
..............................
..............................
..............................
..............................
..............................
...........................X..
......rQ......................
..............................
..............................
..S...........................
..............................
";

    static readonly Dictionary<string, TileBase> cache = new Dictionary<string, TileBase>();

    [MenuItem("Tools/Maze/Build Level 1")]
    public static void Build()
    {
        string[] road = Rows(RoadLayer), fog = Rows(FogLayer), obj = Rows(ObjectLayer);
        int H = road.Length, W = road[0].Length;

        Tilemap ground = FindMap("Ground"), roads = FindMap("Roads"), walls = FindMap("Walls"),
                props = FindMap("Props"), dark = FindMap("DarkOverlay");
        if (!ground || !roads || !walls || !props || !dark) return;
        Tilemap landmarks = FindMapOptional("Landmarks");
        cache.Clear();

        foreach (var t in new[] { ground, roads, walls, props, dark }) t.ClearAllTiles();
        if (landmarks != null) landmarks.ClearAllTiles();
        foreach (var n in new[] { "Scenery", "Level1Objects" })
        {
            var o = GameObject.Find(n);
            if (o != null) UnityEngine.Object.DestroyImmediate(o);
        }
        var parent = new GameObject("Level1Objects").transform;

        var pr = props.GetComponent<TilemapRenderer>();
        int layerId = pr != null ? pr.sortingLayerID : 0;
        int order = pr != null ? pr.sortingOrder + 2 : 6;

        var roadSet = new HashSet<Vector3Int>();
        for (int r = 0; r < H; r++)
            for (int c = 0; c < W; c++)
                if (road[r][c] == 'R') roadSet.Add(new Vector3Int(c, H - 1 - r, 0));

        TileBase lit = GetTile("lit_path"), fogTile = GetTile("Fog");
        Transform playerT = null, returnT = null;
        TwistPortal portal = null; ClueNpc npc = null; WeaponPickup weapon = null;

        for (int r = 0; r < H; r++)
        {
            for (int c = 0; c < W; c++)
            {
                var cell = new Vector3Int(c, H - 1 - r, 0);
                ground.SetTile(cell, lit);
                if (roadSet.Contains(cell)) roads.SetTile(cell, GetTile("road_" + Shape(roadSet, cell)));
                if (fog[r][c] == 'F') dark.SetTile(cell, fogTile);

                Vector3 pos = ground.GetCellCenterWorld(cell);
                switch (obj[r][c])
                {
                    case 'S':
                        var p = GameObject.Find("Player");
                        if (p != null) { p.transform.position = pos; playerT = p.transform; }
                        else Debug.LogWarning("No object named 'Player' in the scene.");
                        break;
                    case 'r':
                        var rp = new GameObject("ReturnPoint"); rp.transform.SetParent(parent); rp.transform.position = pos;
                        returnT = rp.transform;
                        break;
                    case 'Q':
                        var pg = new GameObject("TwistPortal"); pg.transform.SetParent(parent); pg.transform.position = pos;
                        var psr = pg.AddComponent<SpriteRenderer>();
                        psr.sprite = LoadSprite("twist_portal");
                        psr.sortingLayerID = layerId; psr.sortingOrder = order;
                        var pc = pg.AddComponent<CircleCollider2D>(); pc.isTrigger = true; pc.radius = 0.4f;
                        portal = pg.AddComponent<TwistPortal>();
                        break;
                    case 'N':
                        var prefab = FindNpcPrefab();
                        GameObject n;
                        if (prefab != null) { n = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent); }
                        else
                        {
                            Debug.LogWarning("No NPC prefab (with a ClueNpc on it) found. Placed a plain marker, replace it with your NPC prefab.");
                            n = new GameObject("NPC_marker"); n.transform.SetParent(parent);
                            var nsr = n.AddComponent<SpriteRenderer>(); nsr.sprite = LoadSprite("npc_penny");
                            nsr.sortingLayerID = layerId; nsr.sortingOrder = order;
                            var nc = n.AddComponent<CircleCollider2D>(); nc.isTrigger = true; nc.radius = 1.2f;
                            n.AddComponent<ClueNpc>();
                        }
                        n.transform.position = pos;
                        npc = n.GetComponentInChildren<ClueNpc>();
                        break;
                    case 'X':
                        var wg = new GameObject("WeaponPickup"); wg.transform.SetParent(parent); wg.transform.position = pos;
                        var wsr = wg.AddComponent<SpriteRenderer>();
                        wsr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                        wsr.color = new Color(1f, 0.84f, 0f);
                        wsr.sortingLayerID = layerId; wsr.sortingOrder = order;
                        wg.transform.localScale = Vector3.one * (0.5f / wsr.sprite.bounds.size.x);
                        var wc = wg.AddComponent<CircleCollider2D>(); wc.isTrigger = true; wc.radius = 0.8f / wg.transform.localScale.x;
                        weapon = wg.AddComponent<WeaponPickup>();
                        break;
                    case 'B':
                        if (landmarks != null) landmarks.SetTile(cell, GetTile("landmark_bank"));
                        break;
                }
            }
        }

        // camera bounds for the Cinemachine Confiner 2D
        var cb = GameObject.Find("CameraBounds");
        if (cb == null) cb = new GameObject("CameraBounds");
        cb.transform.position = new Vector3(W / 2f, H / 2f, 0);
        var box = cb.GetComponent<BoxCollider2D>();
        if (box == null) box = cb.AddComponent<BoxCollider2D>();
        box.size = new Vector2(W + 12, H + 12);
        box.isTrigger = true;

        // tutorial manager, pre-wired
        var tmGo = GameObject.Find("TutorialManager");
        if (tmGo == null) tmGo = new GameObject("TutorialManager");
        var tm = tmGo.GetComponent<TutorialManager>();
        if (tm == null) tm = tmGo.AddComponent<TutorialManager>();
        tm.player = playerT; tm.portal = portal; tm.npc = npc; tm.weapon = weapon; tm.returnPoint = returnT;
        var arrow = UnityEngine.Object.FindFirstObjectByType<GuideArrow>();
        if (arrow != null) { tm.arrow = arrow; arrow.player = playerT; }
        EditorUtility.SetDirty(tm);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("Level 1 built: " + roadSet.Count + " road cells. Now run Tools > Maze > Populate Negative Space.");
    }

    static string[] Rows(string s)
    {
        var list = new List<string>();
        foreach (var line in s.Split('\n'))
        {
            var t = line.TrimEnd('\r');
            if (t.Length > 0) list.Add(t);
        }
        return list.ToArray();
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

    static GameObject FindNpcPrefab()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            if (go != null && go.GetComponentInChildren<ClueNpc>(true) != null) return go;
        }
        return null;
    }

    static Sprite LoadSprite(string name)
    {
        foreach (var guid in AssetDatabase.FindAssets(name + " t:Sprite"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == name) return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        Debug.LogWarning("Sprite not found: " + name + " (import it into the project first)");
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
                if (Path.GetFileNameWithoutExtension(path) == name) { t = AssetDatabase.LoadAssetAtPath<TileBase>(path); break; }
            }
            if (t == null) Debug.LogWarning("Tile asset not found: " + name);
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
