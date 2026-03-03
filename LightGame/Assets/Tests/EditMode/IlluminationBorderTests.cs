using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using UnityEngine;

// ============================================================
// Feature: illumination-border
// Property 1: Border edge correctness and completeness
// Validates: Requirements 1.1, 1.2, 1.3
// ============================================================

[TestFixture]
public class BorderEdgeCorrectnessPropertyTests
{
    public struct BorderTestCase
    {
        public HashSet<Vector2Int> IlluminatedTiles;
        public HashSet<Vector2Int> WallTiles;

        public Func<Vector2Int, bool> GetIsWall()
        {
            var walls = WallTiles;
            return pos => walls.Contains(pos);
        }

        public override string ToString() =>
            $"Illuminated={IlluminatedTiles.Count} Walls={WallTiles.Count}";
    }

    private static Arbitrary<BorderTestCase> BorderTestArbitrary()
    {
        var gen = from tileCount in Gen.Choose(0, 15)
                  from wallCount in Gen.Choose(0, 8)
                  from seed in Arb.Generate<int>()
                  select BuildTestCase(tileCount, wallCount, seed);
        return Arb.From(gen);
    }

    private static BorderTestCase BuildTestCase(int tileCount, int wallCount, int seed)
    {
        var rng = new System.Random(seed);
        var illuminated = new HashSet<Vector2Int>();
        var walls = new HashSet<Vector2Int>();

        for (int i = 0; i < tileCount; i++)
            illuminated.Add(new Vector2Int(rng.Next(-5, 6), rng.Next(-5, 6)));

        for (int i = 0; i < wallCount; i++)
        {
            var wall = new Vector2Int(rng.Next(-5, 6), rng.Next(-5, 6));
            if (!illuminated.Contains(wall))
                walls.Add(wall);
        }

        return new BorderTestCase
        {
            IlluminatedTiles = illuminated,
            WallTiles = walls
        };
    }

    /// <summary>
    /// **Property 1: Border edge correctness and completeness**
    /// **Validates: Requirements 1.1, 1.2, 1.3**
    /// An edge appears in the output iff it separates an illuminated tile
    /// from a non-illuminated or wall tile.
    /// </summary>
    [Test]
    public void BorderEdges_ExactlyMatchBoundary()
    {
        var arb = BorderTestArbitrary();

        Prop.ForAll(arb, tc =>
        {
            var isWall = tc.GetIsWall();
            var edges = IlluminationBorderLogic.ComputeBorderEdges(tc.IlluminatedTiles, isWall);

            // Build expected set of edges
            var expected = new HashSet<BorderEdge>();
            var dirs = new[]
            {
                (new Vector2Int(1, 0),  new Func<int,int,BorderEdge>((x,y) => new BorderEdge(new Vector2(x+1,y), new Vector2(x+1,y+1)))),
                (new Vector2Int(0, 1),  new Func<int,int,BorderEdge>((x,y) => new BorderEdge(new Vector2(x,y+1), new Vector2(x+1,y+1)))),
                (new Vector2Int(-1, 0), new Func<int,int,BorderEdge>((x,y) => new BorderEdge(new Vector2(x,y),   new Vector2(x,y+1)))),
                (new Vector2Int(0, -1), new Func<int,int,BorderEdge>((x,y) => new BorderEdge(new Vector2(x,y),   new Vector2(x+1,y))))
            };

            foreach (var tile in tc.IlluminatedTiles)
            {
                foreach (var (dir, makeEdge) in dirs)
                {
                    var neighbor = tile + dir;
                    if (!tc.IlluminatedTiles.Contains(neighbor) || isWall(neighbor))
                        expected.Add(makeEdge(tile.x, tile.y));
                }
            }

            // Verify counts match
            Assert.AreEqual(expected.Count, edges.Count,
                $"Expected {expected.Count} edges but got {edges.Count}. " + tc);

            // Verify every expected edge is present
            var edgeSet = new HashSet<BorderEdge>(edges);
            foreach (var e in expected)
            {
                Assert.IsTrue(edgeSet.Contains(e),
                    $"Missing expected edge {e}. " + tc);
            }
        }).QuickCheckThrowOnFailure();
    }
}

// ============================================================
// Feature: illumination-border
// Property 2: Border edge coordinate validity
// Validates: Requirements 1.4
// ============================================================

