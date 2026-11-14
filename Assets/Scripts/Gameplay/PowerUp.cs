using UnityEngine;

/// <summary>
/// Power-ups dropped by destroyed asteroids. Built entirely at runtime (no prefab, no collider:
/// collection is a distance check, so it never triggers the player's damage logic or bullet hits).
///
///   Health      - mint "+": restores one life. Lives can go above the ship's normal maximum
///                 ("overhealth") up to <see cref="MaxLives"/>. Drops more often when you're damaged.
///   TimeFreeze  - purple clock (rare): freezes asteroids, enemies and their shots for
///                 <see cref="FreezeDuration"/> seconds. See <see cref="TimeFreeze"/>.
/// </summary>
public class PowerUp : MonoBehaviour
{
    public enum Kind { Health, TimeFreeze }

    /// <summary>Absolute life cap, including overhealth.</summary>
    public const int MaxLives = 6;
    public const float FreezeDuration = 4f;

    // Drop chances per destroyed asteroid.
    public static float HealthChanceWhenDamaged = 0.35f;
    public static float HealthChanceWhenFull = 0.15f;
    public static float FreezeChance = 0.06f;

    private const float Lifetime = 12f;
    private const float BlinkTime = 3f;
    private const float DriftSpeed = 0.6f;
    private const float MagnetRadius = 3f;
    private const float MagnetSpeed = 9f;
    private const float CollectRadius = 0.9f;
    private const float Size = 1.1f;
    private const float CollectAnimTime = 0.3f;

    private static readonly Color HealthColor = new Color(0.55f, 1f, 0.75f);
    private static readonly Color FreezeColor = new Color(0.68f, 0.42f, 1f);

    private static Material healthIconMat, healthHaloMat, freezeIconMat, freezeHaloMat;
    private static Transform player;

    public Kind kind;
    private Transform icon, halo;
    private Renderer iconRenderer, haloRenderer;
    private float age;
    private float collectTimer = -1f;
    private Vector3 velocity;

    // ------------------------------------------------------------------ spawning

    /// <summary>Rolls for a drop when an asteroid is destroyed.</summary>
    public static void TryDropFromAsteroid(Vector3 position)
    {
        int classMax = TwinStickMovement.playerClass != null ? Mathf.RoundToInt(TwinStickMovement.playerClass.hp) : 3;
        float healthChance = GameLogic.lives < classMax ? HealthChanceWhenDamaged : HealthChanceWhenFull;

        float roll = Random.value;
        if (roll < FreezeChance) Spawn(Kind.TimeFreeze, position);
        else if (roll < FreezeChance + healthChance) Spawn(Kind.Health, position);
    }

    public static PowerUp Spawn(Kind kind, Vector3 position)
    {
        EnsureMaterials();
        position.z = 0f;

        var go = new GameObject(kind == Kind.Health ? "HealthPowerUp" : "TimeFreezePowerUp");
        go.transform.position = position;
        var p = go.AddComponent<PowerUp>();
        p.kind = kind;
        bool health = kind == Kind.Health;
        p.halo = p.MakeQuad("Halo", health ? healthHaloMat : freezeHaloMat, Size * 2.2f, out p.haloRenderer);
        p.icon = p.MakeQuad("Icon", health ? healthIconMat : freezeIconMat, Size, out p.iconRenderer);
        return p;
    }

