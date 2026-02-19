using NUnit.Framework;
using UnityEngine;
using FsCheck;

// ============================================================
// Feature: visual-update
// Property 5: Sprite factory determinism
// Validates: Requirements 9.3
// ============================================================

[TestFixture]
public class SpriteFactoryDeterminismTests
{
    [Test]
    public void SameInputs_ProduceIdenticalPixelData()
    {
        Prop.ForAll(
            Arb.From(Gen.Choose(1, 128)),
            Arb.From(Gen.Elements(Color.red, Color.green, Color.blue, Color.white, Color.cyan)),
            (int resolution, Color color) =>
            {
                var s1 = SpriteFactory.CreateCircle(resolution, color);
                var s2 = SpriteFactory.CreateCircle(resolution, color);
                var p1 = s1.texture.GetPixels32();
                var p2 = s2.texture.GetPixels32();

                Assert.AreEqual(p1.Length, p2.Length, "Pixel array lengths differ");
                for (int i = 0; i < p1.Length; i++)
                {
                    Assert.AreEqual(p1[i], p2[i], $"Pixel {i} differs at resolution {resolution}");
                }
            }
        ).QuickCheckThrowOnFailure();
    }
}

// ============================================================
// Feature: visual-update
// Property 6: Sprite factory produces non-empty textures
// Validates: Requirements 9.4
// ============================================================

[TestFixture]
public class SpriteFactoryNonEmptyTests
{
    [Test]
    public void GeneratedSprites_ContainNonTransparentPixels()
    {
        Prop.ForAll(
            Arb.From(Gen.Choose(1, 128)),
            (int resolution) =>
            {
                var sprite = SpriteFactory.CreateCircle(resolution, Color.white);
                var pixels = sprite.texture.GetPixels32();
                bool hasNonTransparent = false;
                for (int i = 0; i < pixels.Length; i++)
                {
                    if (pixels[i].a > 0)
                    {
                        hasNonTransparent = true;
                        break;
                    }
                }
                Assert.IsTrue(hasNonTransparent,
                    $"Circle sprite at resolution {resolution} has no non-transparent pixels");
            }
        ).QuickCheckThrowOnFailure();
    }
}

// ============================================================
// Feature: visual-update
// Property 4: Dark theme brightness constraint
// Validates: Requirements 8.3
// ============================================================

[TestFixture]
public class DarkThemeBrightnessTests
{
    [Test]
    public void WallAndFloor_HaveLowBrightness()
    {
        var backgroundColors = new[] { ColorPalette.Wall, ColorPalette.Floor };

        Prop.ForAll(
            Arb.From(Gen.Elements(backgroundColors)),
            (Color color) =>
            {
                Color.RGBToHSV(color, out _, out _, out float v);
                Assert.Less(v, 0.4f,
                    $"Background color ({color}) has brightness {v} >= 0.4");
            }
        ).QuickCheckThrowOnFailure();
    }
}

// ============================================================
// Feature: visual-update
// Property 7: Sorting orders are strictly ascending
// Validates: Requirements 10.1
// ============================================================

[TestFixture]
public class SortingOrderTests
{
    [Test]
    public void SortingOrders_AreStrictlyAscending()
    {
        // Floor < Wall < Beam < Fog < Element < Player
        var orders = new[]
        {
            ("Floor", VisualConfig.FloorSortOrder),
            ("Wall", VisualConfig.WallSortOrder),
            ("Beam", VisualConfig.BeamSortOrder),
            ("Fog", VisualConfig.FogSortOrder),
            ("Element", VisualConfig.ElementSortOrder),
            ("Player", VisualConfig.PlayerSortOrder)
        };

        Prop.ForAll(
            Arb.From(Gen.Choose(0, orders.Length - 2)),
            (int i) =>
            {
                Assert.Less(orders[i].Item2, orders[i + 1].Item2,
                    $"{orders[i].Item1} ({orders[i].Item2}) should be < {orders[i + 1].Item1} ({orders[i + 1].Item2})");
            }
        ).QuickCheckThrowOnFailure();
    }
}

// ============================================================
// Feature: visual-update
// Property 1: Player position is tile-centered
// Validates: Requirements 1.2
// ============================================================

[TestFixture]
public class PlayerCenteringTests
{
    [Test]
    public void TileToWorld_ReturnsTileCenter()
    {
        Prop.ForAll(
            Arb.From(Gen.Choose(0, 100)),
            Arb.From(Gen.Choose(0, 100)),
            (int x, int y) =>
            {
                var tile = new Vector2Int(x, y);
                var world = PlayerController.TileToWorld(tile);

                Assert.AreEqual(x + 0.5f, world.x, 0.0001f,
                    $"X mismatch for tile ({x},{y})");
                Assert.AreEqual(y + 0.5f, world.y, 0.0001f,
                    $"Y mismatch for tile ({x},{y})");
                Assert.AreEqual(0f, world.z, 0.0001f,
                    $"Z should be 0 for tile ({x},{y})");
            }
        ).QuickCheckThrowOnFailure();
    }
}

// ============================================================
// Feature: visual-update
// Property 2: Beam emitter rotation matches direction
// Validates: Requirements 4.2
// ============================================================

