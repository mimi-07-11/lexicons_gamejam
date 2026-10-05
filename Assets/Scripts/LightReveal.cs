using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

// Put this on the Player. Space (or gamepad A) spends light energy to clear the fog inside the circle.
public class LightReveal : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap darkOverlay;
    public Tilemap roads;

    [Header("Light")]
    public float radius = 2.5f;
    public string levelId = "Level1";                 // change per 2D level, a new id = energy resets to startEnergy
    [Range(0f, 1f)] public float startEnergy = 0f;    // 0 = the player must win the 3D arena first
    public bool clearUnreachableFog = false;          // fog too deep to ever be lit: leave black (false) or remove at start (true)

    [Header("Optional")]
    public SpriteRenderer ring;                       // the circle sprite that shows the radius

    void Start()
    {
        DarkField.Overlay = darkOverlay;

        // which fog cells can the player ever light? those within the radius of some road cell
        var reachable = new HashSet<Vector3Int>();
        int r = Mathf.CeilToInt(radius);
        foreach (var rc in roads.cellBounds.allPositionsWithin)
        {
            if (!roads.HasTile(rc)) continue;
            Vector3 center = roads.GetCellCenterWorld(rc);
            Vector3Int oc = darkOverlay.WorldToCell(center);
            for (int x = -r; x <= r; x++)
                for (int y = -r; y <= r; y++)
                {
                    var cell = oc + new Vector3Int(x, y, 0);
                    if (!darkOverlay.HasTile(cell)) continue;
                    if ((darkOverlay.GetCellCenterWorld(cell) - center).sqrMagnitude <= radius * radius)
                        reachable.Add(cell);
                }
        }

        // Use the inspector's startEnergy unless they brought back the final light
        float start = GameProgress.FinalLightCarried ? 1f : startEnergy; 
        
        // Initialize the level using the current levelId and the calculated reach
        bool fresh = LightEnergy.BeginLevel(levelId, reachable.Count, start);
        if (clearUnreachableFog)
        {
            var dead = new List<Vector3Int>();
            foreach (var p in darkOverlay.cellBounds.allPositionsWithin)
                if (darkOverlay.HasTile(p) && !reachable.Contains(p)) dead.Add(p);
            foreach (var p in dead) darkOverlay.SetTile(p, null);
        }

        // coming back from the 3D scene: put the already-lit paths back
        if (!fresh)
            foreach (var c in LightEnergy.Revealed) darkOverlay.SetTile(c, null);
    }

    void Update()
    {
        var k = Keyboard.current; var g = Gamepad.current;
        bool pressed = (k != null && k.spaceKey.wasPressedThisFrame) || (g != null && g.buttonSouth.wasPressedThisFrame);
        if (pressed) { Debug.Log("space pressed"); TryReveal(); }

        if (ring != null && ring.sprite != null)
        {
            ring.transform.localScale = Vector3.one * (radius * 2f / ring.sprite.bounds.size.x);
            var c = ring.color; c.a = LightEnergy.Current > 0f ? 0.55f : 0.15f; ring.color = c;
        }
    }

    void TryReveal()
{
    var found = new List<KeyValuePair<float, Vector3Int>>();
    Vector3Int oc = darkOverlay.WorldToCell(transform.position);
    int r = Mathf.CeilToInt(radius);
    for (int x = -r; x <= r; x++)
        for (int y = -r; y <= r; y++)
        {
            var cell = oc + new Vector3Int(x, y, 0);
            if (!darkOverlay.HasTile(cell)) continue;
            float d = (darkOverlay.GetCellCenterWorld(cell) - transform.position).magnitude;
            if (d <= radius) found.Add(new KeyValuePair<float, Vector3Int>(d, cell));
        }

    Debug.Log("fog cells in circle: " + found.Count + "  energy: " + LightEnergy.Current +
              "  affordable: " + LightEnergy.AffordableCells() + "  cost/cell: " + LightEnergy.CostPerCell);

    if (found.Count == 0) { Debug.Log("EXIT: no fog inside the circle"); return; }

    int afford = LightEnergy.AffordableCells();
    if (afford <= 0) {LightEnergy.NotifyEmpty(); return; }

    found.Sort((a, b) => a.Key.CompareTo(b.Key));
    int n = Mathf.Min(afford, found.Count);
    for (int i = 0; i < n; i++)
    {
        darkOverlay.SetTile(found[i].Value, null);
        LightEnergy.MarkRevealed(found[i].Value);
    }
    LightEnergy.Spend(n);
    AudioManager.Play("reveal");
    
}
}
