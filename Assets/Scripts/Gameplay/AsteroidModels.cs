using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Swaps each spawned asteroid's mesh for a random rock from
/// Assets/Resources/Asteroids/Asteroids.fbx, with the lava material (AsteroidLava.mat).
///
/// The rocks are re-centred (so they tumble around their middle, whatever the Blender origin was)
/// and scaled to the same size as the original asteroid mesh, so the existing size range,
/// collider and gameplay stay the same. Prepared once, then shared by every asteroid.
/// </summary>
public static class AsteroidModels
{
    private const string FbxPath = "Asteroids/Asteroids";        // Resources path, no extension
    private const string MaterialPath = "Asteroids/AsteroidLava";

    private static readonly List<Mesh> Meshes = new List<Mesh>();
    private static Material material;
    private static bool loaded;

    public static void Apply(GameObject asteroid)
    {
        var filter = asteroid.GetComponentInChildren<MeshFilter>();
        if (filter == null || filter.sharedMesh == null) return;

        if (!loaded || Meshes.Count == 0 || Meshes[0] == null)
            Load(filter.sharedMesh.bounds.extents.magnitude);
        if (Meshes.Count == 0) return; // FBX missing: keep the original rock

        Mesh mesh = Meshes[Random.Range(0, Meshes.Count)];
        filter.sharedMesh = mesh;

        var renderer = filter.GetComponent<MeshRenderer>();
        if (renderer != null && material != null)
        {
            var mats = new Material[Mathf.Max(1, mesh.subMeshCount)];
            for (int i = 0; i < mats.Length; i++) mats[i] = material;
            renderer.sharedMaterials = mats;
        }
    }

    private static void Load(float targetRadius)
    {
        loaded = true;
        Meshes.Clear();

        material = Resources.Load<Material>(MaterialPath);
        if (material == null) Debug.LogWarning($"AsteroidModels: material Resources/{MaterialPath} not found.");

        foreach (var source in Resources.LoadAll<Mesh>(FbxPath))
        {
            if (source == null || source.vertexCount < 12) continue;
            Meshes.Add(Normalise(source, targetRadius));
        }

        if (Meshes.Count == 0) Debug.LogWarning($"AsteroidModels: no meshes found in Resources/{FbxPath}.fbx.");
    }

    /// <summary>Copy of the mesh centred on its bounds and scaled so its half-diagonal = targetRadius.</summary>
    private static Mesh Normalise(Mesh source, float targetRadius)
    {
        if (!source.isReadable || targetRadius <= 0f) return source; // can't edit vertices: use as-is

        Bounds b = source.bounds;
        float scale = b.extents.magnitude > 1e-5f ? targetRadius / b.extents.magnitude : 1f;

        Mesh mesh = Object.Instantiate(source);
        mesh.name = source.name + " (asteroid)";
        mesh.hideFlags = HideFlags.DontUnloadUnusedAsset;

        var verts = mesh.vertices;
        for (int i = 0; i < verts.Length; i++) verts[i] = (verts[i] - b.center) * scale;
        mesh.vertices = verts;
        mesh.RecalculateBounds(); // normals/tangents are unaffected by uniform scale + offset
        return mesh;
    }
}
