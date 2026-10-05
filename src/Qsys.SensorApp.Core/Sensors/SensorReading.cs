namespace Qsys.SensorApp.Core.Sensors;

/// <summary>A timestamped snapshot of the device sensors used by indoor navigation.</summary>
public sealed record SensorReading
{
    /// <summary>Creates a sensor snapshot, converting heading to the range [0, 360).</summary>
    public SensorReading(
        DateTimeOffset timestamp,
        double samplingIntervalMilliseconds,
        SensorVector3? acceleration = null,
        SensorVector3? accelerationIncludingGravity = null,
        SensorVector3? rotationRateDegreesPerSecond = null,
        SensorVector3? magneticFieldMicrotesla = null,
        double? headingDegrees = null,
        double? pitchDegrees = null,
        double? rollDegrees = null,
        bool orientationIsAbsolute = false,
        double? pressureKilopascals = null)
    {
        if (!double.IsFinite(samplingIntervalMilliseconds) || samplingIntervalMilliseconds < 0)
            throw new ArgumentOutOfRangeException(nameof(samplingIntervalMilliseconds));
        ValidateOptional(headingDegrees, nameof(headingDegrees));
        ValidateOptional(pitchDegrees, nameof(pitchDegrees));
        ValidateOptional(rollDegrees, nameof(rollDegrees));
        ValidateOptional(pressureKilopascals, nameof(pressureKilopascals));
        if (pressureKilopascals is < 0) throw new ArgumentOutOfRangeException(nameof(pressureKilopascals));

        TimestampUtc = timestamp.ToUniversalTime();
        SamplingIntervalMilliseconds = samplingIntervalMilliseconds;
        Acceleration = acceleration;
        AccelerationIncludingGravity = accelerationIncludingGravity;
        RotationRateDegreesPerSecond = rotationRateDegreesPerSecond;
        MagneticFieldMicrotesla = magneticFieldMicrotesla;
        HeadingDegrees = headingDegrees is null ? null : ((headingDegrees % 360) + 360) % 360;
        PitchDegrees = pitchDegrees;
        RollDegrees = rollDegrees;
        OrientationIsAbsolute = orientationIsAbsolute;
        PressureKilopascals = pressureKilopascals;
    }

    /// <summary>Gets the UTC time at which the measurement was captured.</summary>
    public DateTimeOffset TimestampUtc { get; }
    /// <summary>Gets the sampling interval reported by the source in milliseconds.</summary>
    public double SamplingIntervalMilliseconds { get; }
    /// <summary>Gets linear acceleration excluding gravity in meters per second squared.</summary>
    public SensorVector3? Acceleration { get; }
    /// <summary>Gets linear acceleration including gravity in meters per second squared.</summary>
    public SensorVector3? AccelerationIncludingGravity { get; }
    /// <summary>Gets angular rotation rate in degrees per second.</summary>
    public SensorVector3? RotationRateDegreesPerSecond { get; }
    /// <summary>Gets magnetic field strength in microteslas.</summary>
    public SensorVector3? MagneticFieldMicrotesla { get; }
    /// <summary>Gets absolute compass heading in degrees clockwise from north, when available.</summary>
    public double? HeadingDegrees { get; }
    /// <summary>Gets device pitch in degrees.</summary>
    public double? PitchDegrees { get; }
    /// <summary>Gets device roll in degrees.</summary>
    public double? RollDegrees { get; }
    /// <summary>Gets whether orientation is referenced to the earth coordinate frame.</summary>
    public bool OrientationIsAbsolute { get; }
    /// <summary>Gets pressure in kilopascals, when a barometer is available.</summary>
    public double? PressureKilopascals { get; }

    private static void ValidateOptional(double? value, string parameterName)
    {
        if (value is not null && !double.IsFinite(value.Value)) throw new ArgumentOutOfRangeException(parameterName);
    }
}
