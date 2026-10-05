using Qsys.SensorApp.Core.Geometry;

namespace Qsys.SensorApp.Core.Tests.Geometry;

[TestClass]
public sealed class OperationsTests
{
    [TestMethod]
    [DataRow(0d, 0d)]
    [DataRow(90d, 90d)]
    [DataRow(360d, 0d)]
    [DataRow(-90d, 270d)]
    [DataRow(725d, 5d)]
    [DataRow(-double.Epsilon, 0d)]
    public void TestThat_heading_normalizes_degrees(double input, double expected)
    {
        Assert.AreEqual(expected, new Heading(input).Degrees, 1e-12);
        Assert.AreEqual(expected, MathHelper.NormalizeAngle(input), 1e-12);
    }

    [TestMethod]
    public void TestThat_bearing_is_clockwise_from_north()
    {
        Assert.AreEqual(0, Bearing.Between(Vector2.Zero, Vector2.UnitY).Degrees, 1e-12);
        Assert.AreEqual(90, Bearing.Between(Vector2.Zero, Vector2.UnitX).Degrees, 1e-12);
        Assert.AreEqual(180, Bearing.Between(Vector2.Zero, -Vector2.UnitY).Degrees, 1e-12);
        Assert.AreEqual(270, Bearing.Between(Vector2.Zero, -Vector2.UnitX).Degrees, 1e-12);
    }

    [TestMethod]
    public void TestThat_bearing_rejects_coincident_points()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Bearing.Between(Vector2.Zero, Vector2.Zero));
    }

    [TestMethod]
    public void TestThat_angle_converts_between_degrees_and_radians()
    {
        Assert.AreEqual(Math.PI, new Angle(180).Radians, 1e-12);
        Assert.AreEqual(90, Angle.FromRadians(Math.PI / 2d).Degrees, 1e-12);
        Assert.AreEqual(90d, Heading.FromRadians(Math.PI / 2d).Degrees, 1e-12);
    }

    [TestMethod]
    public void TestThat_math_helper_normalizes_radians_and_compares_with_tolerance()
    {
        Assert.AreEqual(3d * Math.PI / 2d, MathHelper.NormalizeRadians(-Math.PI / 2d), 1e-12);
        Assert.AreEqual(0d, MathHelper.NormalizeRadians(-double.Epsilon));
        Assert.IsTrue(MathHelper.NearlyEqual(1, 1.0000000001, 1e-9));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => MathHelper.NormalizeAngle(double.PositiveInfinity));
    }

    [TestMethod]
    public void TestThat_projection_clamps_to_segment_and_projects_to_unbounded_line()
    {
        var segment = new Line(Vector2.Zero, new Vector2(2, 0));

        Assert.AreEqual(new Vector2(1, 0), Projection.OntoLine(new Vector2(1, 3), segment));
        Assert.AreEqual(new Vector2(2, 0), Projection.OntoSegment(new Vector2(3, 3), segment));
        Assert.AreEqual(new Vector2(2, 0), Projection.OntoVector(new Vector2(2, 4), Vector2.UnitX));
        Assert.AreEqual(3, Distance.PointToLine(new Vector2(1, 3), segment), 1e-12);
        Assert.AreEqual(Math.Sqrt(13), Distance.PointToSegment(new Vector2(4, 3), segment), 1e-12);
    }

    [TestMethod]
    public void TestThat_projection_of_zero_length_line_returns_its_start()
    {
        var segment = new Line(new Vector2(2, 3), new Vector2(2, 3));

        Assert.AreEqual(segment.Start, Projection.OntoSegment(Vector2.Zero, segment));
        Assert.AreEqual(Math.Sqrt(13), Distance.PointToSegment(Vector2.Zero, segment), 1e-12);
    }

    [TestMethod]
    public void TestThat_segment_intersection_finds_crossing_and_collinear_overlap()
    {
        var first = new Line(new Vector2(0, 0), new Vector2(4, 4));
        var second = new Line(new Vector2(0, 4), new Vector2(4, 0));
        var overlap = new Line(new Vector2(2, 2), new Vector2(5, 5));

        Assert.IsTrue(Intersection.TryIntersectSegments(first, second, out var crossing));
        Assert.AreEqual(new Vector2(2, 2), crossing);
        Assert.IsTrue(Intersection.TryIntersectSegments(first, overlap, out var overlapStart));
        Assert.AreEqual(new Vector2(2, 2), overlapStart);
    }

    [TestMethod]
    public void TestThat_segment_intersection_rejects_disjoint_and_parallel_segments()
    {
        var first = new Line(Vector2.Zero, Vector2.UnitX);

        Assert.IsFalse(Intersection.SegmentsIntersect(first, new Line(new Vector2(2, 0), new Vector2(3, 0))));
        Assert.IsFalse(Intersection.SegmentsIntersect(first, new Line(new Vector2(0, 1), new Vector2(1, 1))));
        Assert.AreEqual(1, Distance.SegmentToSegment(first, new Line(new Vector2(0, 1), new Vector2(1, 1))), 1e-12);
    }

    [TestMethod]
    public void TestThat_segment_circle_intersection_returns_nearest_boundary_point()
    {
        var circle = new Circle(Vector2.Zero, 1);
        var segment = new Line(new Vector2(-2, 0), new Vector2(2, 0));

        Assert.IsTrue(Intersection.TryIntersectSegmentCircle(segment, circle, out var point));
        Assert.AreEqual(new Vector2(-1, 0), point);
        Assert.IsFalse(Intersection.TryIntersectSegmentCircle(new Line(new Vector2(2, 2), new Vector2(3, 3)), circle, out _));
    }

    [TestMethod]
    public void TestThat_transform_composes_and_inverts_affine_operations()
    {
        var transform = Transform.Scale(2, 3).Then(Transform.Translation(5, -1));
        var point = new Vector2(4, 2);

        Assert.AreEqual(new Vector2(13, 5), transform.Apply(point));
        Assert.IsTrue(transform.TryInvert(out var inverse));
        Assert.AreEqual(point.X, inverse.Apply(transform.Apply(point)).X, 1e-12);
        Assert.AreEqual(point.Y, inverse.Apply(transform.Apply(point)).Y, 1e-12);
        Assert.IsFalse(Transform.Scale(0, 1).TryInvert(out _));
    }

    [TestMethod]
    public void TestThat_line_point_at_uses_unbounded_parameter()
    {
        var line = new Line(Vector2.Zero, Vector2.UnitX);

        Assert.AreEqual(new Vector2(2.5, 0), line.PointAt(2.5));
        Assert.IsTrue(new Line(Vector2.Zero, Vector2.Zero).IsDegenerate);
    }
}
