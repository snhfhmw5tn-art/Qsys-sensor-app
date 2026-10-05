using Qsys.SensorApp.Core.Geometry;

namespace Qsys.SensorApp.Core.Drawing;

/// <summary>Describes the view transform used to display a warehouse drawing.</summary>
public sealed record DrawingViewport
{
    /// <summary>Gets or initializes the drawing zoom.</summary>
    public double Zoom { get; init; } = 1;
    /// <summary>Gets or initializes the screen-space pan offset.</summary>
    public Vector2 Pan { get; init; } = Vector2.Zero;
    /// <summary>Gets or initializes clockwise rotation in degrees.</summary>
    public double RotationDegrees { get; init; }

    /// <summary>Returns a viewport with zoom adjusted by a factor.</summary>
    public DrawingViewport Scale(double factor)
    {
        if (!double.IsFinite(factor) || factor <= 0) throw new ArgumentOutOfRangeException(nameof(factor));
        return this with { Zoom = Math.Clamp(Zoom * factor, 0.1, 10) };
    }

    /// <summary>Returns a viewport panned by the specified screen-space delta.</summary>
    public DrawingViewport Translate(Vector2 delta) => this with { Pan = Pan + delta };

    /// <summary>Returns a viewport rotated by the specified number of degrees.</summary>
    public DrawingViewport Rotate(double degrees)
    {
        if (!double.IsFinite(degrees)) throw new ArgumentOutOfRangeException(nameof(degrees));
        return this with { RotationDegrees = (RotationDegrees + degrees) % 360 };
    }
}
