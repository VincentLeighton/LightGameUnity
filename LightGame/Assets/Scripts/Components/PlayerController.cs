using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles player movement via tap-to-move pathfinding.
/// Attach to the Player GameObject.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Tooltip("Movement speed in tiles per second")]
    public float MoveSpeed = 5f;

    /// <summary>Current tile position of the player on the grid.</summary>
    public Vector2Int CurrentTilePosition { get; private set; }

    /// <summary>Whether the player is currently moving along a path.</summary>
    public bool IsMoving { get; private set; }

    /// <summary>Fired when the player arrives at a new tile. Passes the new tile position.</summary>
    public event Action<Vector2Int> OnPlayerMoved;

    /// <summary>
    /// Callback used by the pathfinder to check walkability.
    /// Must be set by the LevelManager or similar system after level load.
    /// </summary>
    public Func<Vector2Int, bool> IsWalkable { get; set; }

    private List<Vector2Int> _currentPath;
    private int _pathIndex;
    private Vector3 _moveFrom;
    private Vector3 _moveTo;
    private float _moveProgress;

    private void Awake()
    {
        _currentPath = new List<Vector2Int>();
        _pathIndex = 0;
    }

    /// <summary>
    /// Initializes the player at the given tile position.
    /// Call this after level load to place the player on the grid.
    /// </summary>
    /// <summary>
    /// Initializes the player at the given tile position.
    /// Call this after level load to place the player on the grid.
    /// </summary>
    public void Initialize(Vector2Int startTile)
    {
        CurrentTilePosition = startTile;
        transform.position = TileToWorld(startTile);
        _currentPath.Clear();
        _pathIndex = 0;
        IsMoving = false;

        // Apply visual configuration
        transform.localScale = Vector3.one * VisualConfig.PlayerScale;

        // Only create sprite once
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteFactory.CreateCircle(VisualConfig.SpriteResolution, ColorPalette.Player);
            sr.color = Color.white; // Sprite already has color baked in
            sr.sortingOrder = VisualConfig.PlayerSortOrder;
        }
    }


    /// <summary>
    /// Requests the player to move to the target tile.
    /// Recalculates path even if already moving (supports requirement 1.4).
    /// </summary>
    public void MoveTo(Vector2Int targetTile)
    {
        if (IsWalkable == null)
            return;

        List<Vector2Int> path = Pathfinder.FindPath(CurrentTilePosition, targetTile, IsWalkable);

        if (path == null || path.Count <= 1)
            return; // No valid path or already at target

        _currentPath = path;
        _pathIndex = 1; // Skip index 0 (current position)
        _moveFrom = transform.position;
        _moveTo = TileToWorld(_currentPath[_pathIndex]);
        _moveProgress = 0f;
        IsMoving = true;
    }

    private void Update()
    {
        if (!IsMoving || _currentPath == null || _pathIndex >= _currentPath.Count)
            return;

        // Advance movement progress based on speed
        _moveProgress += Time.deltaTime * MoveSpeed;

        if (_moveProgress >= 1f)
        {
            // Arrived at the next tile
            CurrentTilePosition = _currentPath[_pathIndex];
            transform.position = TileToWorld(CurrentTilePosition);

            OnPlayerMoved?.Invoke(CurrentTilePosition);

            _pathIndex++;

            if (_pathIndex < _currentPath.Count)
            {
                // Continue to next tile in path
                _moveFrom = transform.position;
                _moveTo = TileToWorld(_currentPath[_pathIndex]);
                _moveProgress -= 1f; // Carry over excess for smooth movement
            }
            else
            {
                // Path complete
                IsMoving = false;
                _currentPath.Clear();
                _pathIndex = 0;
            }
        }
        else
        {
            // Interpolate position between tiles
            transform.position = Vector3.Lerp(_moveFrom, _moveTo, _moveProgress);
        }
    }

    /// <summary>
    /// Converts a tile grid position to a world position.
    /// Each tile is 1 unit. Tile (0,0) maps to world (0.5, 0.5) — center of tile.
    /// </summary>
    public static Vector3 TileToWorld(Vector2Int tile)
    {
        return new Vector3(tile.x + 0.5f, tile.y + 0.5f, 0f);
    }
}
