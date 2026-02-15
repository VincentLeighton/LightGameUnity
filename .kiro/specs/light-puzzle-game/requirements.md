# Requirements Document

## Introduction

A top-down puzzle game built in Unity where the player explores a dark world using light sources and mirrors. The core gameplay revolves around strategically placing light sources with limited illumination radii and positioning mirrors to redirect light beams into hard-to-reach areas of maze-like maps. The player must solve puzzles by illuminating key areas to progress.

## Glossary

- **Player**: The character controlled by the user, capable of moving through the game world and interacting with objects.
- **Game_World**: The 2D top-down environment containing walls, floors, light sources, beam emitters, mirrors, and puzzle objectives.
- **Light_Source**: An object that emits ambient light in a circular radius, illuminating the surrounding area. Light_Sources provide visibility but do not emit directional beams.
- **Illumination_Radius**: The circular area around a Light_Source within which tiles and objects become visible to the Player.
- **Beam_Emitter**: An object that emits a directional Light_Beam in a single configured direction, but only when the Beam_Emitter's Tile is illuminated by a Light_Source. Beam_Emitters are dormant in Dark_Areas.
- **Mirror**: An object the Player can pick up, place, and rotate to redirect a light beam in a new direction.
- **Light_Beam**: A directional ray of light emitted from a Beam_Emitter or reflected off a Mirror, traveling in a straight line until it hits a wall or another Mirror.
- **Dark_Area**: Any region of the Game_World not currently illuminated by a Light_Source, Beam_Emitter, or Light_Beam.
- **Tile**: A single grid cell in the Game_World; tiles are either walls (blocking light and movement) or floors (allowing both).
- **Puzzle_Objective**: A target area or object that must be illuminated to complete a level.
- **Level**: A self-contained map with a specific arrangement of walls, Light_Sources, Beam_Emitters, Mirrors, and Puzzle_Objectives.
- **Fog_of_War**: The visual overlay that hides Dark_Areas from the Player's view.
- **Inventory**: The collection of Mirrors currently held by the Player and available for placement.

## Requirements

### Requirement 1: Player Movement

**User Story:** As a player, I want to move my character around the game world using touch input on my Android device, so that I can explore the environment and interact with objects.

#### Acceptance Criteria

1. WHEN the Player taps on a reachable floor Tile, THE Game_World SHALL move the Player along the shortest path to that Tile.
2. WHEN the Player attempts to move into a wall Tile, THE Game_World SHALL prevent the movement and keep the Player at the current position.
3. WHEN the Player is moving along a path, THE Game_World SHALL animate the Player's movement at a speed of 5 Tiles per second.
4. WHEN the Player taps a new destination while already moving, THE Game_World SHALL recalculate the path to the new destination and redirect the Player.
5. WHEN the Player moves, THE Game_World SHALL update the visible area based on the Player's proximity to Light_Sources and Light_Beams.
6. WHEN the Player taps on an unreachable Tile (wall or blocked path), THE Game_World SHALL provide brief visual feedback indicating the Tile is unreachable.

### Requirement 2: Light Source Illumination

**User Story:** As a player, I want light sources to illuminate a circular area around them, so that I can see nearby parts of the dark world.

#### Acceptance Criteria

1. THE Light_Source SHALL illuminate all floor Tiles within its configured Illumination_Radius.
2. WHEN a wall Tile exists between a Light_Source and a floor Tile within the Illumination_Radius, THE Light_Source SHALL NOT illuminate that blocked floor Tile.
3. WHEN a Light_Source is placed in the Game_World, THE Fog_of_War SHALL be removed from all Tiles illuminated by that Light_Source.
4. WHEN a Light_Source is removed from the Game_World, THE Fog_of_War SHALL be restored to any Tiles no longer illuminated by any remaining light.

### Requirement 3: Light Beam Propagation

**User Story:** As a player, I want beam emitters to shoot directional light beams, so that I can direct light into specific areas using mirrors.

#### Acceptance Criteria

