using UnityEngine;

/// <summary>
/// Pure static helpers for camera position clamping and zoom clamping.
/// Testable without MonoBehaviour or a running scene.
/// </summary>
public static class CameraLogic
{
    /// <summary>
    /// Clamps a camera position so the viewport stays within level bounds.
    /// </summary>
    /// <param name="cameraPos">Desired camera world position (x, y).</param>
    /// <param name="orthoSize">Current orthographic size (half-height in world units).</param>
    /// <param name="aspectRatio">Camera aspect ratio (width / height).</param>
    /// <param name="levelMin">Minimum corner of the level bounds.</param>
    /// <param name="levelMax">Maximum corner of the level bounds.</param>
    /// <returns>Clamped camera position.</returns>
    public static Vector2 ClampPosition(
        Vector2 cameraPos,
        float orthoSize,
        float aspectRatio,
        Vector2 levelMin,
        Vector2 levelMax)
    {
        float halfHeight = orthoSize;
        float halfWidth = orthoSize * aspectRatio;

        float minX = levelMin.x + halfWidth;
        float maxX = levelMax.x - halfWidth;
        float minY = levelMin.y + halfHeight;
        float maxY = levelMax.y - halfHeight;

        // If the level is smaller than the viewport, center the camera
        float clampedX = (minX > maxX) ? (levelMin.x + levelMax.x) * 0.5f : Mathf.Clamp(cameraPos.x, minX, maxX);
        float clampedY = (minY > maxY) ? (levelMin.y + levelMax.y) * 0.5f : Mathf.Clamp(cameraPos.y, minY, maxY);

        return new Vector2(clampedX, clampedY);
    }

    /// <summary>
    /// Clamps orthographic size between min and max.
    /// </summary>
    public static float ClampZoom(float orthoSize, float minZoom, float maxZoom)
    {
        return Mathf.Clamp(orthoSize, minZoom, maxZoom);
    }
}
