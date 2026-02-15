using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure logic for tracing light beams through a grid.
/// Handles beam emitter activation, beam propagation, and mirror reflections.
/// No MonoBehaviour dependency.
/// </summary>
public static class BeamTracer
{
    public struct BeamEmitterInput
    {
        public Vector2Int Position;
        public Vector2Int Direction;
    }

    public struct MirrorInput
    {
        public Vector2Int Position;
        public int RotationIndex;
    }

    public struct TraceResult
    {
        public HashSet<Vector2Int> IlluminatedTiles;
        public List<BeamSegment> Segments;
    }

    /// <summary>
    /// Traces all beams from active beam emitters through the grid.
    /// 
    /// A beam emitter is active only if its tile is in the illuminatedByLightSource set.
    /// Each active emitter shoots a beam in its configured direction.
    /// Beams propagate tile-by-tile, stopping at walls, reflecting at mirrors.
    /// Reflections are capped at maxReflections to prevent infinite loops.
    /// </summary>
    public static TraceResult TraceBeams(
        BeamEmitterInput[] beamEmitters,
        MirrorInput[] mirrors,
        Func<Vector2Int, bool> isWall,
        HashSet<Vector2Int> illuminatedByLightSource,
        int maxReflections = 20)
    {
        var result = new TraceResult
        {
            IlluminatedTiles = new HashSet<Vector2Int>(),
            Segments = new List<BeamSegment>()
        };

        if (beamEmitters == null) return result;

        // Build mirror lookup by position for O(1) access
        var mirrorMap = new Dictionary<Vector2Int, MirrorInput>();
        if (mirrors != null)
        {
            foreach (var m in mirrors)
                mirrorMap[m.Position] = m;
        }

        // Process each beam emitter
        foreach (var emitter in beamEmitters)
        {
            // Only active emitters (illuminated by a light source) produce beams
            if (!illuminatedByLightSource.Contains(emitter.Position))
                continue;

            TraceSingleBeam(
                emitter.Position,
                emitter.Direction,
                isWall,
                mirrorMap,
                maxReflections,
                result);
        }

        return result;
    }

    /// <summary>
    /// Traces a single beam from a starting position in a given direction.
    /// The beam propagates tile-by-tile, reflecting off mirrors and stopping at walls.
    /// </summary>
    private static void TraceSingleBeam(
        Vector2Int startPos,
        Vector2Int direction,
        Func<Vector2Int, bool> isWall,
        Dictionary<Vector2Int, MirrorInput> mirrorMap,
        int maxReflections,
        TraceResult result)
    {
        var currentPos = startPos;
        var currentDir = direction;
        int reflections = 0;

        while (reflections <= maxReflections)
        {
            var segmentStart = currentPos;
            var nextPos = currentPos + currentDir;

            // Walk tile-by-tile in the current direction
            while (true)
            {
                // Hit a wall — end this segment
                if (isWall(nextPos))
                {
                    result.Segments.Add(new BeamSegment(
                        new Vector2(segmentStart.x, segmentStart.y),
                        new Vector2(currentPos.x, currentPos.y),
                        currentDir));
                    return; // Beam terminates at wall
                }

                // Hit a mirror — end segment, reflect
                if (mirrorMap.TryGetValue(nextPos, out var mirror))
                {
                    // Add segment up to the mirror tile
                    result.Segments.Add(new BeamSegment(
                        new Vector2(segmentStart.x, segmentStart.y),
                        new Vector2(nextPos.x, nextPos.y),
                        currentDir));

                    // Illuminate the mirror tile
                    result.IlluminatedTiles.Add(nextPos);

                    // Calculate reflected direction
                    var reflected = ReflectionTable.GetReflectedDirection(currentDir, mirror.RotationIndex);
                    if (reflected == Vector2Int.zero)
                        return; // Beam absorbed by mirror

                    // Continue from mirror position in reflected direction
                    currentPos = nextPos;
                    currentDir = reflected;
                    reflections++;
                    break; // Break inner loop to start new segment
                }

                // Floor tile — illuminate it and continue
                result.IlluminatedTiles.Add(nextPos);
                currentPos = nextPos;
                nextPos = currentPos + currentDir;
            }
        }
    }
}
