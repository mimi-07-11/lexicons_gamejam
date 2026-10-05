using UnityEngine;

// On the robot model: shows the rolled newspaper (Starter) or the golden hammer (anything else).
public class WeaponVisual : MonoBehaviour
{
    public GameObject starter;
    public GameObject superComic;

    void Start() { Apply(); WeaponLoadout.Changed += OnChanged; }
    void OnDestroy() { WeaponLoadout.Changed -= OnChanged; }
    void OnChanged(float hp, float max) { Apply(); }

    void Apply()
    {
        bool super = WeaponLoadout.Current.name != "Starter";
        if (starter != null) starter.SetActive(!super);
        if (superComic != null) superComic.SetActive(super);
    }
}
