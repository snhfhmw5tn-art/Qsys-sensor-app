namespace Qsys.SensorApp.Core.Geometry;

/// <summary>A compass heading in degrees clockwise from north, normalized to [0, 360).</summary>
public readonly record struct Heading
{
    /// <summary>Creates a normalized compass heading.</summary>
    public Heading(double degrees) => Degrees = MathHelper.NormalizeAngle(degrees);

    /// <summary>Gets the normalized heading in degrees clockwise from north.</summary>
    public double Degrees { get; }

    /// <summary>Creates a heading from an angle in radians clockwise from north.</summary>
    public static Heading FromRadians(double radians)
    {
        if (!double.IsFinite(radians)) throw new ArgumentOutOfRangeException(nameof(radians));
        return new(MathHelper.NormalizeRadians(radians) * (180d / Math.PI));
    }
}
