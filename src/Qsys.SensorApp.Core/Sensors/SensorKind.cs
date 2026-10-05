namespace Qsys.SensorApp.Core.Sensors;

/// <summary>Physical or fused sensor channel available to indoor navigation.</summary>
public enum SensorKind
{
    /// <summary>Linear acceleration excluding gravity.</summary>
    Accelerometer,
    /// <summary>Angular rotation rate.</summary>
    Gyroscope,
    /// <summary>Magnetic field vector.</summary>
    Magnetometer,
    /// <summary>Device attitude and compass heading.</summary>
    Orientation,
    /// <summary>Combined device motion event channel.</summary>
    DeviceMotion,
    /// <summary>Ambient air pressure.</summary>
    Barometer
}