1. WHILE a Beam_Emitter's Tile is illuminated by a Light_Source, THE Beam_Emitter SHALL emit a Light_Beam in its configured direction as a straight line across floor Tiles.
2. WHILE a Beam_Emitter's Tile is not illuminated by any Light_Source, THE Beam_Emitter SHALL remain dormant and emit no Light_Beam.
3. WHEN a Light_Source is placed or removed causing a Beam_Emitter's Tile to change illumination state, THE Game_World SHALL recalculate all Light_Beams affected by that Beam_Emitter.
4. WHEN a Light_Beam reaches a wall Tile, THE Light_Beam SHALL stop and not propagate further.
5. WHEN a Light_Beam reaches a Mirror, THE Light_Beam SHALL change direction based on the Mirror's current rotation angle.
6. WHEN a Light_Beam passes through a floor Tile, THE Fog_of_War SHALL be removed from that Tile.
7. WHEN a Mirror is moved or rotated, THE Game_World SHALL recalculate all affected Light_Beams within the same frame.

### Requirement 4: Mirror Interaction

**User Story:** As a player, I want to pick up, place, and rotate mirrors using touch gestures, so that I can redirect light beams to solve puzzles.

#### Acceptance Criteria

1. WHEN the Player moves onto a Tile containing a Mirror, THE Game_World SHALL add the Mirror to the Player's Inventory.
2. WHEN the Player taps an empty floor Tile while in placement mode, THE Game_World SHALL place a Mirror from the Inventory onto that Tile.
3. WHEN the Player taps a placed Mirror, THE Mirror SHALL rotate by 45 degrees clockwise.
4. WHEN the Player long-presses a placed Mirror, THE Game_World SHALL pick up the Mirror and return it to the Inventory.
5. WHEN the Player attempts to place a Mirror and the Inventory is empty, THE Game_World SHALL display a message indicating no Mirrors are available.
6. WHEN the Player attempts to place a Mirror on a wall Tile or an occupied Tile, THE Game_World SHALL prevent the placement and keep the Mirror in the Inventory.
7. WHEN a Mirror is placed, rotated, or picked up, THE Game_World SHALL recalculate all Light_Beams that interact with that Mirror.

### Requirement 5: Fog of War System

**User Story:** As a player, I want the world to be hidden in darkness until illuminated, so that exploration feels rewarding and puzzles require strategic light management.

#### Acceptance Criteria

1. WHEN a Level starts, THE Fog_of_War SHALL cover all Tiles in the Game_World except those illuminated by initial Light_Sources and Beam_Emitters.
2. THE Fog_of_War SHALL visually obscure Dark_Areas so that the Player cannot see walls, floors, or objects within them.
3. WHEN a Tile transitions from dark to illuminated, THE Fog_of_War SHALL fade out over 0.3 seconds to reveal the Tile.
4. WHEN a Tile transitions from illuminated to dark, THE Fog_of_War SHALL fade in over 0.3 seconds to hide the Tile.
5. WHILE a Tile is illuminated by at least one Light_Source or Light_Beam, THE Fog_of_War SHALL remain removed from that Tile.

### Requirement 6: Puzzle Objective and Level Completion

**User Story:** As a player, I want clear objectives that require illuminating specific areas, so that I have a goal to work toward in each level.

#### Acceptance Criteria

1. THE Level SHALL define one or more Puzzle_Objectives that must be illuminated to complete the Level.
2. WHEN all Puzzle_Objectives in a Level are simultaneously illuminated, THE Game_World SHALL display a level-complete notification.
3. WHEN the level-complete notification is displayed, THE Game_World SHALL transition the Player to the next Level after a 2-second delay.
4. IF no next Level exists, THEN THE Game_World SHALL display a game-complete screen.
5. THE Puzzle_Objective SHALL provide a subtle visual indicator (a faint glow) so the Player can locate objectives even in Dark_Areas.

### Requirement 7: Level Map Structure

**User Story:** As a player, I want maze-like levels with walls and corridors, so that puzzles require creative use of mirrors to redirect light around obstacles.

