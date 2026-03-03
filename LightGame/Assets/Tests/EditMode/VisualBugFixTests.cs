using NUnit.Framework;
using FsCheck;

// ============================================================
// Feature: visual-bug-fixes
// Property 1: Sorting order hierarchy is strictly ascending with fog below elements
// Validates: Requirements 1.4, 1.5, 1.6
// ============================================================

[TestFixture]
public class SortingOrderHierarchyPropertyTests
{
    [Test]
    public void SortingOrder_FogBelowElements_StrictlyAscending()
    {
        // Expected order: Floor < Wall < Beam < Fog < Element < Player
        var layers = new[]
        {
            ("Floor", VisualConfig.FloorSortOrder),
            ("Wall", VisualConfig.WallSortOrder),
            ("Beam", VisualConfig.BeamSortOrder),
            ("Fog", VisualConfig.FogSortOrder),
            ("Border", VisualConfig.BorderSortOrder),
            ("Element", VisualConfig.ElementSortOrder),
            ("Player", VisualConfig.PlayerSortOrder)
        };

        Prop.ForAll(
            Arb.From(Gen.Choose(0, layers.Length - 2)),
            (int i) =>
            {
                Assert.Less(layers[i].Item2, layers[i + 1].Item2,
                    $"{layers[i].Item1} ({layers[i].Item2}) must be < {layers[i + 1].Item1} ({layers[i + 1].Item2})");
            }
        ).QuickCheckThrowOnFailure();
    }
}

// ============================================================
// Feature: visual-bug-fixes
// Property 2: All unobstructed floor tiles within radius are illuminated
// Validates: Requirements 2.1, 2.2, 2.3
// ============================================================

[TestFixture]
public class UnobstructedIlluminationPropertyTests
{
    [Test]
    public void AllUnobstructedFloorTilesWithinRadius_AreIlluminated()
    {
        var arb = LightGridArbitrary.Generate();

        Prop.ForAll(arb, tc =>
        {
            var isWall = tc.GetIsWall();
            var illuminated = LightSourceLogic.GetRadiusIlluminatedTiles(tc.LightPos, tc.Radius, isWall);

            for (int dx = -tc.Radius; dx <= tc.Radius; dx++)
            {
                for (int dy = -tc.Radius; dy <= tc.Radius; dy++)
                {
                    if (dx * dx + dy * dy > tc.Radius * tc.Radius)
                        continue;

                    var tile = new UnityEngine.Vector2Int(tc.LightPos.x + dx, tc.LightPos.y + dy);

                    if (isWall(tile))
                        continue;

                    if (LightSourceLogic.HasLineOfSight(tc.LightPos, tile, isWall))
                    {
                        NUnit.Framework.Assert.IsTrue(illuminated.Contains(tile),
                            $"Floor tile {tile} within radius with LOS should be illuminated. " + tc);
                    }
                }
            }
        }).QuickCheckThrowOnFailure();
    }
}

// ============================================================
// Feature: visual-bug-fixes
// Property 3: Diagonal wall gaps block line-of-sight
// Validates: Requirements 2.4
// ============================================================

[TestFixture]
public class DiagonalWallGapBlockingPropertyTests
{
    public struct DiagonalWallTestCase
    {
        public UnityEngine.Vector2Int Source;
        public UnityEngine.Vector2Int Target;
        public int GridSize;

        public override string ToString() =>
            $"Source={Source} Target={Target} GridSize={GridSize}";
    }

    private static Arbitrary<DiagonalWallTestCase> DiagonalWallArbitrary()
    {
        var gen = from size in Gen.Choose(5, 12)
                  from sx in Gen.Choose(1, size - 3)
                  from sy in Gen.Choose(1, size - 3)
                  from dirX in Gen.Elements(new[] { -1, 1 })
                  from dirY in Gen.Elements(new[] { -1, 1 })
                  where sx + dirX >= 0 && sx + dirX < size
                      && sy + dirY >= 0 && sy + dirY < size
                  select new DiagonalWallTestCase
                  {
                      Source = new UnityEngine.Vector2Int(sx, sy),
                      Target = new UnityEngine.Vector2Int(sx + dirX, sy + dirY),
                      GridSize = size
                  };
        return Arb.From(gen);
    }

