using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Finds enemies for homing bullets. The enemy list is cached and refreshed at most a few
/// times per second, so many bullets searching at once stays cheap.
/// </summary>
public static class HomingTargets
{
    private const float RefreshInterval = 0.2f;

    private static readonly List<Transform> Targets = new List<Transform>();
    private static float lastRefresh = -1f;
    private static int lastFrame = -1;

    /// <summary>
    /// Nearest enemy within range that is roughly in front of the bullet (within ~70°).
    /// Returns null if there is none.
    /// </summary>
    public static Transform FindBest(Vector3 position, Vector3 forward, float range)
    {
        Refresh();

        Transform best = null;
        float bestScore = float.MaxValue;
        float rangeSqr = range * range;

        foreach (var t in Targets)
        {
            if (t == null || !t.gameObject.activeInHierarchy) continue;

            Vector2 to = t.position - position;
            float distSqr = to.sqrMagnitude;
            if (distSqr > rangeSqr || distSqr < 0.0001f) continue;

            float facing = Vector2.Dot(forward, to.normalized);
            if (facing < 0.35f) continue; // only things ahead of the bullet

            // Prefer close targets that need little turning.
            float score = distSqr * (2f - facing);
            if (score < bestScore)
            {
                bestScore = score;
                best = t;
            }
        }
        return best;
    }

    private static void Refresh()
    {
        if (Time.frameCount == lastFrame) return;
        if (Time.time - lastRefresh < RefreshInterval && lastRefresh >= 0f) return;
        lastFrame = Time.frameCount;
        lastRefresh = Time.time;

        Targets.Clear();
        Add(Object.FindObjectsByType<Enemy1DamageSystem>());
        Add(Object.FindObjectsByType<Enemy2Movement>());
        Add(Object.FindObjectsByType<Enemy3>());
        Add(Object.FindObjectsByType<Enemy>());
    }

    private static void Add<T>(T[] found) where T : Component
    {
        foreach (var c in found) Targets.Add(c.transform);
    }
}
