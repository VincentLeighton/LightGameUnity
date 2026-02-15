using System;
using UnityEngine;

/// <summary>
/// Precomputed lookup table for mirror reflections.
/// Maps (incomingDirectionIndex, mirrorRotationIndex) to outgoingDirectionIndex.
/// 
/// Direction indices 0-7 map to: Right, UpRight, Up, UpLeft, Left, DownLeft, Down, DownRight
/// (matching GridDirections.All ordering)
/// 
/// Mirror rotation indices 0-7 map to surface angles: 0°, 45°, 90°, 135°, 180°, 225°, 270°, 315°
/// The mirror's surface normal is perpendicular to its surface (surface angle + 90°).
/// </summary>
public static class ReflectionTable
{
    // reflectionTable[incomingDirectionIndex][mirrorRotationIndex] = outgoingDirectionIndex
    // -1 means the beam hits the mirror edge-on (parallel to surface normal) and bounces back
    private static readonly int[,] Table;

    static ReflectionTable()
    {
        Table = new int[8, 8];
        BuildTable();
    }

    /// <summary>
    /// Gets the reflected direction index for a beam arriving from incomingDirection
    /// hitting a mirror at the given rotation index.
    /// Returns -1 if the beam is absorbed (hits edge-on, i.e., incoming == normal direction).
    /// </summary>
    public static int GetReflectedDirectionIndex(int incomingDirectionIndex, int mirrorRotationIndex)
    {
        return Table[incomingDirectionIndex, mirrorRotationIndex];
    }

    /// <summary>
    /// Gets the reflected direction vector for a beam arriving from incomingDirection
    /// hitting a mirror at the given rotation index.
    /// Returns Vector2Int.zero if the beam is absorbed.
    /// </summary>
    public static Vector2Int GetReflectedDirection(Vector2Int incomingDirection, int mirrorRotationIndex)
    {
        int inIdx = DirectionToIndex(incomingDirection);
        if (inIdx < 0) return Vector2Int.zero;

        int outIdx = Table[inIdx, mirrorRotationIndex];
        if (outIdx < 0) return Vector2Int.zero;

        return GridDirections.All[outIdx];
    }

    /// <summary>
    /// Converts a direction vector to its index in GridDirections.All.
    /// Returns -1 if not a valid direction.
    /// </summary>
    public static int DirectionToIndex(Vector2Int dir)
    {
        for (int i = 0; i < GridDirections.All.Length; i++)
        {
            if (GridDirections.All[i] == dir)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Builds the 8x8 reflection lookup table using the law of reflection.
    /// 
    /// Mirror at rotation index r has its surface oriented at angle r*45°.
    /// The surface normal is at angle (r*45° + 90°).
    /// 
    /// Reflection formula: reflected = incoming - 2 * dot(incoming, normal) * normal
    /// Since all directions are unit-length on the grid, we work in angle space
    /// and snap results to the nearest of the 8 grid directions.
    /// </summary>
    private static void BuildTable()
    {
        // Direction angles in degrees: index i -> angle i*45°
        // 0=Right(0°), 1=UpRight(45°), 2=Up(90°), 3=UpLeft(135°),
        // 4=Left(180°), 5=DownLeft(225°), 6=Down(270°), 7=DownRight(315°)

        for (int inDir = 0; inDir < 8; inDir++)
        {
            double inAngle = inDir * 45.0 * Math.PI / 180.0;
            Vector2 inVec = new Vector2((float)Math.Cos(inAngle), (float)Math.Sin(inAngle));

            for (int mirrorRot = 0; mirrorRot < 8; mirrorRot++)
            {
                // Mirror surface normal is at (mirrorRot * 45 + 90) degrees
                double normalAngle = (mirrorRot * 45.0 + 90.0) * Math.PI / 180.0;
                Vector2 normal = new Vector2((float)Math.Cos(normalAngle), (float)Math.Sin(normalAngle));

                // Reflection: r = d - 2(d·n)n
                float dot = Vector2.Dot(inVec, normal);
                Vector2 reflected = inVec - 2 * dot * normal;

                // Convert reflected vector back to angle and snap to nearest 45° increment
                double refAngle = Math.Atan2(reflected.y, reflected.x) * 180.0 / Math.PI;
                if (refAngle < 0) refAngle += 360.0;

                int outDir = (int)Math.Round(refAngle / 45.0) % 8;
                Table[inDir, mirrorRot] = outDir;
            }
        }
    }
}
