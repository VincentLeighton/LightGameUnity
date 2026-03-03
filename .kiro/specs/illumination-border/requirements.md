# Requirements Document

## Introduction

This feature adds a visible red border outline at the edge of each light source's illumination area. The border visually delineates where illuminated tiles meet non-illuminated tiles, giving players a clear sense of each light source's reach. The border is rendered as a red line, 2 pixels wide, drawn on the edges of illuminated tiles that are adjacent to non-illuminated or wall tiles.

## Glossary

- **Illumination_Border_Logic**: Pure C# static class responsible for computing which tile edges form the illumination boundary, given a set of illuminated tiles and a wall-check function.
- **Illumination_Border_Renderer**: MonoBehaviour component responsible for drawing the computed border edges as red lines in the Unity scene.
- **Border_Edge**: A segment between two world-space points representing one side of a tile that lies on the illumination boundary.
- **Illuminated_Tile_Set**: The set of floor tiles currently illuminated by a light source, as computed by LightSourceLogic.
- **Boundary_Tile**: An illuminated tile that has at least one non-illuminated or wall neighbor in the four cardinal directions (up, down, left, right).

## Requirements

### Requirement 1: Compute Illumination Boundary Edges

**User Story:** As a developer, I want a pure logic function that computes the boundary edges of an illumination area, so that the rendering layer can draw a border without coupling to game logic.

#### Acceptance Criteria

1. WHEN a set of illuminated tiles is provided, THE Illumination_Border_Logic SHALL return a list of Border_Edge segments representing all tile edges where an illuminated tile is adjacent to a non-illuminated tile or a wall tile in the four cardinal directions.
2. WHEN an illuminated tile has no non-illuminated or wall neighbors in any cardinal direction, THE Illumination_Border_Logic SHALL exclude that tile from the boundary output.
3. WHEN the illuminated tile set is empty, THE Illumination_Border_Logic SHALL return an empty list of Border_Edge segments.
4. THE Illumination_Border_Logic SHALL produce Border_Edge segments using world-space coordinates consistent with the existing tile coordinate system (tile center at (x+0.5, y+0.5)).

### Requirement 2: Render Illumination Border

**User Story:** As a player, I want to see a red border at the edge of each light source's illumination area, so that I can clearly understand the boundary of the light.

#### Acceptance Criteria

1. WHEN illumination changes, THE Illumination_Border_Renderer SHALL draw red lines along all computed Border_Edge segments.
2. THE Illumination_Border_Renderer SHALL render the border with a width of 2 pixels.
3. THE Illumination_Border_Renderer SHALL render the border using the color red (RGB 255, 0, 0).
4. THE Illumination_Border_Renderer SHALL render the border at a sorting order between the fog layer and the element layer so that the border is visible above the fog but below game elements.
5. WHEN the illuminated tile set changes, THE Illumination_Border_Renderer SHALL update the border to reflect the new boundary.

### Requirement 3: Integration with Existing Illumination System

**User Story:** As a developer, I want the illumination border to integrate with the existing LightBeamSystem, so that the border updates automatically when illumination changes.

#### Acceptance Criteria

1. WHEN LightBeamSystem fires OnIlluminationChanged, THE Illumination_Border_Renderer SHALL recompute and redraw the border using the updated illuminated tile set.
2. WHEN a level is loaded, THE Illumination_Border_Renderer SHALL display the border for the initial illumination state.
3. WHEN a level is unloaded or destroyed, THE Illumination_Border_Renderer SHALL clean up all border rendering resources.
