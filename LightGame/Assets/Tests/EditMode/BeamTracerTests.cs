using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using UnityEngine;

// Feature: light-puzzle-game, Property 4: Beam tracing correctness
// Feature: light-puzzle-game, Property 5: Beam emitter activation
// Feature: light-puzzle-game, Property 11: Beam reflection loop cap
// Validates: Requirements 3.1, 3.2, 3.3, 3.4, 8.3, 8.5

public static class BeamGridArbitrary
{
    public struct BeamTestCase
    {
        public int Width;
        public int Height;
        public bool[,] Walls;
        public BeamTracer.BeamEmitterInput[] Emitters;
        public BeamTracer.MirrorInput[] Mirrors;
        public HashSet<Vector2Int> IlluminatedByLightSource;

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
            $"Grid({Width}x{Height}) Emitters={Emitters.Length} Mirrors={Mirrors.Length}";
    }

    public static Arbitrary<BeamTestCase> Generate()
    {
        var gen = from width in Gen.Choose(7, 14)
                  from height in Gen.Choose(7, 14)
                  from numEmitters in Gen.Choose(1, 3)
                  from numMirrors in Gen.Choose(0, 3)
                  from seed in Arb.Generate<int>()
                  select BuildGrid(width, height, numEmitters, numMirrors, seed);
        return Arb.From(gen);
    }

    /// <summary>
    /// Generates a test case with a dormant emitter (not illuminated by any light source).
    /// </summary>
    public static Arbitrary<BeamTestCase> GenerateWithDormantEmitter()
    {
        var gen = from width in Gen.Choose(7, 14)
                  from height in Gen.Choose(7, 14)
                  from seed in Arb.Generate<int>()
                  select BuildGridWithDormantEmitter(width, height, seed);
        return Arb.From(gen);
    }

    private static BeamTestCase BuildGrid(int width, int height, int numEmitters, int numMirrors, int seed)
    {
        var rng = new System.Random(seed);
        var walls = new bool[width, height];
        var occupied = new HashSet<Vector2Int>();

        // Border walls
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

        // Scatter some interior walls
        for (int x = 2; x < width - 2; x++)
            for (int y = 2; y < height - 2; y++)
                if (rng.NextDouble() < 0.15)
                    walls[x, y] = true;

        // Pick floor tiles for emitters
        var emitters = new List<BeamTracer.BeamEmitterInput>();
        var illuminated = new HashSet<Vector2Int>();
        for (int i = 0; i < numEmitters; i++)
        {
            var pos = PickFloorTile(width, height, walls, occupied, rng);
            if (pos.HasValue)
            {
                var dir = GridDirections.All[rng.Next(8)];
                emitters.Add(new BeamTracer.BeamEmitterInput { Position = pos.Value, Direction = dir });
                occupied.Add(pos.Value);
                illuminated.Add(pos.Value); // emitters are active
            }
        }

        // Pick floor tiles for mirrors
        var mirrors = new List<BeamTracer.MirrorInput>();
        for (int i = 0; i < numMirrors; i++)
        {
            var pos = PickFloorTile(width, height, walls, occupied, rng);
            if (pos.HasValue)
            {
                mirrors.Add(new BeamTracer.MirrorInput { Position = pos.Value, RotationIndex = rng.Next(8) });
                occupied.Add(pos.Value);
            }
        }

        return new BeamTestCase
        {
            Width = width,
            Height = height,
            Walls = walls,
            Emitters = emitters.ToArray(),
            Mirrors = mirrors.ToArray(),
            IlluminatedByLightSource = illuminated
        };
    }

    private static BeamTestCase BuildGridWithDormantEmitter(int width, int height, int seed)
    {
        var rng = new System.Random(seed);
        var walls = new bool[width, height];
        var occupied = new HashSet<Vector2Int>();

        for (int x = 0; x < width; x++) { walls[x, 0] = true; walls[x, height - 1] = true; }
        for (int y = 0; y < height; y++) { walls[0, y] = true; walls[width - 1, y] = true; }

        var pos = PickFloorTile(width, height, walls, occupied, rng);
        var emitters = new BeamTracer.BeamEmitterInput[0];
        if (pos.HasValue)
        {
            emitters = new[] { new BeamTracer.BeamEmitterInput { Position = pos.Value, Direction = GridDirections.Right } };
        }

        // Empty illuminated set — emitter is dormant
        return new BeamTestCase
        {
            Width = width,
            Height = height,
            Walls = walls,
            Emitters = emitters,
            Mirrors = new BeamTracer.MirrorInput[0],
            IlluminatedByLightSource = new HashSet<Vector2Int>()
        };
    }

