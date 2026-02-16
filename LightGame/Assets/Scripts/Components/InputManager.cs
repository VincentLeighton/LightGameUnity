using System;
using UnityEngine;

/// <summary>
/// Translates touch input into game actions.
/// Handles tap, long press, and pinch-to-zoom.
/// Routes input to PlayerController or MirrorInteraction based on current mode.
/// </summary>
public class InputManager : MonoBehaviour
{
    [Tooltip("Reference to the PlayerController")]
    public PlayerController Player;

    [Tooltip("Reference to the MirrorInteraction handler")]
    public MirrorInteraction MirrorHandler;

    [Tooltip("Reference to the CameraController for pinch zoom")]
    public CameraController CameraCtrl;

    [Tooltip("Long press threshold in seconds")]
    public float LongPressThreshold = 0.2f;

    [Tooltip("Minimum interval between mirror rotations in seconds")]
    public float RotationDebounce = 0.2f;

    [Tooltip("Pinch zoom sensitivity multiplier")]
    public float PinchSensitivity = 0.01f;

    /// <summary>Current interaction mode.</summary>
    public InteractionMode CurrentMode { get; private set; } = InteractionMode.Movement;

    /// <summary>Fired when a tile is tapped. Passes the tile position.</summary>
    public event Action<Vector2Int> OnTileTapped;

    /// <summary>Fired when a tile is long-pressed. Passes the tile position.</summary>
    public event Action<Vector2Int> OnTileLongPressed;

    /// <summary>Fired on pinch zoom. Passes the zoom delta.</summary>
    public event Action<float> OnPinchZoom;

    // Callback to convert screen position to tile position.
    // Must be set after level load (needs camera reference).
    public Func<Vector2, Vector2Int> ScreenToTile { get; set; }

    private float _touchStartTime;
    private Vector2 _touchStartPos;
    private bool _touchActive;
    private float _lastRotationTime;
    private float _pinchStartDistance;

    /// <summary>
    /// Switches between Movement and MirrorPlacement modes.
    /// </summary>
    public void SetMode(InteractionMode mode)
    {
        CurrentMode = mode;
    }

    private void Update()
    {
        HandleTouchInput();
    }

    private void HandleTouchInput()
    {
        int touchCount = Input.touchCount;

        // Pinch-to-zoom: exactly two touches
        if (touchCount == 2)
        {
            HandlePinch();
            _touchActive = false; // Cancel any single-touch gesture
            return;
        }

        // Single touch
        if (touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    _touchStartTime = Time.unscaledTime;
                    _touchStartPos = touch.position;
                    _touchActive = true;
                    break;

                case TouchPhase.Stationary:
                case TouchPhase.Moved:
                    if (_touchActive && Time.unscaledTime - _touchStartTime >= LongPressThreshold)
                    {
                        // Long press detected
                        Vector2Int tile = ScreenToTilePosition(touch.position);
                        OnTileLongPressed?.Invoke(tile);
                        HandleLongPress(tile);
                        _touchActive = false;
                    }
                    break;

                case TouchPhase.Ended:
                    if (_touchActive)
                    {
                        float duration = Time.unscaledTime - _touchStartTime;
                        if (duration < LongPressThreshold)
                        {
                            // Single tap
                            Vector2Int tile = ScreenToTilePosition(touch.position);
                            OnTileTapped?.Invoke(tile);
                            HandleTap(tile);
                        }
                        _touchActive = false;
                    }
                    break;

                case TouchPhase.Canceled:
                    _touchActive = false;
                    break;
            }
        }

        // Editor mouse fallback (for testing in Unity Editor)
#if UNITY_EDITOR
        HandleMouseInput();
#endif
    }

    private void HandlePinch()
    {
        Touch t0 = Input.GetTouch(0);
        Touch t1 = Input.GetTouch(1);

        if (t1.phase == TouchPhase.Began)
        {
            _pinchStartDistance = Vector2.Distance(t0.position, t1.position);
            return;
        }

        float currentDistance = Vector2.Distance(t0.position, t1.position);
        float delta = (_pinchStartDistance - currentDistance) * PinchSensitivity;
        _pinchStartDistance = currentDistance;

        OnPinchZoom?.Invoke(delta);

        if (CameraCtrl != null)
            CameraCtrl.HandlePinchZoom(delta);
    }

    private void HandleTap(Vector2Int tile)
    {
        if (CurrentMode == InteractionMode.Movement)
        {
            if (Player != null)
                Player.MoveTo(tile);
        }
        else if (CurrentMode == InteractionMode.MirrorPlacement)
        {
            if (MirrorHandler != null)
            {
                // Try rotate first (if mirror exists at tile), with debounce
                if (Time.unscaledTime - _lastRotationTime >= RotationDebounce
                    && MirrorHandler.TryRotateMirror(tile))
                {
                    _lastRotationTime = Time.unscaledTime;
                }
                else
                {
                    // Otherwise try to place a mirror
                    MirrorHandler.TryPlaceMirror(tile);
                }
            }
        }
    }

    private void HandleLongPress(Vector2Int tile)
    {
        if (MirrorHandler != null)
            MirrorHandler.TryPickupMirror(tile);
    }

    private Vector2Int ScreenToTilePosition(Vector2 screenPos)
    {
        if (ScreenToTile != null)
            return ScreenToTile(screenPos);

        // Default: use main camera to convert screen to world, then to tile
        Camera cam = Camera.main;
        if (cam == null)
            return Vector2Int.zero;

        Vector3 worldPos = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
        return new Vector2Int(Mathf.FloorToInt(worldPos.x), Mathf.FloorToInt(worldPos.y));
    }

#if UNITY_EDITOR
    private bool _mouseDown;
    private float _mouseDownTime;

    private void HandleMouseInput()
    {
        if (Input.touchCount > 0) return; // Don't double-handle

        if (Input.GetMouseButtonDown(0))
        {
            _mouseDown = true;
            _mouseDownTime = Time.unscaledTime;
        }
        else if (Input.GetMouseButton(0) && _mouseDown)
        {
            if (Time.unscaledTime - _mouseDownTime >= LongPressThreshold)
            {
                Vector2Int tile = ScreenToTilePosition(Input.mousePosition);
                OnTileLongPressed?.Invoke(tile);
                HandleLongPress(tile);
                _mouseDown = false;
            }
        }
        else if (Input.GetMouseButtonUp(0) && _mouseDown)
        {
            float duration = Time.unscaledTime - _mouseDownTime;
            if (duration < LongPressThreshold)
            {
                Vector2Int tile = ScreenToTilePosition(Input.mousePosition);
                OnTileTapped?.Invoke(tile);
                HandleTap(tile);
            }
            _mouseDown = false;
        }
    }
#endif
}
