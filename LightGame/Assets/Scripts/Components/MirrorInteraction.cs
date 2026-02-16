using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles mirror pickup, placement, and rotation via touch input.
/// Triggers LightBeamSystem.RecalculateAllBeams on any mirror change.
/// </summary>
public class MirrorInteraction : MonoBehaviour
{
    [Tooltip("Reference to the player's inventory")]
    public Inventory PlayerInventory;

    [Tooltip("Reference to the light beam system for recalculation")]
    public LightBeamSystem BeamSystem;

    /// <summary>
    /// Callback to check if a tile is a wall.
    /// Must be set by LevelManager after level load.
    /// </summary>
    public Func<Vector2Int, bool> IsWall { get; set; }

    /// <summary>
    /// Callback to check if a tile is occupied by another object (mirror, emitter, etc.).
    /// Must be set by LevelManager after level load.
    /// </summary>
    public Func<Vector2Int, bool> IsOccupied { get; set; }

    /// <summary>Lookup of placed mirrors by tile position.</summary>
    private Dictionary<Vector2Int, Mirror> _placedMirrors = new Dictionary<Vector2Int, Mirror>();

    /// <summary>Prefab used to instantiate new mirrors.</summary>
    public GameObject MirrorPrefab;

    /// <summary>Parent transform for instantiated mirrors.</summary>
    public Transform MirrorParent;

    /// <summary>
    /// Registers an existing mirror (from level data) in the lookup.
    /// </summary>
    public void RegisterMirror(Mirror mirror)
    {
        _placedMirrors[mirror.TilePosition] = mirror;
    }

    /// <summary>
    /// Attempts to place a mirror from inventory onto the given tile.
    /// Returns false if the tile is a wall, occupied, or inventory is empty.
    /// </summary>
    public bool TryPlaceMirror(Vector2Int tilePosition)
    {
        if (IsWall != null && IsWall(tilePosition))
            return false;

        if (IsOccupied != null && IsOccupied(tilePosition))
            return false;

        if (_placedMirrors.ContainsKey(tilePosition))
            return false;

        if (PlayerInventory == null || !PlayerInventory.RemoveMirror())
            return false;

        // Instantiate mirror
        Mirror mirror;
        if (MirrorPrefab != null)
        {
            var go = UnityEngine.Object.Instantiate(MirrorPrefab, MirrorParent);
            go.transform.position = new Vector3(tilePosition.x + 0.5f, tilePosition.y + 0.5f, 0f);
            mirror = go.GetComponent<Mirror>();
        }
        else
        {
            var go = new GameObject("Mirror");
            if (MirrorParent != null) go.transform.SetParent(MirrorParent);
            go.transform.position = new Vector3(tilePosition.x + 0.5f, tilePosition.y + 0.5f, 0f);
            mirror = go.AddComponent<Mirror>();
        }

        mirror.TilePosition = tilePosition;
        mirror.RotationIndex = 0;
        _placedMirrors[tilePosition] = mirror;

        if (BeamSystem != null)
            BeamSystem.RecalculateAllBeams();

        return true;
    }

    /// <summary>
    /// Attempts to rotate the mirror at the given tile by 45° clockwise.
    /// Returns false if no mirror exists at that position.
    /// </summary>
    public bool TryRotateMirror(Vector2Int tilePosition)
    {
        if (!_placedMirrors.TryGetValue(tilePosition, out var mirror))
            return false;

        mirror.Rotate();

        if (BeamSystem != null)
            BeamSystem.RecalculateAllBeams();

        return true;
    }

    /// <summary>
    /// Attempts to pick up the mirror at the given tile and return it to inventory.
    /// Returns false if no mirror exists at that position.
    /// </summary>
    public bool TryPickupMirror(Vector2Int tilePosition)
    {
        if (!_placedMirrors.TryGetValue(tilePosition, out var mirror))
            return false;

        _placedMirrors.Remove(tilePosition);

        if (PlayerInventory != null)
            PlayerInventory.AddMirror();

        if (mirror != null && mirror.gameObject != null)
            UnityEngine.Object.Destroy(mirror.gameObject);

        if (BeamSystem != null)
            BeamSystem.RecalculateAllBeams();

        return true;
    }

    /// <summary>
    /// Clears all tracked mirrors. Call when unloading a level.
    /// </summary>
    public void Clear()
    {
        _placedMirrors.Clear();
    }
}
