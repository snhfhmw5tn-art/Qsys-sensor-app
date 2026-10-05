namespace Qsys.SensorApp.Core.Geometry;

/// <summary>A compass bearing in degrees clockwise from north, normalized to [0, 360).</summary>
public readonly record struct Bearing
{
    /// <summary>Creates a normalized compass bearing.</summary>
    public Bearing(double degrees) => Degrees = MathHelper.NormalizeAngle(degrees);

    /// <summary>Gets the normalized bearing in degrees clockwise from north.</summary>
    public double Degrees { get; }

    /// <summary>Gets the initial bearing from one Cartesian point to another, where +Y is north.</summary>
    public static Bearing Between(Vector2 origin, Vector2 destination)
    {
        var delta = destination - origin;
        if (delta.Length == 0) throw new ArgumentException("A bearing is undefined for coincident points.", nameof(destination));
        return new(Math.Atan2(delta.X, delta.Y) * (180d / Math.PI));
    }
}
