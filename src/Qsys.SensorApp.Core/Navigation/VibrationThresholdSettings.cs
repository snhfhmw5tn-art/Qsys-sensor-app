namespace Qsys.SensorApp.Core.Navigation;

/// <summary>Device-tunable starting thresholds for the live vibration inspector.</summary>
public sealed class VibrationThresholdSettings
{
    /// <summary>Gets or sets the largest vibration RMS treated as still.</summary>
    public double StandingRmsMaxMetersPerSecondSquared { get; set; } = 0.15;
    /// <summary>Gets or sets the minimum RMS required before frequency-based motion labels are considered.</summary>
    public double ActiveRmsMinMetersPerSecondSquared { get; set; } = 0.25;
    /// <summary>Gets or sets the lower frequency bound for walking-like vibration.</summary>
    public double WalkingMinHz { get; set; } = 0.7;
    /// <summary>Gets or sets the upper frequency bound for walking-like vibration.</summary>
    public double WalkingMaxHz { get; set; } = 2.5;
    /// <summary>Gets or sets the lower frequency bound for running-like vibration.</summary>
    public double RunningMinHz { get; set; } = 2.5;
    /// <summary>Gets or sets the upper frequency bound for running-like vibration.</summary>
    public double RunningMaxHz { get; set; } = 4.0;
    /// <summary>Gets or sets the lower frequency bound for truck-like vibration.</summary>
    public double TruckMinHz { get; set; } = 4.0;
    /// <summary>Gets or sets the upper frequency bound for truck-like vibration.</summary>
    public double TruckMaxHz { get; set; } = 12.0;
    /// <summary>Gets or sets the rolling sample window length used to estimate vibration frequency.</summary>
    public int EstimationWindowSeconds { get; set; } = 4;

    /// <summary>Validates that the editable activity bands are finite, ordered and within a 25 Hz sensor's Nyquist range.</summary>
    public void Validate()
    {
        if (!double.IsFinite(StandingRmsMaxMetersPerSecondSquared) || StandingRmsMaxMetersPerSecondSquared is < 0.01 or > 5)
            throw new ArgumentOutOfRangeException(nameof(StandingRmsMaxMetersPerSecondSquared));
        if (!double.IsFinite(ActiveRmsMinMetersPerSecondSquared) || ActiveRmsMinMetersPerSecondSquared is < 0.01 or > 5)
            throw new ArgumentOutOfRangeException(nameof(ActiveRmsMinMetersPerSecondSquared));
        if (StandingRmsMaxMetersPerSecondSquared >= ActiveRmsMinMetersPerSecondSquared)
            throw new ArgumentException("The active vibration floor must be higher than the stillness threshold.");
        if (EstimationWindowSeconds is < 1 or > 10)
            throw new ArgumentOutOfRangeException(nameof(EstimationWindowSeconds), "The rolling estimate window must be between 1 and 10 seconds.");

        ValidateBand(WalkingMinHz, WalkingMaxHz, nameof(WalkingMinHz));
        ValidateBand(RunningMinHz, RunningMaxHz, nameof(RunningMinHz));
        ValidateBand(TruckMinHz, TruckMaxHz, nameof(TruckMinHz));
        if (WalkingMaxHz > RunningMinHz || RunningMaxHz > TruckMinHz)
            throw new ArgumentException("Activity frequency bands must be ordered without overlaps: walk, run, then truck.");
    }

    private static void ValidateBand(double minimum, double maximum, string parameterName)
    {
        if (!double.IsFinite(minimum) || !double.IsFinite(maximum) || minimum is < 0.1 or > 12.5 || maximum is < 0.1 or > 12.5 || minimum >= maximum)
            throw new ArgumentOutOfRangeException(parameterName, "Frequency bounds must be ordered and between 0.1 and 12.5 Hz.");
    }
}
