using UnityEngine;

[System.Serializable]
public struct BeamSegment
{
    public Vector2 Start;
    public Vector2 End;
    public Vector2Int Direction;

    public BeamSegment(Vector2 start, Vector2 end, Vector2Int direction)
    {
        Start = start;
        End = end;
        Direction = direction;
    }
}