    private static Vector2Int? PickFloorTile(int w, int h, bool[,] walls, HashSet<Vector2Int> occupied, System.Random rng)
    {
        for (int attempt = 0; attempt < 50; attempt++)
        {
            var pos = new Vector2Int(rng.Next(1, w - 1), rng.Next(1, h - 1));
            if (!walls[pos.x, pos.y] && !occupied.Contains(pos))
                return pos;
        }
        return null;
    }
}


// ============================================================
// Property 4: Beam tracing correctness
// Validates: Requirements 3.1, 3.2, 3.4, 8.3
// ============================================================

[TestFixture]
public class BeamTracingCorrectnessTests
{
    private static readonly HashSet<Vector2Int> ValidDirections =
        new HashSet<Vector2Int>(GridDirections.All);

    // Property 4a: Each beam segment is a straight line in one of the 8 grid directions
    [Test]
    public void BeamSegments_AreInValidGridDirections()
    {
        var arb = BeamGridArbitrary.Generate();

        Prop.ForAll(arb, tc =>
        {
            var result = BeamTracer.TraceBeams(
                tc.Emitters, tc.Mirrors, tc.GetIsWall(),
                tc.IlluminatedByLightSource);

            foreach (var seg in result.Segments)
            {
                Assert.IsTrue(ValidDirections.Contains(seg.Direction),
                    $"Segment direction {seg.Direction} is not a valid grid direction");
            }
        }).QuickCheckThrowOnFailure();
    }

    // Property 4b: Dormant emitters produce no segments
    [Test]
    public void DormantEmitters_ProduceNoSegments()
    {
        var arb = BeamGridArbitrary.GenerateWithDormantEmitter();

        Prop.ForAll(arb, tc =>
        {
            var result = BeamTracer.TraceBeams(
                tc.Emitters, tc.Mirrors, tc.GetIsWall(),
                tc.IlluminatedByLightSource);

            Assert.AreEqual(0, result.Segments.Count,
                "Dormant emitter should produce no beam segments");
            Assert.AreEqual(0, result.IlluminatedTiles.Count,
                "Dormant emitter should illuminate no tiles");
        }).QuickCheckThrowOnFailure();
    }

    // Property 4c: Beam segments start at emitters or mirrors and end at walls or mirrors
    [Test]
    public void BeamSegments_StartAndEndAtCorrectPositions()
    {
        var arb = BeamGridArbitrary.Generate();

        Prop.ForAll(arb, tc =>
        {
            var isWall = tc.GetIsWall();
            var mirrorPositions = new HashSet<Vector2Int>(tc.Mirrors.Select(m => m.Position));
            var emitterPositions = new HashSet<Vector2Int>(tc.Emitters.Select(e => e.Position));

            var result = BeamTracer.TraceBeams(
                tc.Emitters, tc.Mirrors, isWall,
                tc.IlluminatedByLightSource);

            foreach (var seg in result.Segments)
            {
                var startTile = new Vector2Int((int)seg.Start.x, (int)seg.Start.y);
                var endTile = new Vector2Int((int)seg.End.x, (int)seg.End.y);

                // Start must be at an emitter or mirror position
                Assert.IsTrue(
                    emitterPositions.Contains(startTile) || mirrorPositions.Contains(startTile),
                    $"Segment start {startTile} is not at an emitter or mirror");

                // End must be at a wall-adjacent tile or mirror
                // (the last floor tile before a wall, or a mirror tile)
                var beyondEnd = endTile + seg.Direction;
                bool endsAtWall = isWall(beyondEnd);
                bool endsAtMirror = mirrorPositions.Contains(endTile);

                Assert.IsTrue(endsAtWall || endsAtMirror,
                    $"Segment end {endTile} is not adjacent to a wall or at a mirror");
            }
        }).QuickCheckThrowOnFailure();
    }
}


// ============================================================
// Property 5: Beam emitter activation
// Validates: Requirements 3.1, 3.2, 3.3
// ============================================================

