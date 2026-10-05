using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Video;

// Put on an empty object in a scene called "Outro".
// Plays Assets/StreamingAssets/outro.mp4 and skips straight to playing.
public class OutroVideo : MonoBehaviour
{
    public string fileName = "outro.mp4";
    public string nextScene = "Intro";
    public float prepareTimeout = 12f;

    enum State { Loading, Playing, Done }
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

    void OnPrepared(VideoPlayer p) 
    { 
        if (state == State.Loading) 
        {
            state = State.Playing;
            vp.Play();
        }
    }
    
    void OnEnded(VideoPlayer p) { Finish(); }
    void OnError(VideoPlayer p, string msg) { Debug.LogWarning("OutroVideo: " + msg + " - skipping outro."); Finish(); }

    void Update()
    {
        if (state == State.Loading && Time.unscaledTime - t0 > prepareTimeout)
        {
            Debug.LogWarning("OutroVideo: video did not load in time - skipping outro.");
            Finish();
            return;
        }
        
        Keyboard k = Keyboard.current; 
        
        if (state == State.Playing)
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
        
        if (state == State.Playing)
        {
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), rt, ScaleMode.ScaleToFit, false);
            if (GUI.Button(new Rect(Screen.width - 170, Screen.height - 60, 150, 40), "Skip  (Esc)")) Finish();
        }
        else if (state != State.Done)
        {
            var center = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            center.fontSize = Mathf.RoundToInt(Screen.height * 0.035f); 
            GUI.Label(new Rect(0, Screen.height * 0.55f, Screen.width, 60), "Loading...", center);
        }
    }
}