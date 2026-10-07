using Qsys.SensorApp.Core.Sensors;

namespace Qsys.SensorApp.Core.Navigation;

/// <summary>Classifies recent motion using transparent cadence, vibration and pressure heuristics.</summary>
public sealed class ActivityClassifier
{
    private const double WindowSeconds = 4;
    private const double MotionWindowSeconds = 0.8;
    private const double StepIdleTimeoutSeconds = 1.25;
    private const double TruckConfirmationSeconds = 1.5;
    private const double Gravity = 9.80665;
    private readonly Queue<MotionSample> _samples = new();
    private readonly Queue<DateTimeOffset> _steps = new();
    private DateTimeOffset? _lastTimestamp;
    private DateTimeOffset? _lastStepTimestamp;
    private DateTimeOffset? _truckVibrationSince;

    /// <summary>Adds one sensor sample and returns the current coarse activity estimate.</summary>
    public ActivityEstimate Update(SensorReading reading, bool stepDetected, double? relativeAltitudeMeters)
    {
        ArgumentNullException.ThrowIfNull(reading);
        if (_lastTimestamp is { } last && reading.TimestampUtc <= last) return ActivityEstimate.Unknown;
        _lastTimestamp = reading.TimestampUtc;
        var acceleration = reading.Acceleration is { } linear ? linear.Magnitude : reading.AccelerationIncludingGravity is { } gravity ? Math.Abs(gravity.Magnitude - Gravity) : (double?)null;
        var rotation = reading.RotationRateDegreesPerSecond?.Magnitude;
        _samples.Enqueue(new MotionSample(reading.TimestampUtc, acceleration, rotation, relativeAltitudeMeters));
        if (stepDetected)
        {
            _steps.Enqueue(reading.TimestampUtc);
            _lastStepTimestamp = reading.TimestampUtc;
            _truckVibrationSince = null;
        }
        var cutoff = reading.TimestampUtc.AddSeconds(-WindowSeconds);
        while (_samples.TryPeek(out var sample) && sample.Timestamp < cutoff) _samples.Dequeue();
        while (_steps.TryPeek(out var timestamp) && timestamp < cutoff) _steps.Dequeue();

        if (_samples.Count < 3) return reading.Acceleration is null && reading.AccelerationIncludingGravity is null ? ActivityEstimate.Unknown : new(ActivityType.Unknown, 0.1);
        var seconds = Math.Max(0.5, Math.Min(WindowSeconds, (reading.TimestampUtc - _samples.Peek().Timestamp).TotalSeconds));
        var cadence = _steps.Count / seconds;
        var recentSamples = _samples.Where(item => (reading.TimestampUtc - item.Timestamp).TotalSeconds <= MotionWindowSeconds).ToArray();
        var recentAcceleration = recentSamples.Where(item => item.Acceleration is not null).Select(item => item.Acceleration!.Value).ToArray();
        var accelerationRms = Rms(recentAcceleration);
        var accelerationVariation = Variation(recentAcceleration);
        var stepIsRecent = _lastStepTimestamp is { } lastStep && (reading.TimestampUtc - lastStep).TotalSeconds <= StepIdleTimeoutSeconds;
        var altitudeSamples = _samples.Where(item => item.Altitude is not null).ToArray();
        var altitudeChange = altitudeSamples.Length > 1 ? altitudeSamples[^1].Altitude!.Value - altitudeSamples[0].Altitude!.Value : 0;
        var verticalRate = Math.Abs(altitudeChange) / seconds;
        // One or two peaks can come from rocking the handset. Require several evenly spaced steps before calling it gait.
        var hasWalkingCadence = HasStableWalkingCadence(_steps);

        if (altitudeSamples.Length > 1 && Math.Abs(altitudeChange) >= 0.45 && cadence < 0.35)
            return new(ActivityType.Elevator, Math.Clamp(0.55 + Math.Abs(altitudeChange) * 0.12, 0.55, 0.9));
        if (altitudeSamples.Length > 1 && Math.Abs(altitudeChange) >= 0.18 && stepIsRecent && hasWalkingCadence && cadence < 2.7)
            return new(ActivityType.Stairs, Math.Clamp(0.5 + Math.Abs(altitudeChange) * 0.25 + Math.Min(cadence, 1.5) * 0.08, 0.5, 0.88));
        if (stepIsRecent && hasWalkingCadence && cadence >= 2.15)
            return new(ActivityType.Running, Math.Clamp(0.5 + (cadence - 2.15) * 0.18 + Math.Min(accelerationRms, 2) * 0.08, 0.5, 0.92));
        if (stepIsRecent && hasWalkingCadence && cadence >= 0.45)
            return new(ActivityType.Walking, Math.Clamp(0.48 + Math.Min(cadence, 1.8) * 0.18, 0.48, 0.82));
        var possibleTruck = !stepIsRecent && recentAcceleration.Length >= 8 && accelerationVariation >= 0.18 && accelerationVariation < 1.8 && verticalRate < 0.08;
        if (possibleTruck)
        {
            _truckVibrationSince ??= reading.TimestampUtc;
            if ((reading.TimestampUtc - _truckVibrationSince.Value).TotalSeconds >= TruckConfirmationSeconds)
                return new(ActivityType.Truck, Math.Clamp(0.35 + accelerationVariation * 0.1, 0.35, 0.55));
        }
        else
        {
            _truckVibrationSince = null;
        }

        if (!stepIsRecent && recentAcceleration.Length >= 3 && accelerationRms < 0.3 && accelerationVariation < 0.2)
            return new(ActivityType.Standing, Math.Clamp(0.72 - accelerationRms * 0.4, 0.5, 0.72));
        return new(ActivityType.Unknown, 0.2);
    }

    /// <summary>Clears the classifier's rolling window.</summary>
    public void Reset() { _samples.Clear(); _steps.Clear(); _lastTimestamp = null; _lastStepTimestamp = null; _truckVibrationSince = null; }

    private static double Rms(IEnumerable<double> values) { var data = values.ToArray(); return data.Length == 0 ? 0 : Math.Sqrt(data.Sum(value => value * value) / data.Length); }
    private static double Variation(IEnumerable<double> values) { var data = values.ToArray(); if (data.Length < 2) return 0; var mean = data.Average(); return Math.Sqrt(data.Sum(value => (value - mean) * (value - mean)) / data.Length); }
    private static bool HasStableWalkingCadence(IEnumerable<DateTimeOffset> timestamps)
    {
        var recent = timestamps.TakeLast(3).ToArray();
        if (recent.Length < 3) return false;

        var firstInterval = (recent[1] - recent[0]).TotalSeconds;
        var secondInterval = (recent[2] - recent[1]).TotalSeconds;
        if (firstInterval is < 0.3 or > 1.5 || secondInterval is < 0.3 or > 1.5) return false;

        // Compare neighbouring intervals so isolated taps and irregular phone shakes do not establish a gait cadence.
        var meanInterval = (firstInterval + secondInterval) / 2;
        return Math.Abs(firstInterval - secondInterval) <= meanInterval * 0.4;
    }
    private sealed record MotionSample(DateTimeOffset Timestamp, double? Acceleration, double? Rotation, double? Altitude);
}
