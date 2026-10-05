namespace Qsys.SensorApp.Core.Geometry;

/// <summary>An axis-aligned two-dimensional bounding box.</summary>
public readonly record struct BoundingBox
{
    /// <summary>Creates a box from its minimum and maximum corners.</summary>
    public BoundingBox(Vector2 minimum, Vector2 maximum)
    {
        if (minimum.X > maximum.X || minimum.Y > maximum.Y)
            throw new ArgumentException("Minimum coordinates must not exceed maximum coordinates.", nameof(minimum));
        Minimum = minimum;
        Maximum = maximum;
    }

    /// <summary>Gets the minimum corner.</summary>
    public Vector2 Minimum { get; }
    /// <summary>Gets the maximum corner.</summary>
    public Vector2 Maximum { get; }
    /// <summary>Gets the box width.</summary>
    public double Width => Maximum.X - Minimum.X;
    /// <summary>Gets the box height.</summary>
    public double Height => Maximum.Y - Minimum.Y;
    /// <summary>Gets the box center.</summary>
    public Vector2 Center => new((Minimum.X / 2d) + (Maximum.X / 2d), (Minimum.Y / 2d) + (Maximum.Y / 2d));

    /// <summary>Returns whether the box contains a point, including its boundary.</summary>
    public bool Contains(Vector2 point) => point.X >= Minimum.X && point.X <= Maximum.X && point.Y >= Minimum.Y && point.Y <= Maximum.Y;

    /// <summary>Returns whether this box intersects another box, including boundary contact.</summary>
    public bool Intersects(BoundingBox other) =>
        Minimum.X <= other.Maximum.X && Maximum.X >= other.Minimum.X &&
        Minimum.Y <= other.Maximum.Y && Maximum.Y >= other.Minimum.Y;

    /// <summary>Returns the smallest box containing this box and another box.</summary>
    public BoundingBox Union(BoundingBox other) => new(
        new(Math.Min(Minimum.X, other.Minimum.X), Math.Min(Minimum.Y, other.Minimum.Y)),
        new(Math.Max(Maximum.X, other.Maximum.X), Math.Max(Maximum.Y, other.Maximum.Y)));

    /// <summary>Creates the smallest box containing the supplied points.</summary>
    public static BoundingBox FromPoints(IEnumerable<Vector2> points)
    {
        ArgumentNullException.ThrowIfNull(points);
        using var enumerator = points.GetEnumerator();
        if (!enumerator.MoveNext()) throw new ArgumentException("At least one point is required.", nameof(points));
        var minimumX = enumerator.Current.X;
        var maximumX = minimumX;
        var minimumY = enumerator.Current.Y;
        var maximumY = minimumY;
        while (enumerator.MoveNext())
        {
            minimumX = Math.Min(minimumX, enumerator.Current.X);
            maximumX = Math.Max(maximumX, enumerator.Current.X);
            minimumY = Math.Min(minimumY, enumerator.Current.Y);
            maximumY = Math.Max(maximumY, enumerator.Current.Y);
        }
        return new(new(minimumX, minimumY), new(maximumX, maximumY));
    }
}
