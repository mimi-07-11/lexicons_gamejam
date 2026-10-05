using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// "Twist" spiral wipe between scenes (2D <-> 3D). Call SceneTransition.Go("Arena") instead of SceneManager.LoadScene.
public class SceneTransition : MonoBehaviour
{
    static SceneTransition inst;
    static bool busy;
    Texture2D spiral;
    float t, angle;
    bool drawing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { inst = null; busy = false; }

    public static void Go(string sceneName)
    {
        if (busy) return;
        if (inst == null)
        {
            GameObject go = new GameObject("SceneTransition");
            DontDestroyOnLoad(go);
            inst = go.AddComponent<SceneTransition>();
        }
        inst.StartCoroutine(inst.Run(sceneName));
    }

    IEnumerator Run(string scene)
    {
        busy = true; drawing = true; t = 0f;
        if (spiral == null) BuildSpiral();
        AudioManager.Play("portal");
        yield return Animate(0f, 1f, 0.8f);
        AsyncOperation op = SceneManager.LoadSceneAsync(scene);
        while (!op.isDone) { angle += 420f * Time.unscaledDeltaTime; yield return null; }
        yield return new WaitForSecondsRealtime(0.15f);
        yield return Animate(1f, 0f, 0.7f);
        drawing = false; busy = false;
    }

    IEnumerator Animate(float from, float to, float dur)
    {
        for (float e = 0f; e < dur; e += Time.unscaledDeltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, e / dur);
            t = Mathf.Lerp(from, to, k);
            angle += 420f * Time.unscaledDeltaTime;
            yield return null;
        }
        t = to;
    }

    void BuildSpiral()
    {
        const int N = 256;
        spiral = new Texture2D(N, N, TextureFormat.RGBA32, false);
        spiral.wrapMode = TextureWrapMode.Clamp;
        Color dark = new Color(0.08f, 0.08f, 0.12f, 1f);
        Color gold = new Color(1f, 0.85f, 0.2f, 1f);
        Color[] px = new Color[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float nx = (x - 127.5f) / 127.5f, ny = (y - 127.5f) / 127.5f;
                float r = Mathf.Sqrt(nx * nx + ny * ny);
                float a = Mathf.Clamp01((1f - r) * 127f);
                float v = Mathf.Repeat(Mathf.Atan2(ny, nx) / (2f * Mathf.PI) * 3f + r * 3f, 1f);
                Color c = v < 0.5f ? dark : gold;
                if (r > 0.92f) c = Color.white;
                c.a = a;
                px[y * N + x] = c;
            }
        spiral.SetPixels(px);
        spiral.Apply();
    }

    void OnGUI()
    {
        if (!drawing || spiral == null) return;
        GUI.depth = -1000;
        float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;
        float diag = Mathf.Sqrt(Screen.width * Screen.width + Screen.height * Screen.height);
        float d = Mathf.Lerp(0f, diag * 1.45f, t);
        Matrix4x4 old = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, new Vector2(cx, cy));
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(cx - d * 0.5f, cy - d * 0.5f, d, d), spiral, ScaleMode.StretchToFill, true);
        GUI.matrix = old;
    }
}
