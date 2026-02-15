using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using UnityEngine;

// Feature: light-puzzle-game, Property 1: Pathfinding correctness
// Validates: Requirements 1.1, 1.2

/// <summary>
/// Generates random grids with guaranteed connected floor regions
/// and pairs of floor positions for pathfinding tests.
/// </summary>
public static class GridArbitrary
{
    public struct GridTestCase
    {
        public int Width;
        public int Height;
        public bool[,] Walkable; // true = floor, false = wall
        public Vector2Int Start;
        public Vector2Int End;

        public Func<Vector2Int, bool> GetIsWalkable()
        {
            var w = Width;
            var h = Height;
            var grid = Walkable;
            return pos => pos.x >= 0 && pos.x < w &&
                          pos.y >= 0 && pos.y < h &&
                          grid[pos.x, pos.y];
        }

        public override string ToString() =>
            $"Grid({Width}x{Height}) Start={Start} End={End}";
    }

    /// <summary>
    /// Generates a grid where start and end are guaranteed to be connected
    /// by carving a random walk between them.
    /// </summary>
    public static Arbitrary<GridTestCase> ConnectedPair()
    {
        var gen = from width in Gen.Choose(5, 15)
                  from height in Gen.Choose(5, 15)
                  from seed in Arb.Generate<int>()
                  select BuildConnectedGrid(width, height, seed);
        return Arb.From(gen);
    }

    /// <summary>
    /// Generates a grid where start and end are on disconnected floor islands.
    /// </summary>
    public static Arbitrary<GridTestCase> DisconnectedPair()
    {
        var gen = from width in Gen.Choose(7, 15)
                  from height in Gen.Choose(7, 15)
                  from seed in Arb.Generate<int>()
                  select BuildDisconnectedGrid(width, height, seed);
        return Arb.From(gen);
    }

    private static GridTestCase BuildConnectedGrid(int width, int height, int seed)
    {
        var rng = new System.Random(seed);
        var walkable = new bool[width, height];

        // Start with all walls, then carve floors
        // Pick start and end in interior
        var start = new Vector2Int(rng.Next(1, width - 1), rng.Next(1, height - 1));
        var end = new Vector2Int(rng.Next(1, width - 1), rng.Next(1, height - 1));

        // Ensure start != end
        while (end == start)
            end = new Vector2Int(rng.Next(1, width - 1), rng.Next(1, height - 1));

        walkable[start.x, start.y] = true;
        walkable[end.x, end.y] = true;

        // Carve a random walk from start to end to guarantee connectivity
        var current = start;
        while (current != end)
        {
            int dx = end.x - current.x;
            int dy = end.y - current.y;

            // Bias toward the goal but add randomness
            if (rng.NextDouble() < 0.7)
            {
                if (Mathf.Abs(dx) >= Mathf.Abs(dy))
                    current = new Vector2Int(current.x + Math.Sign(dx), current.y);
                else
                    current = new Vector2Int(current.x, current.y + Math.Sign(dy));
            }
            else
            {
                // Random cardinal step staying in bounds
                var dirs = new[] {
                    new Vector2Int(1, 0), new Vector2Int(-1, 0),
                    new Vector2Int(0, 1), new Vector2Int(0, -1)
                };
                var d = dirs[rng.Next(4)];
                var next = current + d;
                if (next.x > 0 && next.x < width - 1 && next.y > 0 && next.y < height - 1)
                    current = next;
            }
            walkable[current.x, current.y] = true;
        }

        // Scatter some extra floor tiles for variety
        int extraFloors = (width * height) / 4;
        for (int i = 0; i < extraFloors; i++)
        {
            int x = rng.Next(1, width - 1);
            int y = rng.Next(1, height - 1);
            walkable[x, y] = true;
        }

        return new GridTestCase
        {
            Width = width,
            Height = height,
            Walkable = walkable,
            Start = start,
            End = end
        };
    }

    private static GridTestCase BuildDisconnectedGrid(int width, int height, int seed)
    {
        var rng = new System.Random(seed);
        var walkable = new bool[width, height];

        // Create two isolated floor islands separated by a wall column
        int wallCol = width / 2;

        // Left island
        var start = new Vector2Int(rng.Next(1, wallCol), rng.Next(1, height - 1));
        walkable[start.x, start.y] = true;
        for (int i = 0; i < 3; i++)
        {
            int x = rng.Next(1, wallCol);
            int y = rng.Next(1, height - 1);
            walkable[x, y] = true;
        }

        // Right island
        var end = new Vector2Int(rng.Next(wallCol + 1, width - 1), rng.Next(1, height - 1));
        walkable[end.x, end.y] = true;
        for (int i = 0; i < 3; i++)
        {
            int x = rng.Next(wallCol + 1, width - 1);
            int y = rng.Next(1, height - 1);
            walkable[x, y] = true;
        }

        // Ensure the wall column is solid (all false, which is default)
        // Already all false by default

        return new GridTestCase
        {
            Width = width,
            Height = height,
            Walkable = walkable,
            Start = start,
            End = end
        };
    }
}

