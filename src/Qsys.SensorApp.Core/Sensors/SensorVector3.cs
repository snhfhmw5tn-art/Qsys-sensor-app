using Qsys.SensorApp.Core.Geometry;

namespace Qsys.SensorApp.Core.Sensors;

/// <summary>A validated three-axis sensor measurement.</summary>
public readonly record struct SensorVector3
{
    /// <summary>Creates a finite three-axis measurement.</summary>
    public SensorVector3(double x, double y, double z)
    {
        if (!double.IsFinite(x)) throw new ArgumentOutOfRangeException(nameof(x));
        if (!double.IsFinite(y)) throw new ArgumentOutOfRangeException(nameof(y));
        if (!double.IsFinite(z)) throw new ArgumentOutOfRangeException(nameof(z));
        X = x; Y = y; Z = z;
    }

    /// <summary>Gets the X-axis value.</summary>
    public double X { get; }
    /// <summary>Gets the Y-axis value.</summary>
    public double Y { get; }
    /// <summary>Gets the Z-axis value.</summary>
    public double Z { get; }
    /// <summary>Gets the vector magnitude.</summary>
    public double Magnitude => MathHelper.Hypot(MathHelper.Hypot(X, Y), Z);
}