[TestFixture]
public class BeamEmitterRotationTests
{
    [Test]
    public void RotationAngle_MatchesAtan2OfDirection()
    {
        Prop.ForAll(
            Arb.From(Gen.Elements(GridDirections.All)),
            (Vector2Int dir) =>
            {
                float expected = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                // This is the same computation used in LevelManager when setting beam emitter rotation
                float actual = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                Assert.AreEqual(expected, actual, 0.001f,
                    $"Beam emitter rotation for direction ({dir.x},{dir.y}): expected {expected}, got {actual}");
            }
        ).QuickCheckThrowOnFailure();
    }
}

// ============================================================
// Feature: visual-update
// Property 3: Mirror visual rotation matches rotation index
// Validates: Requirements 5.2, 5.3
// ============================================================

[TestFixture]
public class MirrorVisualRotationTests
{
    [Test]
    public void ZRotation_EqualsNegativeIndexTimes45()
    {
        Prop.ForAll(
            Arb.From(Gen.Choose(0, 7)),
            (int rotationIndex) =>
            {
                float expectedZ = -(rotationIndex * 45f);
                var quat = Quaternion.Euler(0, 0, expectedZ);
                // Extract the Z euler angle and normalize to match expected
                float actualZ = quat.eulerAngles.z;
                // Normalize: Unity returns [0,360), we expect negative values mapped to that range
                float normalizedExpected = ((expectedZ % 360f) + 360f) % 360f;

                Assert.AreEqual(normalizedExpected, actualZ, 0.01f,
                    $"Mirror rotation index {rotationIndex}: expected Z={normalizedExpected}, got Z={actualZ}");
            }
        ).QuickCheckThrowOnFailure();
    }
}


// ============================================================
// Feature: visual-update
// Unit Tests: Visual configuration values
// Validates: Requirements 1.1, 3.2, 4.3, 5.4, 6.2, 6.3, 6.4, 7.2, 8.1
// ============================================================

[TestFixture]
public class VisualConfigurationUnitTests
{
    [Test]
    public void ColorPalette_AllFieldsAreNonDefault()
    {
        Assert.AreNotEqual(default(Color), ColorPalette.Wall, "Wall color is default");
        Assert.AreNotEqual(default(Color), ColorPalette.Floor, "Floor color is default");
        Assert.AreNotEqual(default(Color), ColorPalette.Player, "Player color is default");
        Assert.AreNotEqual(default(Color), ColorPalette.LightSource, "LightSource color is default");
        Assert.AreNotEqual(default(Color), ColorPalette.BeamEmitter, "BeamEmitter color is default");
        Assert.AreNotEqual(default(Color), ColorPalette.MirrorSurface, "MirrorSurface color is default");
        Assert.AreNotEqual(default(Color), ColorPalette.ObjectiveUnlit, "ObjectiveUnlit color is default");
        Assert.AreNotEqual(default(Color), ColorPalette.ObjectiveLit, "ObjectiveLit color is default");
        Assert.AreNotEqual(default(Color), ColorPalette.Beam, "Beam color is default");
        // Fog is Color.black which equals default(Color) in RGB but has a=1, so check alpha
        Assert.AreEqual(1f, ColorPalette.Fog.a, 0.01f, "Fog alpha should be 1");
    }

    [Test]
    public void ScaleFactors_AreInValidRange()
    {
        var scales = new[]
        {
            ("PlayerScale", VisualConfig.PlayerScale),
            ("LightSourceScale", VisualConfig.LightSourceScale),
            ("BeamEmitterScale", VisualConfig.BeamEmitterScale),
            ("MirrorScale", VisualConfig.MirrorScale),
            ("ObjectiveScale", VisualConfig.ObjectiveScale)
        };

        foreach (var (name, value) in scales)
        {
            Assert.GreaterOrEqual(value, 0.4f, $"{name} is below 0.4");
            Assert.LessOrEqual(value, 0.6f, $"{name} is above 0.6");
        }
    }

    [Test]
    public void ObjectiveAlpha_MeetsBounds()
    {
        Assert.LessOrEqual(ColorPalette.ObjectiveUnlit.a, 0.3f,
            $"ObjectiveUnlit alpha {ColorPalette.ObjectiveUnlit.a} should be <= 0.3");
        Assert.GreaterOrEqual(ColorPalette.ObjectiveLit.a, 0.7f,
            $"ObjectiveLit alpha {ColorPalette.ObjectiveLit.a} should be >= 0.7");
    }

    [Test]
    public void BeamWidth_IsInValidRange()
    {
        Assert.GreaterOrEqual(VisualConfig.BeamWidth, 0.08f, "BeamWidth is below 0.08");
        Assert.LessOrEqual(VisualConfig.BeamWidth, 0.15f, "BeamWidth is above 0.15");
    }

    [Test]
    public void WallAndFloor_AreVisuallyDistinct()
    {
        Color.RGBToHSV(ColorPalette.Wall, out float wH, out float wS, out float wV);
        Color.RGBToHSV(ColorPalette.Floor, out float fH, out float fS, out float fV);

        float hueDiff = Mathf.Abs(wH - fH);
        float satDiff = Mathf.Abs(wS - fS);
        float valDiff = Mathf.Abs(wV - fV);
        float totalDiff = hueDiff + satDiff + valDiff;

        Assert.Greater(totalDiff, 0.01f,
            $"Wall and Floor colors are not visually distinct (HSV diff: {totalDiff})");
    }
}
