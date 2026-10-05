namespace Qsys.SensorApp.Core.Geometry;

/// <summary>Tests geometric intersections and returns deterministic intersection points.</summary>
public static class Intersection
{
    /// <summary>Attempts to intersect two finite segments; collinear overlap returns its first point along the first segment.</summary>
    public static bool TryIntersectSegments(Line first, Line second, out Vector2 point, double tolerance = MathHelper.DefaultTolerance)
    {
        ValidateTolerance(tolerance);
        var r = first.Direction;
        var s = second.Direction;
        var rLengthSquared = r.LengthSquared;
        var sLengthSquared = s.LengthSquared;
        var rLength = Math.Sqrt(rLengthSquared);
        var sLength = Math.Sqrt(sLengthSquared);

        if (rLengthSquared <= tolerance * tolerance)
        {
            point = first.Start;
            if (sLengthSquared <= tolerance * tolerance) return Distance.Between(first.Start, second.Start) <= tolerance;
            return Distance.PointToSegment(first.Start, second) <= tolerance;
        }
        if (sLengthSquared <= tolerance * tolerance)
        {
            point = second.Start;
            return Distance.PointToSegment(second.Start, first) <= tolerance;
        }

        var offset = second.Start - first.Start;
        var denominator = r.Cross(s);
        if (Math.Abs(denominator) <= tolerance * rLength * sLength)
        {
            if (Math.Abs(offset.Cross(r)) > tolerance * rLength)
            {
                point = default;
                return false;
            }
            var firstParameter = offset.Dot(r) / rLengthSquared;
            var secondParameter = firstParameter + (s.Dot(r) / rLengthSquared);
            var overlapStart = Math.Max(0d, Math.Min(firstParameter, secondParameter));
            var overlapEnd = Math.Min(1d, Math.Max(firstParameter, secondParameter));
            if (overlapStart > overlapEnd + tolerance)
            {
                point = default;
                return false;
            }
            point = first.PointAt(Math.Clamp(overlapStart, 0d, 1d));
            return true;
        }

        var t = offset.Cross(s) / denominator;
        var u = offset.Cross(r) / denominator;
        if (t < -tolerance || t > 1d + tolerance || u < -tolerance || u > 1d + tolerance)
        {
            point = default;
            return false;
        }
        point = first.PointAt(Math.Clamp(t, 0d, 1d));
        return true;
    }

    /// <summary>Returns whether two segments intersect or touch.</summary>
    public static bool SegmentsIntersect(Line first, Line second, double tolerance = MathHelper.DefaultTolerance) =>
        TryIntersectSegments(first, second, out _, tolerance);

    /// <summary>Attempts to find the nearest segment-circle intersection point from the segment start.</summary>
    public static bool TryIntersectSegmentCircle(Line segment, Circle circle, out Vector2 point, double tolerance = MathHelper.DefaultTolerance)
    {
        ValidateTolerance(tolerance);
        var direction = segment.Direction;
        var offset = segment.Start - circle.Center;
        var a = direction.Dot(direction);
        if (a <= tolerance * tolerance)
        {
            point = segment.Start;
            return Math.Abs(Distance.Between(segment.Start, circle.Center) - circle.Radius) <= tolerance;
        }
        var b = 2d * offset.Dot(direction);
        var c = offset.Dot(offset) - (circle.Radius * circle.Radius);
        var discriminant = (b * b) - (4d * a * c);
        if (discriminant < -tolerance)
        {
            point = default;
            return false;
        }
        var root = Math.Sqrt(Math.Max(0d, discriminant));
        var firstParameter = (-b - root) / (2d * a);
        var secondParameter = (-b + root) / (2d * a);
        var parameter = firstParameter is >= 0 and <= 1 ? firstParameter : secondParameter is >= 0 and <= 1 ? secondParameter : double.NaN;
        if (!double.IsFinite(parameter))
        {
            point = default;
            return false;
        }
        point = segment.PointAt(parameter);
        return true;
    }

    private static void ValidateTolerance(double tolerance)
    {
        if (!double.IsFinite(tolerance) || tolerance < 0) throw new ArgumentOutOfRangeException(nameof(tolerance));
    }
}
