using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Adds "MAIN MENU" buttons to the in-game menus by cloning existing buttons, so they match
/// the current style without editing the scene:
///   ESC menu:   RESUME, MAIN MENU (new), EXIT (now quits the game)
///   Death menu: RESTART, MAIN MENU (new)
/// Buttons are found by the GameLogic method they call, not by name.
/// </summary>
public static class MenuExtras
{
    private const string AddedName = "MainMenuButton";
    [Tooltip("Pixels of empty space between RESTART and MAIN MENU on the death screen.")]
    private const float DeathMenuGap = 40f;

    public static void Setup(GameLogic logic, GameObject escMenu, GameObject losingCanvas)
    {
        SetupEscMenu(logic, escMenu);
        SetupDeathMenu(logic, losingCanvas);
    }

    private static void SetupEscMenu(GameLogic logic, GameObject escMenu)
    {
        if (escMenu == null || HasAdded(escMenu)) return;

        Button resume = FindByMethod(escMenu, "ResumeGame");
        Button exit = FindByMethod(escMenu, "ExitGame");
        if (exit == null) return;

        // Clone EXIT: it already closes the menu and calls ExitGame (= load main menu).
        Button mainMenu = Clone(exit, "MAIN MENU");

        // Put MAIN MENU where EXIT was, and move EXIT one slot further down.
        var exitRect = (RectTransform)exit.transform;
        var menuRect = (RectTransform)mainMenu.transform;
        float step = resume != null
            ? exitRect.anchoredPosition.y - ((RectTransform)resume.transform).anchoredPosition.y
            : -(exitRect.rect.height + 20f);
        menuRect.anchoredPosition = exitRect.anchoredPosition;
        exitRect.anchoredPosition += new Vector2(0f, step);
        mainMenu.transform.SetSiblingIndex(exit.transform.GetSiblingIndex());

        // EXIT now really quits.
        DisablePersistent(exit, "ExitGame");
        exit.onClick.AddListener(logic.QuitGame);
    }

    private static void SetupDeathMenu(GameLogic logic, GameObject losingCanvas)
    {
        if (losingCanvas == null || HasAdded(losingCanvas)) return;

        Button restart = FindByMethod(losingCanvas, "Restart");
        if (restart == null) return;

        Button mainMenu = Clone(restart, "MAIN MENU");
        DisablePersistent(mainMenu, "Restart");
        mainMenu.onClick.AddListener(logic.GoToMainMenu);

        // Space by the button's on-screen height (it is scaled up in the scene) plus a clear gap.
        var restartRect = (RectTransform)restart.transform;
        float visibleHeight = restartRect.rect.height * Mathf.Abs(restartRect.localScale.y);
        ((RectTransform)mainMenu.transform).anchoredPosition =
            restartRect.anchoredPosition - new Vector2(0f, visibleHeight + DeathMenuGap);

        // Final score / highscore above the buttons.
        ScoreReadout.Create(restart.transform.parent,
            restartRect.anchoredPosition + new Vector2(0f, visibleHeight + 90f));
    }

    // ------------------------------------------------------------------ helpers

    private static bool HasAdded(GameObject root)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == AddedName) return true;
        return false;
    }

    private static Button FindByMethod(GameObject root, string method)
    {
        foreach (var b in root.GetComponentsInChildren<Button>(true))
        {
            for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
                if (b.onClick.GetPersistentMethodName(i) == method) return b;
        }
        return null;
    }

    private static Button Clone(Button source, string label)
    {
        GameObject go = Object.Instantiate(source.gameObject, source.transform.parent, false);
        go.name = AddedName;

        var tmp = go.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) tmp.text = label;
        var legacy = go.GetComponentInChildren<Text>(true);
        if (legacy != null) legacy.text = label;

        return go.GetComponent<Button>();
    }

    private static void DisablePersistent(Button button, string method)
    {
        for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            if (button.onClick.GetPersistentMethodName(i) == method)
                button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
    }
}
