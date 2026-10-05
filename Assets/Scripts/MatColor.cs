using UnityEngine;

// Safe color get/set that works with URP Lit (_BaseColor), Built-in (_Color) and ignores shaders with neither.
public static class MatColor
{
    public static Color Get(Renderer r)
    {
        if (r == null) return Color.white;
        var m = r.material;
        if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor");
        if (m.HasProperty("_Color")) return m.GetColor("_Color");
        return Color.white;
    }

    public static void Set(Renderer r, Color c)
    {
        if (r == null) return;
        var m = r.material;
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
    }
}
