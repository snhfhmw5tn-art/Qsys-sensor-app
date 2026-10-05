namespace Qsys.SensorApp.Core.Geometry;

/// <summary>Provides deterministic angle and numeric helpers for geometry operations.</summary>
public static class MathHelper
{
    /// <summary>Gets the default tolerance used by geometric comparisons.</summary>
    public const double DefaultTolerance = 1e-9;

    /// <summary>Normalizes degrees into the half-open interval [0, 360).</summary>
    public static double NormalizeAngle(double degrees)
    {
        if (!double.IsFinite(degrees)) throw new ArgumentOutOfRangeException(nameof(degrees));
        var normalized = degrees % 360d;
        normalized = normalized < 0 ? normalized + 360d : normalized;
        return normalized >= 360d ? 0 : normalized;
    }

    /// <summary>Normalizes radians into the half-open interval [0, 2π).</summary>
    public static double NormalizeRadians(double radians)
    {
        if (!double.IsFinite(radians)) throw new ArgumentOutOfRangeException(nameof(radians));
        var fullTurn = 2d * Math.PI;
        var normalized = radians % fullTurn;
        normalized = normalized < 0 ? normalized + fullTurn : normalized;
        return normalized >= fullTurn ? 0 : normalized;
    }

    /// <summary>Returns whether two finite values differ by no more than a non-negative tolerance.</summary>
    public static bool NearlyEqual(double left, double right, double tolerance = DefaultTolerance)
    {
        if (!double.IsFinite(tolerance) || tolerance < 0) throw new ArgumentOutOfRangeException(nameof(tolerance));
        if (!double.IsFinite(left) || !double.IsFinite(right)) return left.Equals(right);
        return Math.Abs(left - right) <= tolerance;
    }

    /// <summary>Calculates the 2D Euclidean norm without intermediate overflow where the result is representable.</summary>
    public static double Hypot(double x, double y)
    {
        if (!double.IsFinite(x)) throw new ArgumentOutOfRangeException(nameof(x), "Coordinate must be finite.");
        if (!double.IsFinite(y)) throw new ArgumentOutOfRangeException(nameof(y), "Coordinate must be finite.");
        var scale = Math.Max(Math.Abs(x), Math.Abs(y));
        if (scale == 0) return 0;
        var scaledX = x / scale;
        var scaledY = y / scale;
        return scale * Math.Sqrt((scaledX * scaledX) + (scaledY * scaledY));
    }

    /// <summary>Calculates the 3D Euclidean norm without intermediate overflow where the result is representable.</summary>
    public static double Hypot(double x, double y, double z)
    {
        if (!double.IsFinite(x)) throw new ArgumentOutOfRangeException(nameof(x), "Coordinate must be finite.");
        if (!double.IsFinite(y)) throw new ArgumentOutOfRangeException(nameof(y), "Coordinate must be finite.");
        if (!double.IsFinite(z)) throw new ArgumentOutOfRangeException(nameof(z), "Coordinate must be finite.");
        var scale = Math.Max(Math.Abs(x), Math.Max(Math.Abs(y), Math.Abs(z)));
        if (scale == 0) return 0;
        var scaledX = x / scale;
        var scaledY = y / scale;
        var scaledZ = z / scale;
        return scale * Math.Sqrt((scaledX * scaledX) + (scaledY * scaledY) + (scaledZ * scaledZ));
    }
}
