using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared look for the minimalist sci-fi HUD: thin white lines, low-alpha tracks,
/// small widely-spaced uppercase labels.
/// </summary>
public static class HudStyle
{
    public static readonly Color Solid = new Color(1f, 1f, 1f, 0.95f);
    public static readonly Color Dim   = new Color(1f, 1f, 1f, 0.55f);
    public static readonly Color Track = new Color(1f, 1f, 1f, 0.10f);
    public static readonly Color Frame = new Color(1f, 1f, 1f, 0.35f);

    public const float LabelSize = 12f;
    public const float LabelSpacing = 14f;

    public static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    /// <summary>Plain white quad (no sprite = crisp edges).</summary>
    public static Image Block(string name, Transform parent, Color color)
    {
        var rt = Rect(name, parent);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    public static TextMeshProUGUI Label(string name, Transform parent, string text, TextAlignmentOptions align, TMP_FontAsset font = null)
    {
        var rt = Rect(name, parent);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = LabelSize;
        t.characterSpacing = LabelSpacing;
        t.alignment = align;
        t.color = Dim;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        return t;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>Places a rect relative to a parent corner. anchor = pivot = corner.</summary>
    public static void Place(RectTransform rt, Vector2 corner, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = corner;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    /// <summary>Four thin L-shaped corner brackets around the parent rect.</summary>
    public static void CornerBrackets(Transform parent, float length = 12f, float thickness = 1f)
    {
        Vector2[] corners = { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
        foreach (var c in corners)
        {
            var h = Block("BracketH", parent, Frame).rectTransform;
            Place(h, c, Vector2.zero, new Vector2(length, thickness));
            var v = Block("BracketV", parent, Frame).rectTransform;
            Place(v, c, Vector2.zero, new Vector2(thickness, length));
        }
    }
}
