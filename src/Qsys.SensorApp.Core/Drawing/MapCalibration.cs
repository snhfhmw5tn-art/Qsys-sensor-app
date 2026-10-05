using Qsys.SensorApp.Core.Geometry;

namespace Qsys.SensorApp.Core.Drawing;

/// <summary>Maps drawing units to real-world meters using a known measured distance.</summary>
public sealed record MapCalibration
{
    /// <summary>Gets the real-world meters represented by one drawing unit.</summary>
    public double MetersPerDrawingUnit { get; }

    /// <summary>Creates a calibration from two drawing points and their known real-world distance.</summary>
    public MapCalibration(Vector2 firstPoint, Vector2 secondPoint, double knownDistanceMeters)
    {
        if (!double.IsFinite(knownDistanceMeters) || knownDistanceMeters <= 0)
            throw new ArgumentOutOfRangeException(nameof(knownDistanceMeters), "Known distance must be positive and finite.");
        var drawingDistance = Distance.Between(firstPoint, secondPoint);
        if (drawingDistance <= 0) throw new ArgumentException("Calibration points must be different.");
        MetersPerDrawingUnit = knownDistanceMeters / drawingDistance;
    }

    /// <summary>Converts a drawing distance to meters.</summary>
    public double ToMeters(double drawingUnits) => drawingUnits * MetersPerDrawingUnit;
}
