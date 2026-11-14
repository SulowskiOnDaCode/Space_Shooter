using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central scene navigation. Every scene load resets the time scale and audio, so effects like
/// the death slow-motion (or a paused ESC menu) can never leak into the next scene.
/// </summary>
public static class SceneFlow
{
    public const int MainMenuScene = 0;
    public const int GameScene = 1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // Runs after the old scene's objects (and their coroutines) are gone, before Start().
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetTime();
        if (scene.buildIndex == MainMenuScene) HighscoreBoard.Create();
    }

    public static void ResetTime()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        // The ESC menu mutes audio; restore the player's saved master volume (set in the options menu).
        AudioListener.volume = PlayerPrefs.GetFloat("masterVolume", 1f);
    }

    public static void RestartGame()
    {
        ResetTime();
        SceneManager.LoadScene(GameScene);
    }

    public static void LoadMainMenu()
    {
        ResetTime();
        SceneManager.LoadScene(MainMenuScene);
    }

    public static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
