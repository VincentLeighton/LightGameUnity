using System;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using UnityEngine;

// Feature: light-puzzle-game, Property 9: Level data serialization round-trip

public static class LevelDataArbitrary
{
    private static readonly Vector2Int[] ValidDirections = GridDirections.All;

    public static Arbitrary<LevelData> Generate()
    {
        var gen = from width in Gen.Choose(3, 10)
                  from height in Gen.Choose(3, 10)
                  from seed in Arb.Generate<int>()
                  select BuildLevelData(width, height, seed);
        return Arb.From(gen);
    }

    private static LevelData BuildLevelData(int width, int height, int seed)
    {
        var rng = new System.Random(seed);

        var tiles = new TileRow[height];
        for (int y = 0; y < height; y++)
        {
            var row = new string[width];
            for (int x = 0; x < width; x++)
            {
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                    row[x] = "#";
                else
                    row[x] = rng.NextDouble() < 0.25 ? "#" : "_";
            }
            tiles[y] = new TileRow(row);
        }

        tiles[1].row[1] = "_";
        tiles[1].row[2] = "_";
        if (height > 2 && width > 2) tiles[2].row[1] = "_";

        var floors = new System.Collections.Generic.List<(int x, int y)>();
        for (int y = 1; y < height - 1; y++)
            for (int x = 1; x < width - 1; x++)
                if (tiles[y].row[x] == "_") floors.Add((x, y));

        (int x, int y) PickFloor() => floors[rng.Next(floors.Count)];

        var playerStart = PickFloor();

        int lightCount = rng.Next(1, Math.Min(4, floors.Count));
        var lightSources = new LightSourceData[lightCount];
        for (int i = 0; i < lightCount; i++)
        {
            var pos = PickFloor();
            lightSources[i] = new LightSourceData { x = pos.x, y = pos.y, radius = rng.Next(1, 5) };
        }

        int emitterCount = rng.Next(1, Math.Min(3, floors.Count));
        var beamEmitters = new BeamEmitterData[emitterCount];
        for (int i = 0; i < emitterCount; i++)
        {
            var pos = PickFloor();
            var dir = ValidDirections[rng.Next(ValidDirections.Length)];
            beamEmitters[i] = new BeamEmitterData
            {
                x = pos.x,
                y = pos.y,
                beamDirection = new PositionData { x = dir.x, y = dir.y }
            };
        }

        int mirrorCount = rng.Next(0, Math.Min(4, floors.Count));
        var mirrors = new MirrorData[mirrorCount];
        for (int i = 0; i < mirrorCount; i++)
        {
            var pos = PickFloor();
            mirrors[i] = new MirrorData { x = pos.x, y = pos.y, rotationIndex = rng.Next(0, 8) };
        }

        int objCount = rng.Next(1, Math.Min(3, floors.Count));
        var objectives = new PositionData[objCount];
        for (int i = 0; i < objCount; i++)
        {
            var pos = PickFloor();
            objectives[i] = new PositionData { x = pos.x, y = pos.y };
        }

        return new LevelData
        {
            levelIndex = rng.Next(0, 100),
            width = width,
            height = height,
            tiles = tiles,
            playerStart = new PositionData { x = playerStart.x, y = playerStart.y },
            lightSources = lightSources,
            beamEmitters = beamEmitters,
            mirrors = mirrors,
            puzzleObjectives = objectives
        };
    }
}

[TestFixture]
public class LevelDataSerializationTests
{
    // Property 9: Level data serialization round-trip
    // Validates: Requirements 7.3
    [Test]
    public void SerializeDeserializeRoundTrip_PreservesAllFields()
    {
        var arb = LevelDataArbitrary.Generate();

        Prop.ForAll(arb, original =>
        {
            string json = JsonUtility.ToJson(original);
            var deserialized = JsonUtility.FromJson<LevelData>(json);

            Assert.AreEqual(original.levelIndex, deserialized.levelIndex, "levelIndex mismatch");
            Assert.AreEqual(original.width, deserialized.width, "width mismatch");
            Assert.AreEqual(original.height, deserialized.height, "height mismatch");

            // Tiles
            Assert.AreEqual(original.tiles.Length, deserialized.tiles.Length, "tiles row count mismatch");
            for (int y = 0; y < original.tiles.Length; y++)
            {
                Assert.AreEqual(original.tiles[y].row.Length, deserialized.tiles[y].row.Length,
                    $"tiles row {y} column count mismatch");
                for (int x = 0; x < original.tiles[y].row.Length; x++)
                    Assert.AreEqual(original.tiles[y].row[x], deserialized.tiles[y].row[x],
                        $"tile [{y}][{x}] mismatch");
            }

            // Player start
            Assert.AreEqual(original.playerStart.x, deserialized.playerStart.x, "playerStart.x");
            Assert.AreEqual(original.playerStart.y, deserialized.playerStart.y, "playerStart.y");

            // Light sources
            Assert.AreEqual(original.lightSources.Length, deserialized.lightSources.Length);
            for (int i = 0; i < original.lightSources.Length; i++)
            {
                Assert.AreEqual(original.lightSources[i].x, deserialized.lightSources[i].x);
                Assert.AreEqual(original.lightSources[i].y, deserialized.lightSources[i].y);
                Assert.AreEqual(original.lightSources[i].radius, deserialized.lightSources[i].radius);
            }

            // Beam emitters
            Assert.AreEqual(original.beamEmitters.Length, deserialized.beamEmitters.Length);
            for (int i = 0; i < original.beamEmitters.Length; i++)
            {
                Assert.AreEqual(original.beamEmitters[i].x, deserialized.beamEmitters[i].x);
                Assert.AreEqual(original.beamEmitters[i].y, deserialized.beamEmitters[i].y);
                Assert.AreEqual(original.beamEmitters[i].beamDirection.x,
                    deserialized.beamEmitters[i].beamDirection.x);
                Assert.AreEqual(original.beamEmitters[i].beamDirection.y,
                    deserialized.beamEmitters[i].beamDirection.y);
            }

            // Mirrors
            Assert.AreEqual(original.mirrors.Length, deserialized.mirrors.Length);
            for (int i = 0; i < original.mirrors.Length; i++)
            {
                Assert.AreEqual(original.mirrors[i].x, deserialized.mirrors[i].x);
                Assert.AreEqual(original.mirrors[i].y, deserialized.mirrors[i].y);
                Assert.AreEqual(original.mirrors[i].rotationIndex, deserialized.mirrors[i].rotationIndex);
            }

            // Puzzle objectives
            Assert.AreEqual(original.puzzleObjectives.Length, deserialized.puzzleObjectives.Length);
            for (int i = 0; i < original.puzzleObjectives.Length; i++)
            {
                Assert.AreEqual(original.puzzleObjectives[i].x, deserialized.puzzleObjectives[i].x);
                Assert.AreEqual(original.puzzleObjectives[i].y, deserialized.puzzleObjectives[i].y);
            }
        }).QuickCheckThrowOnFailure();
    }
}
