using Qsys.SensorApp.Core.Geometry;

namespace Qsys.SensorApp.Core.Tests.Geometry;

[TestClass]
public sealed class VectorTests
{
    [TestMethod]
    public void TestThat_vector2_supports_arithmetic_and_dot_cross_products()
    {
        var first = new Vector2(3, 4);
        var second = new Vector2(2, -1);

        Assert.AreEqual(new Vector2(5, 3), first + second);
        Assert.AreEqual(new Vector2(1, 5), first - second);
        Assert.AreEqual(new Vector2(-3, -4), -first);
        Assert.AreEqual(new Vector2(6, 8), first * 2);
        Assert.AreEqual(new Vector2(1.5, 2), first / 2);
        Assert.AreEqual(2, first.Dot(second));
        Assert.AreEqual(-11, first.Cross(second));
    }

    [TestMethod]
    public void TestThat_vector2_rotates_counterclockwise_and_normalizes()
    {
        var rotated = Vector2.UnitX.Rotate(Math.PI / 2d);

        Assert.AreEqual(0, rotated.X, 1e-12);
        Assert.AreEqual(1, rotated.Y, 1e-12);
        Assert.AreEqual(1, new Vector2(3, 4).Normalized.Length, 1e-12);
        Assert.AreEqual(Vector2.Zero, Vector2.Zero.Normalized);
    }

    [TestMethod]
    public void TestThat_normalization_is_stable_for_large_finite_components()
    {
        var vector2 = new Vector2(1e300, 1e300).Normalized;
        var vector3 = new Vector3(1e300, 1e300, 1e300).Normalized;

        Assert.AreEqual(1, vector2.Length, 1e-12);
        Assert.AreEqual(1, vector3.Length, 1e-12);
        Assert.AreEqual(1e300, new Vector2(1e300, 0).Length);
    }

    [TestMethod]
    public void TestThat_quaternion_normalization_and_inverse_are_stable_for_large_components()
    {
        var quaternion = new Quaternion(1e300, -1e300, 1e300, 1e300);
        var normalized = quaternion.Normalized;
        var inverse = quaternion.Inverse;

        Assert.AreEqual(1, normalized.Length, 1e-12);
        Assert.IsTrue(double.IsFinite(inverse.X));
        Assert.IsTrue(double.IsFinite(inverse.W));
        Assert.AreEqual(1, quaternion.Rotate(Vector3.UnitX).Length, 1e-12);
    }

    [TestMethod]
    public void TestThat_vectors_reject_non_finite_components_and_zero_divisors()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Vector2(double.NaN, 0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Vector3(0, double.PositiveInfinity, 0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = Vector2.UnitX / 0);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => _ = Vector2.UnitX * double.PositiveInfinity);
    }

    [TestMethod]
    public void TestThat_vector3_cross_product_is_orthogonal_to_inputs()
    {
        var cross = Vector3.UnitX.Cross(Vector3.UnitY);

        Assert.AreEqual(Vector3.UnitZ, cross);
        Assert.AreEqual(0, cross.Dot(Vector3.UnitX));
        Assert.AreEqual(0, cross.Dot(Vector3.UnitY));
    }

    [TestMethod]
    public void TestThat_quaternion_rotates_a_vector_about_an_axis()
    {
        var rotation = Quaternion.FromAxisAngle(Vector3.UnitZ, Math.PI / 2d);
        var result = rotation.Rotate(Vector3.UnitX);

        Assert.AreEqual(0, result.X, 1e-12);
        Assert.AreEqual(1, result.Y, 1e-12);
        Assert.AreEqual(0, result.Z, 1e-12);
    }

    [TestMethod]
    public void TestThat_quaternion_inverse_restores_the_original_vector()
    {
        var rotation = Quaternion.FromAxisAngle(new Vector3(1, 2, 3), 1.25);
        var original = new Vector3(4, -2, 7);

        var restored = rotation.Inverse.Rotate(rotation.Rotate(original));

        Assert.AreEqual(original.X, restored.X, 1e-12);
        Assert.AreEqual(original.Y, restored.Y, 1e-12);
        Assert.AreEqual(original.Z, restored.Z, 1e-12);
    }

    [TestMethod]
    public void TestThat_quaternion_rejects_zero_rotation_axis()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Quaternion.FromAxisAngle(Vector3.Zero, 1));
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = new Quaternion(0, 0, 0, 0).Normalized);
    }
}
