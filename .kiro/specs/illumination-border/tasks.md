# Implementation Plan: Illumination Border

## Overview

Implement a red border at the edge of light source illumination areas. Pure boundary logic in `Core/`, rendering in `Components/`, integrated with the existing `LightBeamSystem` event system.

## Tasks

- [x] 1. Create BorderEdge struct and IlluminationBorderLogic
  - [x] 1.1 Create `BorderEdge` struct in `LightGame/Assets/Scripts/Core/BorderEdge.cs`
    - Struct with `Vector2 Start` and `Vector2 End` fields
    - Implement `IEquatable<BorderEdge>` with direction-independent equality (A→B == B→A)
    - _Requirements: 1.4_

  - [x] 1.2 Create `IlluminationBorderLogic` static class in `LightGame/Assets/Scripts/Core/IlluminationBorderLogic.cs`
    - Implement `ComputeBorderEdges(HashSet<Vector2Int> illuminatedTiles, Func<Vector2Int, bool> isWall)` returning `List<BorderEdge>`
    - For each illuminated tile, check four cardinal neighbors (right, up, left, down)
    - Emit a border edge for each side where the neighbor is not illuminated or is a wall
    - Handle null inputs gracefully (empty set → empty list, null isWall → default false)
    - Use world-space coordinates: tile (x,y) has corners at (x,y), (x+1,y), (x+1,y+1), (x,y+1)
    - _Requirements: 1.1, 1.2, 1.3, 1.4_

  - [x] 1.3 Write property tests for IlluminationBorderLogic
    - [x] 1.3.1 Property 1: Border edge correctness and completeness
      - **Property 1: Border edge correctness and completeness**
      - **Validates: Requirements 1.1, 1.2, 1.3**
      - Generate random `HashSet<Vector2Int>` illuminated tiles and random wall sets
      - Verify output contains an edge iff it separates an illuminated tile from a non-illuminated/wall tile
    - [x] 1.3.2 Property 2: Border edge coordinate validity
      - **Property 2: Border edge coordinate validity**
      - **Validates: Requirements 1.4**
      - Generate random illuminated tile sets
      - Verify every edge has integer coordinates, is axis-aligned, and has length 1

  - [x] 1.4 Write unit tests for IlluminationBorderLogic
    - Test single tile produces 4 edges
    - Test two adjacent tiles produce 6 edges
    - Test empty set produces 0 edges
    - _Requirements: 1.1, 1.2, 1.3_

- [x] 2. Update VisualConfig and create IlluminationBorderRenderer
  - [x] 2.1 Update `VisualConfig` sort order constants
    - Add `BorderSortOrder = 4`
    - Bump `ElementSortOrder` to 5 and `PlayerSortOrder` to 6
    - _Requirements: 2.4_

  - [x] 2.2 Create `IlluminationBorderRenderer` MonoBehaviour in `LightGame/Assets/Scripts/Components/IlluminationBorderRenderer.cs`
    - Public fields: `LightBeamSystem BeamSystem`, `float BorderWidth`, `Color BorderColor`
    - Default `BorderWidth` to `2f / VisualConfig.SpriteResolution` (2px in world units)
    - Default `BorderColor` to `Color.red`
    - Subscribe to `BeamSystem.OnIlluminationChanged` in `OnEnable`, unsubscribe in `OnDisable`
    - On illumination change: call `IlluminationBorderLogic.ComputeBorderEdges`, pass `BeamSystem.IsWall`
    - Render each `BorderEdge` using pooled `LineRenderer` objects (same pattern as `BeamRenderer`)
    - Set `sortingOrder` to `VisualConfig.BorderSortOrder`
    - Clean up LineRenderers in `OnDestroy`
    - _Requirements: 2.1, 2.2, 2.3, 2.4, 2.5, 3.1, 3.3_

  - [x] 2.3 Write unit test for VisualConfig sort order
    - Assert `FogSortOrder < BorderSortOrder < ElementSortOrder < PlayerSortOrder`
    - _Requirements: 2.4_

- [x] 3. Integrate with LevelManager
  - [x] 3.1 Wire `IlluminationBorderRenderer` into the level setup
    - In `LevelManager` (or wherever `LightBeamSystem` and `BeamRenderer` are wired), add and configure `IlluminationBorderRenderer`
    - Ensure the renderer receives the initial illumination state after level load
    - _Requirements: 3.1, 3.2_

## Notes

- Pure logic in `Core/` is fully testable in EditMode without Unity runtime
- The renderer follows the same pooled-LineRenderer pattern as `BeamRenderer`
- Property tests use FsCheck (already available via `LightGame/Assets/Plugins/FsCheck.dll`)
