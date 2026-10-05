namespace Qsys.SensorApp.Core.Geometry;

/// <summary>A finite three-dimensional vector in Cartesian coordinates.</summary>
public readonly record struct Vector3
{
    /// <summary>Creates a vector from its Cartesian components.</summary>
    public Vector3(double x, double y, double z)
    {
        if (!double.IsFinite(x)) throw new ArgumentOutOfRangeException(nameof(x), "Coordinate must be finite.");
        if (!double.IsFinite(y)) throw new ArgumentOutOfRangeException(nameof(y), "Coordinate must be finite.");
        if (!double.IsFinite(z)) throw new ArgumentOutOfRangeException(nameof(z), "Coordinate must be finite.");
        X = x;
        Y = y;
        Z = z;
    }

    /// <summary>Gets the X component.</summary>
    public double X { get; }
    /// <summary>Gets the Y component.</summary>
    public double Y { get; }
    /// <summary>Gets the Z component.</summary>
    public double Z { get; }
    /// <summary>Gets the zero vector.</summary>
    public static Vector3 Zero => new(0, 0, 0);
    /// <summary>Gets the unit vector along the X axis.</summary>
    public static Vector3 UnitX => new(1, 0, 0);
    /// <summary>Gets the unit vector along the Y axis.</summary>
    public static Vector3 UnitY => new(0, 1, 0);
    /// <summary>Gets the unit vector along the Z axis.</summary>
    public static Vector3 UnitZ => new(0, 0, 1);
    /// <summary>Gets the squared Euclidean length.</summary>
    public double LengthSquared => (X * X) + (Y * Y) + (Z * Z);
    /// <summary>Gets the Euclidean length.</summary>
    public double Length => MathHelper.Hypot(X, Y, Z);
    /// <summary>Gets a unit vector in the same direction, or zero when this vector is zero.</summary>
    public Vector3 Normalized
    {
        get
        {
            var scale = Math.Max(Math.Abs(X), Math.Max(Math.Abs(Y), Math.Abs(Z)));
            if (scale == 0) return Zero;
            var scaledX = X / scale;
            var scaledY = Y / scale;
            var scaledZ = Z / scale;
            var scaledLength = Math.Sqrt((scaledX * scaledX) + (scaledY * scaledY) + (scaledZ * scaledZ));
            return new(scaledX / scaledLength, scaledY / scaledLength, scaledZ / scaledLength);
        }
    }
    /// <summary>Returns the dot product with another vector.</summary>
    public double Dot(Vector3 other) => (X * other.X) + (Y * other.Y) + (Z * other.Z);
    /// <summary>Returns the cross product with another vector.</summary>
    public Vector3 Cross(Vector3 other) => new((Y * other.Z) - (Z * other.Y), (Z * other.X) - (X * other.Z), (X * other.Y) - (Y * other.X));
    /// <summary>Adds two vectors.</summary>
    public static Vector3 operator +(Vector3 left, Vector3 right) => new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);
    /// <summary>Subtracts one vector from another.</summary>
    public static Vector3 operator -(Vector3 left, Vector3 right) => new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
    /// <summary>Negates a vector.</summary>
    public static Vector3 operator -(Vector3 value) => new(-value.X, -value.Y, -value.Z);
    /// <summary>Scales a vector by a finite scalar.</summary>
    public static Vector3 operator *(Vector3 value, double scalar)
    {
        if (!double.IsFinite(scalar)) throw new ArgumentOutOfRangeException(nameof(scalar));
        return new(value.X * scalar, value.Y * scalar, value.Z * scalar);
    }
    /// <summary>Scales a vector by a finite scalar.</summary>
    public static Vector3 operator *(double scalar, Vector3 value) => value * scalar;

    /// <summary>Divides a vector by a non-zero finite scalar.</summary>
    public static Vector3 operator /(Vector3 value, double scalar)
    {
        if (!double.IsFinite(scalar) || scalar == 0) throw new ArgumentOutOfRangeException(nameof(scalar));
        return new(value.X / scalar, value.Y / scalar, value.Z / scalar);
    }
}