#### Acceptance Criteria

1. THE Level SHALL be represented as a 2D grid of Tiles where each Tile is designated as either a wall (`#`) or a floor (`_`).
2. THE Level SHALL define the starting position of the Player, all Light_Sources, all Beam_Emitters, all Mirrors, and all Puzzle_Objectives.
3. THE Level SHALL be loadable from a data file (such as JSON) so that new levels can be created without code changes.
4. WHEN a Level is loaded, THE Game_World SHALL validate that the Level contains at least one Beam_Emitter, at least one Puzzle_Objective, and a valid Player starting position.
5. IF a Level fails validation, THEN THE Game_World SHALL display an error message and return to the level selection screen.

### Requirement 8: Light Beam Reflection Physics

**User Story:** As a player, I want mirrors to reflect light beams at predictable angles, so that I can plan my puzzle solutions.

#### Acceptance Criteria

1. THE Mirror SHALL support 8 rotation angles (0, 45, 90, 135, 180, 225, 270, 315 degrees).
2. WHEN a Light_Beam hits a Mirror, THE Mirror SHALL reflect the Light_Beam according to the law of reflection relative to the Mirror's surface normal.
3. WHEN a Light_Beam is reflected by a Mirror, THE reflected Light_Beam SHALL continue propagating until it hits a wall or another Mirror.
4. WHEN multiple Mirrors form a chain, THE Light_Beam SHALL propagate through each Mirror in sequence, reflecting at each one.
5. IF a Light_Beam reflects back toward its own source creating an infinite loop, THEN THE Game_World SHALL cap beam propagation at a maximum of 20 reflections.

### Requirement 9: Game Camera

**User Story:** As a player, I want the camera to follow my character smoothly and support pinch-to-zoom, so that I always have a clear view of my surroundings on my mobile screen.

#### Acceptance Criteria

1. THE Game_Camera SHALL follow the Player's position with smooth interpolation (lerp) so movement feels fluid.
2. THE Game_Camera SHALL maintain a fixed top-down orthographic perspective.
3. WHEN the Player is near the edge of the Level, THE Game_Camera SHALL clamp its position so it does not show areas outside the Level boundaries.
4. WHEN the Player performs a pinch gesture, THE Game_Camera SHALL zoom in or out within a configured minimum and maximum orthographic size.
5. WHEN the Game_Camera zoom level changes, THE Game_Camera SHALL maintain the Player at the center of the viewport.

### Requirement 10: User Interface

**User Story:** As a player, I want a simple touch-friendly interface showing my inventory and current objective, so that I always know what resources I have and what I need to do.

#### Acceptance Criteria

1. THE UI SHALL display the number of Mirrors currently in the Player's Inventory.
2. THE UI SHALL display the count of remaining un-illuminated Puzzle_Objectives in the current Level.
3. WHEN the Inventory count changes, THE UI SHALL update the displayed count within the same frame.
4. WHEN a Puzzle_Objective becomes illuminated, THE UI SHALL update the remaining objectives count within the same frame.
5. THE UI SHALL provide a toggle button to switch between movement mode and mirror-placement mode.
6. THE UI SHALL use touch targets with a minimum size of 48x48 density-independent pixels for all interactive elements.
7. THE UI SHALL scale proportionally across different Android screen resolutions and aspect ratios.

### Requirement 11: Android Platform Support

**User Story:** As a player, I want the game to run smoothly on my Android device, so that I have a good experience without performance issues.

#### Acceptance Criteria

1. THE Game_World SHALL render at a stable 30 frames per second or higher on devices with at least 2 GB of RAM.
2. WHEN the Android back button is pressed during gameplay, THE Game_World SHALL pause the game and display a pause menu.
3. WHEN the game is sent to the background, THE Game_World SHALL save the current Level state so progress is not lost.
4. WHEN the game is resumed from the background, THE Game_World SHALL restore the saved Level state and continue from where the Player left off.
5. THE Game_World SHALL support portrait orientation as the primary display mode.
