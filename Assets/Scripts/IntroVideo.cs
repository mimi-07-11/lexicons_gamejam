using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Video;

// Put on an empty object in a scene called "Intro" (first scene in Build Settings).
// Plays Assets/StreamingAssets/<fileName> (.mp4, H.264). Web builds can't use imported VideoClips, only URLs,
// and browsers block autoplay, so the player clicks first. If the video fails to load we skip straight to the game.
public class IntroVideo : MonoBehaviour
{
    public string fileName = "intro.mp4";
    public string nextScene = "Level1";
    public string title = "TWIST & TURN";
    public float prepareTimeout = 12f;

    enum State { Loading, Ready, Playing, Done }
    State state = State.Loading;
    VideoPlayer vp;
    RenderTexture rt;
    float t0;

    void Start()
    {
        rt = new RenderTexture(1280, 720, 0);
        rt.Create();
        vp = gameObject.AddComponent<VideoPlayer>();
        vp.playOnAwake = false;
        vp.isLooping = false;
        vp.source = VideoSource.Url;
        vp.url = Application.streamingAssetsPath + "/" + fileName;
        vp.renderMode = VideoRenderMode.RenderTexture;
        vp.targetTexture = rt;
        vp.aspectRatio = VideoAspectRatio.FitInside;
        vp.audioOutputMode = VideoAudioOutputMode.Direct;
        vp.prepareCompleted += OnPrepared;
        vp.loopPointReached += OnEnded;
        vp.errorReceived += OnError;
        vp.Prepare();
        t0 = Time.unscaledTime;
    }

    void OnPrepared(VideoPlayer p) { if (state == State.Loading) state = State.Ready; }
    void OnEnded(VideoPlayer p) { Finish(); }
    void OnError(VideoPlayer p, string msg) { Debug.LogWarning("IntroVideo: " + msg + " - skipping intro."); Finish(); }

    void Update()
    {
        if (state == State.Loading && Time.unscaledTime - t0 > prepareTimeout)
        {
            Debug.LogWarning("IntroVideo: video did not load in time - skipping intro.");
            Finish();
            return;
        }
        Keyboard k = Keyboard.current; Mouse m = Mouse.current;
        if (state == State.Ready)
        {
            bool any = (k != null && k.anyKey.wasPressedThisFrame) || (m != null && m.leftButton.wasPressedThisFrame);
            if (any) { vp.Play(); state = State.Playing; }
        }
        else if (state == State.Playing)
        {
            if (k != null && k.escapeKey.wasPressedThisFrame) Finish();
        }
    }

    void Finish()
    {
        if (state == State.Done) return;
        state = State.Done;
        if (vp != null) vp.Stop();
        SceneTransition.Go(nextScene);
    }

    void OnDestroy() { if (rt != null) rt.Release(); }

    void OnGUI()
    {
        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
        var center = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };

        if (state == State.Playing)
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), rt, ScaleMode.ScaleToFit, false);
            if (GUI.Button(new Rect(Screen.width - 170, Screen.height - 60, 150, 40), "Skip  (Esc)")) Finish();
        }
        else if (state != State.Done)
        {
            center.fontSize = Mathf.RoundToInt(Screen.height * 0.1f); center.fontStyle = FontStyle.Bold;
            GUI.Label(new Rect(0, Screen.height * 0.28f, Screen.width, Screen.height * 0.2f), title, center);
            center.fontSize = Mathf.RoundToInt(Screen.height * 0.035f); center.fontStyle = FontStyle.Normal;
            string msg = state == State.Ready ? "Click or press any key to start" : "Loading...";
            if (state == State.Ready && Mathf.Repeat(Time.unscaledTime, 1.2f) > 0.9f) msg = "";
            GUI.Label(new Rect(0, Screen.height * 0.55f, Screen.width, 60), msg, center);
        }
    }
}
