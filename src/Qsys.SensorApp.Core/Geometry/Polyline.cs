using System.Collections.ObjectModel;

namespace Qsys.SensorApp.Core.Geometry;

/// <summary>An immutable ordered sequence of at least two points connected by straight segments.</summary>
public sealed class Polyline
{
    private readonly ReadOnlyCollection<Vector2> points;

    /// <summary>Creates a polyline by copying its points.</summary>
    public Polyline(IEnumerable<Vector2> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        var copy = points.ToArray();
        if (copy.Length < 2) throw new ArgumentException("A polyline requires at least two points.", nameof(points));
        this.points = Array.AsReadOnly(copy);
    }

    /// <summary>Gets the polyline vertices in order.</summary>
    public IReadOnlyList<Vector2> Points => points;
    /// <summary>Gets the number of vertices.</summary>
    public int Count => points.Count;
    /// <summary>Gets the sum of segment lengths.</summary>
    public double Length => Enumerable.Range(0, points.Count - 1).Sum(index => Distance.Between(points[index], points[index + 1]));
    /// <summary>Gets the smallest axis-aligned box containing all vertices.</summary>
    public BoundingBox Bounds => BoundingBox.FromPoints(points);
    /// <summary>Enumerates consecutive line segments.</summary>
    public IEnumerable<Line> Segments
    {
        get
        {
            for (var index = 0; index < points.Count - 1; index++)
                yield return new(points[index], points[index + 1]);
        }
    }
}
