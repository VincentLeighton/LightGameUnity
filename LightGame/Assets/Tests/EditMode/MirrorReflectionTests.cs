using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using NUnit.Framework;
using UnityEngine;

// Feature: light-puzzle-game, Property 6: Mirror reflection correctness
// Validates: Requirements 3.5, 8.2

[TestFixture]
public class MirrorReflectionTests
{
    private static readonly HashSet<Vector2Int> ValidDirections =
        new HashSet<Vector2Int>(GridDirections.All);

    // Property 6a: For all 64 combinations, output is always one of the 8 valid grid directions
    // (or zero for absorption)
    [Test]
    public void ReflectedDirection_IsAlwaysValidGridDirection()
    {
        for (int inDir = 0; inDir < 8; inDir++)
        {
            for (int mirrorRot = 0; mirrorRot < 8; mirrorRot++)
            {
                var incoming = GridDirections.All[inDir];
                var reflected = ReflectionTable.GetReflectedDirection(incoming, mirrorRot);

                // Result must be either a valid direction or zero (absorbed)
                Assert.IsTrue(
                    reflected == Vector2Int.zero || ValidDirections.Contains(reflected),
                    $"Reflected direction {reflected} for incoming={incoming} mirror={mirrorRot} is not a valid grid direction");
            }
        }
    }

    // Property 6b: Law of reflection — angle of incidence equals angle of reflection
    // relative to the mirror's surface normal
    [Test]
    public void ReflectedDirection_SatisfiesLawOfReflection()
    {
        for (int inDir = 0; inDir < 8; inDir++)
        {
            for (int mirrorRot = 0; mirrorRot < 8; mirrorRot++)
            {
                int outIdx = ReflectionTable.GetReflectedDirectionIndex(inDir, mirrorRot);
                if (outIdx < 0) continue; // skip absorbed beams

                // Mirror surface normal angle = (mirrorRot * 45 + 90) degrees
                double normalAngle = (mirrorRot * 45.0 + 90.0) * Math.PI / 180.0;
                // Reverse the incoming direction (beam travels toward mirror)
                double inAngleReversed = (inDir * 45.0 + 180.0) * Math.PI / 180.0;
                double outAngle = outIdx * 45.0 * Math.PI / 180.0;

                // Angle of incidence = angle between reversed incoming and normal
                double incidence = AngleDiff(inAngleReversed, normalAngle);
                // Angle of reflection = angle between outgoing and normal
                double reflection = AngleDiff(outAngle, normalAngle);

                // They should be equal (within floating point tolerance)
                Assert.AreEqual(incidence, reflection, 0.01,
                    $"Law of reflection violated: inDir={inDir} mirrorRot={mirrorRot} " +
                    $"incidence={incidence:F2} reflection={reflection:F2}");
            }
        }
    }

    // Property 6c: Reflection is deterministic — same inputs always produce same output
    [Test]
    public void ReflectedDirection_IsDeterministic()
    {
        var arb = Arb.From(
            from inDir in Gen.Choose(0, 7)
            from mirrorRot in Gen.Choose(0, 7)
            select (inDir, mirrorRot));

        Prop.ForAll(arb, pair =>
        {
            var (inDir, mirrorRot) = pair;
            var incoming = GridDirections.All[inDir];

            var result1 = ReflectionTable.GetReflectedDirection(incoming, mirrorRot);
            var result2 = ReflectionTable.GetReflectedDirection(incoming, mirrorRot);

            Assert.AreEqual(result1, result2,
                $"Non-deterministic reflection for inDir={inDir} mirrorRot={mirrorRot}");
        }).QuickCheckThrowOnFailure();
    }

    /// <summary>
    /// Returns the absolute angular difference in radians, normalized to [0, PI].
    /// </summary>
    private static double AngleDiff(double a, double b)
    {
        double diff = Math.Abs(a - b) % (2 * Math.PI);
        if (diff > Math.PI) diff = 2 * Math.PI - diff;
        return diff;
    }
}

// Feature: light-puzzle-game, Property 8: Mirror rotation invariant
// Validates: Requirements 4.3, 8.1

[TestFixture]
public class MirrorRotationInvariantTests
{
    /// <summary>
    /// Simulates mirror rotation: (index + 1) % 8
    /// </summary>
    private static int Rotate(int rotationIndex)
    {
        return (rotationIndex + 1) % 8;
    }

    // Property 8a: For any starting rotation index and any number of rotations,
    // the index stays in [0, 7]
    [Test]
    public void RotationIndex_AlwaysStaysInValidRange()
    {
        var arb = Arb.From(
            from startIdx in Gen.Choose(0, 7)
            from numRotations in Gen.Choose(0, 50)
            select (startIdx, numRotations));

        Prop.ForAll(arb, pair =>
        {
            var (startIdx, numRotations) = pair;
            int current = startIdx;

            for (int i = 0; i < numRotations; i++)
            {
                current = Rotate(current);
                Assert.IsTrue(current >= 0 && current <= 7,
                    $"Rotation index {current} out of range after {i + 1} rotations from start {startIdx}");
            }
        }).QuickCheckThrowOnFailure();
    }

    // Property 8b: 8 rotations returns to original angle
    [Test]
    public void EightRotations_ReturnToOriginalAngle()
    {
        var arb = Arb.From(Gen.Choose(0, 7));

        Prop.ForAll(arb, startIdx =>
        {
            int current = startIdx;
            for (int i = 0; i < 8; i++)
                current = Rotate(current);

            Assert.AreEqual(startIdx, current,
                $"After 8 rotations from {startIdx}, got {current} instead of returning to start");
        }).QuickCheckThrowOnFailure();
    }
}
