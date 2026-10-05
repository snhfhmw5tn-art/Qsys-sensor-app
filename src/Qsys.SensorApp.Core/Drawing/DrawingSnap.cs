using Qsys.SensorApp.Core.Geometry;

namespace Qsys.SensorApp.Core.Drawing;

/// <summary>Finds the closest snap point within a drawing-unit tolerance.</summary>
public static class DrawingSnap
{
    /// <summary>Returns the nearest candidate point, or the input unchanged if no candidate is close enough.</summary>
    public static Vector2 ToNearest(Vector2 point, IEnumerable<Vector2> candidates, double tolerance)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (!double.IsFinite(tolerance) || tolerance < 0) throw new ArgumentOutOfRangeException(nameof(tolerance));
        var matches = candidates.Select(candidate => (Point: candidate, Distance: Distance.Between(point, candidate)))
            .Where(candidate => candidate.Distance <= tolerance)
            .OrderBy(candidate => candidate.Distance)
            .Take(1)
            .ToArray();
        return matches.Length > 0 ? matches[0].Point : point;
    }
}
