using Qsys.SensorApp.Core.Geometry;

namespace Qsys.SensorApp.Core.Maps;

/// <summary>Builds a planar navigation graph from warehouse feature centerlines.</summary>
public static class NavigationGraphGenerator
{
    private sealed record Segment(Guid FeatureId, WarehouseFeatureKind Kind, string Level, Line Line);

    /// <summary>Generates a graph, joining nearby endpoints and splitting at centerline intersections.</summary>
    public static NavigationGraph Generate(IEnumerable<WarehouseFeature> features, double connectionTolerance = 0.25)
    {
        ArgumentNullException.ThrowIfNull(features);
        if (!double.IsFinite(connectionTolerance) || connectionTolerance < 0)
            throw new ArgumentOutOfRangeException(nameof(connectionTolerance));

        var allFeatures = features.ToArray();
        var obstacles = allFeatures.Where(feature => feature.Kind == WarehouseFeatureKind.ForbiddenArea)
            .Select(feature => (feature.Level, Polygon: new Polygon(feature.Points)))
            .ToArray();
        var segments = allFeatures
            .Where(feature => feature.Kind != WarehouseFeatureKind.ForbiddenArea && feature.Kind != WarehouseFeatureKind.Barcode)
            .SelectMany(feature => Enumerable.Range(0, feature.Points.Count - 1)
                .Select(index => new Segment(feature.Id, feature.Kind, feature.Level, new Line(feature.Points[index], feature.Points[index + 1]))))
            .ToArray();

        var nodes = new List<NavigationNode>();
        var edges = new List<NavigationEdge>();
        var uniqueEdges = new HashSet<(int From, int To, Guid Feature)>();

        int GetNode(Vector2 point, string level)
        {
            var existing = nodes.FirstOrDefault(node =>
                string.Equals(node.Level, level, StringComparison.OrdinalIgnoreCase) &&
                Distance.Between(node.Position, point) <= connectionTolerance);
            if (existing is not null) return existing.Id;
            var id = nodes.Count;
            nodes.Add(new NavigationNode(id, point, level));
            return id;
        }

        foreach (var segment in segments)
        {
            var splitPoints = new List<Vector2> { segment.Line.Start, segment.Line.End };
            foreach (var other in segments)
            {
                if (ReferenceEquals(segment, other) || !string.Equals(segment.Level, other.Level, StringComparison.OrdinalIgnoreCase)) continue;
                if (Intersection.TryIntersectSegments(segment.Line, other.Line, out var intersection))
                    AddUnique(splitPoints, intersection, MathHelper.DefaultTolerance);
            }
            foreach (var obstacle in obstacles)
            {
                if (!string.Equals(segment.Level, obstacle.Level, StringComparison.OrdinalIgnoreCase)) continue;
                foreach (var edge in obstacle.Polygon.Edges)
                    if (Intersection.TryIntersectSegments(segment.Line, edge, out var intersection))
                        AddUnique(splitPoints, intersection, MathHelper.DefaultTolerance);
            }

            var direction = segment.Line.Direction;
            var lengthSquared = direction.LengthSquared;
            var ordered = splitPoints.OrderBy(point => lengthSquared == 0 ? 0 : (point - segment.Line.Start).Dot(direction) / lengthSquared).ToArray();
            for (var index = 0; index < ordered.Length - 1; index++)
            {
                var start = ordered[index];
                var end = ordered[index + 1];
                var length = Distance.Between(start, end);
                if (length <= connectionTolerance * 0.01) continue;
                var midpoint = (start + end) * 0.5;
                if (obstacles.Any(obstacle => string.Equals(obstacle.Level, segment.Level, StringComparison.OrdinalIgnoreCase) && obstacle.Polygon.Contains(midpoint, connectionTolerance * 0.01))) continue;

                var from = GetNode(start, segment.Level);
                var to = GetNode(end, segment.Level);
                if (from == to) continue;
                var normalized = from < to ? (from, to, segment.FeatureId) : (to, from, segment.FeatureId);
                if (!uniqueEdges.Add(normalized)) continue;
                edges.Add(new NavigationEdge(from, to, segment.FeatureId, segment.Kind, length));
            }
        }

        return new NavigationGraph(nodes, edges);
    }

    private static void AddUnique(List<Vector2> points, Vector2 point, double tolerance)
    {
        if (points.All(existing => Distance.Between(existing, point) > tolerance)) points.Add(point);
    }
}
