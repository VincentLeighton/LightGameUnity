using System;
using UnityEngine;

/// <summary>
/// A reflective object the player can place and rotate.
/// Uses ReflectionTable for beam direction calculations.
/// </summary>
public class Mirror : MonoBehaviour
{
    [Tooltip("Grid position of this mirror")]
    public Vector2Int TilePosition;

    [Tooltip("Rotation index 0-7, representing 0° to 315° in 45° steps")]
    [Range(0, 7)]
    public int RotationIndex;

    /// <summary>Fired when the mirror is placed, rotated, or picked up.</summary>
    public event Action OnMirrorChanged;

    /// <summary>
    /// Rotates the mirror by 45° clockwise. Wraps around at 360° (index 8 → 0).
    /// </summary>
    public void Rotate()
    {
        RotationIndex = (RotationIndex + 1) % 8;
        transform.rotation = Quaternion.Euler(0, 0, -RotationIndex * 45f);
        OnMirrorChanged?.Invoke();
    }

    /// <summary>
    /// Returns the reflected direction for a beam arriving from the given direction.
    /// Returns Vector2Int.zero if the beam is absorbed.
    /// </summary>
    public Vector2Int GetReflectedDirection(Vector2Int incomingDirection)
    {
        return ReflectionTable.GetReflectedDirection(incomingDirection, RotationIndex);
    }
}
