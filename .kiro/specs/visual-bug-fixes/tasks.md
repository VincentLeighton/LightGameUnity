# Implementation Plan: Visual Bug Fixes

## Overview

Fix three visual bugs: sprite clipping by fog overlay (sorting order reorder), illumination gap (Bresenham LOS diagonal fix), and UI hidden behind fog (Canvas render mode). Changes touch `VisualConfig.cs`, `LightSourceLogic.cs`, the Canvas setup, and existing tests that reference old sorting order values.

## Tasks

- [x] 1. Fix sorting order hierarchy in VisualConfig
  - [x] 1.1 Update sorting order constants in `LightGame/Assets/Scripts/Core/VisualConfig.cs`
    - Change `FogSortOrder` from 10 to 3
    - Change `ElementSortOrder` from 3 to 4
    - Change `PlayerSortOrder` from 4 to 5
    - Verify the new chain: Floor(0) < Wall(1) < Beam(2) < Fog(3) < Element(4) < Player(5)
    - _Requirements: 1.4, 1.5, 1.6_

  - [x] 1.2 Update existing tests in `LightGame/Assets/Tests/EditMode/VisualUpdateTests.cs` that reference old sorting order values
    - Update `SortingOrderTests` to expect the new hierarchy with Fog between Beam and Element
    - Update `VisualConfigurationUnitTests` if any assert specific numeric values
    - _Requirements: 1.6_

  - [x] 1.3 Write property test for sorting order hierarchy
    - **Property 1: Sorting order hierarchy is strictly ascending with fog below elements**
    - Verify Floor < Wall < Beam < Fog < Element < Player
    - **Validates: Requirements 1.4, 1.5, 1.6**

- [x] 2. Fix Bresenham LOS diagonal wall gap in LightSourceLogic
  - [x] 2.1 Update `HasLineOfSight` in `LightGame/Assets/Scripts/Core/LightSourceLogic.cs`
    - Add diagonal step detection (when both stepX and stepY are true)
    - Check both cardinal neighbors at diagonal steps
    - Block LOS if both cardinal neighbors are walls (diagonal wall gap)
    - _Requirements: 2.1, 2.2, 2.3, 2.4_

  - [x] 2.2 Write property test for unobstructed floor tiles within radius are illuminated
    - **Property 2: All unobstructed floor tiles within radius are illuminated**
    - Reuse `LightGridArbitrary` generator from `LightSourceLogicTests.cs`
    - **Validates: Requirements 2.1, 2.2, 2.3**

  - [x] 2.3 Write property test for diagonal wall gap blocking
    - **Property 3: Diagonal wall gaps block line-of-sight**
    - Generate grids with diagonal wall configurations, verify LOS blocked when both cardinal neighbors are walls
    - **Validates: Requirements 2.4**

  - [x] 2.4 Write property test for illumination symmetry
    - **Property 4: Illumination is symmetric**
    - Generate wall-symmetric grids, verify illuminated set is symmetric around light source
    - **Validates: Requirements 2.5**

- [x] 3. Checkpoint - Run all existing and new tests
  - Ensure all tests pass, ask the user if questions arise.
  - Run `LightSourceLogicTests`, `FogOfWarTests`, `VisualUpdateTests`, and new `VisualBugFixTests` in Unity Test Runner

- [x] 4. Fix UI Canvas render mode
  - [x] 4.1 Ensure the UI Canvas uses Screen Space - Overlay render mode
    - If Canvas render mode is set in code, update to `RenderMode.ScreenSpaceOverlay`
    - If only set in Inspector, instruct user to change Canvas render mode to "Screen Space - Overlay" in the scene
    - _Requirements: 3.1, 3.2, 3.3_

- [x] 5. Final checkpoint - Verify all fixes
  - Ensure all tests pass, ask the user if questions arise.
  - Ask user to visually verify: player and light source sprites are no longer clipped, tiles above light source are illuminated, and UI is visible above fog

## Notes

- All new tests go in `LightGame/Assets/Tests/EditMode/VisualBugFixTests.cs`
- Reuse `LightGridArbitrary` from `LightSourceLogicTests.cs` for property test generators
- Tests must be run in Unity Editor Test Runner (not command line)
- Property tests use FsCheck with NUnit, minimum 100 iterations
