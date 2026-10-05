using Qsys.SensorApp.Core.Geometry;

namespace Qsys.SensorApp.Core.Navigation;

/// <summary>Current dead-reckoned location and quality metrics.</summary>
public sealed record NavigationState(
    DateTimeOffset TimestampUtc,
    Vector3 PositionMeters,
    double? HeadingDegrees,
    double SpeedMetersPerSecond,
    int StepCount,
    double Confidence,
    double PositionUncertaintyMeters,
    bool IsMapMatched,
    Vector2? MapMatchedPositionMeters,
    ActivityEstimate Activity);
