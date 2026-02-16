using System;
using UnityEngine;

/// <summary>
/// A target that must be illuminated to complete the level.
/// Tracks illumination state and renders a subtle faint glow visible even in dark areas.
/// </summary>
public class PuzzleObjective : MonoBehaviour
{
    [Tooltip("Grid position of this objective")]
    public Vector2Int TilePosition;

    /// <summary>Whether this objective is currently illuminated.</summary>
    public bool IsIlluminated { get; private set; }

    /// <summary>Fired when the illumination state changes.</summary>
    public event Action<PuzzleObjective> OnStateChanged;

    [Tooltip("Optional SpriteRenderer for the faint glow indicator")]
    public SpriteRenderer GlowRenderer;

    [Tooltip("Alpha for the faint glow when in a dark area")]
    public float GlowAlpha = 0.15f;

    [Tooltip("Alpha for the glow when illuminated")]
    public float IlluminatedAlpha = 0.8f;

    /// <summary>
    /// Updates the illumination state. Called by LightBeamSystem via OnIlluminationChanged.
    /// </summary>
    public void UpdateIlluminationState(bool illuminated)
    {
        if (IsIlluminated == illuminated)
            return;

        IsIlluminated = illuminated;

        // Update glow visual
        if (GlowRenderer != null)
        {
            var color = GlowRenderer.color;
            color.a = illuminated ? IlluminatedAlpha : GlowAlpha;
            GlowRenderer.color = color;
        }

        OnStateChanged?.Invoke(this);
    }
}
