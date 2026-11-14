using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Three-point lighting + ambient + reflections, applied automatically when the main menu or the
/// game scene loads (neither scene has generated lighting data, so ambient was black).
///
///   Key   - the scene's existing directional light, re-aimed from the camera's upper-left,
///           softened, with soft shadows. (Game scene only; the menu keeps its own key light.)
///   Fill  - cool light from the opposite side so the shadow side is never pitch black.
///   Rim   - teal light from behind to outline the silhouettes.
///   Ambient - trilight gradient tinted to match the nebula background.
///   Reflections - one realtime reflection probe capturing the skybox, so metal panels reflect.
///
/// Fill and rim are "Not Important" lights: Unity folds them into the per-object spherical
/// harmonics instead of rendering every object again, so they cost almost nothing.
/// Tweak values on the "LightingRig" object in Play mode, then copy them back here.
/// </summary>
public class LightingRig : MonoBehaviour
{
    [Header("Ambient (gamma colours)")]
    public Color ambientSky = new Color(0.22f, 0.26f, 0.32f);      // cool steel
    public Color ambientEquator = new Color(0.24f, 0.26f, 0.31f);  // neutral cool (faces the camera)
    public Color ambientGround = new Color(0.07f, 0.08f, 0.10f);   // deep shadow

    [Header("Key light (game scene)")]
    public bool adjustKeyLight = true;
    public Vector3 keyDirection = new Vector3(0.45f, -0.5f, 0.75f);   // camera space, direction light travels
    public Color keyColor = new Color(0.95f, 0.97f, 1f);           // clean white
    public float keyIntensity = 1.25f;
    [Range(0f, 1f)] public float keyShadowStrength = 0.7f;
    public bool keyShadows = true;

    [Header("Fill light")]
    public Vector3 fillDirection = new Vector3(-0.6f, 0.35f, 0.7f);
    public Color fillColor = new Color(0.45f, 0.55f, 0.72f);       // steel blue
    public float fillIntensity = 0.55f;

    [Header("Rim light")]
    public Vector3 rimDirection = new Vector3(-0.2f, -0.55f, -0.8f);
    public Color rimColor = new Color(0.78f, 0.9f, 1f);            // icy white rim
    public float rimIntensity = 0.9f;
    [Tooltip("Pixel lights give crisp fill/rim (used for the menu preview); vertex lights are cheaper (game).")]
    public bool pixelFillAndRim = false;

    [Header("Reflections")]
    public bool reflectionProbe = true;
    [Tooltip("Game scene: reflect a nebula sky matching the background instead of the scene's HDRI.")]
    public bool useSpaceSkybox = true;
    public float reflectionIntensity = 1f;

    private Light fill, rim;
    private ReflectionProbe probe;

    // ------------------------------------------------------------------ auto setup

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        bool isMenu = scene.buildIndex == SceneFlow.MainMenuScene;
        bool isGame = scene.buildIndex == SceneFlow.GameScene;
        if (!isMenu && !isGame) return;

