using UnityEngine;

/// <summary>
/// What the camera sees on the gameplay plane (z = 0), for orthographic AND perspective cameras.
/// Everything that used orthographicSize / ViewportToWorldPoint(z = 0) goes through here, so the
/// game works the same whichever projection the camera uses.
/// </summary>
public static class CameraView
{
    public const float GameplayPlaneZ = 0f;

    /// <summary>Half of the visible height (world units) on the gameplay plane.</summary>
    public static float HalfHeight(Camera cam)
    {
        if (cam == null) return 7f;
        if (cam.orthographic) return cam.orthographicSize;
        float distance = Mathf.Abs(GameplayPlaneZ - cam.transform.position.z);
        return distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
    }

    /// <summary>Half of the visible width (world units) on the gameplay plane.</summary>
    public static float HalfWidth(Camera cam) => HalfHeight(cam) * (cam != null ? cam.aspect : 16f / 9f);

    /// <summary>Viewport point (0..1, 0..1) projected onto the gameplay plane.</summary>
    public static Vector3 ViewportToPlane(Camera cam, float x, float y)
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(x, y, 0f));
        if (Mathf.Abs(ray.direction.z) < 1e-5f) return new Vector3(ray.origin.x, ray.origin.y, GameplayPlaneZ);
        float t = (GameplayPlaneZ - ray.origin.z) / ray.direction.z;
        Vector3 p = ray.origin + ray.direction * t;
        p.z = GameplayPlaneZ;
        return p;
    }
}
