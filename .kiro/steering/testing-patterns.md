---
inclusion: fileMatch
fileMatchPattern: '*Tests*.cs'
---

# Testing Patterns for Light Puzzle Game

## Property-Based Testing with FsCheck

- Library: FsCheck with NUnit integration
- Import: `using FsCheck;` and `using NUnit.Framework;`
- Pattern: `Prop.ForAll(arbitrary, property).QuickCheckThrowOnFailure();`
- Each test method uses `[Test]` attribute (NUnit), not FsCheck's `[Property]` attribute — we call FsCheck manually for better control.

## Existing Generators

`LevelDataArbitrary` (in `LevelDataTests.cs`) generates valid random `LevelData` objects:
- Grid size 3-10, border walls, ~25% interior walls
- Guarantees floor tiles at (1,1) and (2,1) for player start
- Random light sources (1-3), beam emitters (1-2), mirrors (0-3), objectives (1-2)
- All objects placed on floor tiles within bounds

Reuse this generator when testing anything that needs a valid level. Don't duplicate it.

## Test File Naming

- One test file per logical area: `{Feature}Tests.cs`
- Place in `LightGame/Assets/Tests/EditMode/`
- Tag with comment: `// Feature: light-puzzle-game, Property N: Description`

## Mutation Testing Pattern

When testing that validation rejects bad input, start from a valid generated level and mutate one thing:
- Set `beamEmitters = new BeamEmitterData[0]` to test missing emitters
- Set `playerStart` to a wall position `(0,0)` (borders are always walls)
- Set positions to `width+5, height+5` for out-of-bounds
- Replace a tile value with an invalid string for bad tile tests
- When mutating tiles, skip tiles occupied by player start or game objects to avoid cascading validation errors

## Key Gotcha: JsonUtility Limitations

Unity's `JsonUtility` cannot handle:
- Jagged arrays directly (hence `TileRow` wrapper)
- Polymorphic types
- Dictionary types
- Null fields in some cases — prefer empty arrays over null

When writing serialization round-trip tests, use `JsonUtility.ToJson()` / `JsonUtility.FromJson<T>()`.
