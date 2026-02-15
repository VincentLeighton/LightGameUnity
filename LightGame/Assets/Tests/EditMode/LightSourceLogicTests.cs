using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using UnityEngine;

// Feature: light-puzzle-game, Property 2: Radius illumination with shadow casting
// Validates: Requirements 2.1, 2.2

public static class LightGridArbitrary
{
    public struct LightTestCase
    {
        public int Width;
        public int Height;
        public bool[,] Walls; // true = wall
        public Vector2Int LightPos;
        public int Radius;

        public Func<Vector2Int, bool> GetIsWall()
        {
            var w = Width;
            var h = Height;
            var walls = Walls;
            return pos => pos.x < 0 || pos.x >= w ||
                          pos.y < 0 || pos.y >= h ||
                          walls[pos.x, pos.y];
        }

        public override string ToString() =>
            $"Grid({Width}x{Height}) Light={LightPos} Radius={Radius}";
    }

    public static Arbitrary<LightTestCase> Generate()
    {
        var gen = from width in Gen.Choose(5, 12)
                  from height in Gen.Choose(5, 12)
                  from radius in Gen.Choose(1, 5)
                  from seed in Arb.Generate<int>()
                  select BuildGrid(width, height, radius, seed);
        return Arb.From(gen);
    }

    private static LightTestCase BuildGrid(int width, int height, int radius, int seed)
    {
        var rng = new System.Random(seed);
        var walls = new bool[width, height];

        // Border is always wall
        for (int x = 0; x < width; x++)
        {
            walls[x, 0] = true;
            walls[x, height - 1] = true;
        }
        for (int y = 0; y < height; y++)
        {
            walls[0, y] = true;
            walls[width - 1, y] = true;
        }

        // Scatter interior walls
        for (int x = 1; x < width - 1; x++)
            for (int y = 1; y < height - 1; y++)
                walls[x, y] = rng.NextDouble() < 0.2;

        // Pick a floor tile for the light source
        var lightPos = new Vector2Int(rng.Next(1, width - 1), rng.Next(1, height - 1));
        walls[lightPos.x, lightPos.y] = false;

        return new LightTestCase
        {
            Width = width,
            Height = height,
            Walls = walls,
            LightPos = lightPos,
            Radius = radius
        };
    }
}


[TestFixture]
public class LightSourceLogicTests
{
    // Property 2a: Floor tiles within radius with clear LOS are illuminated
    [Test]
    public void FloorTilesWithinRadiusWithLOS_AreIlluminated()
    {
        var arb = LightGridArbitrary.Generate();

        Prop.ForAll(arb, tc =>
        {
            var isWall = tc.GetIsWall();
            var illuminated = LightSourceLogic.GetRadiusIlluminatedTiles(tc.LightPos, tc.Radius, isWall);

            // For every floor tile within radius that has LOS, it must be illuminated
            for (int dx = -tc.Radius; dx <= tc.Radius; dx++)
            {
                for (int dy = -tc.Radius; dy <= tc.Radius; dy++)
                {
                    if (dx * dx + dy * dy > tc.Radius * tc.Radius)
                        continue;

                    var tile = new Vector2Int(tc.LightPos.x + dx, tc.LightPos.y + dy);

                    if (isWall(tile))
                        continue;

                    if (LightSourceLogic.HasLineOfSight(tc.LightPos, tile, isWall))
                    {
                        Assert.IsTrue(illuminated.Contains(tile),
                            $"Floor tile {tile} within radius with LOS should be illuminated");
                    }
                }
            }
        }).QuickCheckThrowOnFailure();
    }

    // Property 2b: Wall tiles are never in the illuminated set
    [Test]
    public void WallTiles_AreNeverIlluminated()
    {
        var arb = LightGridArbitrary.Generate();

        Prop.ForAll(arb, tc =>
        {
            var isWall = tc.GetIsWall();
            var illuminated = LightSourceLogic.GetRadiusIlluminatedTiles(tc.LightPos, tc.Radius, isWall);

            foreach (var tile in illuminated)
            {
                Assert.IsFalse(isWall(tile),
                    $"Wall tile {tile} should not be in illuminated set");
            }
        }).QuickCheckThrowOnFailure();
    }

    // Property 2c: Tiles outside the radius are never illuminated
    [Test]
    public void TilesOutsideRadius_AreNeverIlluminated()
    {
        var arb = LightGridArbitrary.Generate();

        Prop.ForAll(arb, tc =>
        {
            var isWall = tc.GetIsWall();
            var illuminated = LightSourceLogic.GetRadiusIlluminatedTiles(tc.LightPos, tc.Radius, isWall);

            foreach (var tile in illuminated)
            {
                int dx = tile.x - tc.LightPos.x;
                int dy = tile.y - tc.LightPos.y;
                Assert.IsTrue(dx * dx + dy * dy <= tc.Radius * tc.Radius,
                    $"Tile {tile} is outside radius {tc.Radius} from {tc.LightPos}");
            }
        }).QuickCheckThrowOnFailure();
    }

    // Property 2d: Tiles blocked by walls are not illuminated
    [Test]
    public void TilesBlockedByWalls_AreNotIlluminated()
    {
        var arb = LightGridArbitrary.Generate();

        Prop.ForAll(arb, tc =>
        {
            var isWall = tc.GetIsWall();
            var illuminated = LightSourceLogic.GetRadiusIlluminatedTiles(tc.LightPos, tc.Radius, isWall);

            foreach (var tile in illuminated)
            {
                // Every illuminated tile must have LOS from the light source
                Assert.IsTrue(LightSourceLogic.HasLineOfSight(tc.LightPos, tile, isWall),
                    $"Illuminated tile {tile} does not have LOS from {tc.LightPos}");
            }
        }).QuickCheckThrowOnFailure();
    }
}
