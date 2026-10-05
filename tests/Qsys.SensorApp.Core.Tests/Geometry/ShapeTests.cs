using Qsys.SensorApp.Core.Geometry;

namespace Qsys.SensorApp.Core.Tests.Geometry;

[TestClass]
public sealed class ShapeTests
{
    [TestMethod]
    public void TestThat_circle_reports_area_circumference_and_boundary_containment()
    {
        var circle = new Circle(new Vector2(1, 2), 2);

        Assert.AreEqual(4 * Math.PI, circle.Area, 1e-12);
        Assert.AreEqual(4 * Math.PI, circle.Circumference, 1e-12);
        Assert.IsTrue(circle.Contains(new Vector2(3, 2)));
        Assert.IsFalse(circle.Contains(new Vector2(3.1, 2)));
    }

    [TestMethod]
    public void TestThat_circle_rejects_negative_or_non_finite_radius()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Circle(Vector2.Zero, -1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Circle(Vector2.Zero, double.NaN));
    }

    [TestMethod]
    public void TestThat_rectangle_includes_edges_and_returns_its_bounds()
    {
        var rectangle = new Rectangle(2, 3, 5, 4);

        Assert.AreEqual(new Vector2(4.5, 5), rectangle.Center);
        Assert.AreEqual(new BoundingBox(new Vector2(2, 3), new Vector2(7, 7)), rectangle.Bounds);
        Assert.IsTrue(rectangle.Contains(new Vector2(7, 7)));
        Assert.IsFalse(rectangle.Contains(new Vector2(7.01, 7)));
    }

    [TestMethod]
    public void TestThat_rectangle_rejects_negative_dimensions()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new Rectangle(0, 0, -1, 2));
    }

    [TestMethod]
    public void TestThat_bounding_boxes_include_touching_boundaries_and_union()
    {
        var first = new BoundingBox(new Vector2(0, 0), new Vector2(2, 2));
        var touching = new BoundingBox(new Vector2(2, 1), new Vector2(4, 3));

        Assert.IsTrue(first.Intersects(touching));
        Assert.AreEqual(new BoundingBox(new Vector2(0, 0), new Vector2(4, 3)), first.Union(touching));
        Assert.IsTrue(first.Contains(new Vector2(0, 1)));
    }

    [TestMethod]
    public void TestThat_bounding_box_from_points_finds_minimum_and_maximum()
    {
        var bounds = BoundingBox.FromPoints([new Vector2(4, -2), new Vector2(-1, 7), new Vector2(3, 1)]);

        Assert.AreEqual(new BoundingBox(new Vector2(-1, -2), new Vector2(4, 7)), bounds);
        Assert.ThrowsExactly<ArgumentException>(() => BoundingBox.FromPoints([]));
    }

    [TestMethod]
    public void TestThat_polyline_copies_points_and_sums_segment_lengths()
    {
        var source = new[] { Vector2.Zero, new Vector2(3, 0), new Vector2(3, 4) };
        var polyline = new Polyline(source);
        source[1] = new Vector2(300, 0);

        Assert.AreEqual(7, polyline.Length);
        Assert.AreEqual(2, polyline.Segments.Count());
        Assert.AreEqual(new Vector2(3, 0), polyline.Points[1]);
    }

    [TestMethod]
    public void TestThat_polyline_requires_at_least_two_points()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new Polyline([Vector2.Zero]));
    }

    [TestMethod]
    public void TestThat_polygon_calculates_area_perimeter_and_contains_boundary()
    {
        var polygon = new Polygon([Vector2.Zero, new Vector2(4, 0), new Vector2(4, 3), new Vector2(0, 3), Vector2.Zero]);

        Assert.AreEqual(12, polygon.Area);
        Assert.AreEqual(14, polygon.Perimeter);
        Assert.AreEqual(4, polygon.Count);
        Assert.IsTrue(polygon.Contains(new Vector2(2, 1)));
        Assert.IsTrue(polygon.Contains(new Vector2(4, 1)));
        Assert.IsFalse(polygon.Contains(new Vector2(5, 1)));
    }

    [TestMethod]
    public void TestThat_polygon_area_sign_indicates_vertex_winding()
    {
        var counterclockwise = new Polygon([Vector2.Zero, Vector2.UnitX, Vector2.UnitY]);
        var clockwise = new Polygon([Vector2.Zero, Vector2.UnitY, Vector2.UnitX]);

        Assert.IsGreaterThan(0d, counterclockwise.SignedArea);
        Assert.IsLessThan(0d, clockwise.SignedArea);
    }

    [TestMethod]
    public void TestThat_polygon_requires_three_distinct_vertices()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new Polygon([Vector2.Zero, Vector2.UnitX, Vector2.Zero]));
    }

    [TestMethod]
    public void TestThat_polygon_rejects_degenerate_and_self_intersecting_boundaries()
    {
        Assert.ThrowsExactly<ArgumentException>(() => new Polygon([Vector2.Zero, Vector2.UnitX, new Vector2(2, 0)]));
        Assert.ThrowsExactly<ArgumentException>(() => new Polygon([
            Vector2.Zero, new Vector2(4, 4), new Vector2(0, 4), new Vector2(3, 0)]));
        Assert.ThrowsExactly<ArgumentException>(() => new Polygon([
            Vector2.Zero, new Vector2(3, 0), new Vector2(1, 0), Vector2.UnitY]));
    }
}
