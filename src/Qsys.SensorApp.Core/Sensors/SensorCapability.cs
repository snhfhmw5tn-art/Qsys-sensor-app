namespace Qsys.SensorApp.Core.Sensors;

/// <summary>Current support and permission information for one sensor channel.</summary>
public sealed record SensorCapability(SensorKind Kind, bool IsSupported, SensorPermissionState Permission, string? Diagnostic = null);
