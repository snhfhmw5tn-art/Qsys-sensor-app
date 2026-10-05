namespace Qsys.SensorApp.Core.Geometry;

/// <summary>An angle measured in degrees; conversion to radians is explicit.</summary>
public readonly record struct Angle
{
    /// <summary>Creates an angle from a finite degree value.</summary>
    public Angle(double degrees)
    {
        if (!double.IsFinite(degrees)) throw new ArgumentOutOfRangeException(nameof(degrees), "Angle must be finite.");
        Degrees = degrees;
    }

    /// <summary>Gets the angle in degrees.</summary>
    public double Degrees { get; }
    /// <summary>Gets the angle in radians.</summary>
    public double Radians => Degrees * (Math.PI / 180d);

    /// <summary>Creates an angle from radians.</summary>
    public static Angle FromRadians(double radians)
    {
        if (!double.IsFinite(radians)) throw new ArgumentOutOfRangeException(nameof(radians), "Angle must be finite.");
        return new(radians * (180d / Math.PI));
    }
}
