using UnityEngine;

/// <summary>
/// Sleek bullet visuals, applied at runtime (prefabs and colliders are untouched):
///   Player shots - crisp white elongated "bolt" with a short fading trail.
///   Enemy shots  - glowing coral orb with a short trail, so danger reads at a glance.
/// The original mesh renderer is hidden; a camera-facing glow quad + TrailRenderer replace it.
/// </summary>
public class BulletStyle : MonoBehaviour
{
    public static readonly Color PlayerColor = new Color(0.88f, 0.95f, 1f);
    public static readonly Color HomingColor = new Color(0.45f, 0.85f, 1f);
    public static readonly Color EnemyColor = new Color(1f, 0.36f, 0.3f);

    private static Material boltMat, orbMat, trailMat;
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private Transform visual;
    private Renderer visualRenderer;
    private TrailRenderer trail;
    private Rigidbody body;
    private MaterialPropertyBlock block;
    private bool alignToVelocity;
    private float pulseSeed;
    private float baseSize;

    // ------------------------------------------------------------------ public API

    /// <summary>Player bolt: about 0.75 x 0.2 world units, aligned with the bullet's flight direction.</summary>
    public static BulletStyle ApplyPlayer(GameObject bullet)
    {
        var s = Apply(bullet, boltMat, new Vector2(0.22f, 0.75f), PlayerColor, false);
        s.SetupTrail(0.09f, 0.10f, PlayerColor);
        return s;
    }

    /// <summary>Enemy orb, sized from the original sphere (worldDiameter).</summary>
    public static BulletStyle ApplyEnemy(GameObject bullet, float worldDiameter)
    {
        float d = Mathf.Max(0.25f, worldDiameter) * 1.6f;
        var s = Apply(bullet, orbMat, new Vector2(d, d), EnemyColor, true);
        s.SetupTrail(d * 0.45f, 0.12f, EnemyColor);
        return s;
    }

    public void SetColor(Color c)
    {
        block.SetColor(ColorId, c);
        visualRenderer.SetPropertyBlock(block);
        if (trail != null) trail.colorGradient = Fade(c);
    }

    // ------------------------------------------------------------------ setup

    private static BulletStyle Apply(GameObject bullet, Material mat, Vector2 worldSize, Color color, bool alignToVelocity)
    {
        EnsureMaterials();

        foreach (var r in bullet.GetComponents<MeshRenderer>()) r.enabled = false; // hide old capsule/sphere

        var s = bullet.GetComponent<BulletStyle>();
        if (s == null) s = bullet.AddComponent<BulletStyle>();
        s.alignToVelocity = alignToVelocity;
        s.body = bullet.GetComponent<Rigidbody>();
        s.pulseSeed = Random.value * 10f;
        s.baseSize = worldSize.x;

        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = "BulletGlow";
        Destroy(q.GetComponent<Collider>());
        q.transform.SetParent(bullet.transform, false);

        // Compensate for the bullet's own (possibly non-uniform) scale so the glow has a fixed world size.
        Vector3 ls = bullet.transform.lossyScale;
        q.transform.localScale = new Vector3(worldSize.x / Mathf.Max(1e-4f, ls.x), worldSize.y / Mathf.Max(1e-4f, ls.y), 1f);
        q.transform.localPosition = new Vector3(0f, 0f, -0.05f / Mathf.Max(1e-4f, ls.z));

        s.visual = q.transform;
        s.visualRenderer = q.GetComponent<MeshRenderer>();
        s.visualRenderer.sharedMaterial = mat;
        s.visualRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        s.visualRenderer.receiveShadows = false;
        s.block = new MaterialPropertyBlock();
        s.SetColor(color);
        return s;
    }

