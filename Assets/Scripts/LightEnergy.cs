using System;
using System.Collections.Generic;
using UnityEngine;

// Static, so it survives the trip to the 3D scene and back.
// Energy is stored as 0..1:  1 = a FULL bar = enough to reveal every revealable dark cell of the level.
public static class LightEnergy
{
    public static float Current { get; private set; }          // 0..1
    public static float CostPerCell { get; private set; } = 0.01f;
    public static string LevelId { get; private set; } = "";

    // 1 = a full bar reveals exactly all the darkness. Use 0.7 to make a full bar reveal only 70% (harder).
    public static float CostMultiplier = 1f;

    public static event Action<float> Changed;                  // the bar listens to this
    public static event Action Empty;                           // fired when the player tries to light with an empty bar

    static readonly HashSet<Vector3Int> revealed = new HashSet<Vector3Int>();
    public static IEnumerable<Vector3Int> Revealed { get { return revealed; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        Current = 0f; LevelId = ""; revealed.Clear(); Changed = null; Empty = null;
    }

    // Call when a 2D level starts. Returns true if it is a NEW level (fog starts full, bar starts at startFraction).
    // Returns false when we are just coming back from the 3D scene (energy and revealed paths are kept).
    public static bool BeginLevel(string id, int revealableCells, float startFraction)
    {
        CostPerCell = revealableCells > 0 ? CostMultiplier / revealableCells : 1f;
        bool fresh = id != LevelId;
        if (fresh)
        {
            LevelId = id;
            revealed.Clear();
            Current = Mathf.Clamp01(startFraction);
        }
        if (Changed != null) Changed(Current);
        return fresh;
    }

    // Call from the 3D scene when the arena is won.
    public static void Refill() { Add(1f); }

    public static void Add(float fraction)
    {
        Current = Mathf.Clamp01(Current + fraction);
        if (Changed != null) Changed(Current);
    }

    public static int AffordableCells()
    {
        return Mathf.FloorToInt(Current / CostPerCell + 0.0001f);
    }

    public static void Spend(int cells)
    {
        Current = Mathf.Max(0f, Current - cells * CostPerCell);
        if (Current < 0.0001f) Current = 0f;
        if (Changed != null) Changed(Current);
    }

    public static void MarkRevealed(Vector3Int cell) { revealed.Add(cell); }
    public static void NotifyEmpty() { if (Empty != null) Empty(); }
}
