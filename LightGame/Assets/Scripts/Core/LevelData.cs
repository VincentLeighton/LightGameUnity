using System;
using UnityEngine;

[Serializable]
public class TileRow
{
    public string[] row;

    public TileRow() { }
    public TileRow(string[] row) { this.row = row; }
}

[Serializable]
public class LevelData
{
    public int levelIndex;
    public int width;
    public int height;
    public TileRow[] tiles;
    public PositionData playerStart;
    public LightSourceData[] lightSources;
    public BeamEmitterData[] beamEmitters;
    public MirrorData[] mirrors;
    public PositionData[] puzzleObjectives;

    /// <summary>
    /// Helper to get the tile value at grid position [y][x].
    /// </summary>
    public string GetTile(int x, int y) => tiles[y].row[x];
}

[Serializable]
public class PositionData
{
    public int x;
    public int y;

    public Vector2Int ToVector2Int() => new(x, y);
}

[Serializable]
public class LightSourceData
{
    public int x;
    public int y;
    public int radius;

    public Vector2Int ToVector2Int() => new(x, y);
}

[Serializable]
public class BeamEmitterData
{
    public int x;
    public int y;
    public PositionData beamDirection;

    public Vector2Int ToVector2Int() => new(x, y);
    public Vector2Int GetDirection() => beamDirection.ToVector2Int();
}

[Serializable]
public class MirrorData
{
    public int x;
    public int y;
    public int rotationIndex;

    public Vector2Int ToVector2Int() => new(x, y);
}
