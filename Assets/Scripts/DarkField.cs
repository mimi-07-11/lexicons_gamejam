using UnityEngine;
using UnityEngine.Tilemaps;

// Lets NPCs, the weapon, etc. ask "is this spot still buried in darkness?"
public static class DarkField
{
    public static Tilemap Overlay;

    public static bool IsDark(Vector3 world)
    {
        return Overlay != null && Overlay.HasTile(Overlay.WorldToCell(world));
    }
}
