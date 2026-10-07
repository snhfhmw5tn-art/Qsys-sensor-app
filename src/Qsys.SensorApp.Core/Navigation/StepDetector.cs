using Qsys.SensorApp.Core.Sensors;

namespace Qsys.SensorApp.Core.Navigation;

/// <summary>Detects walking steps from a gravity-compensated acceleration magnitude.</summary>
public sealed class StepDetector
{
    private const double GravityMetersPerSecondSquared = 9.80665;
    private const double BaselineTimeConstantSeconds = 0.8;
    private const double MinimumStepIntervalSeconds = 0.28;
    // Rotation counts as a turn only when gyro yaw is strong and linear acceleration does not corroborate translation.
    private const double InPlaceTurnThresholdDegreesPerSecond = 45;
    private const double MinimumTranslationAccelerationMetersPerSecondSquared = 0.35;
    private double? _baseline;
    private double? _previousFiltered;
    private double? _previousPreviousFiltered;
    private DateTimeOffset? _previousTimestamp;
    private DateTimeOffset? _lastStepTimestamp;

    /// <summary>Creates a detector with a positive peak threshold in m/s².</summary>
    public StepDetector(double thresholdMetersPerSecondSquared = 0.65)
    {
        if (!double.IsFinite(thresholdMetersPerSecondSquared) || thresholdMetersPerSecondSquared <= 0)
            throw new ArgumentOutOfRangeException(nameof(thresholdMetersPerSecondSquared));
        ThresholdMetersPerSecondSquared = thresholdMetersPerSecondSquared;
    }

    /// <summary>Gets the configured minimum filtered acceleration peak.</summary>
    public double ThresholdMetersPerSecondSquared { get; }
    /// <summary>Gets the number of detected steps since reset.</summary>
    public int StepCount { get; private set; }

    /// <summary>Returns true when gyro indicates a turn but linear acceleration does not indicate translation.</summary>
    public static bool IsTurningWithoutTranslation(SensorReading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);
        var verticalTurnRate = GetVerticalTurnRate(reading);
        var hasTranslationAcceleration = reading.Acceleration is { } linearAcceleration &&
            linearAcceleration.Magnitude >= MinimumTranslationAccelerationMetersPerSecondSquared;
        return verticalTurnRate is { } turnRate && Math.Abs(turnRate) >= InPlaceTurnThresholdDegreesPerSecond && !hasTranslationAcceleration;
    }

    /// <summary>Processes one measurement and returns its step event, if a new step was detected.</summary>
    public StepDetection Update(SensorReading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);
        var vector = reading.Acceleration ?? reading.AccelerationIncludingGravity;
        if (vector is null) return StepDetection.None;

        var magnitude = vector.Value.Magnitude;
        if (reading.Acceleration is null) magnitude -= GravityMetersPerSecondSquared;
        var timestamp = reading.TimestampUtc;
        if (_previousTimestamp is { } lastTimestamp && timestamp <= lastTimestamp) return StepDetection.None;

        var elapsed = _previousTimestamp is { } previous
            ? (timestamp - previous).TotalSeconds
            : reading.SamplingIntervalMilliseconds / 1000;
        if (!double.IsFinite(elapsed) || elapsed <= 0) elapsed = 0.04;

        _baseline ??= magnitude;
        var smoothing = 1 - Math.Exp(-elapsed / BaselineTimeConstantSeconds);
        var filtered = magnitude - _baseline.Value;
        _baseline += smoothing * filtered;

        // This is a per-sample gyro/accelerometer comparison, not a time-based cooldown: real step acceleration still passes during a turn.
        var turnWithoutTranslation = IsTurningWithoutTranslation(reading);
        var detected = false;
        var stepInterval = 0d;
        var peak = _previousFiltered ?? 0;
        if (!turnWithoutTranslation && _previousPreviousFiltered is { } beforePrevious && _previousFiltered is { } previousFiltered &&
            previousFiltered > beforePrevious && previousFiltered >= filtered && previousFiltered >= ThresholdMetersPerSecondSquared)
        {
            var interval = _lastStepTimestamp is { } priorStep ? (timestamp - priorStep).TotalSeconds : double.PositiveInfinity;
            if (interval >= MinimumStepIntervalSeconds)
            {
                StepCount++;
                _lastStepTimestamp = timestamp;
                detected = true;
                stepInterval = double.IsFinite(interval) ? interval : 0;
            }
        }

        _previousPreviousFiltered = _previousFiltered;
        _previousFiltered = filtered;
        _previousTimestamp = timestamp;
        return detected
            ? new StepDetection(true, StepCount, timestamp, stepInterval, Math.Clamp(0.55 + ((peak - ThresholdMetersPerSecondSquared) / 3), 0, 1))
            : StepDetection.None;
    }

    /// <summary>Clears detector history and resets its step count.</summary>
    public void Reset()
    {
        _baseline = null;
        _previousFiltered = null;
        _previousPreviousFiltered = null;
        _previousTimestamp = null;
        _lastStepTimestamp = null;
        StepCount = 0;
    }

    private static double? GetVerticalTurnRate(SensorReading reading)
    {
        if (reading.RotationRateDegreesPerSecond is not { } rotation) return null;
        // Project device-relative rotation onto gravity so the yaw estimate works with a tilted device.
        if (reading.AccelerationIncludingGravity is not { } gravity || gravity.Magnitude < 6 || gravity.Magnitude > 13)
            return rotation.Magnitude;
        return ((rotation.X * gravity.X) + (rotation.Y * gravity.Y) + (rotation.Z * gravity.Z)) / gravity.Magnitude;
    }

}

/// <summary>Result of one step-detection update.</summary>
public sealed record StepDetection(bool Detected, int TotalSteps, DateTimeOffset TimestampUtc, double IntervalSeconds, double Confidence)
{
    /// <summary>Gets the no-step result.</summary>
    public static StepDetection None { get; } = new(false, 0, DateTimeOffset.MinValue, 0, 0);
}
