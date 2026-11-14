using UnityEngine;

/// <summary>
/// Gives enemy ships a subtle coral glow outline (same colour as enemy shots) so they read
/// clearly against the dark background. For every mesh of the ship a "GlowShell" child is added
/// that draws the same mesh with the EnemyGlow shader (an inflated, edge-only fresnel halo).
/// Called by the Spawner for every ship it spawns.
/// </summary>
public static class EnemyGlow
{
    private const string ShellName = "GlowShell";
    private static Material glowMaterial;

    public static void Apply(GameObject enemy)
    {
        if (enemy == null) return;
        Material mat = GetMaterial();
        if (mat == null) return;

        foreach (var filter in enemy.GetComponentsInChildren<MeshFilter>(true))
        {
            if (filter.sharedMesh == null || filter.name == ShellName) continue;
            var source = filter.GetComponent<MeshRenderer>();
            if (source == null) continue;
            if (filter.transform.Find(ShellName) != null) continue; // already applied

            var shell = new GameObject(ShellName);
            shell.layer = filter.gameObject.layer;
            shell.transform.SetParent(filter.transform, false);

            shell.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
            var r = shell.AddComponent<MeshRenderer>();
            var mats = new Material[Mathf.Max(1, filter.sharedMesh.subMeshCount)];
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            r.sharedMaterials = mats;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            r.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }
    }

    private static Material GetMaterial()
    {
        if (glowMaterial != null) return glowMaterial;
        Shader shader = Resources.Load<Shader>("VFX/EnemyGlow");
        if (shader == null) shader = Shader.Find("SpaceShooter/EnemyGlow");
        if (shader == null || !shader.isSupported) return null;
        glowMaterial = new Material(shader) { name = "Enemy Glow" };
        return glowMaterial;
    }
}
