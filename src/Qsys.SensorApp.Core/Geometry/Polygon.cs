using System.Collections.ObjectModel;

namespace Qsys.SensorApp.Core.Geometry;

/// <summary>An immutable simple closed polygon whose edges connect the vertices cyclically.</summary>
public sealed class Polygon
{
    private readonly ReadOnlyCollection<Vector2> vertices;

    /// <summary>Creates a polygon from at least three distinct vertices.</summary>
    public Polygon(IEnumerable<Vector2> vertices)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        var copy = new List<Vector2>();
        foreach (var vertex in vertices)
        {
            if (copy.Count == 0 || copy[^1] != vertex) copy.Add(vertex);
        }
        if (copy.Count > 1 && copy[0] == copy[^1]) copy.RemoveAt(copy.Count - 1);
        if (copy.Count < 3 || copy.Distinct().Count() < 3)
            throw new ArgumentException("A polygon requires at least three distinct vertices.", nameof(vertices));
        var origin = copy[0];
        var twiceArea = 0d;
        for (var index = 0; index < copy.Count; index++)
        {
            var current = copy[index] - origin;
            var next = copy[(index + 1) % copy.Count] - origin;
            twiceArea += current.Cross(next);
        }
        if (twiceArea == 0) throw new ArgumentException("Polygon vertices must enclose a non-zero area.", nameof(vertices));

        var edges = Enumerable.Range(0, copy.Count)
            .Select(index => new Line(copy[index], copy[(index + 1) % copy.Count]))
            .ToArray();
        for (var index = 0; index < copy.Count; index++)
        {
            var previous = copy[(index + copy.Count - 1) % copy.Count] - copy[index];
            var next = copy[(index + 1) % copy.Count] - copy[index];
            if (Math.Abs(previous.Cross(next)) <= MathHelper.DefaultTolerance * previous.Length * next.Length && previous.Dot(next) > 0)
                throw new ArgumentException("Adjacent polygon edges must not overlap.", nameof(vertices));
        }
        for (var firstIndex = 0; firstIndex < edges.Length; firstIndex++)
        {
            for (var secondIndex = firstIndex + 1; secondIndex < edges.Length; secondIndex++)
            {
                var adjacent = secondIndex == firstIndex + 1 || (firstIndex == 0 && secondIndex == edges.Length - 1);
                if (!adjacent && Intersection.SegmentsIntersect(edges[firstIndex], edges[secondIndex]))
                    throw new ArgumentException("Polygon edges must not cross or touch except at adjacent vertices.", nameof(vertices));
            }
        }
        this.vertices = Array.AsReadOnly(copy.ToArray());
    }

    /// <summary>Gets the polygon vertices in boundary order; the closing vertex is implicit.</summary>
    public IReadOnlyList<Vector2> Vertices => vertices;
    /// <summary>Gets the number of vertices.</summary>
    public int Count => vertices.Count;
    /// <summary>Gets the signed area; positive values indicate counterclockwise winding.</summary>
    public double SignedArea
    {
        get
        {
            var twiceArea = 0d;
            var origin = vertices[0];
            for (var index = 0; index < vertices.Count; index++)
            {
                var current = vertices[index] - origin;
                var next = vertices[(index + 1) % vertices.Count] - origin;
                twiceArea += current.Cross(next);
            }
            return twiceArea / 2d;
        }
    }

    /// <summary>Gets the unsigned area.</summary>
    public double Area => Math.Abs(SignedArea);
    /// <summary>Gets the sum of edge lengths.</summary>
    public double Perimeter => Edges.Sum(edge => edge.Length);
    /// <summary>Gets the smallest axis-aligned box containing all vertices.</summary>
    public BoundingBox Bounds => BoundingBox.FromPoints(vertices);
    /// <summary>Enumerates polygon edges in boundary order.</summary>
    public IEnumerable<Line> Edges
    {
        get
        {
            for (var index = 0; index < vertices.Count; index++)
                yield return new(vertices[index], vertices[(index + 1) % vertices.Count]);
        }
    }

    /// <summary>Returns whether a point is inside the polygon or on its boundary.</summary>
    public bool Contains(Vector2 point, double tolerance = MathHelper.DefaultTolerance)
    {
        if (!double.IsFinite(tolerance) || tolerance < 0) throw new ArgumentOutOfRangeException(nameof(tolerance));
        var inside = false;
        foreach (var edge in Edges)
        {
            if (Distance.PointToSegment(point, edge) <= tolerance) return true;
            var crosses = (edge.Start.Y > point.Y) != (edge.End.Y > point.Y);
            if (crosses)
            {
                var intersectionX = edge.Start.X + ((point.Y - edge.Start.Y) * (edge.End.X - edge.Start.X) / (edge.End.Y - edge.Start.Y));
                if (point.X < intersectionX) inside = !inside;
            }
        }
        return inside;
    }
}
