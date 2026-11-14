using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "Time stop" from the purple power-up.
///
/// While active:
///  - Asteroids, enemy ships and enemy shots stop dead: their scripts are disabled, rigidbodies
///    are parked (velocity stored and restored) and pending self-destruct timers are paused.
///  - The spawner sends nothing new (it waits on <see cref="Active"/>).
///  - The player keeps moving and shooting. Player bullets coast to a stop just ahead of the ship
///    and hang there with their hitboxes off, piling up. When time resumes they fly on and hit
///    whatever is in their path (see Projectile.cs).
///
/// The real time scale is never touched, so the player, UI and sounds run normally.
/// </summary>
public class TimeFreeze : MonoBehaviour
{
    public static bool Active => instance != null && instance.active;

    /// <summary>Seconds left in the current freeze (0 when not frozen).</summary>
    public static float Remaining => Active ? Mathf.Max(0f, instance.endTime - Time.time) : 0f;

    private static TimeFreeze instance;

    private bool active;
    private float endTime;
    private float duration;
    private float rescanTimer;

    private class Frozen
    {
        public readonly List<MonoBehaviour> behaviours = new List<MonoBehaviour>();
        public readonly List<(Rigidbody rb, Vector3 vel, Vector3 ang, bool kinematic)> bodies =
            new List<(Rigidbody, Vector3, Vector3, bool)>();
        public readonly List<(Animator anim, float speed)> animators = new List<(Animator, float)>();
        public readonly List<ParticleSystem> particles = new List<ParticleSystem>();
        public Enemy1Laser laser;
        public Enemy asteroid;
    }

    private readonly Dictionary<GameObject, Frozen> frozen = new Dictionary<GameObject, Frozen>();

    // Overlay UI
    private CanvasGroup overlay;
    private RectTransform barFill;
    private float overlayAlpha;
    private AudioSource audioSource;

    // ------------------------------------------------------------------ public API

    public static void Begin(float seconds)
    {
        if (instance == null)
        {
            var go = new GameObject("TimeFreeze");
            instance = go.AddComponent<TimeFreeze>();
            instance.BuildOverlay();
        }
        instance.StartFreeze(seconds);
    }

    private void StartFreeze(float seconds)
    {
        if (active)
        {
            endTime = Mathf.Max(endTime, Time.time + seconds); // stacking extends the freeze
            duration = endTime - Time.time;
        }
        else
        {
            active = true;
            endTime = Time.time + seconds;
            duration = seconds;
            Play("Sound/TimeFreezeStart");
        }
        CaptureTargets();
    }

    // ------------------------------------------------------------------ freeze / unfreeze

    private void Update()
    {
        if (active)
        {
            // Catch anything that appeared since (e.g. a shot fired the same frame).
            rescanTimer -= Time.deltaTime;
            if (rescanTimer <= 0f)
            {
                rescanTimer = 0.25f;
                CaptureTargets();
            }

            if (Time.time >= endTime) EndFreeze();
        }

        UpdateOverlay();
    }

    private void CaptureTargets()
    {
        var roots = new HashSet<GameObject>();
        Collect<Enemy>(roots);
        Collect<Enemy1Movement>(roots);
        Collect<Enemy1DamageSystem>(roots);
        Collect<Enemy1Projectile>(roots);
        Collect<Enemy2Movement>(roots);
        Collect<Enemy3>(roots);
        Collect<Enemy1Laser>(roots);

        foreach (var root in roots)
        {
            if (frozen.ContainsKey(root)) continue;
            frozen[root] = Freeze(root);
        }
    }

    private static void Collect<T>(HashSet<GameObject> roots) where T : Component
    {
        foreach (var c in FindObjectsByType<T>())
            if (c != null) roots.Add(c.transform.root.gameObject);
    }

