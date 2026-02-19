# Implementation Plan: Light Puzzle Game

## Overview

Build a top-down puzzle game in Unity for Android where the player uses light sources and mirrors to illuminate dark maze-like levels. Implementation follows an incremental approach: data models and pure logic first (testable without Unity runtime), then Unity components, then integration and UI.

## Tasks

- [ ] 1. Set up Unity project structure and core data types
  - [x] 1.1 Create Unity project with 2D template, set Android build target, configure portrait orientation
    - ⚠️ **REQUIRES HUMAN INPUT — Unity Editor steps:**
      1. Open Unity Hub → click "New Project"
      2. Select the "2D (URP)" or "2D" template
      3. Name the project (e.g. `LightPuzzleGame`), pick a location, click "Create project"
      4. Once open, go to File → Build Settings → select "Android" → click "Switch Platform"
      5. Go to Edit → Project Settings → Player → Resolution and Presentation → set Default Orientation to "Portrait"
      6. In the Project window, create folders (right-click → Create → Folder):
         - `Assets/Scripts/Core/`
         - `Assets/Scripts/Components/`
         - `Assets/Scripts/UI/`
         - `Assets/Levels/`
         - `Assets/Tests/EditMode/`
         - `Assets/Tests/PlayMode/`
      7. Install FsCheck: open a terminal in the project root and run `dotnet add package FsCheck` and `dotnet add package FsCheck.NUnit`, or use the NuGet For Unity package from the Asset Store
      8. For the Test Runner: go to Window → General → Test Runner. Add Assembly Definition files (`.asmdef`) in each test folder so Unity auto-detects them
    - _Requirements: 11.5_

  - [x] 1.2 Define core enums, constants, and data model classes
    - Create `GameState` enum (MainMenu, Playing, Paused, LevelComplete, GameComplete)
    - Create `InteractionMode` enum (Movement, MirrorPlacement)
    - Create `GridDirections` static class with all 8 direction vectors
    - Create `LevelData` serializable class with tiles (string[][]), playerStart, lightSources, beamEmitters, mirrors, puzzleObjectives
    - Create `SaveData` serializable class with currentLevelIndex and completedLevels
    - Create `BeamSegment` struct with Start, End, Direction
    - _Requirements: 7.1, 7.2, 7.3, 8.1_

  - [x] 1.3 Write property tests for LevelData serialization round-trip
    - **Property 9: Level data serialization round-trip**
    - Create LevelDataGenerator that produces valid random LevelData objects
    - Verify serialize-then-deserialize produces equivalent objects
    - **Validates: Requirements 7.3**

  - [x] 1.4 Write property tests for level validation
    - **Property 10: Level validation correctness**
    - Test that validation accepts valid levels and rejects levels missing required elements
    - Test invalid tile values, missing beam emitters, missing objectives, out-of-bounds positions
    - **Validates: Requirements 6.1, 7.1, 7.2, 7.4**

- [x] 2. Implement pathfinding
  - [x] 2.1 Implement A* pathfinder as a static utility class
    - Create `Pathfinder.FindPath(start, end, isWalkable)` returning `List<Vector2Int>`
    - Use Manhattan distance heuristic for grid-based movement
    - Support 4-directional movement (up, down, left, right)
    - Return empty list if no path exists
    - _Requirements: 1.1, 1.2_

  - [x] 2.2 Write property tests for pathfinding correctness
    - **Property 1: Pathfinding correctness**
    - Create GridGenerator that produces random grids with connected floor regions
    - Verify returned paths contain only walkable tiles with adjacent consecutive tiles
    - Verify shortest path property (no shorter path exists)
    - **Validates: Requirements 1.1, 1.2**

