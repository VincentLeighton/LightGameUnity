using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure logic for fog-of-war state tracking.
/// Mirrors the alpha-based visibility logic of FogOfWarSystem
/// without Unity runtime dependencies (no RenderTexture, no Update loop).
/// Testable in EditMode.
/// </summary>
public class FogOfWarLogic
{
    public int GridWidth { get; private set; }
    public int GridHeight { get; private set; }

    private float[,] _currentAlpha;
    private float[,] _targetAlpha;

    public FogOfWarLogic(int gridWidth, int gridHeight)
    {
        GridWidth = gridWidth;
        GridHeight = gridHeight;
        _currentAlpha = new float[gridWidth, gridHeight];
        _targetAlpha = new float[gridWidth, gridHeight];

        // Start fully fogged
        for (int x = 0; x < gridWidth; x++)
            for (int y = 0; y < gridHeight; y++)
            {
                _currentAlpha[x, y] = 1f;
                _targetAlpha[x, y] = 1f;
            }
    }

    /// <summary>
    /// Sets target alpha based on illuminated tiles.
    /// Illuminated tiles target 0 (visible), others target 1 (fogged).
    /// </summary>
    public void UpdateIllumination(HashSet<Vector2Int> illuminatedTiles)
    {
        for (int x = 0; x < GridWidth; x++)
            for (int y = 0; y < GridHeight; y++)
            {
                _targetAlpha[x, y] = illuminatedTiles.Contains(new Vector2Int(x, y)) ? 0f : 1f;
            }
    }

    /// <summary>
    /// Completes all pending transitions instantly (snaps current to target).
    /// Simulates waiting for all fade animations to finish.
    /// </summary>
    public void CompleteTransitions()
    {
        for (int x = 0; x < GridWidth; x++)
            for (int y = 0; y < GridHeight; y++)
                _currentAlpha[x, y] = _targetAlpha[x, y];
    }

    /// <summary>
    /// Returns the set of tiles currently visible (alpha below 0.5).
    /// </summary>
    public HashSet<Vector2Int> GetVisibleTiles()
    {
        var visible = new HashSet<Vector2Int>();
        for (int x = 0; x < GridWidth; x++)
            for (int y = 0; y < GridHeight; y++)
            {
                if (_currentAlpha[x, y] < 0.5f)
                    visible.Add(new Vector2Int(x, y));
            }
        return visible;
    }
}