    private static Frozen Freeze(GameObject root)
    {
        var f = new Frozen();

        foreach (var mb in root.GetComponentsInChildren<MonoBehaviour>())
        {
            if (mb == null || !mb.enabled) continue;
            mb.enabled = false; // stops Update/FixedUpdate: movement, aiming, shooting timers
            f.behaviours.Add(mb);
        }

        foreach (var rb in root.GetComponentsInChildren<Rigidbody>())
        {
            f.bodies.Add((rb, rb.isKinematic ? Vector3.zero : rb.linearVelocity,
                              rb.isKinematic ? Vector3.zero : rb.angularVelocity, rb.isKinematic));
            if (!rb.isKinematic)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            rb.isKinematic = true;
        }

        foreach (var anim in root.GetComponentsInChildren<Animator>())
        {
            f.animators.Add((anim, anim.speed));
            anim.speed = 0f;
        }

        foreach (var ps in root.GetComponentsInChildren<ParticleSystem>())
        {
            if (!ps.isPlaying) continue;
            ps.Pause(false);
            f.particles.Add(ps);
        }

        // Pause self-destruct timers so frozen shots/rocks don't vanish mid-freeze.
        f.laser = root.GetComponent<Enemy1Laser>();
        if (f.laser != null) f.laser.CancelInvoke();
        f.asteroid = root.GetComponent<Enemy>();
        if (f.asteroid != null) f.asteroid.CancelInvoke();

        return f;
    }

    private void EndFreeze()
    {
        active = false;

        foreach (var pair in frozen)
        {
            if (pair.Key == null) continue; // destroyed while frozen (e.g. rammed by the player)
            Frozen f = pair.Value;

            foreach (var (rb, vel, ang, kinematic) in f.bodies)
            {
                if (rb == null) continue;
                rb.isKinematic = kinematic;
                if (!kinematic)
                {
                    rb.linearVelocity = vel;
                    rb.angularVelocity = ang;
                }
            }
            foreach (var mb in f.behaviours) if (mb != null) mb.enabled = true;
            foreach (var (anim, speed) in f.animators) if (anim != null) anim.speed = speed;
            foreach (var ps in f.particles) if (ps != null) ps.Play(false);

            // Restart the paused self-destruct timers.
            if (f.laser != null) f.laser.Invoke("DestroyBullet", 3f);
            if (f.asteroid != null) f.asteroid.Invoke("DestroyAsteroid", 15f);
        }
        frozen.Clear();

        Play("Sound/TimeFreezeEnd");
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    // ------------------------------------------------------------------ feedback

    private void Play(string resourcePath)
    {
        var clip = Resources.Load<AudioClip>(resourcePath);
        if (clip == null) return;
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }
        audioSource.PlayOneShot(clip, 0.7f);
    }

    /// <summary>Subtle violet screen tint, a draining bar and a small label (HUD style).</summary>
    private void BuildOverlay()
    {
        var canvasGo = new GameObject("TimeFreezeOverlay", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        overlay = canvasGo.AddComponent<CanvasGroup>();
        overlay.alpha = 0f;
        overlay.blocksRaycasts = false;
        overlay.interactable = false;

        var tint = HudStyle.Block("Tint", canvasGo.transform, new Color(0.45f, 0.3f, 0.85f, 0.10f));
        HudStyle.Stretch(tint.rectTransform);

        // Draining bar under the top edge.
        var track = HudStyle.Block("BarTrack", canvasGo.transform, new Color(1f, 1f, 1f, 0.12f)).rectTransform;
        HudStyle.Place(track, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(480f, 3f));
        var fill = HudStyle.Block("BarFill", track, new Color(0.85f, 0.75f, 1f, 0.95f));
        barFill = fill.rectTransform;
        barFill.anchorMin = Vector2.zero;
        barFill.anchorMax = Vector2.one;
        barFill.offsetMin = barFill.offsetMax = Vector2.zero;

        var label = HudStyle.Label("Label", canvasGo.transform, "TIME FREEZE", TMPro.TextAlignmentOptions.Center);
        HudStyle.Place(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(480f, 18f));
        label.rectTransform.pivot = new Vector2(0.5f, 1f);
        label.color = new Color(0.9f, 0.85f, 1f, 0.9f);
    }

    private void UpdateOverlay()
    {
        if (overlay == null) return;
        float target = active ? 1f : 0f;
        overlayAlpha = Mathf.MoveTowards(overlayAlpha, target, Time.unscaledDeltaTime / (active ? 0.15f : 0.3f));
        overlay.alpha = overlayAlpha;

        if (barFill != null)
        {
            float k = active && duration > 0f ? Mathf.Clamp01((endTime - Time.time) / duration) : 0f;
            barFill.anchorMax = new Vector2(k, 1f);
        }
    }
}