- [x] 3. Implement light and beam systems (pure logic)
  - [x] 3.1 Implement radius illumination with shadow casting
    - Create `LightSourceLogic.GetRadiusIlluminatedTiles(position, radius, isWall)` returning `HashSet<Vector2Int>`
    - Use Bresenham line-of-sight check from source to each tile within radius
    - Wall tiles block line-of-sight; only floor tiles with clear LOS are illuminated
    - _Requirements: 2.1, 2.2_

  - [x] 3.2 Write property tests for radius illumination
    - **Property 2: Radius illumination with shadow casting**
    - Generate random grids with light source positions and radii
    - Verify floor tiles within radius with LOS are illuminated, blocked tiles are not
    - **Validates: Requirements 2.1, 2.2**

  - [x] 3.3 Implement mirror reflection lookup table
    - Create `ReflectionTable` static class with precomputed 8x8 reflection results
    - Map each (incomingDirectionIndex, mirrorRotationIndex) to outgoingDirectionIndex
    - Implement `GetReflectedDirection(incomingDirection, mirrorRotationIndex)` method
    - _Requirements: 3.5, 8.1, 8.2_

  - [x] 3.4 Write property tests for mirror reflection
    - **Property 6: Mirror reflection correctness**
    - For all 64 combinations of incoming direction × mirror angle, verify law of reflection holds
    - Verify output is always one of the 8 valid grid directions
    - **Validates: Requirements 3.5, 8.2**

  - [x] 3.5 Write property tests for mirror rotation invariant
    - **Property 8: Mirror rotation invariant**
    - For any starting rotation index and any number of rotations, verify index stays in [0,7]
    - Verify 8 rotations returns to original angle
    - **Validates: Requirements 4.3, 8.1**

  - [x] 3.6 Implement beam tracing algorithm
    - Create `BeamTracer.TraceBeams(beamEmitters, mirrors, isWall, illuminatedByLightSource, maxReflections)` returning `(HashSet<Vector2Int> illuminatedTiles, List<BeamSegment> segments)`
    - First filter beam emitters to only active ones (tile illuminated by a light source)
    - For each active emitter, trace beam tile-by-tile: stop at walls, reflect at mirrors using ReflectionTable
    - Cap reflections at maxReflections (default 20)
    - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 8.3, 8.4, 8.5_

  - [x] 3.7 Write property tests for beam tracing
    - **Property 4: Beam tracing correctness**
    - Generate grids with beam emitters and mirrors, verify beam segments are straight lines in valid directions
    - Verify segments start at emitters/mirrors and end at walls/mirrors
    - Verify dormant emitters produce no segments
    - **Validates: Requirements 3.1, 3.2, 3.4, 8.3**

  - [x] 3.8 Write property tests for beam emitter activation
    - **Property 5: Beam emitter activation**
    - Generate grids with light sources and beam emitters at various positions
    - Verify emitter is active iff its tile is in a light source's illuminated set
    - **Validates: Requirements 3.1, 3.2, 3.3**

  - [x] 3.9 Write property tests for beam reflection loop cap
    - **Property 11: Beam reflection loop cap**
    - Create mirror configurations that form reflection loops (mirrors facing each other)
    - Verify beam tracing terminates with at most 20 segments
    - **Validates: Requirements 8.5**

- [x] 4. Checkpoint - Ensure all core logic tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 5. Implement inventory and puzzle completion logic
  - [x] 5.1 Implement Inventory class
    - Create `Inventory` with AddMirror(), RemoveMirror() (returns false if empty), MirrorCount property
    - Fire OnMirrorCountChanged event when count changes
    - _Requirements: 4.1, 4.2, 4.4, 4.5_

  - [x] 5.2 Write property tests for inventory round-trip
    - **Property 7: Inventory round-trip**
    - For random sequences of add/remove operations, verify count equals adds minus successful removes
    - Verify pickup-then-place-then-pickup leaves count unchanged
    - **Validates: Requirements 4.1, 4.2, 4.4**

  - [x] 5.3 Implement puzzle completion check and remaining count
    - Create `PuzzleChecker.IsLevelComplete(objectives, illuminatedTiles)` returning bool
    - Create `PuzzleChecker.GetRemainingCount(objectives, illuminatedTiles)` returning int
    - _Requirements: 6.2, 10.2_

  - [x] 5.4 Write property tests for level completion and remaining count
    - **Property 13: Level completion check**
    - **Property 14: Remaining objectives count**
    - Generate random objective positions and illuminated tile sets
    - Verify completion iff all objectives illuminated; remaining count equals un-illuminated objectives
    - **Validates: Requirements 6.2, 10.2**