    private Transform MakeQuad(string name, Material mat, float size, out Renderer r)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.name = name;
        Destroy(q.GetComponent<Collider>());
        q.transform.SetParent(transform, false);
        q.transform.localScale = Vector3.one * size;
        r = q.GetComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return q.transform;
    }

    // ------------------------------------------------------------------ behaviour

    private bool CanCollect()
    {
        if (GameLogic.lives <= 0) return false;
        return kind == Kind.TimeFreeze || GameLogic.lives < MaxLives;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        age += dt;

        if (collectTimer >= 0f)
        {
            AnimateCollect(dt);
            return;
        }

        if (age > Lifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (player == null)
        {
            var mover = FindAnyObjectByType<TwinStickMovement>();
            if (mover != null) player = mover.transform;
        }

        Vector3 pos = transform.position;
        Vector3 drift = Vector3.down * DriftSpeed;

        if (player != null && CanCollect())
        {
            Vector3 to = player.position - pos;
            to.z = 0f;
            float dist = to.magnitude;

            if (dist < CollectRadius)
            {
                Collect();
                return;
            }
            if (dist < MagnetRadius)
            {
                float pull = Mathf.Lerp(MagnetSpeed, MagnetSpeed * 0.35f, dist / MagnetRadius);
                velocity = Vector3.Lerp(velocity, to.normalized * pull, 1f - Mathf.Exp(-10f * dt));
            }
            else velocity = Vector3.Lerp(velocity, drift, 1f - Mathf.Exp(-3f * dt));
        }
        else velocity = Vector3.Lerp(velocity, drift, 1f - Mathf.Exp(-3f * dt));

        transform.position = pos + velocity * dt;

        // Gentle pulse + motion, blink before expiring.
        float pulse = 1f + 0.08f * Mathf.Sin(age * 5f);
        icon.localScale = Vector3.one * Size * pulse;
        icon.localRotation = kind == Kind.Health
            ? Quaternion.Euler(0f, 0f, Mathf.Sin(age * 1.5f) * 12f)
            : Quaternion.Euler(0f, 0f, -age * 90f);                  // clock hand sweep
        halo.localScale = Vector3.one * Size * 2.2f * (1f + 0.15f * Mathf.Sin(age * 3f));

        bool visible = age < Lifetime - BlinkTime || Mathf.Repeat(age * 6f, 1f) > 0.35f;
        iconRenderer.enabled = visible;
        haloRenderer.enabled = visible;
    }

    private void Collect()
    {
        if (kind == Kind.Health)
        {
            GameLogic.lives = Mathf.Min(GameLogic.lives + 1, MaxLives);
            if (SoundManager.soundManager != null && SoundManager.soundManager.ability3 != null)
                SoundManager.soundManager.PlayAbility3Sound();
        }
        else
        {
            TimeFreeze.Begin(FreezeDuration);
        }

        collectTimer = 0f;
        if (player != null) transform.position = new Vector3(player.position.x, player.position.y, 0f);
    }

    private void AnimateCollect(float dt)
    {
        collectTimer += dt;
        float k = Mathf.Clamp01(collectTimer / CollectAnimTime);
        halo.localScale = Vector3.one * Size * Mathf.Lerp(2.2f, kind == Kind.Health ? 5f : 9f, k);
        icon.localScale = Vector3.one * Size * (1f - k);
        haloRenderer.enabled = iconRenderer.enabled = Mathf.Repeat(collectTimer * 30f, 1f) > 0.2f || k < 0.5f;
        if (player != null) transform.position = new Vector3(player.position.x, player.position.y, 0f);
        if (k >= 1f) Destroy(gameObject);
    }

    // ------------------------------------------------------------------ shared visuals

    private static void EnsureMaterials()
    {
        if (healthIconMat != null) return;

        Shader shader = Resources.Load<Shader>("VFX/PickupGlow");
        if (shader == null) shader = Shader.Find("SpaceShooter/PickupGlow");

        Texture2D halo = MakeHaloTexture(64);
        healthIconMat = MakeMat(shader, MakeCrossTexture(64), Color.Lerp(HealthColor, Color.white, 0.55f), 2.2f, "Health Icon");
        healthHaloMat = MakeMat(shader, halo, HealthColor, 1.2f, "Health Halo");
        freezeIconMat = MakeMat(shader, MakeClockTexture(64), Color.Lerp(FreezeColor, Color.white, 0.45f), 2.4f, "Freeze Icon");
        freezeHaloMat = MakeMat(shader, halo, FreezeColor, 1.4f, "Freeze Halo");
    }

    private static Material MakeMat(Shader shader, Texture tex, Color color, float intensity, string name)
    {
        var m = new Material(shader) { name = name, mainTexture = tex };
        m.SetColor("_Color", color);
        m.SetFloat("_Intensity", intensity);
        return m;
    }

    private static Texture2D NewTex(int size)
    {
        return new Texture2D(size, size, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
    }

    /// <summary>Rounded plus sign with a soft glow edge.</summary>
    private static Texture2D MakeCrossTexture(int size)
    {
        var tex = NewTex(size);
        var px = new Color32[size * size];
        float half = (size - 1) * 0.5f;
        const float arm = 0.62f, thick = 0.17f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float u = Mathf.Abs((x - half) / half), v = Mathf.Abs((y - half) / half);
            float d = Mathf.Min(Mathf.Max(u - arm, v - thick), Mathf.Max(u - thick, v - arm));
            px[y * size + x] = new Color(1f, 1f, 1f, Shape(d));
        }
        tex.SetPixels32(px);
        tex.Apply(true, true);
        return tex;
    }

    /// <summary>Clock face: ring plus two hands (the icon spins, so the hands sweep).</summary>
    private static Texture2D MakeClockTexture(int size)
    {
        var tex = NewTex(size);
        var px = new Color32[size * size];
        float half = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            var p = new Vector2((x - half) / half, (y - half) / half);
            float ring = Mathf.Abs(p.magnitude - 0.68f) - 0.08f;
            float handA = Segment(p, Vector2.zero, new Vector2(0f, 0.5f)) - 0.07f;   // long hand
            float handB = Segment(p, Vector2.zero, new Vector2(0.34f, 0f)) - 0.07f;  // short hand
            float d = Mathf.Min(ring, Mathf.Min(handA, handB));
            px[y * size + x] = new Color(1f, 1f, 1f, Shape(d));
        }
        tex.SetPixels32(px);
        tex.Apply(true, true);
        return tex;
    }

    private static float Segment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 pa = p - a, ba = b - a;
        float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
        return (pa - ba * h).magnitude;
    }

    /// <summary>Signed distance -> crisp core with a soft glow falloff.</summary>
    private static float Shape(float d)
    {
        float core = Mathf.Clamp01(1f - d / 0.04f);
        float glow = Mathf.Exp(-Mathf.Max(d, 0f) * 9f) * 0.45f;
        return Mathf.Clamp01(Mathf.Max(core, glow));
    }

    /// <summary>Soft round halo with a faint ring.</summary>
    private static Texture2D MakeHaloTexture(int size)
    {
        var tex = NewTex(size);
        var px = new Color32[size * size];
        float half = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = (x - half) / half, dy = (y - half) / half;
            float r = Mathf.Sqrt(dx * dx + dy * dy);
            float soft = Mathf.Pow(Mathf.Clamp01(1f - r), 2.5f) * 0.6f;
            float ring = Mathf.Exp(-Mathf.Pow((r - 0.8f) / 0.05f, 2f)) * 0.5f;
            px[y * size + x] = new Color(1f, 1f, 1f, r > 0.98f ? 0f : Mathf.Clamp01(soft + ring));
        }
        tex.SetPixels32(px);
        tex.Apply(true, true);
        return tex;
    }
}