    [Test]
    public void DiagonalWallGaps_BlockLineOfSight()
    {
        var arb = DiagonalWallArbitrary();

        Prop.ForAll(arb, tc =>
        {
            int dx = tc.Target.x - tc.Source.x;
            int dy = tc.Target.y - tc.Source.y;

            // Place walls at both cardinal neighbors of the diagonal step
            var hWall = new UnityEngine.Vector2Int(tc.Source.x + dx, tc.Source.y);
            var vWall = new UnityEngine.Vector2Int(tc.Source.x, tc.Source.y + dy);

            System.Func<UnityEngine.Vector2Int, bool> isWall = pos =>
                pos.x < 0 || pos.x >= tc.GridSize ||
                pos.y < 0 || pos.y >= tc.GridSize ||
                pos == hWall || pos == vWall;

            bool los = LightSourceLogic.HasLineOfSight(tc.Source, tc.Target, isWall);

            NUnit.Framework.Assert.IsFalse(los,
                $"LOS from {tc.Source} to {tc.Target} should be blocked by diagonal wall gap " +
                $"(walls at {hWall} and {vWall})");
        }).QuickCheckThrowOnFailure();
    }
}

// ============================================================
// Feature: visual-bug-fixes
// Property 4: Illumination is symmetric
// Validates: Requirements 2.5
// ============================================================

[TestFixture]
public class IlluminationSymmetryPropertyTests
{
    public struct SymmetricGridTestCase
    {
        public int Width;
        public int Height;
        public bool[,] Walls;
        public UnityEngine.Vector2Int LightPos;
        public int Radius;

        public System.Func<UnityEngine.Vector2Int, bool> GetIsWall()
        {
            var w = Width;
            var h = Height;
            var walls = Walls;
            return pos => pos.x < 0 || pos.x >= w ||
                          pos.y < 0 || pos.y >= h ||
                          walls[pos.x, pos.y];
        }

        public override string ToString() =>
            $"SymGrid({Width}x{Height}) Light={LightPos} Radius={Radius}";
    }

    private static Arbitrary<SymmetricGridTestCase> SymmetricGridArbitrary()
    {
        var gen = from width in Gen.Choose(5, 12)
                  from height in Gen.Choose(5, 12)
                  from radius in Gen.Choose(1, 5)
                  from seed in Arb.Generate<int>()
                  select BuildSymmetricGrid(width, height, radius, seed);
        return Arb.From(gen);
    }

    private static SymmetricGridTestCase BuildSymmetricGrid(int width, int height, int radius, int seed)
    {
        var rng = new System.Random(seed);
        var walls = new bool[width, height];

        // Light at center
        int cx = width / 2;
        int cy = height / 2;

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

        // Symmetric interior walls: mirror around center
        for (int x = 1; x <= cx; x++)
        {
            for (int y = 1; y <= cy; y++)
            {
                if (x == cx && y == cy) continue;
                bool isW = rng.NextDouble() < 0.15;
                int mx = 2 * cx - x;
                int my = 2 * cy - y;
                if (mx >= 0 && mx < width && my >= 0 && my < height)
                {
                    walls[x, y] = isW;
                    walls[mx, my] = isW;
                }
            }
        }

        walls[cx, cy] = false;

        return new SymmetricGridTestCase
        {
            Width = width,
            Height = height,
            Walls = walls,
            LightPos = new UnityEngine.Vector2Int(cx, cy),
            Radius = radius
        };
    }

    [Test]
    public void Illumination_IsSymmetric_OnSymmetricGrid()
    {
        var arb = SymmetricGridArbitrary();

        Prop.ForAll(arb, tc =>
        {
            var isWall = tc.GetIsWall();
            var illuminated = LightSourceLogic.GetRadiusIlluminatedTiles(tc.LightPos, tc.Radius, isWall);

            foreach (var tile in illuminated)
            {
                int dx = tile.x - tc.LightPos.x;
                int dy = tile.y - tc.LightPos.y;
                var mirror = new UnityEngine.Vector2Int(tc.LightPos.x - dx, tc.LightPos.y - dy);

                if (!isWall(mirror))
                {
                    NUnit.Framework.Assert.IsTrue(illuminated.Contains(mirror),
                        $"Tile {tile} (offset {dx},{dy}) is illuminated but mirror {mirror} " +
                        $"(offset {-dx},{-dy}) is not. " + tc);
                }
            }
        }).QuickCheckThrowOnFailure();
    }
}