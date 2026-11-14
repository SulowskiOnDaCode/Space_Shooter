using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

/// <summary>
/// Dynamic post-processing that reacts to gameplay (Post Processing Stack v2).
/// Adds a high-priority overlay volume on top of the scene's own profile:
///   - Hit:        chromatic aberration burst + red vignette that fades out
///   - Last life:  slow red vignette pulse
///   - Scoring:    short bloom flash on every point gained
///   - Time freeze: colours drain to near-greyscale with a bit more contrast
///   - Death:      colours slowly desaturate
/// It only watches game state (lives, score, freeze), so no other script has to call it.
/// </summary>
public class DynamicPostFX : MonoBehaviour
{
    private PostProcessVolume volume;
    private Vignette vignette;
    private ChromaticAberration aberration;
    private ColorGrading grading;
    private Bloom bloom;
    private float baseBloom = 1f;

    private int lastLives = -1;
    private int lastScore = -1;
    private float hit;        // 1 -> 0 after a hit
    private float scorePulse; // 1 -> 0 after scoring
    private float freeze;     // 0..1 blend while time is frozen
    private float death;      // 0..1 blend after dying

    public static void Attach()
    {
        if (FindAnyObjectByType<DynamicPostFX>() != null) return;
        new GameObject("DynamicPostFX").AddComponent<DynamicPostFX>();
    }

    private void Start()
    {
        // Base bloom from the scene's profile, so the score flash adds on top of it.
        foreach (var v in FindObjectsByType<PostProcessVolume>())
        {
            if (v.sharedProfile != null && v.sharedProfile.TryGetSettings(out Bloom b) && b.enabled.value)
            {
                baseBloom = b.intensity.value;
                break;
            }
        }

        vignette = ScriptableObject.CreateInstance<Vignette>();
        vignette.enabled.Override(true);
        vignette.intensity.Override(0f);
        vignette.smoothness.Override(0.45f);
        vignette.color.Override(new Color(0.55f, 0.04f, 0.06f));

        aberration = ScriptableObject.CreateInstance<ChromaticAberration>();
        aberration.enabled.Override(true);
        aberration.intensity.Override(0f);

        grading = ScriptableObject.CreateInstance<ColorGrading>();
        grading.enabled.Override(true);
        grading.saturation.Override(0f);
        grading.contrast.Override(0f);

        bloom = ScriptableObject.CreateInstance<Bloom>();
        bloom.enabled.Override(true);
        bloom.intensity.overrideState = false; // only overrides during a score flash

        volume = PostProcessManager.instance.QuickVolume(gameObject.layer, 100f, vignette, aberration, grading, bloom);
    }

    private void Update()
    {
        if (volume == null) return;
        float dt = Time.unscaledDeltaTime;

        int lives = GameLogic.lives;
        if (lastLives >= 0 && lives < lastLives) hit = 1f;
        lastLives = lives;

        int score = GameLogic.score;
        if (lastScore >= 0 && score > lastScore) scorePulse = 1f;
        lastScore = score;

        hit = Mathf.MoveTowards(hit, 0f, dt / 0.6f);
        scorePulse = Mathf.MoveTowards(scorePulse, 0f, dt / 0.25f);
        freeze = Mathf.MoveTowards(freeze, TimeFreeze.Active ? 1f : 0f, dt / 0.2f);
        death = Mathf.MoveTowards(death, lives <= 0 ? 1f : 0f, dt / 1.5f);

        // Vignette: hit flash, plus a slow pulse while on the last life.
        float lowHealth = lives == 1 ? 0.22f + 0.08f * Mathf.Sin(Time.unscaledTime * 3f) : 0f;
        vignette.intensity.value = Mathf.Max(hit * hit * 0.5f, lowHealth);

        aberration.intensity.value = hit * 0.9f;

        grading.saturation.value = Mathf.Min(-85f * freeze, -60f * death);
        grading.contrast.value = 15f * freeze;

        bloom.intensity.overrideState = scorePulse > 0.001f;
        bloom.intensity.value = baseBloom + 2.5f * scorePulse;
    }

    private void OnDestroy()
    {
        if (volume != null) RuntimeUtilities.DestroyVolume(volume, true, true);
    }
}
