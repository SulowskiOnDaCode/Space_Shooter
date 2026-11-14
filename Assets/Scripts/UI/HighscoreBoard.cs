using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Small top-5 highscore list in the top-right corner of the main menu (HUD style).</summary>
public class HighscoreBoard : MonoBehaviour
{
    public static void Create()
    {
        var go = new GameObject("HighscoreBoard", typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        var panel = HudStyle.Rect("Panel", go.transform);
        HudStyle.Place(panel, new Vector2(1f, 1f), new Vector2(-40f, -40f), new Vector2(260f, 190f));
        HudStyle.CornerBrackets(panel);

        var title = HudStyle.Label("Title", panel, "HIGHSCORES", TextAlignmentOptions.TopLeft);
        HudStyle.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(16f, -14f), new Vector2(228f, 20f));

        var scores = Highscores.Load();
        string lines = "";
        for (int i = 0; i < Highscores.MaxEntries; i++)
            lines += i < scores.Count ? $"{i + 1:00}   {scores[i]}\n" : $"{i + 1:00}   ---\n";

        var list = HudStyle.Label("List", panel, lines, TextAlignmentOptions.TopLeft);
        HudStyle.Place(list.rectTransform, new Vector2(0f, 1f), new Vector2(16f, -44f), new Vector2(228f, 140f));
        list.fontSize = 16f;
        list.characterSpacing = 6f;
        list.lineSpacing = 20f;
        list.color = HudStyle.Solid;

        go.AddComponent<HighscoreBoard>();
    }
}
