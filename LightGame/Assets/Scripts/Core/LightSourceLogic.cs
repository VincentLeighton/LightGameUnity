using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure logic for light source radius illumination with shadow casting.
/// No MonoBehaviour dependency — fully testable without Unity runtime.
/// </summary>
public static class LightSourceLogic
{
    /// <summary>
    /// Returns the set of floor tiles illuminated by a light source at the given position
    /// with the given radius. Uses Bresenham line-of-sight checks; wall tiles block LOS.
    /// </summary>
    public static HashSet<Vector2Int> GetRadiusIlluminatedTiles(
        Vector2Int position,
        int radius,
        Func<Vector2Int, bool> isWall)
    {
        var illuminated = new HashSet<Vector2Int>();

        // The source tile itself is illuminated if it's not a wall
        if (!isWall(position))
            illuminated.Add(position);

        // Check every tile within the radius bounding box
        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                var target = new Vector2Int(position.x + dx, position.y + dy);

                // Skip if outside circular radius (use squared distance)
                if (dx * dx + dy * dy > radius * radius)
                    continue;

                // Skip wall tiles — they are never in the illuminated set
                if (isWall(target))
                    continue;

                // Check line-of-sight using Bresenham
                if (HasLineOfSight(position, target, isWall))
                    illuminated.Add(target);
            }
        }

        return illuminated;
    }

    /// <summary>
    /// Bresenham line check from source to target.
    /// Returns true if no wall tile blocks the path.
    /// The source and target tiles themselves are not considered blockers.
    /// Diagonal steps check both cardinal neighbors — if both are walls, LOS is blocked.
    /// </summary>
    public static bool HasLineOfSight(Vector2Int source, Vector2Int target, Func<Vector2Int, bool> isWall)
    {
        int x0 = source.x, y0 = source.y;
        int x1 = target.x, y1 = target.y;

        int dx = Math.Abs(x1 - x0);
        int dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            // If we've reached the target, LOS is clear
            if (x0 == x1 && y0 == y1)
                return true;

            int e2 = 2 * err;
            bool stepX = e2 > -dy;
            bool stepY = e2 < dx;

            // Diagonal step: check both cardinal neighbors
            if (stepX && stepY)
            {
                var hNeighbor = new Vector2Int(x0 + sx, y0);
                var vNeighbor = new Vector2Int(x0, y0 + sy);
                if (isWall(hNeighbor) && isWall(vNeighbor))
                    return false;
            }

            if (stepX)
            {
                err -= dy;
                x0 += sx;
            }
            if (stepY)
            {
                err += dx;
                y0 += sy;
            }

            // Check if we've arrived at the target after stepping
            if (x0 == x1 && y0 == y1)
                return true;

            if (isWall(new Vector2Int(x0, y0)))
                return false;
        }
    }

}