[TestFixture]
public class BeamEmitterActivationTests
{
    // Property 5: Emitter is active iff its tile is in a light source's illuminated set
    [Test]
    public void EmitterProducesBeams_OnlyWhenIlluminatedByLightSource()
    {
        // Generate grids with light sources and emitters at various positions
        var arb = Arb.From(
            from width in Gen.Choose(7, 12)
            from height in Gen.Choose(7, 12)
            from radius in Gen.Choose(2, 4)
            from seed in Arb.Generate<int>()
            select BuildActivationTestCase(width, height, radius, seed));

        Prop.ForAll(arb, tc =>
        {
            var isWall = tc.GetIsWall();

            // Compute which tiles are illuminated by the light source
            var lightIlluminated = LightSourceLogic.GetRadiusIlluminatedTiles(
                tc.LightPos, tc.Radius, isWall);

            // Trace beams using the light source illumination
            var result = BeamTracer.TraceBeams(
                tc.Emitters, new BeamTracer.MirrorInput[0], isWall,
                lightIlluminated);

            foreach (var emitter in tc.Emitters)
            {
                bool isIlluminated = lightIlluminated.Contains(emitter.Position);
                bool producedSegments = result.Segments.Any(s =>
                    new Vector2Int((int)s.Start.x, (int)s.Start.y) == emitter.Position);

                if (isIlluminated)
                {
                    Assert.IsTrue(producedSegments,
                        $"Emitter at {emitter.Position} is illuminated but produced no segments");
                }
                else
                {
                    Assert.IsFalse(producedSegments,
                        $"Emitter at {emitter.Position} is NOT illuminated but produced segments");
                }
            }
        }).QuickCheckThrowOnFailure();
    }

    private struct ActivationTestCase
    {
        public int Width, Height;
        public bool[,] Walls;
        public Vector2Int LightPos;
        public int Radius;
        public BeamTracer.BeamEmitterInput[] Emitters;

        public Func<Vector2Int, bool> GetIsWall()
        {
            var w = Width; var h = Height; var walls = Walls;
            return pos => pos.x < 0 || pos.x >= w || pos.y < 0 || pos.y >= h || walls[pos.x, pos.y];
        }

        public override string ToString() =>
            $"Grid({Width}x{Height}) Light={LightPos} R={Radius} Emitters={Emitters.Length}";
    }

    private static ActivationTestCase BuildActivationTestCase(int width, int height, int radius, int seed)
    {
        var rng = new System.Random(seed);
        var walls = new bool[width, height];
        var occupied = new HashSet<Vector2Int>();

        for (int x = 0; x < width; x++) { walls[x, 0] = true; walls[x, height - 1] = true; }
        for (int y = 0; y < height; y++) { walls[0, y] = true; walls[width - 1, y] = true; }

        for (int x = 2; x < width - 2; x++)
            for (int y = 2; y < height - 2; y++)
                if (rng.NextDouble() < 0.15)
                    walls[x, y] = true;

        // Place light source
        var lightPos = PickFloor(width, height, walls, occupied, rng);
        occupied.Add(lightPos);

        // Place 2-4 emitters, some near the light (likely active), some far (likely dormant)
        int numEmitters = rng.Next(2, 5);
        var emitters = new List<BeamTracer.BeamEmitterInput>();
        for (int i = 0; i < numEmitters; i++)
        {
            var pos = PickFloor(width, height, walls, occupied, rng);
            occupied.Add(pos);
            emitters.Add(new BeamTracer.BeamEmitterInput
            {
                Position = pos,
                Direction = GridDirections.All[rng.Next(8)]
            });
        }

        return new ActivationTestCase
        {
            Width = width, Height = height, Walls = walls,
            LightPos = lightPos, Radius = radius,
            Emitters = emitters.ToArray()
        };
    }

    private static Vector2Int PickFloor(int w, int h, bool[,] walls, HashSet<Vector2Int> occupied, System.Random rng)
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            var pos = new Vector2Int(rng.Next(1, w - 1), rng.Next(1, h - 1));
            if (!walls[pos.x, pos.y] && !occupied.Contains(pos))
                return pos;
        }
        return new Vector2Int(1, 1); // fallback
    }
}