- [x] 6. Implement Unity components - Player and Camera
  - [x] 6.1 Implement PlayerController MonoBehaviour
    - Attach to Player GameObject; use Pathfinder for tap-to-move
    - Animate movement at 5 tiles/second along path
    - Support path recalculation on new tap during movement
    - Fire OnPlayerMoved event when reaching each tile
    - ⚠️ **REQUIRES HUMAN INPUT — Unity Editor steps (after writing the script):**
      1. In the Hierarchy, right-click → 2D Object → Sprite to create the Player GameObject
      2. Rename it "Player", assign a placeholder sprite (simple square or circle)
      3. Drag the `PlayerController` script onto the Player GameObject in the Inspector
    - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5_

  - [x] 6.2 Implement CameraController MonoBehaviour
    - Smooth follow player position using Vector3.Lerp
    - Set orthographic projection for top-down view
    - Clamp camera position within level bounds
    - Handle pinch-to-zoom with min/max orthographic size clamping
    - ⚠️ **REQUIRES HUMAN INPUT — Unity Editor steps (after writing the script):**
      1. Select "Main Camera" in the Hierarchy
      2. Drag the `CameraController` script onto it in the Inspector
      3. Verify the camera's Projection is set to "Orthographic" (should be default with 2D template)
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5_

  - [x] 6.3 Write property tests for camera bounds clamping
    - **Property 12: Camera bounds clamping**
    - For random player positions and level bounds, verify camera stays within bounds
    - For random zoom inputs, verify orthographic size stays within min/max
    - **Validates: Requirements 9.3, 9.4**

- [x] 7. Implement Unity components - Light, Beam, Fog, and Mirror
  - [x] 7.1 Implement LightSource MonoBehaviour
    - Attach to light source GameObjects; configure TilePosition and IlluminationRadius
    - Call LightSourceLogic.GetRadiusIlluminatedTiles for illumination calculation
    - _Requirements: 2.1, 2.2_

  - [x] 7.2 Implement BeamEmitter MonoBehaviour
    - Attach to beam emitter GameObjects; configure TilePosition and BeamDirection
    - Track IsActive state based on whether tile is illuminated by a LightSource
    - Fire OnActiveStateChanged event when activation state changes
    - _Requirements: 3.1, 3.2, 3.3_

  - [x] 7.3 Implement LightBeamSystem MonoBehaviour
    - Orchestrate all illumination: collect light source radii + beam tracer results
    - Expose GetIlluminatedTiles() returning union of all illuminated tiles
    - Expose GetBeamSegments() for beam rendering
    - RecalculateAllBeams() when mirrors or light sources change
    - Fire OnIlluminationChanged event
    - _Requirements: 2.3, 2.4, 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7_

  - [x] 7.4 Implement FogOfWarSystem MonoBehaviour
    - Create render texture sized to level grid (one pixel per tile)
    - Listen to LightBeamSystem.OnIlluminationChanged
    - Lerp tile alpha over 0.3 seconds for fade-in/fade-out transitions
    - Apply fog texture via full-screen shader overlay
    - ⚠️ **REQUIRES HUMAN INPUT — Unity Editor steps (after writing the script and shader):**
      1. In the Project window, right-click → Create → Shader → Unlit Shader, name it "FogOfWar"
      2. Edit the shader to sample the fog render texture and darken tiles accordingly
      3. Create a Material using this shader (right-click → Create → Material, assign the FogOfWar shader)
      4. In the scene, create a full-screen quad or use a Canvas with a RawImage to display the fog overlay
      5. Assign the material reference in the Inspector on the FogOfWarSystem component
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5_

  - [x] 7.5 Write property tests for fog-illumination invariant
    - **Property 3: Fog-illumination invariant**
    - Verify fog visible set equals illuminated tile set after updates
    - **Validates: Requirements 2.3, 2.4, 3.4, 5.1, 5.5**

  - [x] 7.6 Implement Mirror MonoBehaviour and MirrorInteraction
    - Mirror: TilePosition, RotationIndex, Rotate() with wraparound, GetReflectedDirection using ReflectionTable
    - MirrorInteraction: TryPlaceMirror (check empty floor tile, decrement inventory), TryRotateMirror (tap), TryPickupMirror (long-press, increment inventory)
    - Trigger LightBeamSystem.RecalculateAllBeams on any mirror change
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7_

  - [x] 7.7 Implement PuzzleObjective MonoBehaviour
    - Track IsIlluminated state from LightBeamSystem illuminated tiles
    - Render subtle faint glow sprite visible even in dark areas
    - Fire OnStateChanged event
    - _Requirements: 6.1, 6.5_

