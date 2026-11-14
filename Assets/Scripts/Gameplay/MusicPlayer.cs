using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Soundtrack player (Singleton). One instance lives across scene loads (DontDestroyOnLoad)
/// and loops the song from Resources/Music:
///   - Main menu: plays quietly in the background (the intro video has its own audio).
///   - Game: full music volume; the song keeps playing from the menu without restarting.
///   - Win / lose screens: fades out (they have their own audio).
/// Small extras: the music slows down with the death slow-motion and gets muffled while
/// time is frozen.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MusicPlayer : MonoBehaviour
{
    public static MusicPlayer Instance { get; private set; }

    private const string SongPath = "Music/AttackByStorm";
    private const float GameVolume = 0.4f;
    private const float MenuVolume = 0.12f;
    private const float FadeTime = 1.2f;

    private AudioSource source;
    private AudioLowPassFilter lowPass;
    private float targetVolume;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;

        if (scene.buildIndex == SceneFlow.GameScene) GetOrCreate().Play(GameVolume);
        else if (scene.buildIndex == SceneFlow.MainMenuScene) GetOrCreate().Play(MenuVolume);
        else if (Instance != null) Instance.targetVolume = 0f;
    }

    private static MusicPlayer GetOrCreate()
    {
        if (Instance != null) return Instance;
        var go = new GameObject("MusicPlayer");
        DontDestroyOnLoad(go);
        return go.AddComponent<MusicPlayer>(); // Awake assigns Instance
    }

    private void Awake()
    {
        // Classic singleton guard: keep the first instance, discard duplicates.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        source = GetComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = 0f;
        source.clip = Resources.Load<AudioClip>(SongPath);
        if (source.clip == null) Debug.LogWarning($"MusicPlayer: song Resources/{SongPath} not found.");

        lowPass = gameObject.AddComponent<AudioLowPassFilter>();
        lowPass.cutoffFrequency = 22000f;
    }

    private void Play(float volume)
    {
        targetVolume = volume;
        if (source.clip != null && !source.isPlaying) source.Play(); // keeps playing across menu <-> game
    }

    private void Update()
    {
        source.volume = Mathf.MoveTowards(source.volume, targetVolume, Time.unscaledDeltaTime / FadeTime * 0.5f);
        if (targetVolume <= 0f && source.volume <= 0f && source.isPlaying) source.Stop();

        // Slow-motion on death pitches the music down; time freeze muffles it.
        source.pitch = Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(Time.timeScale));
        float cutoff = TimeFreeze.Active ? 900f : 22000f;
        lowPass.cutoffFrequency = Mathf.Lerp(lowPass.cutoffFrequency, cutoff, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
