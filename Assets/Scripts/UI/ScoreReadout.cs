using TMPro;
using UnityEngine;

/// <summary>Death screen readout: final score, best score and a "new highscore" note.</summary>
public class ScoreReadout : MonoBehaviour
{
    private TextMeshProUGUI text;

    public static void Create(Transform parent, Vector2 anchoredPosition)
    {
        var label = HudStyle.Label("ScoreReadout", parent, "", TextAlignmentOptions.Center);
        var rt = label.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = new Vector2(900f, 120f);
        label.fontSize = 30f;
        label.characterSpacing = 10f;
        label.color = HudStyle.Solid;
        label.gameObject.AddComponent<ScoreReadout>().text = label;
    }

    private void OnEnable() => Refresh();
    private void Update() => Refresh();

    private void Refresh()
    {
        if (text == null) return;
        string line = $"SCORE  {GameLogic.score}\n<size=60%>BEST  {Mathf.Max(Highscores.Best, GameLogic.score)}</size>";
        if (Highscores.LastWasNewBest) line += "\n<size=55%>NEW HIGHSCORE</size>";
        text.text = line;
    }
}
