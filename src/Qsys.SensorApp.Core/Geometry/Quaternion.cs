namespace Qsys.SensorApp.Core.Geometry;

/// <summary>A finite quaternion for three-dimensional rotations.</summary>
public readonly record struct Quaternion
{
    /// <summary>Creates a quaternion in X, Y, Z, W order.</summary>
    public Quaternion(double x, double y, double z, double w)
    {
        if (!double.IsFinite(x)) throw new ArgumentOutOfRangeException(nameof(x));
        if (!double.IsFinite(y)) throw new ArgumentOutOfRangeException(nameof(y));
        if (!double.IsFinite(z)) throw new ArgumentOutOfRangeException(nameof(z));
        if (!double.IsFinite(w)) throw new ArgumentOutOfRangeException(nameof(w));
        X = x; Y = y; Z = z; W = w;
    }

    /// <summary>Gets the X component.</summary>
    public double X { get; }
    /// <summary>Gets the Y component.</summary>
    public double Y { get; }
    /// <summary>Gets the Z component.</summary>
    public double Z { get; }
    /// <summary>Gets the scalar component.</summary>
    public double W { get; }
    /// <summary>Gets the identity rotation.</summary>
    public static Quaternion Identity => new(0, 0, 0, 1);
    /// <summary>Gets the squared magnitude.</summary>
    public double LengthSquared => (X * X) + (Y * Y) + (Z * Z) + (W * W);
    /// <summary>Gets the magnitude.</summary>
    public double Length => Math.Sqrt(LengthSquared);
    /// <summary>Gets the normalized quaternion; a zero quaternion is rejected.</summary>
    public Quaternion Normalized
    {
        get
        {
            var scale = Math.Max(Math.Max(Math.Abs(X), Math.Abs(Y)), Math.Max(Math.Abs(Z), Math.Abs(W)));
            if (scale == 0) throw new InvalidOperationException("A zero quaternion cannot be normalized.");
            var scaledX = X / scale;
            var scaledY = Y / scale;
            var scaledZ = Z / scale;
            var scaledW = W / scale;
            var scaledLength = Math.Sqrt((scaledX * scaledX) + (scaledY * scaledY) + (scaledZ * scaledZ) + (scaledW * scaledW));
            return new(scaledX / scaledLength, scaledY / scaledLength, scaledZ / scaledLength, scaledW / scaledLength);
        }
    }
    /// <summary>Gets the conjugate quaternion.</summary>
    public Quaternion Conjugate => new(-X, -Y, -Z, W);

    /// <summary>Creates a rotation from a non-zero axis and angle in radians.</summary>
    public static Quaternion FromAxisAngle(Vector3 axis, double radians)
    {
        if (!double.IsFinite(radians)) throw new ArgumentOutOfRangeException(nameof(radians));
        var normalizedAxis = axis.Normalized;
        if (normalizedAxis == Vector3.Zero) throw new ArgumentException("Rotation axis must be non-zero.", nameof(axis));
        var halfAngle = radians / 2d;
        var sine = Math.Sin(halfAngle);
        return new(normalizedAxis.X * sine, normalizedAxis.Y * sine, normalizedAxis.Z * sine, Math.Cos(halfAngle));
    }

    /// <summary>Returns the inverse quaternion; a zero quaternion is rejected.</summary>
    public Quaternion Inverse
    {
        get
        {
            var scale = Math.Max(Math.Max(Math.Abs(X), Math.Abs(Y)), Math.Max(Math.Abs(Z), Math.Abs(W)));
            if (scale == 0) throw new InvalidOperationException("A zero quaternion has no inverse.");
            var scaledX = X / scale;
            var scaledY = Y / scale;
            var scaledZ = Z / scale;
            var scaledW = W / scale;
            var scaledLengthSquared = (scaledX * scaledX) + (scaledY * scaledY) + (scaledZ * scaledZ) + (scaledW * scaledW);
            var factor = (1d / scale) / scaledLengthSquared;
            return new(-scaledX * factor, -scaledY * factor, -scaledZ * factor, scaledW * factor);
        }
    }

    /// <summary>Rotates a vector by this quaternion.</summary>
    public Vector3 Rotate(Vector3 value)
    {
        var unitRotation = Normalized;
        var vectorQuaternion = new Quaternion(value.X, value.Y, value.Z, 0);
        var rotated = unitRotation * vectorQuaternion * unitRotation.Conjugate;
        return new(rotated.X, rotated.Y, rotated.Z);
    }

    /// <summary>Multiplies two quaternions using Hamilton product order.</summary>
    public static Quaternion operator *(Quaternion left, Quaternion right) => new(
        (left.W * right.X) + (left.X * right.W) + (left.Y * right.Z) - (left.Z * right.Y),
        (left.W * right.Y) - (left.X * right.Z) + (left.Y * right.W) + (left.Z * right.X),
        (left.W * right.Z) + (left.X * right.Y) - (left.Y * right.X) + (left.Z * right.W),
        (left.W * right.W) - (left.X * right.X) - (left.Y * right.Y) - (left.Z * right.Z));

    /// <summary>Divides a quaternion by a non-zero finite scalar.</summary>
    public static Quaternion operator /(Quaternion value, double scalar)
    {
        if (!double.IsFinite(scalar) || scalar == 0) throw new ArgumentOutOfRangeException(nameof(scalar));
        return new(value.X / scalar, value.Y / scalar, value.Z / scalar, value.W / scalar);
    }
}
