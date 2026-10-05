using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Creates ONE Global Light 2D while the game runs and keeps it alive across scene loads.
// Put this on any object in Level1 and in Level2 (the check below stops a second light from being made).
// Then DELETE the Global Light 2D object from every scene, so no scene file stores one.
public class PersistentGlobalLight : MonoBehaviour
{
    [Range(0f, 1f)] public float intensity = 1f;

    static Light2D instance;

    void Awake()
    {
        if (instance != null) return;

        var go = new GameObject("Global Light 2D (runtime)");
        instance = go.AddComponent<Light2D>();
        instance.lightType = Light2D.LightType.Global;
        instance.intensity = intensity;

        // a Light 2D made in code only targets the first sorting layer by default, so target them all
        instance.targetSortingLayers = SortingLayer.layers.Select(l => l.id).ToArray();

        DontDestroyOnLoad(go);
    }
}
