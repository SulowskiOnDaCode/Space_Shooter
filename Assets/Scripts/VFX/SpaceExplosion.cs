using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight sci-fi space explosion: flash, fireball, shockwave ring and sparks.
///
/// Performance notes:
///  - Each colour/size combination is built ONCE into a hidden template; every explosion after
///    that is a plain Instantiate of the template (no runtime particle-system setup, no GC churn).
///  - No dynamic lights (realtime point lights force extra draw passes on every nearby lit mesh).
///  - Textures / materials are generated once at startup and shared.
///
/// If the prefab still has an old ParticleSystem on it, that system is silenced, so the prefab
/// can be reverted by simply removing this component.
/// </summary>
[DisallowMultipleComponent]
public class SpaceExplosion : MonoBehaviour
{
    [Header("Colours")]
    [Tooltip("White-hot core colour (first frames of the blast).")]
    [SerializeField] private Color hotColor = new Color(1f, 0.95f, 0.8f);
    [Tooltip("Main fire / shockwave colour.")]
    [SerializeField] private Color primaryColor = new Color(1f, 0.55f, 0.1f);
    [Tooltip("Cooling colour the fire fades into.")]
    [SerializeField] private Color secondaryColor = new Color(0.9f, 0.1f, 0.05f);

    [Header("Size")]
    [Tooltip("Overall size multiplier (1 = large blast).")]
    [SerializeField] private float size = 1f;

    private const float Lifetime = 1.2f;

    private static readonly Dictionary<string, GameObject> Templates = new Dictionary<string, GameObject>();
    private static Material glowMat, hotGlowMat, ringMat;

    private void Awake()
    {
        SilenceLegacySystem();

        // The old prefabs are rotated/scaled for their old particle system; normalise.
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        GameObject template = GetTemplate();
        GameObject fx = Instantiate(template, transform, false);
        fx.SetActive(true); // particle systems play on awake

        Destroy(gameObject, Lifetime);
    }

    private void SilenceLegacySystem()
    {
        var legacy = GetComponent<ParticleSystem>();
        if (legacy == null) return;

        var main = legacy.main;
        main.stopAction = ParticleSystemStopAction.None; // don't let Stop() destroy us
        legacy.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);

