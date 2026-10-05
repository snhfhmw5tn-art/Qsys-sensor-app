namespace Qsys.SensorApp.Core.Geometry;

/// <summary>An axis-aligned rectangle in Cartesian coordinates.</summary>
public readonly record struct Rectangle
{
    /// <summary>Creates a rectangle from its lower-left origin and non-negative dimensions.</summary>
    public Rectangle(double x, double y, double width, double height)
    {
        if (!double.IsFinite(x)) throw new ArgumentOutOfRangeException(nameof(x));
        if (!double.IsFinite(y)) throw new ArgumentOutOfRangeException(nameof(y));
        if (!double.IsFinite(width) || width < 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (!double.IsFinite(height) || height < 0) throw new ArgumentOutOfRangeException(nameof(height));
        if (!double.IsFinite(x + width)) throw new ArgumentOutOfRangeException(nameof(width), "Rectangle edge must be finite.");
        if (!double.IsFinite(y + height)) throw new ArgumentOutOfRangeException(nameof(height), "Rectangle edge must be finite.");
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>Gets the X coordinate of the lower-left corner.</summary>
    public double X { get; }
    /// <summary>Gets the Y coordinate of the lower-left corner.</summary>
    public double Y { get; }
    /// <summary>Gets the width.</summary>
    public double Width { get; }
    /// <summary>Gets the height.</summary>
    public double Height { get; }
    /// <summary>Gets the center point.</summary>
    public Vector2 Center => new(X + (Width / 2d), Y + (Height / 2d));
    /// <summary>Gets the enclosing axis-aligned box.</summary>
    public BoundingBox Bounds => new(new(X, Y), new(X + Width, Y + Height));
    /// <summary>Returns whether a point lies inside or on the rectangle boundary.</summary>
    public bool Contains(Vector2 point) => Bounds.Contains(point);
}