    private void SetupTrail(float width, float time, Color color)
    {
        trail = gameObject.GetComponent<TrailRenderer>();
        if (trail == null) trail = gameObject.AddComponent<TrailRenderer>();
        trail.sharedMaterial = trailMat;
        trail.time = time;
        trail.minVertexDistance = 0.04f;
        trail.widthMultiplier = width;
        trail.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
        trail.colorGradient = Fade(color);
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.numCapVertices = 2;
    }

    private static Gradient Fade(Color c)
    {
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.Lerp(c, Color.white, 0.4f), 0f), new GradientColorKey(c, 1f) },
                  new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) });
        return g;
    }

    private void LateUpdate()
    {
        if (visual == null) return;

        if (alignToVelocity)
        {
            // Enemy shots: face the camera; gentle pulse so they read as energy.
            visual.rotation = Quaternion.identity;
            float pulse = 1f + 0.12f * Mathf.Sin(Time.time * 14f + pulseSeed);
            Vector3 ls = transform.lossyScale;
            visual.localScale = new Vector3(baseSize * pulse / Mathf.Max(1e-4f, ls.x),
                                            baseSize * pulse / Mathf.Max(1e-4f, ls.y), 1f);
        }
    }

    // ------------------------------------------------------------------ materials

    private static void EnsureMaterials()
    {
        if (boltMat != null) return;

        Shader glow = Resources.Load<Shader>("VFX/PickupGlow");
        if (glow == null) glow = Shader.Find("SpaceShooter/PickupGlow");

        boltMat = new Material(glow) { name = "Bullet Bolt", mainTexture = MakeBolt(32, 128) };
        boltMat.SetFloat("_Intensity", 2.6f);

        orbMat = new Material(glow) { name = "Bullet Orb", mainTexture = MakeOrb(64) };
        orbMat.SetFloat("_Intensity", 2.2f);

        // Trails use vertex colours, so the additive particle-style shader fits.
        Shader additive = Resources.Load<Shader>("VFX/SpaceExplosionAdditive");
        if (additive == null) additive = Shader.Find("Sprites/Default");
        trailMat = new Material(additive) { name = "Bullet Trail" };
        if (trailMat.HasProperty("_Intensity")) trailMat.SetFloat("_Intensity", 1.6f);
    }

    /// <summary>Thin capsule with a white-hot core and soft falloff (vertical).</summary>
    private static Texture2D MakeBolt(int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
        var px = new Color32[w * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float u = (x + 0.5f) / w * 2f - 1f;          // -1..1 across
            float v = (y + 0.5f) / h * 2f - 1f;          // -1..1 along
            float along = Mathf.Max(0f, Mathf.Abs(v) - 0.55f) / 0.45f;   // 0 in the middle section
            float d = Mathf.Sqrt(u * u + along * along);  // capsule distance
            float core = Mathf.Clamp01(1f - d / 0.28f);
            float glow = Mathf.Exp(-d * 3.2f) * 0.55f;
            float a = Mathf.Clamp01(Mathf.Max(core, glow) * (d < 1f ? 1f : 0f));
            float white = Mathf.Lerp(0.75f, 1f, core);
            px[y * w + x] = new Color(white, white, white, a);
        }
        tex.SetPixels32(px);
        tex.Apply(true, true);
        return tex;
    }

    /// <summary>Round orb: bright core, thin bright rim, soft halo.</summary>
    private static Texture2D MakeOrb(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
        var px = new Color32[size * size];
        float half = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = (x - half) / half, dy = (y - half) / half;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float core = Mathf.Clamp01(1f - r / 0.32f);
            float rim = Mathf.Exp(-Mathf.Pow((r - 0.45f) / 0.05f, 2f)) * 0.8f;
            float halo = Mathf.Pow(Mathf.Clamp01(1f - r), 2.2f) * 0.5f;
            float a = r > 0.98f ? 0f : Mathf.Clamp01(core + rim + halo);
            px[y * size + x] = new Color(1f, 1f, 1f, a);
        }
        tex.SetPixels32(px);
        tex.Apply(true, true);
        return tex;
    }
}