[TestFixture]
public class PathfinderTests
{
    // Property 1a: Path contains only walkable tiles
    [Test]
    public void Path_ContainsOnlyWalkableTiles()
    {
        var arb = GridArbitrary.ConnectedPair();

        Prop.ForAll(arb, tc =>
        {
            var isWalkable = tc.GetIsWalkable();
            var path = Pathfinder.FindPath(tc.Start, tc.End, isWalkable);

            Assert.IsTrue(path.Count > 0, "Path should exist for connected grid");

            foreach (var tile in path)
            {
                Assert.IsTrue(isWalkable(tile),
                    $"Path contains non-walkable tile {tile}");
            }
        }).QuickCheckThrowOnFailure();
    }

    // Property 1b: Consecutive tiles in path are adjacent (4-directional)
    [Test]
    public void Path_ConsecutiveTilesAreAdjacent()
    {
        var arb = GridArbitrary.ConnectedPair();

        Prop.ForAll(arb, tc =>
        {
            var isWalkable = tc.GetIsWalkable();
            var path = Pathfinder.FindPath(tc.Start, tc.End, isWalkable);

            Assert.IsTrue(path.Count > 0, "Path should exist for connected grid");
            Assert.AreEqual(tc.Start, path[0], "Path should start at start position");
            Assert.AreEqual(tc.End, path[path.Count - 1], "Path should end at end position");

            for (int i = 1; i < path.Count; i++)
            {
                int dist = Mathf.Abs(path[i].x - path[i - 1].x) +
                           Mathf.Abs(path[i].y - path[i - 1].y);
                Assert.AreEqual(1, dist,
                    $"Tiles {path[i - 1]} and {path[i]} are not adjacent (distance={dist})");
            }
        }).QuickCheckThrowOnFailure();
    }

    // Property 1c: Path is shortest (length equals BFS distance)
    [Test]
    public void Path_IsShortestPath()
    {
        var arb = GridArbitrary.ConnectedPair();

        Prop.ForAll(arb, tc =>
        {
            var isWalkable = tc.GetIsWalkable();
            var path = Pathfinder.FindPath(tc.Start, tc.End, isWalkable);
            int bfsDist = BfsDistance(tc.Start, tc.End, isWalkable);

            Assert.IsTrue(path.Count > 0, "Path should exist for connected grid");
            // path.Count includes start, so path length in steps = Count - 1
            Assert.AreEqual(bfsDist, path.Count - 1,
                $"A* path length ({path.Count - 1}) != BFS distance ({bfsDist})");
        }).QuickCheckThrowOnFailure();
    }

    // Property 1d: Disconnected tiles return empty path
    [Test]
    public void Path_ReturnsEmptyForDisconnectedTiles()
    {
        var arb = GridArbitrary.DisconnectedPair();

        Prop.ForAll(arb, tc =>
        {
            var isWalkable = tc.GetIsWalkable();
            var path = Pathfinder.FindPath(tc.Start, tc.End, isWalkable);
            Assert.AreEqual(0, path.Count,
                $"Expected empty path for disconnected grid, got {path.Count} tiles");
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// BFS to compute shortest distance as ground truth for comparison.
    /// Returns -1 if unreachable.
    /// </summary>
    private static int BfsDistance(Vector2Int start, Vector2Int end, Func<Vector2Int, bool> isWalkable)
    {
        if (start == end) return 0;

        var visited = new HashSet<Vector2Int> { start };
        var queue = new Queue<(Vector2Int pos, int dist)>();
        queue.Enqueue((start, 0));

        var dirs = new[] {
            GridDirections.Up, GridDirections.Down,
            GridDirections.Left, GridDirections.Right
        };

        while (queue.Count > 0)
        {
            var (pos, dist) = queue.Dequeue();
            foreach (var d in dirs)
            {
                var next = pos + d;
                if (next == end) return dist + 1;
                if (isWalkable(next) && visited.Add(next))
                    queue.Enqueue((next, dist + 1));
            }
        }

        return -1;
    }
}
