using Qsys.SensorApp.Core.Navigation;
using Qsys.SensorApp.Core.Sensors;

namespace Qsys.SensorApp.Core.Tests.Navigation;

[TestClass]
public sealed class ActivityClassifierTests
{
    [TestMethod]
    public void TestThat_step_cadence_distinguishes_walking_and_running()
    {
        var walking = FeedCadence(1.2);
        var running = FeedCadence(2.8);

        Assert.AreEqual(ActivityType.Walking, walking.Type);
        Assert.AreEqual(ActivityType.Running, running.Type);
        Assert.IsGreaterThan(0.45, walking.Confidence);
        Assert.IsGreaterThan(0.45, running.Confidence);
    }

    [TestMethod]
    public void TestThat_quiet_linear_acceleration_is_classified_as_standing()
    {
        var classifier = new ActivityClassifier();
        var start = DateTimeOffset.UtcNow;
        ActivityEstimate estimate = ActivityEstimate.Unknown;
        for (var index = 0; index < 100; index++)
            estimate = classifier.Update(Reading(start.AddMilliseconds(index * 40), 0), false, null);

        Assert.AreEqual(ActivityType.Standing, estimate.Type);
        Assert.IsGreaterThan(0.5, estimate.Confidence);
    }

    [TestMethod]
    public void TestThat_sustained_vibration_without_steps_is_a_low_confidence_truck_candidate()
    {
        var classifier = new ActivityClassifier();
        var start = DateTimeOffset.UtcNow;
        ActivityEstimate estimate = ActivityEstimate.Unknown;
        for (var index = 0; index < 100; index++)
            estimate = classifier.Update(Reading(start.AddMilliseconds(index * 40), 0.7 * Math.Abs(Math.Sin(index * 2.1))), false, null);

        Assert.AreEqual(ActivityType.Truck, estimate.Type);
        Assert.IsLessThanOrEqualTo(0.62, estimate.Confidence);
    }

    [TestMethod]
    public void TestThat_vertical_pressure_change_distinguishes_elevator_and_stairs()
    {
        var elevator = FeedVertical(false);
        var stairs = FeedVertical(true);

        Assert.AreEqual(ActivityType.Elevator, elevator.Type);
        Assert.AreEqual(ActivityType.Stairs, stairs.Type);
        Assert.IsGreaterThan(0.45, elevator.Confidence);
        Assert.IsGreaterThan(0.45, stairs.Confidence);
    }

    [TestMethod]
    public void TestThat_missing_motion_data_remains_unknown()
    {
        var classifier = new ActivityClassifier();
        var estimate = classifier.Update(new SensorReading(DateTimeOffset.UtcNow, 40), false, null);

        Assert.AreEqual(ActivityType.Unknown, estimate.Type);
        Assert.AreEqual(0, estimate.Confidence);
    }

    private static ActivityEstimate FeedCadence(double stepsPerSecond)
    {
        var classifier = new ActivityClassifier();
        var start = DateTimeOffset.UtcNow;
        ActivityEstimate estimate = ActivityEstimate.Unknown;
        var stepInterval = 1 / stepsPerSecond;
        var previousStep = -1d;
        for (var index = 0; index < 200; index++)
        {
            var elapsed = index * 0.04;
            var step = elapsed - previousStep >= stepInterval;
            if (step) previousStep = elapsed;
            estimate = classifier.Update(Reading(start.AddMilliseconds(index * 40), 0.1), step, null);
        }
        return estimate;
    }

    private static ActivityEstimate FeedVertical(bool stairs)
    {
        var classifier = new ActivityClassifier();
        var start = DateTimeOffset.UtcNow;
        ActivityEstimate estimate = ActivityEstimate.Unknown;
        var previousStep = -1d;
        for (var index = 0; index < 100; index++)
        {
            var elapsed = index * 0.04;
            var step = stairs && elapsed - previousStep >= 0.8;
            if (step) previousStep = elapsed;
            estimate = classifier.Update(Reading(start.AddMilliseconds(index * 40), 0.15), step, elapsed * 0.12);
        }
        return estimate;
    }

    private static SensorReading Reading(DateTimeOffset timestamp, double acceleration) => new(timestamp, 40, acceleration: new SensorVector3(0, 0, acceleration));
}