        var go = new GameObject("LightingRig");
        SceneManager.MoveGameObjectToScene(go, scene);
        var rig = go.AddComponent<LightingRig>();
        if (isMenu) rig.UseMenuPreset();
        rig.useSpaceSkybox = isGame; // the menu keeps its own visible skybox
        rig.Apply();
    }

    /// <summary>
    /// Main menu: moonlit three-point lighting for the class preview, matched to the menu's
    /// night-sky background (cool white moon key, steel-blue fill, icy rim, dark cool ambient).
    /// </summary>
    public void UseMenuPreset()
    {
        ambientSky = new Color(0.17f, 0.19f, 0.23f);
        ambientEquator = new Color(0.26f, 0.27f, 0.30f);
        ambientGround = new Color(0.10f, 0.10f, 0.11f);

        adjustKeyLight = true;                          // replace the old teal "Sun" angle
        keyDirection = new Vector3(0.5f, -0.55f, 0.65f); // moonlight from the upper left
        keyColor = new Color(0.86f, 0.91f, 1f);
        keyIntensity = 1.15f;
        keyShadows = false;

        fillDirection = new Vector3(-0.65f, 0.25f, 0.7f);
        fillColor = new Color(0.38f, 0.46f, 0.62f);
        fillIntensity = 0.45f;

        rimDirection = new Vector3(-0.35f, -0.45f, -0.82f);
        rimColor = new Color(0.8f, 0.88f, 1f);
        rimIntensity = 1.1f;

        pixelFillAndRim = true;  // only the preview ship is lit here, so crisp lights are cheap
        useSpaceSkybox = false;  // reflect the menu's own night sky
    }

    // ------------------------------------------------------------------ apply

    public void Apply()
    {
        Camera cam = Camera.main;
        Transform view = cam != null ? cam.transform : null;

        // Ambient
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ambientSky;
        RenderSettings.ambientEquatorColor = ambientEquator;
        RenderSettings.ambientGroundColor = ambientGround;
        DynamicGI.UpdateEnvironment(); // recompute the ambient probe (menu has old baked data)

        // Key
        if (adjustKeyLight)
        {
            Light key = FindKeyLight();
            if (key != null)
            {
                key.transform.rotation = Aim(view, keyDirection);
                key.color = keyColor;
                key.intensity = keyIntensity;
                key.shadows = keyShadows ? LightShadows.Soft : LightShadows.None;
                key.shadowStrength = keyShadowStrength;
                key.renderMode = LightRenderMode.ForcePixel;
            }
        }

        // Fill + rim
        fill = MakeLight("Fill Light", view, fillDirection, fillColor, fillIntensity);
        rim = MakeLight("Rim Light", view, rimDirection, rimColor, rimIntensity);

        // Reflections
        RenderSettings.reflectionIntensity = reflectionIntensity;
        if (useSpaceSkybox) ApplySpaceSkybox();
        if (reflectionProbe) MakeReflectionProbe();
    }

    private void OnValidate()
    {
        // Live-tweak in Play mode.
        if (!Application.isPlaying || fill == null) return;
        RenderSettings.ambientSkyColor = ambientSky;
        RenderSettings.ambientEquatorColor = ambientEquator;
        RenderSettings.ambientGroundColor = ambientGround;
        Transform view = Camera.main != null ? Camera.main.transform : null;
        Configure(fill, view, fillDirection, fillColor, fillIntensity);
        Configure(rim, view, rimDirection, rimColor, rimIntensity);
    }

    private static Light FindKeyLight()
    {
        Light best = null;
        foreach (var l in FindObjectsByType<Light>())
        {
            if (l.type != LightType.Directional || !l.isActiveAndEnabled) continue;
            if (best == null || l.intensity > best.intensity) best = l;
        }
        return best;
    }

    private Light MakeLight(string name, Transform view, Vector3 dir, Color color, float intensity)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var l = go.AddComponent<Light>();
        l.type = LightType.Directional;
        l.shadows = LightShadows.None;
        // Vertex lights are folded into SH (no extra draw passes); pixel lights are crisper.
        l.renderMode = pixelFillAndRim ? LightRenderMode.ForcePixel : LightRenderMode.ForceVertex;
        Configure(l, view, dir, color, intensity);
        return l;
    }

    private static void Configure(Light l, Transform view, Vector3 dir, Color color, float intensity)
    {
        l.transform.rotation = Aim(view, dir);
        l.color = color;
        l.intensity = intensity;
    }

    /// <summary>Turns a camera-space direction into a world rotation for a directional light.</summary>
    private static Quaternion Aim(Transform view, Vector3 cameraSpaceDir)
    {
        Vector3 world = view != null ? view.TransformDirection(cameraSpaceDir.normalized) : cameraSpaceDir.normalized;
        return Quaternion.LookRotation(world, view != null ? view.up : Vector3.up);
    }

    /// <summary>
    /// The game camera never draws the skybox (the procedural background covers it), so swap it
    /// for a nebula sky in the same palette; the reflection probe then captures matching space.
    /// </summary>
    private static void ApplySpaceSkybox()
    {
        Shader shader = Resources.Load<Shader>("VFX/SpaceSkybox");
        if (shader == null) shader = Shader.Find("SpaceShooter/SpaceSkybox");
        if (shader == null || !shader.isSupported) return;
        RenderSettings.skybox = new Material(shader) { name = "Space Skybox (runtime)" };
    }

    private void MakeReflectionProbe()
    {
        var go = new GameObject("Sky Reflection Probe");
        go.transform.SetParent(transform, false);
        probe = go.AddComponent<ReflectionProbe>();
        probe.mode = ReflectionProbeMode.Realtime;
        probe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
        probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
        probe.clearFlags = ReflectionProbeClearFlags.Skybox;
        probe.cullingMask = 0;            // capture only the sky, not the ships
        probe.resolution = 128;
        probe.hdr = true;
        probe.size = new Vector3(10000f, 10000f, 10000f);
        probe.importance = 0;
        probe.RenderProbe();              // one capture; the sky doesn't change
    }
}
