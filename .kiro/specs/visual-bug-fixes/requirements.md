# Requirements Document

## Introduction

This document captures the requirements for fixing three visual bugs discovered after the visual-update spec was completed in the Light Puzzle Game. Bug 1 involves game element sprites (player, light source) being visually clipped by the fog-of-war overlay on adjacent fogged tiles. Bug 2 involves an illumination gap where tiles directly above the light source appear dark despite no walls blocking line-of-sight. Bug 3 involves the UI (Canvas) being hidden behind the fog overlay.

## Glossary

- **Fog_Overlay**: The full-screen rendering layer that hides unilluminated tiles with an opaque black overlay. Renders at sorting order 10 using a shader-based render texture approach.
- **Element_Sprite**: A SpriteRenderer attached to a game element (player, light source, beam emitter, mirror, puzzle objective) rendered at a scale of 0.5 relative to the 1-unit tile size.
- **Sorting_Order**: An integer value controlling the draw order of Unity SpriteRenderers. Higher values render on top of lower values.
- **Illuminated_Tile**: A floor tile that is within a light source's radius and has unobstructed line-of-sight from the light source, as computed by LightSourceLogic.
- **Bresenham_LOS**: The line-of-sight algorithm used in LightSourceLogic.HasLineOfSight that traces a Bresenham line from source to target, returning false if any intermediate tile is a wall.
- **Fog_Texture**: A RenderTexture with one pixel per grid tile, where each pixel's alpha channel controls fog opacity (1 = fully fogged, 0 = fully visible).
- **UI_Canvas**: The Unity Canvas component that hosts all UI panels (inventory, objectives, mode toggle, pause menu, level complete, game complete screens).

## Requirements

### Requirement 1: Element sprites render above the fog overlay

**User Story:** As a player, I want to see game element sprites fully visible on illuminated tiles, so that the player character and light sources are not visually clipped or obscured by the fog overlay.

#### Acceptance Criteria

1. WHILE an Element_Sprite occupies an Illuminated_Tile, THE Fog_Overlay SHALL NOT visually obscure any part of the Element_Sprite.
2. WHEN the player moves to an Illuminated_Tile, THE player Element_Sprite SHALL render as a complete circle without any clipping or distortion.
3. WHEN a light source occupies an Illuminated_Tile, THE light source Element_Sprite SHALL render as a complete circle without any clipping.
4. THE Sorting_Order of the player sprite SHALL be greater than the Sorting_Order of the Fog_Overlay.
5. THE Sorting_Order of all Element_Sprites SHALL be greater than the Sorting_Order of the Fog_Overlay.
6. THE Sorting_Order hierarchy SHALL maintain the relative ordering: Floor < Wall < Beam < Fog < Element < Player.

### Requirement 2: Light source illuminates all unobstructed tiles within radius

**User Story:** As a player, I want all floor tiles within a light source's radius to be illuminated when no walls block the path, so that I can see the area around the light source without unexpected dark gaps.

#### Acceptance Criteria

1. WHEN a light source is placed at a floor tile with a given radius, THE LightSourceLogic SHALL include every floor tile within that radius that has an unobstructed path from the light source in the illuminated set.
2. WHEN the path from a light source to a target tile is axis-aligned (purely horizontal or purely vertical) with no intervening walls, THE Bresenham_LOS SHALL return true for that target tile.
3. WHEN the path from a light source to a target tile is diagonal with no intervening walls, THE Bresenham_LOS SHALL return true for that target tile.
4. IF a wall tile exists on the Bresenham line between a light source and a target tile, THEN THE Bresenham_LOS SHALL return false for that target tile.
5. FOR ALL valid light source positions, radii, and wall configurations, THE set of Illuminated_Tiles SHALL be symmetric: if tile (x+dx, y+dy) is illuminated relative to the light at (x, y), then tile (x-dx, y-dy) is illuminated when the mirrored wall configuration is equivalent.

### Requirement 3: UI renders above the fog overlay

**User Story:** As a player, I want to see all UI elements (inventory count, objective count, mode toggle, menus) at all times, so that the fog-of-war overlay does not obscure the game interface.

#### Acceptance Criteria

1. THE UI_Canvas SHALL render above the Fog_Overlay at all times regardless of fog state.
2. WHEN the UI_Canvas uses Screen Space - Camera render mode, THE UI_Canvas sorting order SHALL be greater than the Sorting_Order of the Fog_Overlay.
3. WHEN the game is in any state (playing, paused, level complete), THE UI_Canvas SHALL remain fully visible and not obscured by the Fog_Overlay.
