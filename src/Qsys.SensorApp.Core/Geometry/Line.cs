namespace Qsys.SensorApp.Core.Geometry;

/// <summary>A finite line segment between two points.</summary>
public readonly record struct Line
{
    /// <summary>Creates a line segment between its start and end points.</summary>
    public Line(Vector2 start, Vector2 end)
    {
        Start = start;
        End = end;
    }

    /// <summary>Gets the segment's start point.</summary>
    public Vector2 Start { get; }
    /// <summary>Gets the segment's end point.</summary>
    public Vector2 End { get; }
    /// <summary>Gets the segment direction.</summary>
    public Vector2 Direction => End - Start;
    /// <summary>Gets the segment length.</summary>
    public double Length => Direction.Length;
    /// <summary>Gets whether the segment has zero length.</summary>
    public bool IsDegenerate => Start == End;

    /// <summary>Gets the point at parameter t, where zero is the start and one is the end.</summary>
    public Vector2 PointAt(double t)
    {
        if (!double.IsFinite(t)) throw new ArgumentOutOfRangeException(nameof(t));
        return Start + (Direction * t);
    }
}
