using UnityEngine;

/// <summary>
/// Gives the Tank's shield bubble a white fresnel energy glow (see ShieldFresnel.shader),
/// with a quick pop-in each time the shield is activated.
/// Added at runtime by TwinStickMovement via <see cref="Apply"/>.
/// </summary>
public class ShieldGlow : MonoBehaviour
{
    private const float PopDuration = 0.25f;

    private static Material sharedMaterial;
    private static readonly int FadeId = Shader.PropertyToID("_Fade");

    private Renderer[] renderers;
    private MaterialPropertyBlock block;
    private float timer;
    private Vector3 baseScale;

    public static void Apply(GameObject shield)
    {
        if (shield == null || shield.GetComponent<ShieldGlow>() != null) return;

        if (sharedMaterial == null)
        {
            Shader shader = Resources.Load<Shader>("VFX/ShieldFresnel");
            if (shader == null) shader = Shader.Find("SpaceShooter/ShieldFresnel");
            if (shader == null || !shader.isSupported) return; // keep the original material
            sharedMaterial = new Material(shader) { name = "Shield Fresnel" };
        }

        var glow = shield.AddComponent<ShieldGlow>();
        glow.renderers = shield.GetComponentsInChildren<Renderer>(true);
        foreach (var r in glow.renderers)
        {
            r.sharedMaterial = sharedMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
        glow.baseScale = shield.transform.localScale;
        glow.block = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        timer = 0f;
        if (block != null) UpdateVisual(0f);
    }

    private void OnDisable()
    {
        if (baseScale != Vector3.zero) transform.localScale = baseScale;
    }

    private void Update()
    {
        if (block == null) return;
        timer += Time.deltaTime;
        UpdateVisual(Mathf.Clamp01(timer / PopDuration));
    }

    private void UpdateVisual(float k)
    {
        // Ease-out pop: grows slightly past full size, then settles.
        float eased = 1f - (1f - k) * (1f - k);
        float overshoot = 1f + 0.12f * Mathf.Sin(k * Mathf.PI);
        transform.localScale = baseScale * Mathf.Lerp(0.6f, 1f, eased) * overshoot;

        block.SetFloat(FadeId, eased);
        foreach (var r in renderers) r.SetPropertyBlock(block);
    }
}
