using UnityEngine;

// Final screen. With an end image: fades to black, shows your friend's image, then a Play Again button.
// Without one: rainbow color wash + THE END text. Created at runtime by FunEnding on its OWN object.
public class EndScreen : MonoBehaviour
{
    float seconds = 4f, start;
    string title, body, restartScene;
    Texture2D rainbow, image;

    public void Init(float washSeconds, string t, string b, string restart, Texture2D img = null)
    {
        seconds = Mathf.Max(0.1f, washSeconds); title = t; body = b; restartScene = restart; image = img;
        start = Time.unscaledTime;
        rainbow = new Texture2D(256, 1);
        rainbow.wrapMode = TextureWrapMode.Clamp;
        for (int i = 0; i < 256; i++) rainbow.SetPixel(i, 0, Color.HSVToRGB(i / 255f, 0.55f, 1f));
        rainbow.Apply();
        AudioManager.Music("music_end");
    }

    void Restart()
    {
        GameProgress.ResetAll();
        WeaponLoadout.ResetToStarter();
        AudioManager.Play("click");
        SceneTransition.Go(restartScene);
    }

    void OnGUI()
    {
        if (rainbow == null) return;
        GUI.depth = -100;
        float t = Mathf.Clamp01((Time.unscaledTime - start) / seconds);
        Rect full = new Rect(0, 0, Screen.width, Screen.height);

        if (image != null)
        {
            GUI.color = new Color(0f, 0f, 0f, t);
            GUI.DrawTexture(full, Texture2D.whiteTexture);
            GUI.color = new Color(1f, 1f, 1f, t);
            GUI.DrawTexture(full, image, ScaleMode.ScaleToFit);
            GUI.color = Color.white;
            if (t >= 1f && GUI.Button(new Rect(Screen.width * 0.5f - 110, Screen.height - 80, 220, 50), "Play Again")) Restart();
            return;
        }

        GUI.color = new Color(1f, 1f, 1f, t * 0.8f);
        GUI.DrawTexture(full, rainbow, ScaleMode.StretchToFill);
        GUI.color = Color.white;
        if (t < 1f) return;

        var ts = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(Screen.height * 0.12f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        ts.normal.textColor = Color.white;
        GUI.Label(new Rect(0, Screen.height * 0.18f, Screen.width, Screen.height * 0.2f), title, ts);
        var bs = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(Screen.height * 0.04f), alignment = TextAnchor.UpperCenter, wordWrap = true };
        bs.normal.textColor = Color.white;
        GUI.Label(new Rect(Screen.width * 0.1f, Screen.height * 0.4f, Screen.width * 0.8f, Screen.height * 0.3f), body, bs);
        if (GUI.Button(new Rect(Screen.width * 0.5f - 110, Screen.height * 0.75f, 220, 50), "Play Again")) Restart();
    }
}
