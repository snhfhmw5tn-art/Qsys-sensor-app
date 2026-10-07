using Qsys.SensorApp.Core.Geometry;
using Qsys.SensorApp.Core.Maps;
using Qsys.SensorApp.Core.Sensors;

namespace Qsys.SensorApp.Core.Navigation;

/// <summary>Fuses step events, device heading, pressure and optional map constraints into a local position.</summary>
public sealed class DeadReckoningEngine
{
    private const double StandardAtmosphereKilopascals = 101.325;
    private readonly KalmanPositionFilter _positionFilter;
    private readonly StepDetector _stepDetector;
    private readonly ActivityClassifier _activityClassifier = new();
    private readonly double _stepLengthMeters;
    private readonly double _mapMatchToleranceMeters;
    private readonly NavigationGraph? _graph;
    private readonly string _level;
    private DateTimeOffset? _lastTimestamp;
    private DateTimeOffset? _lastStepTimestamp;
    private double? _headingDegrees;
    private double _headingOffsetDegrees;
    private double _headingQuality;
    private double _stepQuality;
    private double? _referencePressureKilopascals;
    private double? _altitudeMeters;

    /// <summary>Creates a local-coordinate dead-reckoning session.</summary>
    public DeadReckoningEngine(
        Vector3 initialPositionMeters,
        double stepLengthMeters = 0.72,
        double? initialHeadingDegrees = null,
        NavigationGraph? graph = null,
        double mapMatchToleranceMeters = 2,
        string level = "0")
    {
        if (!double.IsFinite(stepLengthMeters) || stepLengthMeters <= 0) throw new ArgumentOutOfRangeException(nameof(stepLengthMeters));
        if (initialHeadingDegrees is { } heading && !double.IsFinite(heading)) throw new ArgumentOutOfRangeException(nameof(initialHeadingDegrees));
        if (!double.IsFinite(mapMatchToleranceMeters) || mapMatchToleranceMeters < 0) throw new ArgumentOutOfRangeException(nameof(mapMatchToleranceMeters));
        ArgumentException.ThrowIfNullOrWhiteSpace(level);

        _positionFilter = new KalmanPositionFilter(new Vector2(initialPositionMeters.X, initialPositionMeters.Y));
        _stepDetector = new StepDetector();
        _stepLengthMeters = stepLengthMeters;
        _headingDegrees = initialHeadingDegrees is null ? null : Normalize(initialHeadingDegrees.Value);
        _headingQuality = initialHeadingDegrees is null ? 0 : 0.5;
        _graph = graph;
        _mapMatchToleranceMeters = mapMatchToleranceMeters;
        _level = level;
        State = new NavigationState(DateTimeOffset.UtcNow, initialPositionMeters, _headingDegrees, 0, 0, 0.15, _positionFilter.StandardDeviation, false, null, ActivityEstimate.Unknown);
    }

    /// <summary>Gets the latest navigation estimate.</summary>
    public NavigationState State { get; private set; }

    /// <summary>Sets the current direction as the local forward direction without changing the current position.</summary>
    public bool ResetHeadingToForward()
    {
        if (_headingDegrees is not { } currentHeading) return false;
        _headingOffsetDegrees = Normalize(_headingOffsetDegrees + currentHeading);
        _headingDegrees = 0;
        State = State with { HeadingDegrees = 0 };
        return true;
    }

    /// <summary>Processes one timestamped measurement and updates the estimated position.</summary>
    public NavigationState Process(SensorReading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);
        if (_lastTimestamp is { } last && reading.TimestampUtc <= last)
            throw new ArgumentException("Sensor readings must arrive in strictly increasing timestamp order.", nameof(reading));

        var elapsedSeconds = _lastTimestamp is { } prior ? (reading.TimestampUtc - prior).TotalSeconds : reading.SamplingIntervalMilliseconds / 1000;
        if (!double.IsFinite(elapsedSeconds) || elapsedSeconds < 0) elapsedSeconds = 0;
        elapsedSeconds = Math.Min(elapsedSeconds, 1);
        _lastTimestamp = reading.TimestampUtc;

