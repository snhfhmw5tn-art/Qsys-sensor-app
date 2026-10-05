namespace Qsys.SensorApp.Core.Sensors;

/// <summary>Permission state reported by the browser for a sensor channel.</summary>
public enum SensorPermissionState
{
    /// <summary>The browser does not expose a permission state.</summary>
    Unknown,
    /// <summary>The user has not granted or denied access yet.</summary>
    Prompt,
    /// <summary>Sensor access is allowed.</summary>
    Granted,
    /// <summary>Sensor access was denied.</summary>
    Denied,
    /// <summary>The browser or device does not support this channel.</summary>
    NotSupported,
    /// <summary>The page is not allowed to access the sensor in the current context.</summary>
    Unavailable
}
