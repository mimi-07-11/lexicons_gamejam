using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Creates itself at game start (no scene setup). Loads clips from Assets/Resources/Audio/<name>.
// AudioManager.Play("hit");  AudioManager.Music("music_2d");  A missing clip is simply skipped.
// Music switches automatically by scene name (table below). Press M in game to mute.
public class AudioManager : MonoBehaviour
{
    public static float MusicVolume = 0.45f;
    public static float SfxVolume = 0.9f;

    static AudioManager inst;
    AudioSource musicSrc;
    AudioSource[] pool;
    int poolIdx;
    readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
    string currentMusic = "";
    Coroutine fade;
    bool muted;

    // scene name -> music clip name (leave a scene out for silence, e.g. Intro)
    static readonly Dictionary<string, string> sceneMusic = new Dictionary<string, string>
    {
        { "Level1", "music_2d" }, { "Level2", "music_2d" },
        { "Arena", "music_3d" }, { "Arena2", "music_3d_final" },
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { inst = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        GameObject go = new GameObject("AudioManager");
        DontDestroyOnLoad(go);
        inst = go.AddComponent<AudioManager>();
    }

    void Awake()
    {
        musicSrc = gameObject.AddComponent<AudioSource>();
        musicSrc.loop = true; musicSrc.playOnAwake = false; musicSrc.spatialBlend = 0f;
        pool = new AudioSource[8];
        for (int i = 0; i < pool.Length; i++)
        {
            pool[i] = gameObject.AddComponent<AudioSource>();
            pool[i].playOnAwake = false; pool[i].spatialBlend = 0f;
        }
        SceneManager.sceneLoaded += OnSceneLoaded;
        LightEnergy.Empty += OnEmpty;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        LightEnergy.Empty -= OnEmpty;
    }

    void OnEmpty() { Play("empty"); }

    void OnSceneLoaded(Scene s, LoadSceneMode mode)
    {
        string clip;
        if (!sceneMusic.TryGetValue(s.name, out clip)) clip = "";
        if (s.name == "Arena2" && Get("music_3d_final") == null) clip = "music_3d";
        SetMusic(clip);
    }

    void Update()
    {
        Keyboard k = Keyboard.current;
        if (k != null && k.mKey.wasPressedThisFrame)
        {
            muted = !muted;
            AudioListener.volume = muted ? 0f : 1f;
        }
    }

    AudioClip Get(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        AudioClip c;
        if (cache.TryGetValue(name, out c)) return c;
        c = Resources.Load<AudioClip>("Audio/" + name);
#if UNITY_EDITOR
        if (c == null) Debug.LogWarning("AudioManager: no clip 'Assets/Resources/Audio/" + name + "' (skipped)");
#endif
        cache[name] = c;
        return c;
    }

    public static void Play(string name, float volume = 1f, float pitchVariation = 0.06f)
    {
        if (inst == null) return;
        AudioClip c = inst.Get(name);
        if (c == null) return;
        AudioSource s = inst.pool[inst.poolIdx];
        inst.poolIdx = (inst.poolIdx + 1) % inst.pool.Length;
        s.clip = c;
        s.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        s.volume = SfxVolume * volume;
        s.Play();
    }

    public static void Music(string name) { if (inst != null) inst.SetMusic(name); }
    public static void StopMusic() { if (inst != null) inst.SetMusic(""); }

    void SetMusic(string name)
    {
        if (name == currentMusic) return;
        currentMusic = name;
        if (fade != null) StopCoroutine(fade);
        fade = StartCoroutine(FadeTo(name));
    }

    IEnumerator FadeTo(string name)
    {
        float v0 = musicSrc.isPlaying ? musicSrc.volume : 0f;
        for (float t = 0f; t < 0.5f; t += Time.unscaledDeltaTime)
        {
            musicSrc.volume = Mathf.Lerp(v0, 0f, t / 0.5f);
            yield return null;
        }
        musicSrc.Stop();
        AudioClip c = Get(name);
        if (c == null) yield break;
        musicSrc.clip = c;
        musicSrc.volume = 0f;
        musicSrc.Play();
        for (float t = 0f; t < 0.8f; t += Time.unscaledDeltaTime)
        {
            musicSrc.volume = Mathf.Lerp(0f, MusicVolume, t / 0.8f);
            yield return null;
        }
        musicSrc.volume = MusicVolume;
    }
}
