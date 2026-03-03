using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure logic for computing illumination boundary edges.
/// No MonoBehaviour dependency — fully testable in EditMode.
/// </summary>
public static class IlluminationBorderLogic
{
    // Cardinal directions and their corresponding edge corners.
    // For tile (x,y) with corners at (x,y), (x+1,y), (x+1,y+1), (x,y+1):
    //   Right  neighbor (x+1,y) → right edge:  (x+1,y) to (x+1,y+1)
    //   Up     neighbor (x,y+1) → top edge:    (x,y+1) to (x+1,y+1)
    //   Left   neighbor (x-1,y) → left edge:   (x,y)   to (x,y+1)
    //   Down   neighbor (x,y-1) → bottom edge: (x,y)   to (x+1,y)

    private static readonly Vector2Int[] Neighbors =
    {
        new Vector2Int(1, 0),   // right
        new Vector2Int(0, 1),   // up
        new Vector2Int(-1, 0),  // left
        new Vector2Int(0, -1)   // down
    };

    /// <summary>
    /// Returns all border edges for the given illuminated tile set.
    /// A border edge exists on any cardinal side of an illuminated tile
    /// where the neighbor is not in the illuminated set or is a wall.
    /// </summary>
    public static List<BorderEdge> ComputeBorderEdges(
        HashSet<Vector2Int> illuminatedTiles,
        Func<Vector2Int, bool> isWall)
    {
        if (illuminatedTiles == null || illuminatedTiles.Count == 0)
            return new List<BorderEdge>();

        if (isWall == null)
            isWall = _ => false;

        var edges = new List<BorderEdge>();

        foreach (var tile in illuminatedTiles)
        {
            int x = tile.x;
            int y = tile.y;

            for (int i = 0; i < 4; i++)
            {
                var neighbor = tile + Neighbors[i];

                if (!illuminatedTiles.Contains(neighbor) || isWall(neighbor))
                {
                    edges.Add(GetEdge(x, y, i));
                }
            }
        }

        return edges;
    }

    private static BorderEdge GetEdge(int x, int y, int dirIndex)
    {
        switch (dirIndex)
        {
            case 0: // right
                return new BorderEdge(new Vector2(x + 1, y), new Vector2(x + 1, y + 1));
            case 1: // up
                return new BorderEdge(new Vector2(x, y + 1), new Vector2(x + 1, y + 1));
            case 2: // left
                return new BorderEdge(new Vector2(x, y), new Vector2(x, y + 1));
            case 3: // down
                return new BorderEdge(new Vector2(x, y), new Vector2(x + 1, y));
            default:
                throw new ArgumentOutOfRangeException(nameof(dirIndex));
        }
    }
}
