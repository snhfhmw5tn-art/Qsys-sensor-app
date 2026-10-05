namespace Qsys.SensorApp.Core.Navigation;

/// <summary>A heuristic activity label and its confidence from zero to one.</summary>
public sealed record ActivityEstimate(ActivityType Type, double Confidence)
{
    /// <summary>Gets the no-evidence estimate.</summary>
    public static ActivityEstimate Unknown { get; } = new(ActivityType.Unknown, 0);
}
