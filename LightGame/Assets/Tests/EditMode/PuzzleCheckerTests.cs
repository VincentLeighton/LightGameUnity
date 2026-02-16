using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using UnityEngine;

// Feature: light-puzzle-game, Property 13: Level completion check
// Feature: light-puzzle-game, Property 14: Remaining objectives count
// Validates: Requirements 6.2, 10.2

[TestFixture]
public class PuzzleCheckerTests
{
    /// <summary>
    /// Generator for random objective positions and illuminated tile sets.
    /// </summary>
    private static Arbitrary<(Vector2Int[] objectives, HashSet<Vector2Int> illuminated)> ObjectiveAndIlluminationArb()
    {
        var gen =
            from objCount in Gen.Choose(1, 10)
            from positions in Gen.ArrayOf(objCount,
                from x in Gen.Choose(0, 19)
                from y in Gen.Choose(0, 19)
                select new Vector2Int(x, y))
            from extraCount in Gen.Choose(0, 15)
            from extraTiles in Gen.ArrayOf(extraCount,
                from x in Gen.Choose(0, 19)
                from y in Gen.Choose(0, 19)
                select new Vector2Int(x, y))
            from illuminateAll in Gen.Elements(true, false)
            select BuildTestData(positions, extraTiles, illuminateAll);

        return Arb.From(gen);
    }

    private static (Vector2Int[] objectives, HashSet<Vector2Int> illuminated) BuildTestData(
        Vector2Int[] objectives, Vector2Int[] extraTiles, bool illuminateAll)
    {
        var illuminated = new HashSet<Vector2Int>(extraTiles);
        if (illuminateAll)
        {
            foreach (var obj in objectives)
                illuminated.Add(obj);
        }
        return (objectives, illuminated);
    }

    // Property 13: Level is complete iff all objectives are illuminated
    [Test]
    public void IsLevelComplete_TrueIffAllObjectivesIlluminated()
    {
        var arb = ObjectiveAndIlluminationArb();

        Prop.ForAll(arb, data =>
        {
            var (objectives, illuminated) = data;
            bool result = PuzzleChecker.IsLevelComplete(objectives, illuminated);
            bool allLit = objectives.All(o => illuminated.Contains(o));

            Assert.AreEqual(allLit, result,
                $"IsLevelComplete returned {result} but allLit={allLit} " +
                $"for {objectives.Length} objectives");
        }).QuickCheckThrowOnFailure();
    }

    // Property 14: Remaining count equals number of un-illuminated objectives
    [Test]
    public void GetRemainingCount_EqualsUnilluminatedObjectives()
    {
        var arb = ObjectiveAndIlluminationArb();

        Prop.ForAll(arb, data =>
        {
            var (objectives, illuminated) = data;
            int remaining = PuzzleChecker.GetRemainingCount(objectives, illuminated);
            int expected = objectives.Count(o => !illuminated.Contains(o));

            Assert.AreEqual(expected, remaining,
                $"Remaining count {remaining} != expected {expected}");

            // Remaining must be non-negative and at most total objectives
            Assert.IsTrue(remaining >= 0 && remaining <= objectives.Length,
                $"Remaining {remaining} out of valid range [0, {objectives.Length}]");
        }).QuickCheckThrowOnFailure();
    }

    // Property 13+14 consistency: complete iff remaining == 0
    [Test]
    public void Completion_ConsistentWithRemainingCount()
    {
        var arb = ObjectiveAndIlluminationArb();

        Prop.ForAll(arb, data =>
        {
            var (objectives, illuminated) = data;
            bool complete = PuzzleChecker.IsLevelComplete(objectives, illuminated);
            int remaining = PuzzleChecker.GetRemainingCount(objectives, illuminated);

            Assert.AreEqual(complete, remaining == 0,
                $"IsLevelComplete={complete} but remaining={remaining}");
        }).QuickCheckThrowOnFailure();
    }
}
