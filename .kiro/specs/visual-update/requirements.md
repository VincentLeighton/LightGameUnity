# Requirements Document

## Introduction

The Light Puzzle Game currently uses placeholder sprites (simple squares and circles) for all game elements. The player character appears too large relative to the tile grid. This feature covers a visual overhaul: scaling game elements correctly, replacing placeholders with proper programmatic visuals, and establishing a cohesive dark-themed aesthetic suitable for a light-and-shadow puzzle game on Android.

## Glossary

- **Visual_System**: The collection of scripts and configurations responsible for rendering game element appearances, including sprite generation, color assignment, and scaling.
- **Player_Visual**: The visual representation of the player character on the grid.
- **Tile_Visual**: The visual representation of wall and floor tiles rendered on the tilemap.
- **Element_Visual**: The visual representation of interactive game objects (light sources, beam emitters, mirrors, puzzle objectives).
- **Sprite_Factory**: A utility that programmatically generates Texture2D and Sprite assets at runtime for game elements.
- **Scale_Factor**: The ratio between a game element's rendered size and the tile cell size (1 Unity unit).
- **Color_Palette**: A centralized set of color definitions used across all game visuals to maintain consistency.

## Requirements

### Requirement 1: Player Scaling

**User Story:** As a player, I want my character to fit naturally within the tile grid, so that movement feels precise and the environment reads clearly.

#### Acceptance Criteria

1. THE Visual_System SHALL render the Player_Visual at a Scale_Factor no greater than 0.6 relative to the tile cell size.
2. WHEN the player moves between tiles, THE Player_Visual SHALL remain visually centered within each tile during and after movement.
3. WHEN a level is loaded, THE Visual_System SHALL apply the player scale before the first frame is rendered to the user.

### Requirement 2: Wall and Floor Visuals

**User Story:** As a player, I want walls and floors to look distinct and atmospheric, so that I can easily read the maze layout.

#### Acceptance Criteria

1. THE Visual_System SHALL render wall tiles using a dark, solid color from the Color_Palette that is visually distinct from floor tiles.
2. THE Visual_System SHALL render floor tiles using a subtle, muted color from the Color_Palette that contrasts with wall tiles.
3. WHEN a level is loaded, THE Tile_Visual for every tile in the grid SHALL be rendered before gameplay begins.
4. THE Visual_System SHALL assign wall tiles a higher sorting order than floor tiles so that walls render on top of adjacent floor edges.

### Requirement 3: Light Source Visuals

**User Story:** As a player, I want light sources to look like glowing objects, so that I can identify them as sources of illumination.

#### Acceptance Criteria

1. THE Visual_System SHALL render each light source with a bright warm-colored sprite from the Color_Palette.
2. THE Visual_System SHALL render each light source at a Scale_Factor between 0.4 and 0.6 relative to the tile cell size.
3. THE Sprite_Factory SHALL generate a circular sprite for light source objects.

### Requirement 4: Beam Emitter Visuals

**User Story:** As a player, I want beam emitters to be visually recognizable and indicate their firing direction, so that I can plan mirror placements.

#### Acceptance Criteria

1. THE Visual_System SHALL render each beam emitter with a distinct sprite that differs from light sources and mirrors.
2. THE Visual_System SHALL rotate the beam emitter sprite to match the beam emission direction.
3. THE Visual_System SHALL render each beam emitter at a Scale_Factor between 0.4 and 0.6 relative to the tile cell size.

### Requirement 5: Mirror Visuals

**User Story:** As a player, I want mirrors to clearly show their orientation, so that I can understand how beams will reflect.

#### Acceptance Criteria

1. THE Visual_System SHALL render each mirror with a rectangular or elongated sprite that visually conveys a reflective surface.
2. THE Visual_System SHALL rotate the mirror sprite to match the current RotationIndex (0–7, in 45° increments).
3. WHEN a mirror is rotated, THE Visual_System SHALL update the mirror sprite rotation to reflect the new RotationIndex.
4. THE Visual_System SHALL render each mirror at a Scale_Factor between 0.4 and 0.6 relative to the tile cell size.

### Requirement 6: Puzzle Objective Visuals

**User Story:** As a player, I want puzzle objectives to be visible even in dark areas and clearly change appearance when illuminated, so that I know what to aim for.

#### Acceptance Criteria

1. THE Visual_System SHALL render each puzzle objective with a distinct sprite that differs from other game elements.
2. WHEN a puzzle objective is not illuminated, THE Visual_System SHALL render the objective with a dim glow at an alpha no greater than 0.3.
3. WHEN a puzzle objective becomes illuminated, THE Visual_System SHALL render the objective with a bright glow at an alpha of at least 0.7.
4. THE Visual_System SHALL render each puzzle objective at a Scale_Factor between 0.4 and 0.6 relative to the tile cell size.

### Requirement 7: Beam Rendering Polish

**User Story:** As a player, I want light beams to look like actual beams of light rather than thin debug lines, so that the game feels polished.

#### Acceptance Criteria

1. THE Visual_System SHALL render beam segments with a warm glow color from the Color_Palette.
2. THE Visual_System SHALL render beam segments with a line width between 0.08 and 0.15 Unity units.
3. THE Visual_System SHALL assign beam segments a sorting order that renders them above floor tiles and below game element sprites.

### Requirement 8: Color Palette and Cohesion

**User Story:** As a player, I want the game to have a consistent dark-themed color scheme, so that the light-and-shadow theme feels immersive.

#### Acceptance Criteria

1. THE Color_Palette SHALL define named color constants for: wall, floor, player, light source, beam emitter, mirror, puzzle objective unlit, puzzle objective lit, beam, and fog.
2. THE Visual_System SHALL reference only Color_Palette constants when assigning colors to game elements.
3. THE Color_Palette SHALL use a dark background theme where floor and wall colors have low brightness values (Value channel below 0.4 in HSV).

### Requirement 9: Sprite Factory

**User Story:** As a developer, I want all sprites generated programmatically at runtime, so that the game requires no external art assets.

#### Acceptance Criteria

1. THE Sprite_Factory SHALL generate Texture2D assets at runtime for all game element sprites.
2. THE Sprite_Factory SHALL produce a circle sprite, a diamond/rhombus sprite, a directional triangle sprite, a rectangle sprite, and a square sprite.
3. WHEN given the same shape parameters, THE Sprite_Factory SHALL produce visually identical sprites across invocations.
4. FOR ALL valid shape parameters, generating a sprite then reading back its pixel data SHALL produce non-empty texture data (round-trip integrity).

### Requirement 10: Sorting Order Consistency

**User Story:** As a developer, I want a clear rendering order for all visual layers, so that elements never occlude each other incorrectly.

#### Acceptance Criteria

1. THE Visual_System SHALL assign sorting orders in ascending visual priority: floor, walls, beams, game elements (light sources, emitters, mirrors, objectives), player, fog overlay.
2. WHEN any two game elements occupy adjacent tiles, THE Visual_System SHALL render each element at the correct sorting layer without z-fighting or overlap artifacts.
