using UnityEngine;

/// <summary>
/// Procedural star field + coloured nebulas rendered on a quad at the back of the camera.
/// Each layer scrolls at a different rate for a subtle parallax, driven by a slow drift
/// plus the camera and player position. Call <see cref="Attach"/> once (GameLogic does this).
/// </summary>
[RequireComponent(typeof(Camera))]
public class SpaceBackground : MonoBehaviour
{
    [Tooltip("Constant drift in world units/sec (stars move opposite to this).")]
    [SerializeField] private Vector2 drift = new Vector2(0f, 0.8f);
    [Tooltip("How much player movement shifts the background layers.")]
    [SerializeField] private float playerParallax = 0.6f;

    private Camera cam;
    private Transform quad;
    private Material material;
    private Transform player;
    private Vector2 driftOffset;

    private static readonly int OffsetId = Shader.PropertyToID("_Offset");
    private static readonly int AspectId = Shader.PropertyToID("_Aspect");
    private static readonly int ScaleId = Shader.PropertyToID("_Scale");

    public static SpaceBackground Attach(Camera camera)
    {
        if (camera == null) return null;
        var bg = camera.GetComponent<SpaceBackground>();
        return bg != null ? bg : camera.gameObject.AddComponent<SpaceBackground>();
    }

    private void Awake()
    {
        cam = GetComponent<Camera>();

        Shader shader = Resources.Load<Shader>("VFX/SpaceBackground");
        if (shader == null) shader = Shader.Find("SpaceShooter/ProceduralSpace");
        if (shader == null || !shader.isSupported)
        {
            Debug.LogWarning("SpaceBackground: shader not available, keeping the existing background.");
            enabled = false;
            return;
        }

        material = new Material(shader) { name = "Procedural Space" };

        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = "ProceduralSpaceBackground";
        Destroy(go.GetComponent<Collider>());
        go.layer = gameObject.layer;
        quad = go.transform;
        quad.SetParent(transform, false);

        var r = go.GetComponent<MeshRenderer>();
        r.sharedMaterial = material;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

        // The quad covers the whole view, so skip drawing the skybox behind it.
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
    }

    private void LateUpdate()
    {
        if (material == null) return;

        // Fit the quad to the view, just in front of the far plane.
        float dist = cam.farClipPlane * 0.95f;
        float height = cam.orthographic
            ? cam.orthographicSize * 2f
            : 2f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float width = height * cam.aspect;

        quad.localPosition = new Vector3(0f, 0f, dist);
        quad.localRotation = Quaternion.identity;
        quad.localScale = new Vector3(width * 1.02f, height * 1.02f, 1f);

        if (player == null)
        {
            var mover = FindAnyObjectByType<TwinStickMovement>();
            if (mover != null) player = mover.transform;
        }

        driftOffset += drift * Time.deltaTime;
        Vector2 offset = (Vector2)cam.transform.position + driftOffset;
        if (player != null) offset += (Vector2)player.position * playerParallax;

        material.SetVector(OffsetId, offset);
        material.SetFloat(AspectId, cam.aspect);
        // Stars/grid are sized in gameplay-plane units (not at the far quad), so they look the same
        // with an orthographic or a perspective camera.
        material.SetFloat(ScaleId, 2f * CameraView.HalfHeight(cam));
    }

    private void OnDestroy()
    {
        if (material != null) Destroy(material);
    }
}
