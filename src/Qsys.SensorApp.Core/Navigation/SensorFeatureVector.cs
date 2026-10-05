using Qsys.SensorApp.Core.Sensors;

namespace Qsys.SensorApp.Core.Navigation;

/// <summary>One timestamped feature row derived from a sensor reading.</summary>
public sealed record SensorFeatureVector(
    DateTimeOffset TimestampUtc,
    double SamplingIntervalMilliseconds,
    SensorVector3? Acceleration,
    double? LinearAccelerationMagnitude,
    double? GyroscopeMagnitude,
    double? MagnetometerMagnitude,
    double? HeadingDegrees,
    double? PitchDegrees,
    double? RollDegrees,
    double? PressureKilopascals);

/// <summary>Creates a stable feature row from a timestamped sensor snapshot.</summary>
public static class SensorFeatureExtractor
{
    /// <summary>Extracts acceleration, rotation, magnetic and orientation features.</summary>
    public static SensorFeatureVector Extract(SensorReading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);
        var acceleration = reading.Acceleration;
        double? magnitude = acceleration?.Magnitude;
        if (magnitude is null && reading.AccelerationIncludingGravity is { } gravity)
            magnitude = Math.Abs(gravity.Magnitude - 9.80665);
        return new SensorFeatureVector(reading.TimestampUtc, reading.SamplingIntervalMilliseconds, acceleration, magnitude,
            reading.RotationRateDegreesPerSecond?.Magnitude, reading.MagneticFieldMicrotesla?.Magnitude,
            reading.HeadingDegrees, reading.PitchDegrees, reading.RollDegrees, reading.PressureKilopascals);
    }
}
