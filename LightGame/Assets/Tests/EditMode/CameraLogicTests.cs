using FsCheck;
using NUnit.Framework;
using UnityEngine;

// Feature: light-puzzle-game, Property 12: Camera bounds clamping
// Validates: Requirements 9.3, 9.4

[TestFixture]
public class CameraLogicTests
{
    // Property 12a: For random player positions and level bounds,
    // the clamped camera viewport stays within level bounds.
    [Test]
    public void ClampPosition_ViewportStaysWithinLevelBounds()
    {
        var arb = Arb.From(
            from camX in Gen.Choose(-100, 100).Select(i => i * 0.5f)
            from camY in Gen.Choose(-100, 100).Select(i => i * 0.5f)
            from ortho in Gen.Choose(1, 20).Select(i => (float)i)
            from aspect in Gen.Choose(5, 30).Select(i => i * 0.1f) // 0.5 to 3.0
            from levelW in Gen.Choose(5, 50).Select(i => (float)i)
            from levelH in Gen.Choose(5, 50).Select(i => (float)i)
            select new { camX, camY, ortho, aspect, levelW, levelH });

        Prop.ForAll(arb, data =>
        {
            Vector2 levelMin = Vector2.zero;
            Vector2 levelMax = new Vector2(data.levelW, data.levelH);

            Vector2 clamped = CameraLogic.ClampPosition(
                new Vector2(data.camX, data.camY),
                data.ortho,
                data.aspect,
                levelMin,
                levelMax);

            float halfH = data.ortho;
            float halfW = data.ortho * data.aspect;

            // Viewport edges
            float viewLeft = clamped.x - halfW;
            float viewRight = clamped.x + halfW;
            float viewBottom = clamped.y - halfH;
            float viewTop = clamped.y + halfH;

            // If the level is large enough to contain the viewport,
            // the viewport must be within bounds.
            if (data.levelW >= halfW * 2f)
            {
                Assert.GreaterOrEqual(viewLeft, levelMin.x - 0.001f,
                    $"Viewport left {viewLeft} < level min x {levelMin.x}");
                Assert.LessOrEqual(viewRight, levelMax.x + 0.001f,
                    $"Viewport right {viewRight} > level max x {levelMax.x}");
            }

            if (data.levelH >= halfH * 2f)
            {
                Assert.GreaterOrEqual(viewBottom, levelMin.y - 0.001f,
                    $"Viewport bottom {viewBottom} < level min y {levelMin.y}");
                Assert.LessOrEqual(viewTop, levelMax.y + 0.001f,
                    $"Viewport top {viewTop} > level max y {levelMax.y}");
            }
        }).QuickCheckThrowOnFailure();
    }

    // Property 12b: For random zoom inputs, orthographic size stays within min/max.
    [Test]
    public void ClampZoom_StaysWithinMinMax()
    {
        var arb = Arb.From(
            from rawZoom in Gen.Choose(-100, 200).Select(i => i * 0.1f)
            from minZoom in Gen.Choose(1, 5).Select(i => (float)i)
            from maxRange in Gen.Choose(1, 20).Select(i => (float)i)
            select new { rawZoom, minZoom, maxZoom = minZoom + maxRange });

        Prop.ForAll(arb, data =>
        {
            float result = CameraLogic.ClampZoom(data.rawZoom, data.minZoom, data.maxZoom);

            Assert.GreaterOrEqual(result, data.minZoom,
                $"Zoom {result} < min {data.minZoom}");
            Assert.LessOrEqual(result, data.maxZoom,
                $"Zoom {result} > max {data.maxZoom}");
        }).QuickCheckThrowOnFailure();
    }

    // Property 12c: When level is large enough, camera centered on level center
    // if level is smaller than viewport, camera should be at level center.
    [Test]
    public void ClampPosition_CentersWhenLevelSmallerThanViewport()
    {
        var arb = Arb.From(
            from camX in Gen.Choose(-50, 50).Select(i => (float)i)
            from camY in Gen.Choose(-50, 50).Select(i => (float)i)
            from ortho in Gen.Choose(10, 30).Select(i => (float)i) // large ortho
            select new { camX, camY, ortho });

        Prop.ForAll(arb, data =>
        {
            // Small level that's definitely smaller than viewport
            Vector2 levelMin = new Vector2(0, 0);
            Vector2 levelMax = new Vector2(5, 5);
            float aspect = 1.0f;

            Vector2 clamped = CameraLogic.ClampPosition(
                new Vector2(data.camX, data.camY),
                data.ortho,
                aspect,
                levelMin,
                levelMax);

            // Should center on the level
            float expectedX = 2.5f;
            float expectedY = 2.5f;

            Assert.AreEqual(expectedX, clamped.x, 0.001f,
                $"Expected centered X={expectedX}, got {clamped.x}");
            Assert.AreEqual(expectedY, clamped.y, 0.001f,
                $"Expected centered Y={expectedY}, got {clamped.y}");
        }).QuickCheckThrowOnFailure();
    }
}
