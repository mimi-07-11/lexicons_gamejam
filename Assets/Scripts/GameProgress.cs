using UnityEngine;

// Small flags that survive scene loads.
public static class GameProgress
{
    public static bool Arena1Won;
    public static bool Arena2Won;
    // Set true by Arena2 on win. Level2 reads it to start with a FULL light bar.
    public static bool FinalLightCarried;

    public static void ResetAll() { Arena1Won = Arena2Won = FinalLightCarried = false; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Arena1Won = Arena2Won = FinalLightCarried = false; }
}
