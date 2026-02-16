using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using UnityEngine;

// Feature: light-puzzle-game, Property 3: Fog-illumination invariant
// Validates: Requirements 2.3, 2.4, 3.4, 5.1, 5.5

public static class FogTestArbitrary
{
    public struct FogTestCase
    {
        public int Width;
        public int Height;
        public bool[,] Walls;
        public Vector2Int LightPos;
        public int Radius;
        public BeamTracer.BeamEmitterInput[] Emitters;
        public BeamTracer.MirrorInput[] Mirrors;

        public override string ToString() =>
            $"Grid({Width}x{Height}) Light={LightPos} R={Radius} Emitters={Emitters.Length} Mirrors={Mirrors.Length}";
    }

    public static Arbitrary<FogTestCase> Generate()
    {
        var gen = from width in Gen.Choose(5, 10)
                  from height in Gen.Choose(5, 10)
                  from radius in Gen.Choose(1, 4)
                  from seed in Arb.Generate<int>()
                  select BuildTestCase(width, height, radius, seed);
        return Arb.From(gen);
    }

    private static FogTestCase BuildTestCase(int width, int height, int radius, int seed)
    {
        var rng = new System.Random(seed);
        var walls = new bool[width, height];
        var occupied = new HashSet<Vector2Int>();

        // Border walls
        for (int x = 0; x < width; x++) { walls[x, 0] = true; walls[x, height - 1] = true; }
        for (int y = 0; y < height; y++) { walls[0, y] = true; walls[width - 1, y] = true; }

        // Scatter interior walls
        for (int x = 1; x < width - 1; x++)
            for (int y = 1; y < height - 1; y++)
                walls[x, y] = rng.NextDouble() < 0.15;

        // Pick light source position on a floor tile
        var lightPos = PickFloor(width, height, walls, occupied, rng);
        occupied.Add(lightPos);

        // Optionally add a beam emitter
        var emitters = new List<BeamTracer.BeamEmitterInput>();
        if (rng.NextDouble() < 0.6)
        {
            var ePos = PickFloor(width, height, walls, occupied, rng);
            occupied.Add(ePos);
            emitters.Add(new BeamTracer.BeamEmitterInput
            {
                Position = ePos,
                Direction = GridDirections.All[rng.Next(8)]
            });
        }

        // Optionally add a mirror
        var mirrors = new List<BeamTracer.MirrorInput>();
        if (rng.NextDouble() < 0.4)
        {
            var mPos = PickFloor(width, height, walls, occupied, rng);
            occupied.Add(mPos);
            mirrors.Add(new BeamTracer.MirrorInput
            {
                Position = mPos,
                RotationIndex = rng.Next(8)
            });
        }

        return new FogTestCase
        {
            Width = width,
            Height = height,
            Walls = walls,
            LightPos = lightPos,
            Radius = radius,
            Emitters = emitters.ToArray(),
            Mirrors = mirrors.ToArray()
        };
    }

    private static Vector2Int PickFloor(int w, int h, bool[,] walls, HashSet<Vector2Int> occupied, System.Random rng)
    {
        for (int attempt = 0; attempt < 200; attempt++)
        {
            var pos = new Vector2Int(rng.Next(1, w - 1), rng.Next(1, h - 1));
            if (!walls[pos.x, pos.y] && !occupied.Contains(pos))
                return pos;
        }
        // Fallback: force a floor tile
        var fallback = new Vector2Int(1, 1);
        walls[1, 1] = false;
        return fallback;
    }
}

[TestFixture]
public class FogOfWarTests
{
    // Property 3: After transitions complete, visible tiles == illuminated tiles
    [Test]
    public void VisibleTilesEqualsIlluminatedTiles_AfterTransitionsComplete()
    {
        var arb = FogTestArbitrary.Generate();

        Prop.ForAll(arb, tc =>
        {
            var w = tc.Width;
            var h = tc.Height;
            var walls = tc.Walls;
            System.Func<Vector2Int, bool> isWall = pos =>
                pos.x < 0 || pos.x >= w || pos.y < 0 || pos.y >= h || walls[pos.x, pos.y];

            // Compute light source illumination
            var lightSourceIlluminated = LightSourceLogic.GetRadiusIlluminatedTiles(
                tc.LightPos, tc.Radius, isWall);

            // Trace beams
            var traceResult = BeamTracer.TraceBeams(
                tc.Emitters, tc.Mirrors, isWall, lightSourceIlluminated);

            // Union of all illuminated tiles
            var allIlluminated = new HashSet<Vector2Int>(lightSourceIlluminated);
            allIlluminated.UnionWith(traceResult.IlluminatedTiles);

            // Run fog logic
            var fog = new FogOfWarLogic(w, h);
            fog.UpdateIllumination(allIlluminated);
            fog.CompleteTransitions();

            var visibleTiles = fog.GetVisibleTiles();

            // Invariant: visible set == illuminated set
            Assert.AreEqual(allIlluminated.Count, visibleTiles.Count,
                $"Visible count {visibleTiles.Count} != illuminated count {allIlluminated.Count}");

            foreach (var tile in allIlluminated)
            {
                Assert.IsTrue(visibleTiles.Contains(tile),
                    $"Illuminated tile {tile} should be visible but is not");
            }

            foreach (var tile in visibleTiles)
            {
                Assert.IsTrue(allIlluminated.Contains(tile),
                    $"Visible tile {tile} should be illuminated but is not");
            }
        }).QuickCheckThrowOnFailure();
    }

    // Property 3b: No tile is visible without illumination
    [Test]
    public void NoTileVisibleWithoutIllumination()
    {
        var arb = FogTestArbitrary.Generate();

        Prop.ForAll(arb, tc =>
        {
            // Empty illumination set
            var fog = new FogOfWarLogic(tc.Width, tc.Height);
            fog.UpdateIllumination(new HashSet<Vector2Int>());
            fog.CompleteTransitions();

            var visibleTiles = fog.GetVisibleTiles();
            Assert.AreEqual(0, visibleTiles.Count,
                "No tiles should be visible when nothing is illuminated");
        }).QuickCheckThrowOnFailure();
    }
}
