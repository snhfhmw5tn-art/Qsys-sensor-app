using Qsys.SensorApp.Core.Geometry;
using Qsys.SensorApp.Core.Maps;

namespace Qsys.SensorApp.Core.Navigation;

/// <summary>Projects a position estimate onto the nearest eligible navigation edge.</summary>
public static class MapMatcher
{
    /// <summary>Finds the nearest graph point within a maximum distance and on the requested level.</summary>
    public static MapMatchResult? Match(Vector2 estimate, NavigationGraph graph, double maximumDistance, string level = "0")
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentException.ThrowIfNullOrWhiteSpace(level);
        if (!double.IsFinite(maximumDistance) || maximumDistance < 0) throw new ArgumentOutOfRangeException(nameof(maximumDistance));
        var nodes = graph.Nodes.ToDictionary(node => node.Id);
        MapMatchResult? nearest = null;

        foreach (var edge in graph.Edges)
        {
            if (!string.Equals(nodes[edge.FromNodeId].Level, level, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(nodes[edge.ToNodeId].Level, level, StringComparison.OrdinalIgnoreCase)) continue;
            var segment = new Line(nodes[edge.FromNodeId].Position, nodes[edge.ToNodeId].Position);
            var projection = Projection.OntoSegment(estimate, segment);
            var distance = Distance.Between(estimate, projection);
            if (distance > maximumDistance || (nearest is not null && distance >= nearest.DistanceMeters)) continue;
            nearest = new MapMatchResult(projection, distance, edge.FeatureId, edge.Kind, edge.FromNodeId, edge.ToNodeId);
        }

        return nearest;
    }
}

/// <summary>The nearest eligible road graph projection.</summary>
public sealed record MapMatchResult(Vector2 Position, double DistanceMeters, Guid FeatureId, WarehouseFeatureKind Kind, int FromNodeId, int ToNodeId);
