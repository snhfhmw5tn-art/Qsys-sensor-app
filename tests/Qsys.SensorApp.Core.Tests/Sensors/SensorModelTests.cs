using Qsys.SensorApp.Core.Sensors;

namespace Qsys.SensorApp.Core.Tests.Sensors;

[TestClass]
public sealed class SensorModelTests
{
    [TestMethod]
    public void TestThat_sensor_vector_reports_magnitude()
    {
        var vector = new SensorVector3(2, -3, 6);

        Assert.AreEqual(7, vector.Magnitude, 1e-12);
    }

    [TestMethod]
    public void TestThat_sensor_vector_rejects_non_finite_values()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new SensorVector3(double.NaN, 0, 0));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new SensorVector3(0, double.PositiveInfinity, 0));
    }

    [TestMethod]
    public void TestThat_sensor_reading_normalizes_heading_and_timestamp()
    {
        var localTimestamp = new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.FromHours(2));
        var reading = new SensorReading(localTimestamp, 100, headingDegrees: -10, orientationIsAbsolute: true, pressureKilopascals: 99.8);

        Assert.AreEqual(localTimestamp.ToUniversalTime(), reading.TimestampUtc);
        Assert.AreEqual(350, reading.HeadingDegrees);
        Assert.AreEqual(99.8, reading.PressureKilopascals);
        Assert.IsTrue(reading.OrientationIsAbsolute);
    }

    [TestMethod]
    public void TestThat_sensor_reading_rejects_invalid_intervals_and_pressure()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new SensorReading(DateTimeOffset.UtcNow, -1));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new SensorReading(DateTimeOffset.UtcNow, 10, pressureKilopascals: -2));
    }
}
