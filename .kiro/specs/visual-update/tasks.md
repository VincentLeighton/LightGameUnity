# Implementation Plan: Visual Update

## Overview

Incrementally add programmatic visuals to the Light Puzzle Game. Start with core configuration and sprite generation, then apply visuals to each game element, and finish with beam rendering polish and integration.

## Tasks

- [x] 1. Create core visual configuration classes
  - [x] 1.1 Create `LightGame/Assets/Scripts/Core/ColorPalette.cs`
    - Define a static class with readonly Color fields for all 10 named colors: Wall, Floor, Player, LightSource, BeamEmitter, MirrorSurface, ObjectiveUnlit, ObjectiveLit, Beam, Fog
    - Use a dark theme: Wall (#1a1a2e), Floor (#0f0f1a), Player (#4ecdc4), LightSource (#f9a825), BeamEmitter (#ff6f00), MirrorSurface (#b0bec5), ObjectiveUnlit (#4a148c at alpha 0.25), ObjectiveLit (#ffd600), Beam (#fff59d), Fog (black)
    - _Requirements: 8.1, 8.3_

  - [x] 1.2 Create `LightGame/Assets/Scripts/Core/VisualConfig.cs`
    - Define a static class with constants for: PlayerScale (0.5f), LightSourceScale (0.5f), BeamEmitterScale (0.5f), MirrorScale (0.5f), ObjectiveScale (0.5f)
    - Define sorting order constants: FloorSortOrder (0), WallSortOrder (1), BeamSortOrder (2), ElementSortOrder (3), PlayerSortOrder (4), FogSortOrder (10)
    - Define SpriteResolution (32) and BeamWidth (0.1f)
    - _Requirements: 1.1, 3.2, 4.3, 5.4, 6.4, 7.2, 10.1_

  - [x] 1.3 Create `LightGame/Assets/Scripts/Core/SpriteFactory.cs`
    - Implement static methods: CreateCircle, CreateDiamond, CreateTriangle, CreateRectangle, CreateSquare
    - Each method creates a Texture2D, fills pixels for the shape geometry, calls Apply(), and returns Sprite.Create with centered pivot
    - Clamp resolution to [1, 512], log warning for out-of-range values
    - _Requirements: 9.1, 9.2, 9.3, 9.4_

  - [x] 1.4 Write property tests for SpriteFactory and configuration
    - **Property 5: Sprite factory determinism** — For any valid resolution and color, calling the same method twice produces identical pixel data
    - **Validates: Requirements 9.3**
    - **Property 6: Sprite factory produces non-empty textures** — For any valid resolution and color with alpha > 0, output contains at least one non-transparent pixel
    - **Validates: Requirements 9.4**
    - **Property 4: Dark theme brightness constraint** — For Wall and Floor colors, HSV Value < 0.4
    - **Validates: Requirements 8.3**
    - **Property 7: Sorting orders are strictly ascending** — Floor < Wall < Beam < Element < Player < Fog
    - **Validates: Requirements 10.1**

- [x] 2. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 3. Apply visuals to player
  - [x] 3.1 Modify `PlayerController.cs` to apply visual configuration
    - In `Initialize()`, set `transform.localScale = Vector3.one * VisualConfig.PlayerScale`
    - In `Initialize()`, ensure a SpriteRenderer is attached with a circle sprite from SpriteFactory, colored with ColorPalette.Player, sorting order VisualConfig.PlayerSortOrder
    - Only create the sprite once (check if SpriteRenderer already exists)
    - _Requirements: 1.1, 1.2, 1.3_

  - [x] 3.2 Write property test for player centering
    - **Property 1: Player position is tile-centered** — For any tile position (x, y), TileToWorld returns (x+0.5, y+0.5, 0)
    - **Validates: Requirements 1.2**

- [x] 4. Apply visuals to level tiles and game elements
  - [x] 4.1 Modify `LevelManager.cs` to render wall and floor tile visuals
    - In `LoadLevelFromData()`, after building the tile grid, iterate all tiles and create GameObjects with SpriteRenderers
    - Wall tiles: square sprite from SpriteFactory, ColorPalette.Wall, sorting order VisualConfig.WallSortOrder, scale Vector3.one (full tile)
    - Floor tiles: square sprite from SpriteFactory, ColorPalette.Floor, sorting order VisualConfig.FloorSortOrder, scale Vector3.one (full tile)
    - Add tile GameObjects to _levelObjects for cleanup
    - _Requirements: 2.1, 2.2, 2.3, 2.4_

  - [x] 4.2 Modify `LevelManager.cs` to apply visuals to light sources
    - When creating light source GameObjects, add SpriteRenderer with circle sprite, ColorPalette.LightSource, sorting order VisualConfig.ElementSortOrder
    - Set localScale to Vector3.one * VisualConfig.LightSourceScale
    - _Requirements: 3.1, 3.2, 3.3_

  - [x] 4.3 Modify `LevelManager.cs` to apply visuals to beam emitters
    - When creating beam emitter GameObjects, add SpriteRenderer with triangle sprite, ColorPalette.BeamEmitter, sorting order VisualConfig.ElementSortOrder
    - Set localScale to Vector3.one * VisualConfig.BeamEmitterScale
    - Rotate sprite to match beam direction: compute angle from direction vector using Mathf.Atan2 and apply as Z rotation
    - _Requirements: 4.1, 4.2, 4.3_

  - [x] 4.4 Write property test for beam emitter rotation
    - **Property 2: Beam emitter rotation matches direction** — For any of the 8 grid directions, the computed rotation angle equals Atan2(dir.y, dir.x) in degrees
    - **Validates: Requirements 4.2**

  - [x] 4.5 Modify `LevelManager.cs` to apply visuals to mirrors
    - When creating mirror GameObjects, add SpriteRenderer with rectangle sprite, ColorPalette.MirrorSurface, sorting order VisualConfig.ElementSortOrder
    - Set localScale to Vector3.one * VisualConfig.MirrorScale
    - Set rotation to Quaternion.Euler(0, 0, -RotationIndex * 45f)
    - _Requirements: 5.1, 5.2, 5.4_

  - [x] 4.6 Modify `Mirror.cs` to update visual rotation on Rotate()
    - In `Rotate()`, after updating RotationIndex, set `transform.rotation = Quaternion.Euler(0, 0, -RotationIndex * 45f)`
    - _Requirements: 5.3_

  - [x] 4.7 Write property test for mirror visual rotation
    - **Property 3: Mirror visual rotation matches rotation index** — For any rotation index in [0,7], the Z-rotation equals -(index * 45) degrees
    - **Validates: Requirements 5.2, 5.3**

  - [x] 4.8 Modify `LevelManager.cs` to apply visuals to puzzle objectives
    - When creating puzzle objective GameObjects, add SpriteRenderer with diamond sprite, ColorPalette.ObjectiveUnlit, sorting order VisualConfig.ElementSortOrder
    - Set localScale to Vector3.one * VisualConfig.ObjectiveScale
    - Set initial alpha to 0.25 (unlit state)
    - _Requirements: 6.1, 6.2, 6.4_

  - [x] 4.9 Modify `PuzzleObjective.cs` to use ColorPalette for illumination states
    - In `UpdateIlluminationState()`, when illuminated: set sprite color to ColorPalette.ObjectiveLit with alpha 0.85
    - When not illuminated: set sprite color to ColorPalette.ObjectiveUnlit with alpha 0.25
    - Use the existing SpriteRenderer (or GlowRenderer) on the GameObject
    - _Requirements: 6.2, 6.3_

- [x] 5. Polish beam rendering
  - [x] 5.1 Modify `BeamRenderer.cs` to use visual configuration
    - Set BeamColor to ColorPalette.Beam
    - Set BeamWidth to VisualConfig.BeamWidth
    - Set sorting order on LineRenderers to VisualConfig.BeamSortOrder
    - _Requirements: 7.1, 7.2, 7.3_

- [x] 6. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

  - [x] 6.1 Write unit tests for visual configuration values
    - Verify all ColorPalette fields are non-default
    - Verify all scale factors are in [0.4, 0.6]
    - Verify objective unlit alpha ≤ 0.3 and lit alpha ≥ 0.7
    - Verify beam width is in [0.08, 0.15]
    - Verify wall and floor colors are visually distinct
    - _Requirements: 1.1, 3.2, 4.3, 5.4, 6.2, 6.3, 6.4, 7.2, 8.1_

- [x] 7. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Property tests validate universal correctness properties
- Unit tests validate specific examples and edge cases
- All tests run in Unity Editor Test Runner (not command line)
- SpriteFactory generates all visuals programmatically — no external art assets needed
