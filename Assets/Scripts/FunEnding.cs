using UnityEngine;

// Level2 ending. Put on the hidden "Fun" object.
// When the player reaches Fun: freeze player -> dialogue -> transitions to Outro scene.
public class FunEnding : MonoBehaviour
{
    public SimpleDialogue dialogue;           // lines for the Fun/Light conversation
    public GameObject[] enableOnStart;        // e.g. the 2D Light character sprite next to Fun
    public string outroScene = "Outro";       // The name of your Outro video scene

    bool started;

    void OnTriggerEnter2D(Collider2D c)
    {
        if (started || !c.CompareTag("Player")) return;
        started = true;
        
        FreezePlayer();
        foreach (var g in enableOnStart) if (g != null) g.SetActive(true);
        
        if (dialogue != null) 
        { 
            dialogue.Finished += LoadOutro; 
            dialogue.Play(); 
        }
        else LoadOutro();
    }

    void FreezePlayer()
    {
        var pc = FindFirstObjectByType<PlayerController>();
        if (pc == null) return;
        pc.enabled = false;
        var rb = pc.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
        var lr = pc.GetComponent<LightReveal>();
        if (lr != null) lr.enabled = false;
    }

    void LoadOutro()
    {
        // Jumps straight to the Outro video scene once dialogue finishes
        SceneTransition.Go(outroScene);
    }
}