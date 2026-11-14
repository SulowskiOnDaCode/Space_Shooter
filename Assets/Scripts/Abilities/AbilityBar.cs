using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ability cooldown bar. Keeps the original Slider for its value, but hides the old
/// graphics and draws a thin white sci-fi bar instead (see <see cref="HudStyle"/>).
/// </summary>
public class AbilityBar : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [Tooltip("No longer used for colour; kept so existing scene references stay valid.")]
    [SerializeField] private Gradient gradient;
    [SerializeField] private Image fill;

    private const float Width = 320f;
    private const float Height = 3f;

    private RectTransform barFill;
    private Image barFillImage;
    private TextMeshProUGUI status;
    private bool built;

    private void Awake()
    {
        BuildSleekVisuals();
    }

    private void BuildSleekVisuals()
    {
        if (built) return;
        built = true;

        // Hide the old border/fill/handle graphics; the slider keeps working invisibly.
        foreach (var g in GetComponentsInChildren<Graphic>(true)) g.enabled = false;
        if (slider != null) slider.interactable = false;

        var rt = (RectTransform)transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 24f);
        rt.sizeDelta = new Vector2(Width, Height);

        var root = HudStyle.Rect("SleekBar", transform);
        HudStyle.Stretch(root);

        var track = HudStyle.Block("Track", root, HudStyle.Track);
        HudStyle.Stretch(track.rectTransform);

        barFillImage = HudStyle.Block("Fill", root, HudStyle.Dim);
        barFill = barFillImage.rectTransform;
        barFill.anchorMin = Vector2.zero;
        barFill.anchorMax = new Vector2(0f, 1f);
        barFill.offsetMin = barFill.offsetMax = Vector2.zero;

        TMP_FontAsset font = TMP_Settings.defaultFontAsset;

        var label = HudStyle.Label("Label", root, "ABILITY", TextAlignmentOptions.BottomLeft, font);
        HudStyle.Place(label.rectTransform, new Vector2(0, 1), new Vector2(0f, 5f), new Vector2(Width * 0.6f, 16f));
        label.rectTransform.pivot = new Vector2(0f, 0f); // sit above the bar

        status = HudStyle.Label("Status", root, "", TextAlignmentOptions.BottomRight, font);
        HudStyle.Place(status.rectTransform, new Vector2(1, 1), new Vector2(0f, 5f), new Vector2(Width * 0.4f, 16f));
        status.rectTransform.pivot = new Vector2(1f, 0f);
        status.characterSpacing = 6f;
    }

    private void Update()
    {
        // Gentle pulse when the ability is ready.
        if (status != null && IsReady)
        {
            float a = Mathf.Lerp(0.6f, 1f, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f));
            var c = HudStyle.Solid; c.a = a;
            barFillImage.color = c;
            status.color = c;
        }
    }

    private bool IsReady => slider != null && slider.normalizedValue >= 0.999f;

    public void SetMaxProgress(float progress)
    {
        slider.maxValue = progress;
        slider.value = progress;
        Refresh();
    }

    public void SetProgress(float progress)
    {
        slider.value = progress;
        Refresh();
    }

    private void Refresh()
    {
        BuildSleekVisuals();
        float n = slider.normalizedValue;
        barFill.anchorMax = new Vector2(n, 1f);

        if (IsReady)
        {
            status.text = "READY";
        }
        else
        {
            status.text = $"{Mathf.FloorToInt(n * 100f):00}%";
            status.color = HudStyle.Dim;
            barFillImage.color = HudStyle.Dim;
        }
    }
}
