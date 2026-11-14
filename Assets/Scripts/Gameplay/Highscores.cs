using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Local highscore list (top 5), saved on the device with PlayerPrefs.
/// A run's score is submitted when the player dies or wins.
/// </summary>
public static class Highscores
{
    private const string Key = "highscores";
    public const int MaxEntries = 5;

    /// <summary>True if the last submitted score became the new best.</summary>
    public static bool LastWasNewBest { get; private set; }

    public static List<int> Load()
    {
        string raw = PlayerPrefs.GetString(Key, "");
        var list = new List<int>();
        foreach (var part in raw.Split(','))
            if (int.TryParse(part, out int v)) list.Add(v);
        return list.OrderByDescending(v => v).Take(MaxEntries).ToList();
    }

    public static int Best
    {
        get
        {
            var list = Load();
            return list.Count > 0 ? list[0] : 0;
        }
    }

    public static void Submit(int score)
    {
        LastWasNewBest = false;
        if (score <= 0) return;

        var list = Load();
        LastWasNewBest = list.Count == 0 || score > list[0];
        list.Add(score);
        list = list.OrderByDescending(v => v).Take(MaxEntries).ToList();

        PlayerPrefs.SetString(Key, string.Join(",", list));
        PlayerPrefs.Save();
    }
}
