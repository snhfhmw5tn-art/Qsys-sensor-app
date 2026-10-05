using Qsys.SensorApp.Core.Geometry;

namespace Qsys.SensorApp.Core.Navigation;

/// <summary>A two-dimensional isotropic Kalman filter for dead-reckoned position.</summary>
public sealed class KalmanPositionFilter
{
    /// <summary>Creates a position filter at the supplied initial point and uncertainty.</summary>
    public KalmanPositionFilter(Vector2 initialPosition, double initialVariance = 1)
    {
        ValidateVariance(initialVariance, nameof(initialVariance), allowZero: false);
        Position = initialPosition;
        Variance = initialVariance;
    }

    /// <summary>Gets the current estimated position.</summary>
    public Vector2 Position { get; private set; }
    /// <summary>Gets the scalar position variance in square meters.</summary>
    public double Variance { get; private set; }
    /// <summary>Gets the estimated one-sigma positional uncertainty in meters.</summary>
    public double StandardDeviation => Math.Sqrt(Variance);

    /// <summary>Predicts a displacement and increases uncertainty by process variance.</summary>
    public void Predict(Vector2 displacement, double processVariance)
    {
        ValidateVariance(processVariance, nameof(processVariance), allowZero: true);
        Position += displacement;
        Variance += processVariance;
    }

    /// <summary>Corrects the current estimate using a position measurement.</summary>
    public void Correct(Vector2 measurement, double measurementVariance)
    {
        ValidateVariance(measurementVariance, nameof(measurementVariance), allowZero: false);
        var gain = Variance / (Variance + measurementVariance);
        Position += (measurement - Position) * gain;
        Variance *= 1 - gain;
    }

    private static void ValidateVariance(double value, string parameterName, bool allowZero)
    {
        if (!double.IsFinite(value) || value < 0 || (!allowZero && value == 0))
            throw new ArgumentOutOfRangeException(parameterName, allowZero ? "Variance must be non-negative and finite." : "Variance must be positive and finite.");
    }
}
