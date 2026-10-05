namespace Qsys.SensorApp.Core.Geometry;

/// <summary>Calculates Euclidean distances between points and geometric primitives.</summary>
public static class Distance
{
    /// <summary>Returns the Euclidean distance between two 2D points.</summary>
    public static double Between(Vector2 first, Vector2 second) => (second - first).Length;
    /// <summary>Returns the Euclidean distance between two 3D points.</summary>
    public static double Between(Vector3 first, Vector3 second) => (second - first).Length;

    /// <summary>Returns the distance from a point to an unbounded line.</summary>
    public static double PointToLine(Vector2 point, Line line)
    {
        if (line.IsDegenerate) return Between(point, line.Start);
        return Math.Abs((point - line.Start).Cross(line.Direction)) / line.Length;
    }

    /// <summary>Returns the shortest distance from a point to a segment.</summary>
    public static double PointToSegment(Vector2 point, Line segment) => Between(point, Projection.OntoSegment(point, segment));

    /// <summary>Returns the shortest distance between two segments.</summary>
    public static double SegmentToSegment(Line first, Line second)
    {
        if (Intersection.TryIntersectSegments(first, second, out _)) return 0;
        return Math.Min(
            Math.Min(PointToSegment(first.Start, second), PointToSegment(first.End, second)),
            Math.Min(PointToSegment(second.Start, first), PointToSegment(second.End, first)));
    }
}
