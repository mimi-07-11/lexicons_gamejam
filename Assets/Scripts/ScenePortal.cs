using UnityEngine;
using UnityEngine.SceneManagement;

// Trigger collider that loads a scene when the 3D player walks in. Used for the arena exit portals.
[RequireComponent(typeof(Collider))]
public class ScenePortal : MonoBehaviour
{
    public string sceneName = "Level1";   // Arena -> "Level1", Arena2 -> "Level2"
    bool used;

    void Reset() { GetComponent<Collider>().isTrigger = true; }

    void OnTriggerEnter(Collider c)
    {
        if (used) return;
        if (c.GetComponentInParent<FighterController>() == null) return;
        used = true;
        SceneTransition.Go(sceneName);
    }
}