- [x] 8. Checkpoint - Ensure all component tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 9. Implement InputManager and game flow
  - [x] 9.1 Implement InputManager MonoBehaviour
    - Detect single tap (raycast to tile position), long press (200ms threshold), and pinch-to-zoom (two-touch)
    - Route taps to PlayerController (movement mode) or MirrorInteraction (placement mode)
    - Route long press to MirrorInteraction.TryPickupMirror
    - Route pinch to CameraController.HandlePinchZoom
    - Debounce mirror rotation at 200ms minimum interval
    - Fire OnTileTapped, OnTileLongPressed, OnPinchZoom events
    - _Requirements: 1.1, 1.4, 1.6, 4.2, 4.3, 4.4, 9.4_

  - [x] 9.2 Implement LevelManager MonoBehaviour
    - Load level JSON from Assets/Levels/ by index
    - Deserialize into LevelData, validate using validation logic
    - Instantiate Grid (Tilemap), Player, LightSources, BeamEmitters, Mirrors, PuzzleObjectives from LevelData
    - ClearLevel() destroys all level objects
    - Handle validation failure: show error, return to level select
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 7.5_

  - [x] 9.3 Implement GameManager singleton MonoBehaviour
    - DontDestroyOnLoad, manage GameState transitions
    - LoadLevel via LevelManager, handle level completion (2-second delay then next level)
    - Handle game-complete when no more levels
    - PauseGame/ResumeGame toggling Time.timeScale
    - SaveProgress/LoadProgress using PlayerPrefs JSON serialization
    - Handle Android back button (Input.GetKeyDown(KeyCode.Escape)) to pause
    - Handle OnApplicationPause for background save/resume
    - _Requirements: 6.2, 6.3, 6.4, 11.2, 11.3, 11.4_

  - [x] 9.4 Write property tests for save/load round-trip
    - **Property 15: Save/load round-trip**
    - Generate random SaveData objects, serialize then deserialize, verify equivalence
    - **Validates: Requirements 11.4**

- [x] 10. Implement UI
  - [x] 10.1 Create Canvas with UI elements
    - ⚠️ **REQUIRES HUMAN INPUT — Unity Editor steps:**
      1. In the Hierarchy, right-click → UI → Canvas
      2. Select the Canvas → in Inspector set Canvas Scaler to "Scale With Screen Size", reference resolution 1080x1920 (portrait)
      3. Right-click the Canvas → UI → Panel for InventoryPanel. Add a Text (TextMeshPro) child showing mirror count
      4. Add another Panel for ObjectivePanel with a Text child for remaining objectives count
      5. Add a Button (UI → Button - TextMeshPro) for the ModeToggleButton. Set its size to at least 48x48
      6. Create a Panel for PauseMenu with a Resume button inside it. Set it inactive by default
      7. Create panels for LevelCompletePanel and GameCompletePanel, also set inactive by default
      8. Drag the `UIManager` script onto the Canvas and wire up references to each panel/button in the Inspector
    - InventoryPanel: display mirror count, update on OnMirrorCountChanged
    - ObjectivePanel: display remaining objectives count, update on illumination changes
    - ModeToggleButton: switch between Movement and MirrorPlacement modes via InputManager.SetMode
    - PauseMenu: resume button, shown on GameState.Paused
    - LevelCompletePanel: shown on level completion
    - GameCompletePanel: shown when all levels finished
    - All touch targets minimum 48x48dp
    - Use Canvas Scaler with "Scale With Screen Size" for responsive layout
    - _Requirements: 10.1, 10.2, 10.3, 10.4, 10.5, 10.6, 10.7_

- [x] 11. Create sample levels and beam rendering
  - [x] 11.1 Create 3 tutorial levels as JSON files
    - Level 1: Simple — one light source, one beam emitter, one mirror, one objective. Teaches basic mechanics.
    - Level 2: Medium — introduces beam emitter activation (emitter starts in dark, player must position light source). Two mirrors.
    - Level 3: Harder — multiple beam emitters, mirror chains, objectives in hard-to-reach corners.
    - Place in Assets/Levels/ as level_0.json, level_1.json, level_2.json
    - _Requirements: 7.1, 7.2, 7.3_

  - [x] 11.2 Implement beam visual rendering
    - Use LineRenderer components to draw beam segments from LightBeamSystem.GetBeamSegments()
    - Update visuals on OnIlluminationChanged
    - Use a bright color (white/yellow) with slight glow effect
    - ⚠️ **REQUIRES HUMAN INPUT — Unity Editor steps (after writing the script):**
      1. Create an empty GameObject called "BeamRenderer"
      2. Add a LineRenderer component (Add Component → Line Renderer)
      3. In the Inspector, set the material to a bright white/yellow, set width to ~0.05
      4. Drag the BeamRenderer from Hierarchy into the Project window to save it as a prefab
      5. The code will manage creating/updating LineRenderers at runtime using this prefab
    - _Requirements: 3.1, 3.4_

