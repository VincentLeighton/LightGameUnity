using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// An ambient light object that illuminates a circular area using shadow casting.
/// Attach to light source GameObjects. Delegates illumination calculation to LightSourceLogic.
/// </summary>
public class LightSource : MonoBehaviour
{
    [Tooltip("Grid position of this light source")]
    public Vector2Int TilePosition;

    [Tooltip("Illumination radius in tiles")]
    public int IlluminationRadius = 3;

    /// <summary>
    /// Callback used to check if a tile is a wall.
    /// Must be set by the LevelManager after level load.
    /// </summary>
    public Func<Vector2Int, bool> IsWall { get; set; }

    /// <summary>
    /// Returns the set of floor tiles illuminated by this light source.
    /// Uses LightSourceLogic for the actual shadow-casting calculation.
    /// </summary>
    public HashSet<Vector2Int> GetIlluminatedTiles()
    {
        if (IsWall == null)
            return new HashSet<Vector2Int>();

        return LightSourceLogic.GetRadiusIlluminatedTiles(TilePosition, IlluminationRadius, IsWall);
    }
}
