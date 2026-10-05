namespace Qsys.SensorApp.Core.Geometry;

/// <summary>A two-dimensional circle with a finite non-negative radius.</summary>
public readonly record struct Circle
{
    /// <summary>Creates a circle.</summary>
    public Circle(Vector2 center, double radius)
    {
        if (!double.IsFinite(radius) || radius < 0) throw new ArgumentOutOfRangeException(nameof(radius));
        Center = center;
        Radius = radius;
    }

    /// <summary>Gets the center point.</summary>
    public Vector2 Center { get; }
    /// <summary>Gets the radius.</summary>
    public double Radius { get; }
    /// <summary>Gets the circle area.</summary>
    public double Area => Math.PI * Radius * Radius;
    /// <summary>Gets the circumference.</summary>
    public double Circumference => 2d * Math.PI * Radius;

    /// <summary>Returns whether a point is inside or on the circle boundary.</summary>
    public bool Contains(Vector2 point, double tolerance = MathHelper.DefaultTolerance)
    {
        if (!double.IsFinite(tolerance) || tolerance < 0) throw new ArgumentOutOfRangeException(nameof(tolerance));
        return Distance.Between(Center, point) <= Radius + tolerance;
    }
}
