using Qsys.SensorApp.Core.Geometry;
using Qsys.SensorApp.Core.Maps;
using Qsys.SensorApp.Core.Navigation;
using Qsys.SensorApp.Core.Sensors;

namespace Qsys.SensorApp.Core.Tests.Navigation;

[TestClass]
public sealed class NavigationEngineTests
{
    [TestMethod]
    public void TestThat_step_detector_finds_refractory_spaced_acceleration_peaks()
    {
        var detector = new StepDetector();
        var start = DateTimeOffset.UtcNow;
        var detected = 0;
        var pulse = new[] { 0d, 0.35, 0.9, 1.5, 1.1, 0.45, 0d, 0d };

        for (var index = 0; index < 250; index++)
        {
            var value = index % 25 < pulse.Length ? pulse[index % 25] : 0;
            var reading = Reading(start.AddMilliseconds(index * 40), new SensorVector3(0, 0, value));
            if (detector.Update(reading).Detected) detected++;
        }

        Assert.IsTrue(detected is >= 7 and <= 10, $"Expected approximately one step per pulse, got {detected}.");
        Assert.AreEqual(detected, detector.StepCount);
    }

    [TestMethod]
    public void TestThat_dead_reckoning_advances_east_and_reports_speed_and_confidence()
    {
        var engine = new DeadReckoningEngine(Vector3.Zero, initialHeadingDegrees: 90);
        var start = DateTimeOffset.UtcNow;
        var pulse = new[] { 0d, 0.35, 0.9, 1.5, 1.1, 0.45, 0d, 0d };
        NavigationState state = engine.State;

        for (var index = 0; index < 250; index++)
        {
            var value = index % 25 < pulse.Length ? pulse[index % 25] : 0;
            state = engine.Process(Reading(start.AddMilliseconds(index * 40), new SensorVector3(0, 0, value), heading: 90, absolute: true));
        }

        Assert.IsGreaterThanOrEqualTo(7, state.StepCount);
        Assert.IsGreaterThan(4d, state.PositionMeters.X);
        Assert.IsLessThan(0.01, Math.Abs(state.PositionMeters.Y));
        Assert.IsGreaterThan(0d, state.SpeedMetersPerSecond);
        Assert.IsGreaterThanOrEqualTo(0d, state.Confidence);
        Assert.IsLessThanOrEqualTo(1d, state.Confidence);
    }

    [TestMethod]
    public void TestThat_dead_reckoning_rejects_out_of_order_samples()
    {
        var engine = new DeadReckoningEngine(Vector3.Zero);
        var now = DateTimeOffset.UtcNow;
        engine.Process(Reading(now, null));

        Assert.ThrowsExactly<ArgumentException>(() => engine.Process(Reading(now, null)));
    }

    [TestMethod]
    public void TestThat_map_match_projects_to_nearest_edge_and_respects_distance_and_level()
    {
        var featureId = Guid.NewGuid();
        var graph = new NavigationGraph(
            [new NavigationNode(0, new Vector2(0, 0), "0"), new NavigationNode(1, new Vector2(10, 0), "0"), new NavigationNode(2, new Vector2(0, 10), "1"), new NavigationNode(3, new Vector2(10, 10), "1")],
            [new NavigationEdge(0, 1, featureId, WarehouseFeatureKind.Aisle, 10), new NavigationEdge(2, 3, featureId, WarehouseFeatureKind.Aisle, 10)]);

        var match = MapMatcher.Match(new Vector2(5, 1), graph, 2);

        Assert.IsNotNull(match);
        Assert.AreEqual(new Vector2(5, 0), match.Position);
        Assert.AreEqual(1, match.DistanceMeters, 1e-12);
        Assert.IsNull(MapMatcher.Match(new Vector2(5, 10), graph, 2, "0"));
        Assert.IsNull(MapMatcher.Match(new Vector2(5, 4), graph, 2, "0"));
    }

    [TestMethod]
    public void TestThat_kalman_filter_reduces_uncertainty_after_correction()
    {
        var filter = new KalmanPositionFilter(Vector2.Zero);
        filter.Predict(new Vector2(1, 0), 0.1);
        var predictedUncertainty = filter.StandardDeviation;
        filter.Correct(new Vector2(1.1, 0), 0.05);

        Assert.IsLessThan(predictedUncertainty, filter.StandardDeviation);
        Assert.IsGreaterThan(1d, filter.Position.X);
        Assert.IsLessThan(1.1, filter.Position.X);
    }

    [TestMethod]
    public void TestThat_barometric_altitude_is_relative_to_first_pressure_reading()
    {
        var engine = new DeadReckoningEngine(Vector3.Zero);
        var start = DateTimeOffset.UtcNow;
        engine.Process(Reading(start, null, pressure: 101.325));
        var state = engine.Process(Reading(start.AddSeconds(1), null, pressure: 100.0));

        Assert.IsGreaterThan(10d, state.PositionMeters.Z);
        Assert.IsLessThan(12d, state.PositionMeters.Z);
    }

    private static SensorReading Reading(DateTimeOffset timestamp, SensorVector3? acceleration, double? heading = null, bool absolute = false, double? pressure = null) =>
        new(timestamp, 40, acceleration: acceleration, headingDegrees: heading, orientationIsAbsolute: absolute, pressureKilopascals: pressure);
}
