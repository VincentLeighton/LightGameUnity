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
        // Floor < Wall < Beam < Element < Player < Fog
        var orders = new[]
        {
            ("Floor", VisualConfig.FloorSortOrder),
            ("Wall", VisualConfig.WallSortOrder),
            ("Beam", VisualConfig.BeamSortOrder),
            ("Element", VisualConfig.ElementSortOrder),
            ("Player", VisualConfig.PlayerSortOrder),
            ("Fog", VisualConfig.FogSortOrder)
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
