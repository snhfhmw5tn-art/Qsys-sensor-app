using Qsys.SensorApp.Core.Navigation;
using Qsys.SensorApp.Core.Sensors;

namespace Qsys.SensorApp.Core.Tests.Navigation;

[TestClass]
public sealed class SensorFeatureExtractorTests
{
    [TestMethod]
    public void TestThat_extractor_preserves_sensor_time_and_derives_magnitudes()
    {
        var timestamp = DateTimeOffset.UtcNow;
        var reading = new SensorReading(timestamp, 20,
            acceleration: new SensorVector3(3, 4, 0),
            rotationRateDegreesPerSecond: new SensorVector3(0, 0, 12),
            magneticFieldMicrotesla: new SensorVector3(3, 4, 12),
            headingDegrees: 370,
            pressureKilopascals: 100.5);

        var features = SensorFeatureExtractor.Extract(reading);

        Assert.AreEqual(timestamp, features.TimestampUtc);
        Assert.AreEqual(5d, features.LinearAccelerationMagnitude);
        Assert.AreEqual(12d, features.GyroscopeMagnitude);
        Assert.AreEqual(13d, features.MagnetometerMagnitude);
        Assert.AreEqual(10d, features.HeadingDegrees);
        Assert.AreEqual(100.5, features.PressureKilopascals);
    }

    [TestMethod]
    public void TestThat_extractor_compensates_gravity_when_linear_acceleration_is_missing()
    {
        var reading = new SensorReading(DateTimeOffset.UtcNow, 40, accelerationIncludingGravity: new SensorVector3(0, 0, 9.80665));

        var features = SensorFeatureExtractor.Extract(reading);

        Assert.AreEqual(0d, features.LinearAccelerationMagnitude!.Value, 1e-9);
    }
}
