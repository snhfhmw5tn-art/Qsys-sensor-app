using System.Collections.ObjectModel;
using Qsys.SensorApp.Core.Geometry;

namespace Qsys.SensorApp.Core.Maps;

/// <summary>An immutable semantic object drawn on a warehouse map.</summary>
public sealed class WarehouseFeature
{
    private readonly ReadOnlyCollection<Vector2> points;

    /// <summary>Creates a warehouse feature with geometry valid for its type.</summary>
    public WarehouseFeature(Guid id, WarehouseFeatureKind kind, string name, IEnumerable<Vector2> points, string level = "0")
    {
        if (id == Guid.Empty) throw new ArgumentException("Feature ID cannot be empty.", nameof(id));
        if (!Enum.IsDefined(kind)) throw new ArgumentOutOfRangeException(nameof(kind));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(level);
        ArgumentNullException.ThrowIfNull(points);
        var copy = points.ToArray();
        var minimum = kind switch { WarehouseFeatureKind.Barcode => 1, WarehouseFeatureKind.ForbiddenArea => 3, _ => 2 };
        if (copy.Length < minimum) throw new ArgumentException($"{kind} requires at least {minimum} point(s).", nameof(points));
        if (kind == WarehouseFeatureKind.ForbiddenArea) _ = new Polygon(copy);
        Id = id;
        Kind = kind;
        Name = name.Trim();
        Level = level.Trim();
        this.points = Array.AsReadOnly(copy);
    }

    /// <summary>Gets the stable feature identifier.</summary>
    public Guid Id { get; }
    /// <summary>Gets the semantic feature kind.</summary>
    public WarehouseFeatureKind Kind { get; }
    /// <summary>Gets the display name.</summary>
    public string Name { get; }
    /// <summary>Gets the floor or level identifier.</summary>
    public string Level { get; }
    /// <summary>Gets immutable feature vertices.</summary>
    public IReadOnlyList<Vector2> Points => points;
}