        var legacyRenderer = GetComponent<ParticleSystemRenderer>();
        if (legacyRenderer != null) legacyRenderer.enabled = false;
    }

    // ------------------------------------------------------------------ template

    private GameObject GetTemplate()
    {
        string key = $"{hotColor}|{primaryColor}|{secondaryColor}|{size}";
        if (Templates.TryGetValue(key, out var t) && t != null) return t;

        EnsureResources();

        var root = new GameObject("SpaceExplosionTemplate");
        root.SetActive(false);
        DontDestroyOnLoad(root);
        Build(root.transform);

        Templates[key] = root;
        return root;
    }

    private void Build(Transform parent)
    {
        float s = size;
        Color white = Color.white;

        // 1. Flash: one white-hot burst.
        {
            var ps = CreateSystem(parent, "Flash", hotGlowMat, 1);
            var main = ps.main;
            main.startLifetime = 0.14f;
            main.startSpeed = 0f;
            main.startSize = 4.5f * s;
            main.startColor = hotColor;
            Burst(ps, 1);
            SizeOverLife(ps, Curve(0f, 1f, 0f, 1f, 0.25f, 0f));
            ColorOverLife(ps, Grad(white, white), Alpha(1f, 0f));
        }

        // 2. Core bloom: a clean plasma glow that swells and fades (no smoky fireball).
        {
            var ps = CreateSystem(parent, "CoreBloom", glowMat, 2);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.42f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(2.4f * s, 3.0f * s);
            main.startColor = white;
            Burst(ps, 2);
            SizeOverLife(ps, Curve(0f, 0.35f, 3f, 1f, 1.1f, 0f));
            ColorOverLife(ps,
                Grad(new[] { hotColor, primaryColor, secondaryColor }, new[] { 0f, 0.3f, 1f }),
                Alpha(new[] { 0.9f, 0.5f, 0f }, new[] { 0f, 0.4f, 1f }));
        }

        // 3. Shockwave: one crisp thin ring...
        {
            var ps = CreateSystem(parent, "Shockwave", ringMat, 1);
            var main = ps.main;
            main.startLifetime = 0.42f;
            main.startSpeed = 0f;
            main.startSize = 7.5f * s;
            main.startColor = Color.Lerp(primaryColor, hotColor, 0.5f);
            Burst(ps, 1);
            SizeOverLife(ps, Curve(0f, 0.05f, 4f, 1f, 1f, 0f));
            ColorOverLife(ps, Grad(white, white), Alpha(new[] { 1f, 0.6f, 0f }, new[] { 0f, 0.45f, 1f }));
        }

        // ...plus a smaller, faster inner ring for a clean double-pulse.
        {
            var ps = CreateSystem(parent, "InnerRing", ringMat, 1);
            var main = ps.main;
            main.startLifetime = 0.22f;
            main.startSpeed = 0f;
            main.startSize = 3.2f * s;
            main.startColor = hotColor;
            Burst(ps, 1);
            SizeOverLife(ps, Curve(0f, 0.1f, 3f, 1f, 1f, 0f));
            ColorOverLife(ps, Grad(white, white), Alpha(1f, 0f));
        }

        // 4. Sparks: thin, crisp, fast streaks that decelerate.
        {
            var ps = CreateSystem(parent, "Sparks", glowMat, 18);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(12f * s, 28f * s);
            main.startSize = new ParticleSystem.MinMaxCurve(0.07f * s, 0.12f * s);
            main.startColor = white;
            Burst(ps, 18);
            Shape(ps, ParticleSystemShapeType.Circle, 0.1f * s);
            Drag(ps, 5f);
            SizeOverLife(ps, Curve(0f, 1f, 0f, 1f, 0f, 0f));
            ColorOverLife(ps,
                Grad(new[] { hotColor, primaryColor, secondaryColor }, new[] { 0f, 0.4f, 1f }),
                Alpha(new[] { 1f, 1f, 0f }, new[] { 0f, 0.6f, 1f }));

            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.06f;
            r.lengthScale = 2.5f;
        }
    }

    // ------------------------------------------------------------------ particle helpers

    private static ParticleSystem CreateSystem(Transform parent, string name, Material mat, int maxParticles)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var ps = go.AddComponent<ParticleSystem>(); // parent is inactive, so it does not start playing
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = 0.2f;
        main.maxParticles = maxParticles;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.gravityModifier = 0f;
        main.stopAction = ParticleSystemStopAction.None;

        var emission = ps.emission;
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.enabled = false;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        r.maxParticleSize = 5f;
        return ps;
    }

    private static void Burst(ParticleSystem ps, int count)
    {
        var emission = ps.emission;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
    }

    private static void Shape(ParticleSystem ps, ParticleSystemShapeType type, float radius)
    {
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = type;
        shape.radius = Mathf.Max(0.0001f, radius);
        shape.radiusThickness = 1f;
    }

    private static void SizeOverLife(ParticleSystem ps, AnimationCurve curve)
    {
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, curve);
    }

    private static void ColorOverLife(ParticleSystem ps, GradientColorKey[] colors, GradientAlphaKey[] alphas)
    {
        var g = new Gradient();
        g.SetKeys(colors, alphas);
        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = new ParticleSystem.MinMaxGradient(g);
    }

    private static void Drag(ParticleSystem ps, float drag)
    {
        var lv = ps.limitVelocityOverLifetime;
        lv.enabled = true;
        lv.limit = 1000f;
        lv.drag = drag;
        lv.multiplyDragByParticleSize = false;
        lv.multiplyDragByParticleVelocity = false;
    }

    /// <summary>Two-key curve: (t0, v0, outTangent) -> (t1, v1, inTangent).</summary>
    private static AnimationCurve Curve(float t0, float v0, float outTan, float t1, float v1, float inTan)
    {
        return new AnimationCurve(new Keyframe(t0, v0, 0f, outTan), new Keyframe(t1, v1, inTan, 0f));
    }

    private static GradientColorKey[] Grad(Color a, Color b)
    {
        return new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 1f) };
    }

    private static GradientColorKey[] Grad(Color[] colors, float[] times)
    {
        var keys = new GradientColorKey[colors.Length];
        for (int i = 0; i < colors.Length; i++)
        {
            var c = colors[i];
            c.a = 1f;
            keys[i] = new GradientColorKey(c, times[i]);
        }
        return keys;
    }

    private static GradientAlphaKey[] Alpha(float a, float b)
    {
        return new[] { new GradientAlphaKey(a, 0f), new GradientAlphaKey(b, 1f) };
    }

    private static GradientAlphaKey[] Alpha(float[] alphas, float[] times)
    {
        var keys = new GradientAlphaKey[alphas.Length];
        for (int i = 0; i < alphas.Length; i++) keys[i] = new GradientAlphaKey(alphas[i], times[i]);
        return keys;
    }

    // ------------------------------------------------------------------ shared resources

    // Build textures/materials at startup so the first explosion does not hitch.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Prewarm() => EnsureResources();

    private static void EnsureResources()
    {
        if (glowMat != null) return;

        Shader shader = Resources.Load<Shader>("VFX/SpaceExplosionAdditive");
        if (shader == null) shader = Shader.Find("SpaceShooter/ExplosionAdditive");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Additive");

        Texture2D glow = MakeGlowTexture(64);
        Texture2D ring = MakeRingTexture(128);

        glowMat    = MakeMaterial(shader, glow, 3.5f, "Explosion Glow");
        hotGlowMat = MakeMaterial(shader, glow, 4f,   "Explosion Hot Glow");
        ringMat    = MakeMaterial(shader, ring, 2.5f, "Explosion Shockwave");
    }

    private static Material MakeMaterial(Shader shader, Texture tex, float intensity, string name)
    {
        var m = new Material(shader) { name = name, mainTexture = tex, hideFlags = HideFlags.DontUnloadUnusedAsset };
        if (m.HasProperty("_Intensity")) m.SetFloat("_Intensity", intensity);
        return m;
    }

    private static Texture2D NewTexture(int size, string name)
    {
        return new Texture2D(size, size, TextureFormat.RGBA32, true)
        {
            name = name,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.DontSave
        };
    }

    /// <summary>Soft radial glow with a hot centre.</summary>
    private static Texture2D MakeGlowTexture(int size)
    {
        var tex = NewTexture(size, "ExplosionGlow");
        var px = new Color32[size * size];
        float half = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = (x - half) / half, dy = (y - half) / half;
            float falloff = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
            float a = Mathf.Pow(falloff, 2.2f) + 0.6f * Mathf.Pow(falloff, 8f);
            px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(a));
        }
        tex.SetPixels32(px);
        tex.Apply(true, true);
        return tex;
    }

    /// <summary>Thin bright ring with a faint inner haze (shockwave).</summary>
    private static Texture2D MakeRingTexture(int size)
    {
        var tex = NewTexture(size, "ExplosionRing");
        var px = new Color32[size * size];
        float half = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = (x - half) / half, dy = (y - half) / half;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float d = (r - 0.88f) / 0.035f;              // thin, crisp ring
            float a = Mathf.Exp(-d * d) + (r < 0.88f ? 0.05f * Mathf.Pow(r / 0.88f, 3f) : 0f);
            if (r > 0.99f) a = 0f;
            px[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(a));
        }
        tex.SetPixels32(px);
        tex.Apply(true, true);
        return tex;
    }
}