        UpdateHeading(reading, elapsedSeconds);
        var step = _stepDetector.Update(reading);
        var speed = State.SpeedMetersPerSecond;
        if (step.Detected)
        {
            _lastStepTimestamp = step.TimestampUtc;
            _stepQuality = step.Confidence;
            if (step.IntervalSeconds > 0)
            {
                speed = Math.Min(2.5, _stepLengthMeters / step.IntervalSeconds);
                if (_headingDegrees is { } heading)
                {
                    var radians = heading * Math.PI / 180;
                    var displacement = new Vector2(Math.Sin(radians), Math.Cos(radians)) * _stepLengthMeters;
                    _positionFilter.Predict(displacement, 0.025 + (_stepLengthMeters * 0.025));
                }
            }
        }
        else if (_lastStepTimestamp is not { } lastStep || (reading.TimestampUtc - lastStep).TotalSeconds > 1)
        {
            speed = 0;
        }

        UpdateAltitude(reading.PressureKilopascals);
        var activity = _activityClassifier.Update(reading, step.Detected, _altitudeMeters);

        MapMatchResult? mapMatch = null;
        if (_graph is { Edges.Count: > 0 })
        {
            mapMatch = MapMatcher.Match(_positionFilter.Position, _graph, _mapMatchToleranceMeters, _level);
            if (mapMatch is not null)
                _positionFilter.Correct(mapMatch.Position, Math.Max(0.01, Math.Pow(mapMatch.DistanceMeters * 0.5 + 0.1, 2)));
        }

        var agedStepQuality = _lastStepTimestamp is { } lastStepAt
            ? _stepQuality * Math.Exp(-Math.Max(0, (reading.TimestampUtc - lastStepAt).TotalSeconds) / 30)
            : 0;
        var sensorQuality = 0.12 + (reading.Acceleration is not null || reading.AccelerationIncludingGravity is not null ? 0.22 : 0) + (_headingQuality * 0.24) + (agedStepQuality * 0.18) + (mapMatch is not null ? 0.24 : 0);
        var uncertaintyQuality = 1 / (1 + (_positionFilter.StandardDeviation / 10));
        var confidence = Math.Clamp(sensorQuality * uncertaintyQuality, 0, 1);
        var position = new Vector3(_positionFilter.Position.X, _positionFilter.Position.Y, _altitudeMeters ?? 0);
        State = new NavigationState(reading.TimestampUtc, position, _headingDegrees, speed, _stepDetector.StepCount, confidence, _positionFilter.StandardDeviation, mapMatch is not null, mapMatch?.Position, activity);
        return State;
    }

    private void UpdateHeading(SensorReading reading, double elapsedSeconds)
    {
        if (GetVerticalHeadingRate(reading) is { } headingRate && _headingDegrees is { } current)
            _headingDegrees = Normalize(current + (headingRate * elapsedSeconds));

        if (reading.HeadingDegrees is { } absoluteHeading && reading.OrientationIsAbsolute)
        {
            var relativeHeading = Normalize(absoluteHeading - _headingOffsetDegrees);
            if (_headingDegrees is null) _headingDegrees = relativeHeading;
            else _headingDegrees = Normalize(_headingDegrees.Value + (ShortestAngleDelta(_headingDegrees.Value, relativeHeading) * 0.25));
            _headingQuality = 1;
        }
        else if (GetVerticalHeadingRate(reading) is not null)
        {
            _headingQuality = Math.Max(_headingQuality * 0.995, 0.35);
        }
        else
        {
            _headingQuality *= 0.999;
        }
    }

    private static double? GetVerticalHeadingRate(SensorReading reading)
    {
        if (reading.RotationRateDegreesPerSecond is not { } rate || reading.AccelerationIncludingGravity is not { } gravity) return null;
        if (gravity.Magnitude < 6 || gravity.Magnitude > 13) return null;
        return ((rate.X * gravity.X) + (rate.Y * gravity.Y) + (rate.Z * gravity.Z)) / gravity.Magnitude;
    }

    private void UpdateAltitude(double? pressureKilopascals)
    {
        if (pressureKilopascals is not { } pressure || pressure <= 0) return;
        _referencePressureKilopascals ??= pressure;
        var altitude = 44330 * (1 - Math.Pow(pressure / _referencePressureKilopascals.Value, 1 / 5.255));
        if (!double.IsFinite(altitude)) return;
        _altitudeMeters = _altitudeMeters is null ? altitude : (_altitudeMeters.Value * 0.9) + (altitude * 0.1);
    }

    private static double Normalize(double degrees) => ((degrees % 360) + 360) % 360;
    private static double ShortestAngleDelta(double from, double to) => ((to - from + 540) % 360) - 180;
}
