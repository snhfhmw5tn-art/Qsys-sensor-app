namespace Qsys.SensorApp.Core.Geometry;

/// <summary>A two-dimensional affine transform using column-vector composition.</summary>
public readonly record struct Transform
{
    /// <summary>Creates an affine transform from a 2×2 matrix and translation.</summary>
    public Transform(double m11, double m12, double m21, double m22, double offsetX, double offsetY)
    {
        var values = new[] { m11, m12, m21, m22, offsetX, offsetY };
        if (values.Any(value => !double.IsFinite(value))) throw new ArgumentOutOfRangeException(nameof(m11), "Transform values must be finite.");
        M11 = m11; M12 = m12; M21 = m21; M22 = m22; OffsetX = offsetX; OffsetY = offsetY;
    }

    /// <summary>Gets the first matrix column's X value.</summary>
    public double M11 { get; }
    /// <summary>Gets the first matrix column's Y value.</summary>
    public double M12 { get; }
    /// <summary>Gets the second matrix column's X value.</summary>
    public double M21 { get; }
    /// <summary>Gets the second matrix column's Y value.</summary>
    public double M22 { get; }
    /// <summary>Gets the translation's X value.</summary>
    public double OffsetX { get; }
    /// <summary>Gets the translation's Y value.</summary>
    public double OffsetY { get; }
    /// <summary>Gets the identity transform.</summary>
    public static Transform Identity => new(1, 0, 0, 1, 0, 0);
    /// <summary>Gets the determinant of the linear part.</summary>
    public double Determinant => (M11 * M22) - (M21 * M12);

    /// <summary>Creates a translation transform.</summary>
    public static Transform Translation(double x, double y) => new(1, 0, 0, 1, x, y);
    /// <summary>Creates a non-uniform scale transform.</summary>
    public static Transform Scale(double x, double y) => new(x, 0, 0, y, 0, 0);

    /// <summary>Creates a counterclockwise rotation transform in radians.</summary>
    public static Transform Rotation(double radians)
    {
        if (!double.IsFinite(radians)) throw new ArgumentOutOfRangeException(nameof(radians));
        var cosine = Math.Cos(radians);
        var sine = Math.Sin(radians);
        return new(cosine, sine, -sine, cosine, 0, 0);
    }

    /// <summary>Transforms a point, including translation.</summary>
    public Vector2 Apply(Vector2 point) => new((M11 * point.X) + (M21 * point.Y) + OffsetX, (M12 * point.X) + (M22 * point.Y) + OffsetY);
    /// <summary>Transforms a direction, excluding translation.</summary>
    public Vector2 ApplyDirection(Vector2 direction) => new((M11 * direction.X) + (M21 * direction.Y), (M12 * direction.X) + (M22 * direction.Y));

    /// <summary>Attempts to calculate the inverse; returns false for a singular transform.</summary>
    public bool TryInvert(out Transform inverse)
    {
        var determinant = Determinant;
        if (determinant == 0 || !double.IsFinite(determinant))
        {
            inverse = Identity;
            return false;
        }
        var inverseDeterminant = 1d / determinant;
        var m11 = M22 * inverseDeterminant;
        var m12 = -M12 * inverseDeterminant;
        var m21 = -M21 * inverseDeterminant;
        var m22 = M11 * inverseDeterminant;
        var offsetX = -((m11 * OffsetX) + (m21 * OffsetY));
        var offsetY = -((m12 * OffsetX) + (m22 * OffsetY));
        if (!double.IsFinite(m11) || !double.IsFinite(m12) || !double.IsFinite(m21) || !double.IsFinite(m22) ||
            !double.IsFinite(offsetX) || !double.IsFinite(offsetY))
        {
            inverse = Identity;
            return false;
        }
        inverse = new(m11, m12, m21, m22, offsetX, offsetY);
        return true;
    }

    /// <summary>Composes this transform followed by the supplied transform.</summary>
    public Transform Then(Transform next) => new(
        (next.M11 * M11) + (next.M21 * M12),
        (next.M12 * M11) + (next.M22 * M12),
        (next.M11 * M21) + (next.M21 * M22),
        (next.M12 * M21) + (next.M22 * M22),
        (next.M11 * OffsetX) + (next.M21 * OffsetY) + next.OffsetX,
        (next.M12 * OffsetX) + (next.M22 * OffsetY) + next.OffsetY);
}