- [-] 12. Implement start screen
  - [x] 12.1 Create StartScreenPanel UI and wire to GameManager
    - Add a StartScreenPanel to the Canvas with a game title (TextMeshPro) and a "Start Game" button (minimum 48x48dp touch target)
    - In UIManager, add a reference to StartScreenPanel and subscribe to GameManager.OnStateChanged
    - Show StartScreenPanel and hide gameplay UI (InventoryPanel, ObjectivePanel, ModeToggleButton) when state is MainMenu
    - Hide StartScreenPanel and show gameplay UI when state transitions away from MainMenu
    - Wire the "Start Game" button's onClick to call GameManager.Instance.StartGame()
    - ⚠️ **REQUIRES HUMAN INPUT — Unity Editor steps (after writing the script changes):**
      1. In the Canvas, right-click → UI → Panel, name it "StartScreenPanel"
      2. Add a TextMeshPro child for the game title
      3. Add a Button (TextMeshPro) child, set text to "Start Game", ensure size is at least 48x48
      4. In the UIManager Inspector, drag the StartScreenPanel reference
      5. Wire the button's OnClick to call GameManager.Instance.StartGame() (or let the code handle it)
    - _Requirements: 12.1, 12.2, 12.3, 12.4_

  - [x] 12.2 Write property test for UI panel visibility per game state
    - **Property 16: UI panel visibility per game state**
    - For each GameState value, verify the correct panels are shown/hidden
    - Verify StartScreenPanel is visible only during MainMenu state
    - **Validates: Requirements 12.1, 12.3**

- [ ] 13. Integration and final wiring
  - [x] 13.1 Wire all systems together in the main scene
    - ⚠️ **REQUIRES HUMAN INPUT — Unity Editor steps:**
      1. Set up the scene hierarchy per design: GameManager at root level, LevelRoot as a container for level objects, MainCamera at root
      2. On the GameManager GameObject, verify `DontDestroyOnLoad` is called in Awake (test by entering Play mode and checking it persists)
      3. Use the Inspector to drag-and-drop references between components:
         - Wire InputManager events to PlayerController, MirrorInteraction, CameraController
         - Wire LightBeamSystem.OnIlluminationChanged to FogOfWarSystem, PuzzleObjectives, and UI
         - Wire Inventory.OnMirrorCountChanged to the InventoryPanel UI
         - Wire PuzzleChecker completion to GameManager.OnLevelComplete
      4. Enter Play mode and test the full game loop: load level → move → place mirrors → illuminate objectives → level complete → next level
    - Connect InputManager events to PlayerController, MirrorInteraction, CameraController
    - Connect LightBeamSystem.OnIlluminationChanged to FogOfWarSystem, PuzzleObjectives, UI
    - Connect Inventory.OnMirrorCountChanged to UI
    - Connect PuzzleChecker completion to GameManager.OnLevelComplete
    - Test full game loop: load level → move → place mirrors → illuminate objectives → level complete → next level
    - _Requirements: All_

  - [x] 13.2 Configure Android build settings
    - ⚠️ **REQUIRES HUMAN INPUT — Unity Editor steps:**
      1. Go to File → Build Settings → Player Settings
      2. Set Company Name, Product Name, Package Name (e.g. `com.yourname.lightpuzzle`)
      3. Under Other Settings, set Minimum API Level to API 21 (Android 5.0) or higher
      4. Set Target API Level to the latest available
      5. Under Resolution and Presentation, confirm portrait-only orientation
      6. Set the Version and Bundle Version Code
      7. Click "Build" or "Build and Run" with an Android device connected or emulator running
      8. Test the build on the device to verify touch input, performance, and orientation
    - Set minimum API level, target API level
    - Configure portrait-only orientation
    - Set application identifier and version
    - Test build on Android device or emulator
    - _Requirements: 11.1, 11.5_

- [x] 14. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP
- Each task references specific requirements for traceability
- Checkpoints ensure incremental validation
- Pure logic (pathfinding, beam tracing, reflection, validation) is implemented first as static/utility classes so it can be tested without Unity runtime
- Unity MonoBehaviours wrap the pure logic and handle rendering, input, and lifecycle
- Property tests validate universal correctness properties; unit tests validate specific examples and edge cases
