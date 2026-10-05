using System.Text.Json;
using System.Text.Json.Serialization;
using Qsys.SensorApp.Core.Geometry;

namespace Qsys.SensorApp.Core.Maps;

/// <summary>Versioned, portable warehouse-map JSON document. Feature coordinates remain in drawing units.</summary>
public sealed record WarehouseMapDocument(
    int SchemaVersion,
    string Name,
    string CoordinateSystem,
    string CoordinateUnits,
    double MetersPerDrawingUnit,
    string? SourceDrawingFile,
    string? SourceFormat,
    IReadOnlyList<WarehouseMapFeatureDocument> Features)
{
    /// <summary>Current supported file schema version.</summary>
    public const int CurrentSchemaVersion = 1;
    /// <summary>Coordinate-system identifier used by this schema.</summary>
    public const string LocalCoordinateSystem = "warehouse-local-xy";
    /// <summary>Unit for feature point coordinates in this schema.</summary>
    public const string DrawingCoordinateUnits = "drawing-unit";

    private static readonly JsonSerializerOptions JsonOptions = CreateOptions();

    /// <summary>Creates a validated document from editor data.</summary>
    public static WarehouseMapDocument Create(string name, IEnumerable<WarehouseFeature> features, double metersPerDrawingUnit, string? sourceDrawingFile = null, string? sourceFormat = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(features);
        var document = new WarehouseMapDocument(CurrentSchemaVersion, name.Trim(), LocalCoordinateSystem, DrawingCoordinateUnits,
            metersPerDrawingUnit, sourceDrawingFile, sourceFormat,
            features.Select(feature => new WarehouseMapFeatureDocument(feature.Id, feature.Kind, feature.Name, feature.Level, feature.Points.ToArray())).ToArray());
        _ = document.ToDomainFeatures();
        document.Validate();
        return document;
    }

    /// <summary>Serializes this map as portable, indented UTF-8-compatible JSON text.</summary>
    public string ToJson()
    {
        Validate();
        return JsonSerializer.Serialize(this, JsonOptions);
    }

    /// <summary>Parses and validates a supported warehouse-map JSON document.</summary>
    public static WarehouseMapDocument FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var document = JsonSerializer.Deserialize<WarehouseMapDocument>(json, JsonOptions)
            ?? throw new JsonException("The warehouse map document is empty.");
        document.Validate();
        _ = document.ToDomainFeatures();
        return document;
    }

    /// <summary>Recreates validated Core map features from this document.</summary>
    public IReadOnlyList<WarehouseFeature> ToDomainFeatures() => Features.Select(feature =>
        new WarehouseFeature(feature.Id, feature.Kind, feature.Name, feature.Points, feature.Level)).ToArray();

    private void Validate()
    {
        if (SchemaVersion != CurrentSchemaVersion) throw new JsonException($"Unsupported warehouse map schema version {SchemaVersion}.");
        if (string.IsNullOrWhiteSpace(Name)) throw new JsonException("The warehouse map name is required.");
        if (!string.Equals(CoordinateSystem, LocalCoordinateSystem, StringComparison.Ordinal)) throw new JsonException($"Coordinate system must be '{LocalCoordinateSystem}'.");
        if (!string.Equals(CoordinateUnits, DrawingCoordinateUnits, StringComparison.Ordinal)) throw new JsonException($"Coordinate units must be '{DrawingCoordinateUnits}'.");
        if (!double.IsFinite(MetersPerDrawingUnit) || MetersPerDrawingUnit <= 0) throw new JsonException("Meters per drawing unit must be positive and finite.");
        if (Features is null) throw new JsonException("The features collection is required.");
        if (Features.Any(feature => feature is null)) throw new JsonException("Feature entries cannot be null.");
        if (Features.Select(feature => feature.Id).Distinct().Count() != Features.Count) throw new JsonException("Feature identifiers must be unique.");
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true, PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        options.Converters.Add(new Vector2JsonConverter());
        return options;
    }

    private sealed class Vector2JsonConverter : JsonConverter<Vector2>
    {
        public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var document = JsonDocument.ParseValue(ref reader);
            var root = document.RootElement;
            return new Vector2(root.GetProperty("x").GetDouble(), root.GetProperty("y").GetDouble());
        }

        public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("x", value.X);
            writer.WriteNumber("y", value.Y);
            writer.WriteEndObject();
        }
    }
}

/// <summary>Serializable semantic feature in a warehouse map file.</summary>
public sealed record WarehouseMapFeatureDocument(Guid Id, WarehouseFeatureKind Kind, string Name, string Level, IReadOnlyList<Vector2> Points);
