using System.Collections.Generic;

/// <summary>
/// Pure validation logic for LevelData. No Unity MonoBehaviour dependency.
/// </summary>
public static class LevelValidator
{
    public struct ValidationResult
    {
        public bool IsValid;
        public List<string> Errors;

        public static ValidationResult Valid() => new ValidationResult { IsValid = true, Errors = new List<string>() };
        public static ValidationResult Invalid(List<string> errors) => new ValidationResult { IsValid = false, Errors = errors };
    }

    public static ValidationResult Validate(LevelData data)
    {
        var errors = new List<string>();

        if (data == null)
        {
            errors.Add("LevelData is null");
            return ValidationResult.Invalid(errors);
        }

        if (data.tiles == null || data.tiles.Length == 0)
        {
            errors.Add("Tiles array is null or empty");
            return ValidationResult.Invalid(errors);
        }

        // Validate tile values
        for (int y = 0; y < data.tiles.Length; y++)
        {
            if (data.tiles[y] == null || data.tiles[y].row == null)
            {
                errors.Add($"Tile row {y} is null");
                continue;
            }
            for (int x = 0; x < data.tiles[y].row.Length; x++)
            {
                string tile = data.tiles[y].row[x];
                if (tile != "_" && tile != "#")
                    errors.Add($"Invalid tile value '{tile}' at [{y}][{x}]");
            }
        }

        // Validate at least one beam emitter
        if (data.beamEmitters == null || data.beamEmitters.Length == 0)
            errors.Add("Level must have at least one beam emitter");

        // Validate at least one puzzle objective
        if (data.puzzleObjectives == null || data.puzzleObjectives.Length == 0)
            errors.Add("Level must have at least one puzzle objective");

        // Validate player start
        if (data.playerStart == null)
        {
            errors.Add("Player start position is null");
        }
        else if (!IsInBounds(data, data.playerStart.x, data.playerStart.y))
        {
            errors.Add("Player start position is out of bounds");
        }
        else if (data.GetTile(data.playerStart.x, data.playerStart.y) != "_")
        {
            errors.Add("Player start position is not on a floor tile");
        }

        // Validate all object positions are in bounds and on floor tiles
        if (data.lightSources != null)
        {
            for (int i = 0; i < data.lightSources.Length; i++)
            {
                var ls = data.lightSources[i];
                if (!IsInBounds(data, ls.x, ls.y))
                    errors.Add($"Light source {i} is out of bounds");
                else if (data.GetTile(ls.x, ls.y) != "_")
                    errors.Add($"Light source {i} is not on a floor tile");
            }
        }

        if (data.beamEmitters != null)
        {
            for (int i = 0; i < data.beamEmitters.Length; i++)
            {
                var be = data.beamEmitters[i];
                if (!IsInBounds(data, be.x, be.y))
                    errors.Add($"Beam emitter {i} is out of bounds");
                else if (data.GetTile(be.x, be.y) != "_")
                    errors.Add($"Beam emitter {i} is not on a floor tile");
            }
        }

        if (data.mirrors != null)
        {
            for (int i = 0; i < data.mirrors.Length; i++)
            {
                var m = data.mirrors[i];
                if (!IsInBounds(data, m.x, m.y))
                    errors.Add($"Mirror {i} is out of bounds");
                else if (data.GetTile(m.x, m.y) != "_")
                    errors.Add($"Mirror {i} is not on a floor tile");
            }
        }

        if (data.puzzleObjectives != null)
        {
            for (int i = 0; i < data.puzzleObjectives.Length; i++)
            {
                var po = data.puzzleObjectives[i];
                if (!IsInBounds(data, po.x, po.y))
                    errors.Add($"Puzzle objective {i} is out of bounds");
                else if (data.GetTile(po.x, po.y) != "_")
                    errors.Add($"Puzzle objective {i} is not on a floor tile");
            }
        }

        return errors.Count == 0 ? ValidationResult.Valid() : ValidationResult.Invalid(errors);
    }

    private static bool IsInBounds(LevelData data, int x, int y)
    {
        return y >= 0 && y < data.tiles.Length
            && x >= 0 && data.tiles[y] != null && data.tiles[y].row != null
            && x < data.tiles[y].row.Length;
    }
}
