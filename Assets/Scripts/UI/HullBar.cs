using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Segmented white "hull integrity" bar that replaces the plain "Lives: N" text.
/// One segment per life (based on the selected ship class), with a glitch/flicker
/// when a segment is lost and a slow pulse when only one is left.
/// Built entirely at runtime: call <see cref="Create"/> once (GameLogic does this).
/// </summary>
public class HullBar : MonoBehaviour
{
    private const float Width = 320f;
    private const float SegmentHeight = 8f;
    private const float SegmentGap = 4f;
    private const float LoseDuration = 0.55f;
    private const float JitterDuration = 0.25f;

    private RectTransform segmentRow;
    private TextMeshProUGUI label;
    private TextMeshProUGUI value;
    private TMP_FontAsset font;

    private Image[] fills = new Image[0];
    private float[] loseTimers = new float[0];
    private float[] gainTimers = new float[0];
    private int classMax;
    private const float GainDuration = 0.45f;
    // Segments above the ship's normal max (overhealth from pickups) get a pale cyan tint.
    private static readonly Color OverhealColor = new Color(0.7f, 0.95f, 1f, 0.95f);
    private int maxLives = -1;
    private int shownLives = -1;
    private float jitterTimer;
    private Vector2 basePos;

    /// <summary>Builds the HUD on the same canvas as the old lives text and hides that text.</summary>
    public static HullBar Create(TextMeshProUGUI livesText)
    {
        if (livesText == null || livesText.canvas == null) return null;

        Transform canvas = livesText.canvas.transform;
        var existing = canvas.GetComponentInChildren<HullBar>(true);
        if (existing != null) return existing;

        livesText.enabled = false;

        // Frame that spans both the hull bar and the ability bar beneath it.
        var frame = HudStyle.Rect("HUD_Frame", canvas);
        HudStyle.Place(frame, new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(Width + 32f, 88f));
        frame.pivot = new Vector2(0.5f, 0f);
        HudStyle.CornerBrackets(frame);

        var root = HudStyle.Rect("HUD_Hull", canvas);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0f);
        root.pivot = new Vector2(0.5f, 0f);
        root.anchoredPosition = new Vector2(0f, 56f);
        root.sizeDelta = new Vector2(Width, 30f);

        var bar = root.gameObject.AddComponent<HullBar>();
        bar.font = livesText.font;
        bar.Build(root);
        return bar;
    }

    private void Build(RectTransform root)
    {
        basePos = root.anchoredPosition;

        label = HudStyle.Label("Label", root, "HULL", TextAlignmentOptions.BottomLeft, font);
        HudStyle.Place(label.rectTransform, new Vector2(0, 1), Vector2.zero, new Vector2(Width * 0.6f, 16f));

        value = HudStyle.Label("Value", root, "", TextAlignmentOptions.BottomRight, font);
        HudStyle.Place(value.rectTransform, new Vector2(1, 1), Vector2.zero, new Vector2(Width * 0.4f, 16f));
        value.characterSpacing = 6f;

        segmentRow = HudStyle.Rect("Segments", root);
        HudStyle.Place(segmentRow, new Vector2(0, 0), Vector2.zero, new Vector2(Width, SegmentHeight));
    }

    private void RebuildSegments(int count)
    {
        for (int i = segmentRow.childCount - 1; i >= 0; i--)
            Destroy(segmentRow.GetChild(i).gameObject);

        count = Mathf.Max(1, count);
        fills = new Image[count];
        loseTimers = new float[count];
        gainTimers = new float[count];

        float segWidth = (Width - SegmentGap * (count - 1)) / count;
        for (int i = 0; i < count; i++)
        {
            var track = HudStyle.Block("Segment" + i, segmentRow, HudStyle.Track).rectTransform;
            HudStyle.Place(track, Vector2.zero, new Vector2(i * (segWidth + SegmentGap), 0f), new Vector2(segWidth, SegmentHeight));

            var fill = HudStyle.Block("Fill", track, HudStyle.Solid);
            HudStyle.Stretch(fill.rectTransform);
            fills[i] = fill;
        }
        // Keep shownLives so a gained segment still animates in after the rebuild.
        textDirty = true;
    }

    private bool textDirty = true;

    private bool AnyLoseAnimating()
    {
        foreach (var t in loseTimers) if (t > 0f) return true;
        return false;
    }

    private void Update()
    {
        int lives = Mathf.Max(0, GameLogic.lives);
        classMax = TwinStickMovement.playerClass != null ? Mathf.RoundToInt(TwinStickMovement.playerClass.hp) : 0;
        int max = Mathf.Max(classMax, lives, 1);

        // Grow immediately for overhealth; only shrink once the lost segment has finished flickering out.
        if (max > maxLives || (max < maxLives && !AnyLoseAnimating()))
        {
            maxLives = max;
            RebuildSegments(max);
        }

        if (lives != shownLives || textDirty)
        {
            if (shownLives >= 0 && lives < shownLives)
            {
                for (int i = lives; i < Mathf.Min(shownLives, fills.Length); i++) loseTimers[i] = LoseDuration;
                jitterTimer = JitterDuration;
            }
            else if (shownLives >= 0 && lives > shownLives)
            {
                for (int i = shownLives; i < Mathf.Min(lives, fills.Length); i++) gainTimers[i] = GainDuration;
            }
            shownLives = lives;
            textDirty = false;

            int baseMax = Mathf.Max(classMax, 1);
            value.text = $"{lives:00} / {baseMax:00}";
            label.text = lives <= 1 ? "HULL  //  CRITICAL"
                       : lives > baseMax ? "HULL  //  OVERCHARGED"
                       : "HULL";
        }

        float dt = Time.unscaledDeltaTime;
        float now = Time.unscaledTime;

        for (int i = 0; i < fills.Length; i++)
        {
            float a;
            if (i < lives)
            {
                // Last remaining segment breathes slowly to signal danger.
                a = lives == 1 ? Mathf.Lerp(0.45f, 1f, 0.5f + 0.5f * Mathf.Sin(now * 5f)) : 1f;
            }
            else if (loseTimers[i] > 0f)
            {
                loseTimers[i] -= dt;
                float k = Mathf.Clamp01(loseTimers[i] / LoseDuration);
                // Digital flicker while it drains out.
                a = k * (Mathf.Repeat(now * 30f + i, 1f) > 0.35f ? 1f : 0.2f);
            }
            else a = 0f;

            // Newly gained segment flickers in with a vertical pop.
            float pop = 1f;
            if (i < lives && gainTimers[i] > 0f)
            {
                gainTimers[i] -= dt;
                float g = Mathf.Clamp01(gainTimers[i] / GainDuration); // 1 -> 0
                a *= Mathf.Repeat(now * 30f + i, 1f) > 0.3f || g < 0.4f ? 1f : 0.25f;
                pop = 1f + 0.9f * g;
            }
            fills[i].rectTransform.localScale = new Vector3(1f, pop, 1f);

            var c = i >= classMax && classMax > 0 ? OverhealColor : HudStyle.Solid;
            c.a *= a;
            fills[i].color = c;
        }

        // Brief horizontal glitch jitter when hit.
        var rt = (RectTransform)transform;
        if (jitterTimer > 0f)
        {
            jitterTimer -= dt;
            float amp = 4f * Mathf.Clamp01(jitterTimer / JitterDuration);
            rt.anchoredPosition = basePos + new Vector2(Random.Range(-amp, amp), 0f);
        }
        else rt.anchoredPosition = basePos;
    }
}
