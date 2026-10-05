using UnityEngine;

// Lets multi-part models (robot, enemies) flash/tint as one. Uses property blocks, so no material copies are made.
public class FlashTint : MonoBehaviour
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static MaterialPropertyBlock block;
    Renderer[] rends;

    void Awake() { rends = GetComponentsInChildren<Renderer>(true); }

    public void Set(Color c)
    {
        if (rends == null) rends = GetComponentsInChildren<Renderer>(true);
        if (block == null) block = new MaterialPropertyBlock();
        foreach (var r in rends)
        {
            if (r == null) continue;
            r.GetPropertyBlock(block);
            block.SetColor(BaseColorId, c);
            block.SetColor(ColorId, c);
            r.SetPropertyBlock(block);
        }
    }

    public void Restore()
    {
        if (rends == null) return;
        foreach (var r in rends) if (r != null) r.SetPropertyBlock(null);
    }
}
