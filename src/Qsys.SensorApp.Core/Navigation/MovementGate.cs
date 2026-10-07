namespace Qsys.SensorApp.Core.Navigation;

/// <summary>Settings for confirming sustained movement from a sequence of acceleration peaks.</summary>
public sealed record MovementGateSettings(
    double StepPeakThresholdMetersPerSecondSquared = 0.65,
    int StepsToConfirmStart = 3,
    double CadenceVariationTolerance = 0.45,
    double StopAfterSeconds = 1.8,
    double TurnRateThresholdDegreesPerSecond = 90);

/// <summary>Requires a repeatable step cadence before allowing step detections to move the estimated position.</summary>
public sealed class MovementGate
{
    private readonly List<StepDetection> _candidates = [];
    private DateTimeOffset? _lastAcceptedStep;
    private DateTimeOffset? _turnQuietSince;
    private double? _previousInterval;

    /// <summary>Gets the currently applied movement confirmation settings.</summary>
    public MovementGateSettings Settings { get; private set; } = new();

    /// <summary>Gets whether the current sensor sequence has confirmed sustained movement.</summary>
    public bool IsMoving { get; private set; }

    /// <summary>Gets whether a rapid device turn is temporarily suppressing step movement.</summary>
    public bool IsTurnSuppressed { get; private set; }

    /// <summary>Gets the provisional steps currently waiting for cadence confirmation.</summary>
    public int PendingStepCount => IsMoving ? 0 : _candidates.Count;

    /// <summary>Applies updated tuning values immediately to the active session.</summary>
    public void Configure(MovementGateSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (!double.IsFinite(settings.StepPeakThresholdMetersPerSecondSquared) || settings.StepPeakThresholdMetersPerSecondSquared is < 0.05 or > 3)
            throw new ArgumentOutOfRangeException(nameof(settings.StepPeakThresholdMetersPerSecondSquared));
        if (settings.StepsToConfirmStart is < 2 or > 6)
            throw new ArgumentOutOfRangeException(nameof(settings.StepsToConfirmStart));
        if (!double.IsFinite(settings.CadenceVariationTolerance) || settings.CadenceVariationTolerance is < 0.01 or > 1)
            throw new ArgumentOutOfRangeException(nameof(settings.CadenceVariationTolerance));
        if (!double.IsFinite(settings.StopAfterSeconds) || settings.StopAfterSeconds is < 0.1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(settings.StopAfterSeconds));
        if (!double.IsFinite(settings.TurnRateThresholdDegreesPerSecond) || settings.TurnRateThresholdDegreesPerSecond is < 40 or > 400)
            throw new ArgumentOutOfRangeException(nameof(settings.TurnRateThresholdDegreesPerSecond));

        Settings = settings;
        _candidates.Clear();
        _previousInterval = null;
    }

    /// <summary>Updates the gate and returns how many steps should now be applied to the track.</summary>
    public int Update(StepDetection step, DateTimeOffset timestamp, double? rotationRateDegreesPerSecond = null)
    {
        if (rotationRateDegreesPerSecond is { } rotation && rotation >= Settings.TurnRateThresholdDegreesPerSecond)
        {
            // A fast pivot often creates acceleration peaks even though the person has not translated; freeze the track while turning.
            ResetMovement();
            IsTurnSuppressed = true;
            _turnQuietSince = null;
            return 0;
        }

        if (IsTurnSuppressed)
        {
            if (rotationRateDegreesPerSecond is not { } calmRate || calmRate > Settings.TurnRateThresholdDegreesPerSecond * 0.6)
            {
                _turnQuietSince = null;
                return 0;
            }

            _turnQuietSince ??= timestamp;
            if ((timestamp - _turnQuietSince.Value).TotalSeconds < 0.6) return 0;
            IsTurnSuppressed = false;
            _turnQuietSince = null;
            return 0;
        }

        if (!step.Detected)
        {
            if (_lastAcceptedStep is { } last && (timestamp - last).TotalSeconds >= Settings.StopAfterSeconds)
                ResetMovement();
            else if (_candidates.Count > 0 && (timestamp - _candidates[^1].TimestampUtc).TotalSeconds >= Settings.StopAfterSeconds)
                _candidates.Clear();
            return 0;
        }

        // A long gap means the user stopped. Treat the next peak as a fresh candidate, not a continuation.
        var gap = _lastAcceptedStep is { } accepted ? (step.TimestampUtc - accepted).TotalSeconds : double.PositiveInfinity;
        if (IsMoving && gap >= Settings.StopAfterSeconds)
            ResetMovement();

        if (IsMoving)
        {
            if (!HasConsistentCadence(step.IntervalSeconds))
            {
                // Abrupt hand motion commonly creates isolated or irregular peaks; pause until cadence is steady again.
                ResetMovement();
                _candidates.Add(step);
                return 0;
            }

            _previousInterval = step.IntervalSeconds;
            _lastAcceptedStep = step.TimestampUtc;
            return 1;
        }

        if (_candidates.Count > 0)
        {
            var previousCandidate = _candidates[^1];
            var interval = (step.TimestampUtc - previousCandidate.TimestampUtc).TotalSeconds;
            if (!IsPlausibleInterval(interval) || (_previousInterval is { } prior && RelativeDifference(interval, prior) > Settings.CadenceVariationTolerance))
            {
                _candidates.Clear();
                _previousInterval = null;
            }
            else
            {
                _previousInterval = interval;
            }
        }

        _candidates.Add(step);
        if (_candidates.Count < Settings.StepsToConfirmStart) return 0;

        // Confirmation releases the buffered steps together so the drawn distance includes the startup sequence.
        IsMoving = true;
        _lastAcceptedStep = step.TimestampUtc;
        return _candidates.Count;
    }

    /// <summary>Clears the current movement confirmation history.</summary>
    public void Reset()
    {
        _candidates.Clear();
        _lastAcceptedStep = null;
        _previousInterval = null;
        IsMoving = false;
        IsTurnSuppressed = false;
        _turnQuietSince = null;
    }

    private bool HasConsistentCadence(double interval) => IsPlausibleInterval(interval) &&
        (_previousInterval is null || RelativeDifference(interval, _previousInterval.Value) <= Settings.CadenceVariationTolerance);

    private static bool IsPlausibleInterval(double interval) => double.IsFinite(interval) && interval is >= 0.2 and <= 2.5;
    private static double RelativeDifference(double left, double right) => Math.Abs(left - right) / Math.Max(left, right);

    private void ResetMovement()
    {
        IsMoving = false;
        _lastAcceptedStep = null;
        _previousInterval = null;
        _candidates.Clear();
    }
}
