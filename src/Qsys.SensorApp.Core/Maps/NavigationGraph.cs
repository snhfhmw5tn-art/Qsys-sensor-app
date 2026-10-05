using System.Collections.ObjectModel;
using Qsys.SensorApp.Core.Geometry;

namespace Qsys.SensorApp.Core.Maps;

/// <summary>A location node in the generated warehouse navigation graph.</summary>
public sealed record NavigationNode(int Id, Vector2 Position, string Level);

/// <summary>A traversable connection between two navigation nodes.</summary>
public sealed record NavigationEdge(int FromNodeId, int ToNodeId, Guid FeatureId, WarehouseFeatureKind Kind, double Length);

/// <summary>An immutable graph generated from navigable warehouse features.</summary>
public sealed class NavigationGraph
{
    /// <summary>Creates a graph from its nodes and edges.</summary>
    public NavigationGraph(IEnumerable<NavigationNode> nodes, IEnumerable<NavigationEdge> edges)
    {
        Nodes = Array.AsReadOnly(nodes.ToArray());
        Edges = Array.AsReadOnly(edges.ToArray());
        var ids = Nodes.Select(node => node.Id).ToHashSet();
        if (Edges.Any(edge => !ids.Contains(edge.FromNodeId) || !ids.Contains(edge.ToNodeId) || edge.FromNodeId == edge.ToNodeId))
            throw new ArgumentException("Every graph edge must connect two distinct existing nodes.", nameof(edges));
    }

    /// <summary>Gets graph nodes.</summary>
    public ReadOnlyCollection<NavigationNode> Nodes { get; }
    /// <summary>Gets graph edges.</summary>
    public ReadOnlyCollection<NavigationEdge> Edges { get; }
}
