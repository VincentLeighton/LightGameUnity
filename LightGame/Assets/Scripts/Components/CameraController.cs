using UnityEngine;

/// <summary>
/// Follows the player with smooth interpolation and supports pinch-to-zoom.
/// Attach to the Main Camera.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Tooltip("Lerp speed for smooth follow (higher = snappier)")]
    public float SmoothSpeed = 5f;

    [Tooltip("Minimum orthographic size (most zoomed in)")]
    public float MinZoom = 2f;

    [Tooltip("Maximum orthographic size (most zoomed out)")]
    public float MaxZoom = 10f;

    /// <summary>The world-space bounds of the current level.</summary>
    public Bounds LevelBounds { get; set; }

    private Transform _target;
    private Camera _camera;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _camera.orthographic = true;
    }

    /// <summary>
    /// Sets the transform the camera should follow (typically the Player).
    /// </summary>
    public void SetTarget(Transform target)
    {
        _target = target;
    }

    /// <summary>
    /// Applies a zoom delta from pinch input. Positive = zoom out, negative = zoom in.
    /// </summary>
    public void HandlePinchZoom(float zoomDelta)
    {
        float newSize = _camera.orthographicSize + zoomDelta;
        _camera.orthographicSize = CameraLogic.ClampZoom(newSize, MinZoom, MaxZoom);
    }

    private void LateUpdate()
    {
        if (_target == null)
            return;

        Vector3 targetPos = _target.position;

        // Smooth follow
        Vector3 smoothed = Vector3.Lerp(transform.position, targetPos, SmoothSpeed * Time.deltaTime);

        // Clamp within level bounds
        Vector2 levelMin = new Vector2(LevelBounds.min.x, LevelBounds.min.y);
        Vector2 levelMax = new Vector2(LevelBounds.max.x, LevelBounds.max.y);

        Vector2 clamped = CameraLogic.ClampPosition(
            new Vector2(smoothed.x, smoothed.y),
            _camera.orthographicSize,
            _camera.aspect,
            levelMin,
            levelMax);

        transform.position = new Vector3(clamped.x, clamped.y, transform.position.z);
    }
}
