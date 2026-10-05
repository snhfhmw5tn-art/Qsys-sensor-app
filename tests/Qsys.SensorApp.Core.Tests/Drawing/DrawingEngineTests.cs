using Qsys.SensorApp.Core.Drawing;
using Qsys.SensorApp.Core.Geometry;

namespace Qsys.SensorApp.Core.Tests.Drawing;

[TestClass]
public sealed class DrawingEngineTests
{
    [TestMethod]
    public void TestThat_viewport_scales_and_rotates_within_limits()
    {
        var viewport = new DrawingViewport().Scale(100).Rotate(90);

        Assert.AreEqual(10, viewport.Zoom);
        Assert.AreEqual(90, viewport.RotationDegrees);
    }

    [TestMethod]
    public void TestThat_calibration_converts_drawing_units_to_meters()
    {
        var calibration = new MapCalibration(new Vector2(1, 1), new Vector2(11, 1), 25);

        Assert.AreEqual(2.5, calibration.MetersPerDrawingUnit, 1e-12);
        Assert.AreEqual(12.5, calibration.ToMeters(5), 1e-12);
    }

    [TestMethod]
    public void TestThat_snap_chooses_nearest_point_only_within_tolerance()
    {
        var input = new Vector2(2.1, 2.1);
        var points = new[] { new Vector2(0, 0), new Vector2(2, 2), new Vector2(5, 5) };

        Assert.AreEqual(new Vector2(2, 2), DrawingSnap.ToNearest(input, points, 1));
        Assert.AreEqual(input, DrawingSnap.ToNearest(input, points, 0.1));
        Assert.AreEqual(input, DrawingSnap.ToNearest(input, [], 1));
    }

    [TestMethod]
    public void TestThat_viewport_rejects_invalid_zoom_factors()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new DrawingViewport().Scale(0));
    }
}
