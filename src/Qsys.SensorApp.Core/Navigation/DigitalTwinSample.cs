namespace Qsys.SensorApp.Core.DigitalTwin;

using Qsys.SensorApp.Core.Navigation;

/// <summary>A local warehouse twin snapshot without a device or user identifier.</summary>
public sealed record DigitalTwinSample(Guid SessionId, DateTimeOffset TimestampUtc, double X, double Y, double Z,
    double? HeadingDegrees, double SpeedMetersPerSecond, int StepCount, double Confidence,
    double PositionUncertaintyMeters, ActivityType Activity, double ActivityConfidence,
    double? DeviceForwardHeadingDegrees = null);

/// <summary>One heatmap cell in warehouse-local metre coordinates.</summary>
public sealed record HeatmapCell(int GridX, int GridY, double CenterX, double CenterY, int SampleCount, double Intensity);

/// <summary>Aggregates recorded navigation snapshots into a regular visit-density grid.</summary>
public static class HeatmapAggregator
{
    /// <summary>Groups finite X/Y positions into cells and normalizes counts to an intensity of zero to one.</summary>
    public static IReadOnlyList<HeatmapCell> Aggregate(IEnumerable<DigitalTwinSample> samples, double cellSizeMeters = 2)
    {
        ArgumentNullException.ThrowIfNull(samples);
        if (!double.IsFinite(cellSizeMeters) || cellSizeMeters <= 0) throw new ArgumentOutOfRangeException(nameof(cellSizeMeters));
        var counts = new Dictionary<(int X, int Y), int>();
        foreach (var sample in samples)
        {
            if (!double.IsFinite(sample.X) || !double.IsFinite(sample.Y)) continue;
            var cell = ((int)Math.Floor(sample.X / cellSizeMeters), (int)Math.Floor(sample.Y / cellSizeMeters));
            counts[cell] = counts.GetValueOrDefault(cell) + 1;
        }
        if (counts.Count == 0) return [];
        var maximum = counts.Values.Max();
        return counts.OrderByDescending(entry => entry.Value).ThenBy(entry => entry.Key.X).ThenBy(entry => entry.Key.Y)
            .Select(entry => new HeatmapCell(entry.Key.X, entry.Key.Y, (entry.Key.X + 0.5) * cellSizeMeters,
                (entry.Key.Y + 0.5) * cellSizeMeters, entry.Value, (double)entry.Value / maximum)).ToArray();
    }
}
