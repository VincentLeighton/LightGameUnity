using System;
using UnityEngine;

/// <summary>
/// A directional beam source that emits a Light_Beam only when its tile
/// is illuminated by a LightSource. Attach to beam emitter GameObjects.
/// </summary>
public class BeamEmitter : MonoBehaviour
{
    [Tooltip("Grid position of this beam emitter")]
    public Vector2Int TilePosition;

    [Tooltip("Direction the beam fires (one of 8 grid directions)")]
    public Vector2Int BeamDirection;

    /// <summary>Whether this emitter is currently active (its tile is illuminated by a light source).</summary>
    public bool IsActive { get; private set; }

    /// <summary>Fired when the activation state changes.</summary>
    public event Action OnActiveStateChanged;

    /// <summary>
    /// Updates the active state based on whether this emitter's tile is illuminated.
    /// Called by LightBeamSystem during recalculation.
    /// </summary>
    public void UpdateActiveState(bool isTileIlluminatedByLightSource)
    {
        if (IsActive == isTileIlluminatedByLightSource)
            return;

        IsActive = isTileIlluminatedByLightSource;
        OnActiveStateChanged?.Invoke();
    }
}
