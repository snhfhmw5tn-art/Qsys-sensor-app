using Qsys.SensorApp.Core.DigitalTwin;
using Qsys.SensorApp.Core.Navigation;

namespace Qsys.SensorApp.Core.Tests.Navigation;

[TestClass]
public sealed class HeatmapAggregatorTests
{
    [TestMethod]
    public void TestThat_heatmap_bins_positions_and_normalizes_to_the_busiest_cell()
    {
        var session = Guid.NewGuid();
        var time = DateTimeOffset.UtcNow;
        var samples = new[]
        {
            Sample(0.1, 0.1, 0), Sample(1.9, 1.9, 1), Sample(2.1, 0.1, 2), Sample(-0.1, 0.1, 3)
        }.Select((point, index) => new DigitalTwinSample(session, time.AddSeconds(index), point.X, point.Y, 0, null, 0, 0, 0, 0, ActivityType.Unknown, 0));

        var cells = HeatmapAggregator.Aggregate(samples, 2);

        Assert.HasCount(3, cells);
        Assert.AreEqual(2, cells[0].SampleCount);
        Assert.AreEqual(1d, cells[0].Intensity);
        Assert.IsGreaterThan(0d, cells[1].Intensity);
        Assert.IsLessThan(1d, cells[1].Intensity);
    }

    [TestMethod]
    public void TestThat_heatmap_requires_a_positive_finite_cell_size()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => HeatmapAggregator.Aggregate([], 0));
    }

    private static (double X, double Y) Sample(double x, double y, int _) => (x, y);
}
