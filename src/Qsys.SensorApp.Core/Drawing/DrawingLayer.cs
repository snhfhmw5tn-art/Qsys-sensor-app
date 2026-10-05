namespace Qsys.SensorApp.Core.Drawing;

/// <summary>A named drawing layer and its visibility state.</summary>
public sealed record DrawingLayer(string Name, bool IsVisible = true, bool IsLocked = false);
