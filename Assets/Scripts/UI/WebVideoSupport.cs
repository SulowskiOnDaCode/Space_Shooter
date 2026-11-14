using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

/// <summary>
/// Browsers can't play Unity VideoClips. In WebGL builds this switches every VideoPlayer to the
/// same video file inside StreamingAssets/Video (copied there by the WebGL build script).
/// Does nothing in the editor or in the Windows build.
/// </summary>
public static class WebVideoSupport
{
#if UNITY_WEBGL && !UNITY_EDITOR
    // Clip name -> file name in StreamingAssets/Video.
    private static readonly System.Collections.Generic.Dictionary<string, string> Files =
        new System.Collections.Generic.Dictionary<string, string>
        {
            { "Sfxstart2-1", "Sfxstart2-1.mp4" },
            { "anim", "anim.webm" },
            { "GAMEOVERSCREEN", "GAMEOVERSCREEN.webm" },
            { "Win-1", "Win-1.mp4" },
        };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        foreach (var player in Object.FindObjectsByType<VideoPlayer>(FindObjectsInactive.Include))
        {
            if (player.source != VideoSource.VideoClip || player.clip == null) continue;
            if (!Files.TryGetValue(player.clip.name, out string file)) continue;

            bool wasPlaying = player.isPlaying || player.playOnAwake;
            player.source = VideoSource.Url;
            player.url = Path.Combine(Application.streamingAssetsPath, "Video", file);
            if (wasPlaying && player.isActiveAndEnabled) player.Play();
        }
    }
#endif
}
