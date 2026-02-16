using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure logic for checking puzzle completion and remaining objective count.
/// </summary>
public static class PuzzleChecker
{
    /// <summary>
    /// Returns true if all objective positions are in the illuminated set.
    /// </summary>
    public static bool IsLevelComplete(Vector2Int[] objectives, HashSet<Vector2Int> illuminatedTiles)
    {
        for (int i = 0; i < objectives.Length; i++)
        {
            if (!illuminatedTiles.Contains(objectives[i]))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Returns the number of objectives not yet illuminated.
    /// </summary>
    public static int GetRemainingCount(Vector2Int[] objectives, HashSet<Vector2Int> illuminatedTiles)
    {
        int remaining = 0;
        for (int i = 0; i < objectives.Length; i++)
        {
            if (!illuminatedTiles.Contains(objectives[i]))
                remaining++;
        }
        return remaining;
    }
}
