using System;
using UnityEngine;

[Serializable]
public class DialogueLine
{
    public string speaker = "Light";
    [TextArea(2, 4)] public string text;
}

// Drop-in typewriter dialogue box (drawn with OnGUI, no Canvas needed). Works in 2D and 3D scenes.
// Advance with J / Space / mouse click. Call Play() from an event (e.g. ArenaManager > On Win).
public class SimpleDialogue : MonoBehaviour
{
    public DialogueLine[] lines =
    {
        new DialogueLine { speaker = "Light", text = "You... you actually beat the Fun Police? Nobody has ever done that." },
        new DialogueLine { speaker = "You",   text = "I came to bring the light back. The comic world is stuck in the dark, and Fun is lost in it." },
        new DialogueLine { speaker = "Light", text = "The two worlds were never meant to be apart. Take my light. It will show you the way." },
        new DialogueLine { speaker = "You",   text = "Thank you. I'll take it home." },
        new DialogueLine { speaker = "Light", text = "Go through the Twist and find Fun before the dark wins!" },
    };

    public float charsPerSecond = 40f;
    public GameObject[] enableWhenStarted;    // e.g. the Light character
    public bool placeInFrontOfPlayer = true;  // 3D arena: moves those objects 3 units in front of the player so they are always on screen
    public GameObject[] enableWhenFinished;   // e.g. the exit portal

    public event Action Finished;

    bool playing; int index; float lineStart, startedAt;

    public void Play()
    {
        if (playing || lines == null || lines.Length == 0) return;
        Debug.Log("SimpleDialogue.Play: " + lines.Length + " lines, " + enableWhenStarted.Length + " object(s) to show");
        var p = FighterController.Instance;
        foreach (var g in enableWhenStarted)
        {
            if (g == null) { Debug.LogWarning("SimpleDialogue: an 'Enable When Started' slot is empty."); continue; }
            if (placeInFrontOfPlayer && p != null)
            {
                Vector3 pos = p.transform.position + p.transform.forward * 3f;
                pos.y = g.transform.position.y;
                g.transform.position = pos;
                Vector3 look = p.transform.position - pos; look.y = 0f;
                if (look.sqrMagnitude > 0.01f) g.transform.rotation = Quaternion.LookRotation(look);
            }
            g.SetActive(true);
        }
        AudioManager.Play("blip");
        playing = true; index = 0;
        lineStart = startedAt = Time.unscaledTime;
    }

    void Update()
    {
        if (!playing || Time.unscaledTime - startedAt < 0.4f) return;   // ignore the press that started it
        if (!(GameInput.AttackPressed || GameInput.DodgePressed)) return;

        int shown = Mathf.FloorToInt((Time.unscaledTime - lineStart) * charsPerSecond);
        if (shown < lines[index].text.Length) { lineStart = -9999f; return; }   // finish the line instantly

        index++;
        if (index >= lines.Length) { End(); return; }
        lineStart = Time.unscaledTime;
        AudioManager.Play("blip");
    }

    void End()
    {
        playing = false;
        Debug.Log("SimpleDialogue: dialogue finished, enabling " + enableWhenFinished.Length + " object(s).");
        foreach (var g in enableWhenFinished)
        {
            if (g == null) { Debug.LogWarning("SimpleDialogue: an 'Enable When Finished' slot is empty."); continue; }
            if (!g.scene.IsValid())
            {
                Debug.LogWarning("SimpleDialogue: '" + g.name + "' is a Project ASSET, not a scene object. Drag it from the Hierarchy instead.");
                continue;
            }
            // a hidden parent keeps the child invisible, so switch parents on too
            for (Transform p = g.transform.parent; p != null; p = p.parent)
                if (!p.gameObject.activeSelf) p.gameObject.SetActive(true);
            g.SetActive(true);
            Debug.Log("SimpleDialogue: enabled '" + g.name + "'");
        }
        if (Finished != null) Finished();
    }

    void OnGUI()
    {
        if (!playing) return;
        var line = lines[index];
        int shown = Mathf.Clamp(Mathf.FloorToInt((Time.unscaledTime - lineStart) * charsPerSecond), 0, line.text.Length);

        float h = Screen.height * 0.24f;
        var box = new Rect(Screen.width * 0.08f, Screen.height - h - 24f, Screen.width * 0.84f, h);
        GUI.color = new Color(0f, 0f, 0f, 0.82f);
        GUI.DrawTexture(box, Texture2D.whiteTexture);
        GUI.color = Color.white;

        Color nameColor = line.speaker == "Light" ? new Color(1f, 0.9f, 0.3f)
                        : line.speaker == "Fun" ? new Color(1f, 0.45f, 0.8f)
                        : new Color(0.5f, 0.9f, 1f);
        var nameStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(Screen.height * 0.04f), fontStyle = FontStyle.Bold };
        nameStyle.normal.textColor = nameColor;
        GUI.Label(new Rect(box.x + 20, box.y + 8, box.width - 40, h * 0.3f), line.speaker, nameStyle);

        var textStyle = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(Screen.height * 0.032f), wordWrap = true };
        GUI.Label(new Rect(box.x + 20, box.y + h * 0.3f, box.width - 40, h * 0.6f), line.text.Substring(0, shown), textStyle);

        var hint = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(Screen.height * 0.022f), alignment = TextAnchor.LowerRight };
        GUI.Label(new Rect(box.x, box.y, box.width - 16, h - 8), "J / Space / Click", hint);
    }
}