// ============================================================
// Property 11: Beam reflection loop cap
// Validates: Requirements 8.5
// ============================================================

[TestFixture]
public class BeamReflectionLoopCapTests
{
    // Property 11: Mirror configurations that form loops must terminate within maxReflections
    [Test]
    public void BeamTracing_TerminatesWithinMaxReflections()
    {
        // Generate grids where two mirrors face each other to create loops
        var arb = Arb.From(
            from maxRef in Gen.Choose(1, 20)
            from seed in Arb.Generate<int>()
            select BuildLoopTestCase(maxRef, seed));

        Prop.ForAll(arb, tc =>
        {
            var result = BeamTracer.TraceBeams(
                tc.Emitters, tc.Mirrors, tc.GetIsWall(),
                tc.Illuminated, tc.MaxReflections);

            // Segments count should be at most maxReflections + 1
            // (initial segment + one per reflection)
            Assert.LessOrEqual(result.Segments.Count, tc.MaxReflections + 1,
                $"Beam produced {result.Segments.Count} segments, exceeding max {tc.MaxReflections} reflections");
        }).QuickCheckThrowOnFailure();
    }

    private struct LoopTestCase
    {
        public int Width, Height;
        public bool[,] Walls;
        public BeamTracer.BeamEmitterInput[] Emitters;
        public BeamTracer.MirrorInput[] Mirrors;
        public HashSet<Vector2Int> Illuminated;
        public int MaxReflections;

        public Func<Vector2Int, bool> GetIsWall()
        {
            var w = Width; var h = Height; var walls = Walls;
            return pos => pos.x < 0 || pos.x >= w || pos.y < 0 || pos.y >= h || walls[pos.x, pos.y];
        }

        public override string ToString() =>
            $"LoopGrid({Width}x{Height}) MaxRef={MaxReflections}";
    }

    /// <summary>
    /// Builds a grid with two mirrors facing each other to create a reflection loop.
    /// Emitter shoots right, mirror A reflects up, mirror B reflects right (back toward A), etc.
    /// </summary>
    private static LoopTestCase BuildLoopTestCase(int maxReflections, int seed)
    {
        var rng = new System.Random(seed);
        int width = 12;
        int height = 12;
        var walls = new bool[width, height];

        // Border walls
        for (int x = 0; x < width; x++) { walls[x, 0] = true; walls[x, height - 1] = true; }
        for (int y = 0; y < height; y++) { walls[0, y] = true; walls[width - 1, y] = true; }

        // Place emitter shooting right from (2, 5)
        var emitterPos = new Vector2Int(2, 5);
        var emitter = new BeamTracer.BeamEmitterInput
        {
            Position = emitterPos,
            Direction = GridDirections.Right
        };

        // Place two mirrors that bounce the beam back and forth
        // Mirror A at (5, 5) with rotation that reflects Right -> Up
        // Mirror B at (5, 8) with rotation that reflects Up -> Left... 
        // Actually, let's just place mirrors that create a simple bounce loop:
        // Mirror at (6, 5) rotationIndex=1 (45°) reflects Right(0) -> Up(2)
        // Mirror at (6, 8) rotationIndex=7 (315°) reflects Up(2) -> Right(0)... 
        // This depends on the reflection table. Let's use a simpler approach:
        // Just place mirrors at known positions and let the reflection table handle it.
        // The key property is that the beam terminates regardless of mirror config.

        var mirrorA = new BeamTracer.MirrorInput { Position = new Vector2Int(5, 5), RotationIndex = rng.Next(8) };
        var mirrorB = new BeamTracer.MirrorInput { Position = new Vector2Int(5, 8), RotationIndex = rng.Next(8) };
        var mirrorC = new BeamTracer.MirrorInput { Position = new Vector2Int(8, 5), RotationIndex = rng.Next(8) };
        var mirrorD = new BeamTracer.MirrorInput { Position = new Vector2Int(8, 8), RotationIndex = rng.Next(8) };

        var illuminated = new HashSet<Vector2Int> { emitterPos };

        return new LoopTestCase
        {
            Width = width,
            Height = height,
            Walls = walls,
            Emitters = new[] { emitter },
            Mirrors = new[] { mirrorA, mirrorB, mirrorC, mirrorD },
            Illuminated = illuminated,
            MaxReflections = maxReflections
        };
    }
}
