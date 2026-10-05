namespace Qsys.SensorApp.Core.Geometry;

/// <summary>A finite two-dimensional vector in Cartesian coordinates.</summary>
public readonly record struct Vector2
{
    /// <summary>Creates a vector from its horizontal and vertical components.</summary>
    public Vector2(double x, double y)
    {
        if (!double.IsFinite(x)) throw new ArgumentOutOfRangeException(nameof(x), "Coordinate must be finite.");
        if (!double.IsFinite(y)) throw new ArgumentOutOfRangeException(nameof(y), "Coordinate must be finite.");
        X = x;
        Y = y;
    }

    /// <summary>Gets the horizontal component.</summary>
    public double X { get; }
    /// <summary>Gets the vertical component.</summary>
    public double Y { get; }
    /// <summary>Gets the zero vector.</summary>
    public static Vector2 Zero => new(0, 0);
    /// <summary>Gets the unit vector along the positive X axis.</summary>
    public static Vector2 UnitX => new(1, 0);
    /// <summary>Gets the unit vector along the positive Y axis.</summary>
    public static Vector2 UnitY => new(0, 1);
    /// <summary>Gets the squared Euclidean length.</summary>
    public double LengthSquared => (X * X) + (Y * Y);
    /// <summary>Gets the Euclidean length.</summary>
    public double Length => MathHelper.Hypot(X, Y);
    /// <summary>Gets a unit vector in the same direction, or zero when this vector is zero.</summary>
    public Vector2 Normalized
    {
        get
        {
            var scale = Math.Max(Math.Abs(X), Math.Abs(Y));
            if (scale == 0) return Zero;
            var scaledX = X / scale;
            var scaledY = Y / scale;
            var scaledLength = Math.Sqrt((scaledX * scaledX) + (scaledY * scaledY));
            return new(scaledX / scaledLength, scaledY / scaledLength);
        }
    }
    /// <summary>Returns the dot product with another vector.</summary>
    public double Dot(Vector2 other) => (X * other.X) + (Y * other.Y);
    /// <summary>Returns the signed 2D cross product with another vector.</summary>
    public double Cross(Vector2 other) => (X * other.Y) - (Y * other.X);

    /// <summary>Returns the vector rotated counterclockwise by an angle in radians.</summary>
    public Vector2 Rotate(double radians)
    {
        if (!double.IsFinite(radians)) throw new ArgumentOutOfRangeException(nameof(radians));
        var cosine = Math.Cos(radians);
        var sine = Math.Sin(radians);
        return new((X * cosine) - (Y * sine), (X * sine) + (Y * cosine));
    }

    /// <summary>Adds two vectors.</summary>
    public static Vector2 operator +(Vector2 left, Vector2 right) => new(left.X + right.X, left.Y + right.Y);
    /// <summary>Subtracts one vector from another.</summary>
    public static Vector2 operator -(Vector2 left, Vector2 right) => new(left.X - right.X, left.Y - right.Y);
    /// <summary>Negates a vector.</summary>
    public static Vector2 operator -(Vector2 value) => new(-value.X, -value.Y);
    /// <summary>Scales a vector by a finite scalar.</summary>
    public static Vector2 operator *(Vector2 value, double scalar)
    {
        if (!double.IsFinite(scalar)) throw new ArgumentOutOfRangeException(nameof(scalar));
        return new(value.X * scalar, value.Y * scalar);
    }
    /// <summary>Scales a vector by a finite scalar.</summary>
    public static Vector2 operator *(double scalar, Vector2 value) => value * scalar;

    /// <summary>Divides a vector by a non-zero finite scalar.</summary>
    public static Vector2 operator /(Vector2 value, double scalar)
    {
        if (!double.IsFinite(scalar) || scalar == 0) throw new ArgumentOutOfRangeException(nameof(scalar));
        return new(value.X / scalar, value.Y / scalar);
    }
}
