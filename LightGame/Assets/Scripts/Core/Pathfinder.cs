using System;
using System.Collections.Generic;
using UnityEngine;

public static class Pathfinder
{
    private static readonly Vector2Int[] Directions =
    {
        GridDirections.Up,
        GridDirections.Down,
        GridDirections.Left,
        GridDirections.Right
    };

    /// <summary>
    /// Finds the shortest path from start to end using A* with Manhattan distance heuristic.
    /// Returns an empty list if no path exists.
    /// The returned path includes both start and end positions.
    /// </summary>
    public static List<Vector2Int> FindPath(
        Vector2Int start,
        Vector2Int end,
        Func<Vector2Int, bool> isWalkable)
    {
        if (start == end)
            return new List<Vector2Int> { start };

        if (!isWalkable(start) || !isWalkable(end))
            return new List<Vector2Int>();

        // Use a List with linear scan for min extraction.
        // Vector2Int doesn't implement IComparable, so SortedSet won't work in Mono.
        var openList = new List<(int fScore, int insertOrder, Vector2Int pos)>();
        var gScore = new Dictionary<Vector2Int, int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var closed = new HashSet<Vector2Int>();
        int insertCounter = 0;

        gScore[start] = 0;
        openList.Add((ManhattanDistance(start, end), insertCounter++, start));

        while (openList.Count > 0)
        {
            int bestIdx = 0;
            for (int i = 1; i < openList.Count; i++)
            {
                var c = openList[i];
                var b = openList[bestIdx];
                if (c.fScore < b.fScore ||
                    (c.fScore == b.fScore && c.insertOrder < b.insertOrder))
                    bestIdx = i;
            }

            var current = openList[bestIdx];
            openList[bestIdx] = openList[openList.Count - 1];
            openList.RemoveAt(openList.Count - 1);

            var currentPos = current.pos;

            if (currentPos == end)
                return ReconstructPath(cameFrom, end);

            if (!closed.Add(currentPos))
                continue;

            int currentG = gScore[currentPos];

            for (int i = 0; i < Directions.Length; i++)
            {
                var neighbor = currentPos + Directions[i];

                if (!isWalkable(neighbor) || closed.Contains(neighbor))
                    continue;

                int tentativeG = currentG + 1;

                if (!gScore.TryGetValue(neighbor, out int existingG) || tentativeG < existingG)
                {
                    gScore[neighbor] = tentativeG;
                    cameFrom[neighbor] = currentPos;
                    int f = tentativeG + ManhattanDistance(neighbor, end);
                    openList.Add((f, insertCounter++, neighbor));
                }
            }
        }

        return new List<Vector2Int>();
    }

    private static int ManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private static List<Vector2Int> ReconstructPath(
        Dictionary<Vector2Int, Vector2Int> cameFrom,
        Vector2Int current)
    {
        var path = new List<Vector2Int> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }
        path.Reverse();
        return path;
    }
}
