using Qsys.SensorApp.Core.Geometry;
using Qsys.SensorApp.Core.Maps;

namespace Qsys.SensorApp.Core.Tests.Maps;

[TestClass]
public sealed class NavigationGraphTests
{
    [TestMethod]
    public void TestThat_graph_splits_centerlines_at_intersections()
    {
        var features = new[]
        {
            Feature(WarehouseFeatureKind.Aisle, new(0, 0), new(10, 0)),
            Feature(WarehouseFeatureKind.TruckAisle, new(5, -5), new(5, 5))
        };

        var graph = NavigationGraphGenerator.Generate(features);

        Assert.HasCount(5, graph.Nodes);
        Assert.HasCount(4, graph.Edges);
        Assert.HasCount(1, graph.Nodes.Where(node => node.Position == new Vector2(5, 0)));
        Assert.IsTrue(graph.Edges.All(edge => edge.Length == 5));
    }

    [TestMethod]
    public void TestThat_graph_joins_nearby_endpoints()
    {
        var features = new[]
        {
            Feature(WarehouseFeatureKind.Aisle, new(0, 0), new(5, 0)),
            Feature(WarehouseFeatureKind.Aisle, new(5.1, 0), new(10, 0))
        };

        var graph = NavigationGraphGenerator.Generate(features, 0.2);

        Assert.HasCount(3, graph.Nodes);
        Assert.HasCount(2, graph.Edges);
        Assert.HasCount(1, graph.Nodes.Where(node => Distance.Between(node.Position, new Vector2(5, 0)) < 0.2));
    }

    [TestMethod]
    public void TestThat_graph_excludes_segments_inside_forbidden_areas()
    {
        var features = new[]
        {
            Feature(WarehouseFeatureKind.Aisle, new(0, 0), new(10, 0)),
            Feature(WarehouseFeatureKind.ForbiddenArea, new(4, -1), new(6, -1), new(6, 1), new(4, 1))
        };

        var graph = NavigationGraphGenerator.Generate(features);

        Assert.HasCount(2, graph.Edges);
        Assert.IsTrue(graph.Edges.All(edge => edge.Length == 4));
        Assert.IsTrue(graph.Edges.All(edge => edge.Kind == WarehouseFeatureKind.Aisle));
    }

    [TestMethod]
    public void TestThat_graph_does_not_connect_features_on_different_levels()
    {
        var features = new[]
        {
            new WarehouseFeature(Guid.NewGuid(), WarehouseFeatureKind.Aisle, "Floor one", [new(0, 0), new(5, 0)], "1"),
            new WarehouseFeature(Guid.NewGuid(), WarehouseFeatureKind.Aisle, "Floor two", [new(5, 0), new(10, 0)], "2")
        };

        var graph = NavigationGraphGenerator.Generate(features, 1);

        Assert.HasCount(4, graph.Nodes);
        Assert.HasCount(2, graph.Edges);
    }

    [TestMethod]
    public void TestThat_graph_omits_edges_collapsed_by_connection_tolerance()
    {
        var feature = Feature(WarehouseFeatureKind.Aisle, new(0, 0), new(0.1, 0));

        var graph = NavigationGraphGenerator.Generate([feature], 0.25);

        Assert.IsEmpty(graph.Edges);
    }

    [TestMethod]
    public void TestThat_feature_rejects_invalid_forbidden_polygon()
    {
        Assert.ThrowsExactly<ArgumentException>(() => Feature(WarehouseFeatureKind.ForbiddenArea, new(0, 0), new(1, 1), new(2, 2)));
    }

    private static WarehouseFeature Feature(WarehouseFeatureKind kind, params Vector2[] points) =>
        new(Guid.NewGuid(), kind, kind.ToString(), points);
}