[TestFixture]
public class BorderEdgeCoordinateValidityPropertyTests
{
    /// <summary>
    /// **Property 2: Border edge coordinate validity**
    /// **Validates: Requirements 1.4**
    /// Every border edge has integer coordinates, is axis-aligned, and has length 1.
    /// </summary>
    [Test]
    public void BorderEdges_HaveValidCoordinates()
    {
        var gen = from count in Gen.Choose(1, 20)
                  from seed in Arb.Generate<int>()
                  select BuildTileSet(count, seed);
        var arb = Arb.From(gen);

        Prop.ForAll(arb, tiles =>
        {
            var edges = IlluminationBorderLogic.ComputeBorderEdges(tiles, _ => false);

            foreach (var edge in edges)
            {
                // Integer coordinates
                Assert.AreEqual(edge.Start.x, Mathf.Round(edge.Start.x), 0.001f,
                    $"Edge {edge} start.x is not integer");
                Assert.AreEqual(edge.Start.y, Mathf.Round(edge.Start.y), 0.001f,
                    $"Edge {edge} start.y is not integer");
                Assert.AreEqual(edge.End.x, Mathf.Round(edge.End.x), 0.001f,
                    $"Edge {edge} end.x is not integer");
                Assert.AreEqual(edge.End.y, Mathf.Round(edge.End.y), 0.001f,
                    $"Edge {edge} end.y is not integer");

                // Axis-aligned (one coordinate is shared)
                bool horizontal = Mathf.Approximately(edge.Start.y, edge.End.y);
                bool vertical = Mathf.Approximately(edge.Start.x, edge.End.x);
                Assert.IsTrue(horizontal || vertical,
                    $"Edge {edge} is not axis-aligned");

                // Length exactly 1
                float length = Vector2.Distance(edge.Start, edge.End);
                Assert.AreEqual(1f, length, 0.001f,
                    $"Edge {edge} has length {length}, expected 1");
            }
        }).QuickCheckThrowOnFailure();
    }

    private static HashSet<Vector2Int> BuildTileSet(int count, int seed)
    {
        var rng = new System.Random(seed);
        var tiles = new HashSet<Vector2Int>();
        for (int i = 0; i < count; i++)
            tiles.Add(new Vector2Int(rng.Next(-5, 6), rng.Next(-5, 6)));
        return tiles;
    }
}

// ============================================================
// Feature: illumination-border
// Unit Tests for IlluminationBorderLogic
// Validates: Requirements 1.1, 1.2, 1.3
// ============================================================

[TestFixture]
public class IlluminationBorderLogicUnitTests
{
    [Test]
    public void SingleTile_Produces4Edges()
    {
        var tiles = new HashSet<Vector2Int> { new Vector2Int(0, 0) };
        var edges = IlluminationBorderLogic.ComputeBorderEdges(tiles, _ => false);
        Assert.AreEqual(4, edges.Count);
    }

    [Test]
    public void TwoAdjacentTiles_Produce6Edges()
    {
        var tiles = new HashSet<Vector2Int>
        {
            new Vector2Int(0, 0),
            new Vector2Int(1, 0)
        };
        var edges = IlluminationBorderLogic.ComputeBorderEdges(tiles, _ => false);
        Assert.AreEqual(6, edges.Count);
    }

    [Test]
    public void EmptySet_ProducesZeroEdges()
    {
        var tiles = new HashSet<Vector2Int>();
        var edges = IlluminationBorderLogic.ComputeBorderEdges(tiles, _ => false);
        Assert.AreEqual(0, edges.Count);
    }
}


// ============================================================
// Feature: illumination-border
// Unit Test for VisualConfig sort order with BorderSortOrder
// Validates: Requirements 2.4
// ============================================================

[TestFixture]
public class VisualConfigBorderSortOrderTests
{
    [Test]
    public void SortOrder_FogBeforeBorderBeforeElementBeforePlayer()
    {
        Assert.Less(VisualConfig.FogSortOrder, VisualConfig.BorderSortOrder,
            "FogSortOrder must be less than BorderSortOrder");
        Assert.Less(VisualConfig.BorderSortOrder, VisualConfig.ElementSortOrder,
            "BorderSortOrder must be less than ElementSortOrder");
        Assert.Less(VisualConfig.ElementSortOrder, VisualConfig.PlayerSortOrder,
            "ElementSortOrder must be less than PlayerSortOrder");
    }
}
