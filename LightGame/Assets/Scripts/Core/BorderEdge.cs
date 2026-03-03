using System;
using UnityEngine;

/// <summary>
/// Represents one edge segment of the illumination boundary.
/// Direction-independent equality: (A→B) == (B→A).
/// </summary>
public struct BorderEdge : IEquatable<BorderEdge>
{
    public Vector2 Start;
    public Vector2 End;

    public BorderEdge(Vector2 start, Vector2 end)
    {
        Start = start;
        End = end;
    }

    public bool Equals(BorderEdge other)
    {
        return (Start == other.Start && End == other.End) ||
               (Start == other.End && End == other.Start);
    }

    public override bool Equals(object obj)
    {
        return obj is BorderEdge other && Equals(other);
    }

    public override int GetHashCode()
    {
        // Order-independent hash: use addition so (A,B) and (B,A) produce the same hash
        return Start.GetHashCode() + End.GetHashCode();
    }

    public override string ToString()
    {
        return $"BorderEdge({Start} → {End})";
    }
}
