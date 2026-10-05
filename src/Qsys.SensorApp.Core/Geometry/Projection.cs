namespace Qsys.SensorApp.Core.Geometry;

/// <summary>Projects points and vectors onto lines, segments and directions.</summary>
public static class Projection
{
    /// <summary>Projects a point onto an unbounded line; a degenerate line projects to its start.</summary>
    public static Vector2 OntoLine(Vector2 point, Line line)
    {
        var direction = line.Direction;
        var lengthSquared = direction.LengthSquared;
        if (lengthSquared == 0) return line.Start;
        var parameter = (point - line.Start).Dot(direction) / lengthSquared;
        return line.PointAt(parameter);
    }

    /// <summary>Projects a point onto a segment, clamping the result to its endpoints.</summary>
    public static Vector2 OntoSegment(Vector2 point, Line segment)
    {
        var direction = segment.Direction;
        var lengthSquared = direction.LengthSquared;
        if (lengthSquared == 0) return segment.Start;
        var parameter = Math.Clamp((point - segment.Start).Dot(direction) / lengthSquared, 0d, 1d);
        return segment.PointAt(parameter);
    }

    /// <summary>Projects one vector onto a non-zero vector.</summary>
    public static Vector2 OntoVector(Vector2 value, Vector2 axis)
    {
        var lengthSquared = axis.LengthSquared;
        if (lengthSquared == 0) throw new ArgumentException("Projection axis must be non-zero.", nameof(axis));
        return axis * (value.Dot(axis) / lengthSquared);
    }
}
