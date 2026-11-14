using UnityEngine;

/// <summary>
/// Shared camera-facing quad for glow sprites (bullets, power-ups).
/// Cheaper than GameObject.CreatePrimitive, which also builds (and then has to destroy) a
/// MeshCollider every time; doing that for every bullet caused small hitches while shooting.
/// </summary>
public static class QuadMesh
{
    private static Mesh mesh;

    public static Mesh Get()
    {
        if (mesh != null) return mesh;
        mesh = new Mesh { name = "Shared Glow Quad", hideFlags = HideFlags.DontUnloadUnusedAsset };
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f)
        };
        mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    /// <summary>Creates a child GameObject with the shared quad (no collider).</summary>
    public static GameObject Create(string name, Transform parent, Material material)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = Get();
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = material;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        return go;
    }
}
