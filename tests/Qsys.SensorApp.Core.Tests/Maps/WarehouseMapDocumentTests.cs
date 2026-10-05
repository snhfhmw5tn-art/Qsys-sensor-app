using System.Text.Json;
using Qsys.SensorApp.Core.Geometry;
using Qsys.SensorApp.Core.Maps;

namespace Qsys.SensorApp.Core.Tests.Maps;

[TestClass]
public sealed class WarehouseMapDocumentTests
{
    [TestMethod]
    public void TestThat_warehouse_map_json_round_trips_scale_features_and_levels()
    {
        var feature = new WarehouseFeature(Guid.NewGuid(), WarehouseFeatureKind.Aisle, "Aisle A", [new Vector2(1.5, 2), new Vector2(10, 2)], "upper-floor");
        var document = WarehouseMapDocument.Create("North warehouse", [feature], 0.25, "north.dxf", "DXF");

        var json = document.ToJson();
        var imported = WarehouseMapDocument.FromJson(json);
        var restored = imported.ToDomainFeatures();

        Assert.Contains("\"schemaVersion\": 1", json);
        Assert.Contains("\"kind\": \"aisle\"", json);
        Assert.AreEqual("North warehouse", imported.Name);
        Assert.AreEqual(0.25, imported.MetersPerDrawingUnit);
        Assert.AreEqual("upper-floor", restored[0].Level);
        Assert.AreEqual(feature.Id, restored[0].Id);
        Assert.AreEqual(feature.Points[0], restored[0].Points[0]);
    }

    [TestMethod]
    public void TestThat_unsupported_schema_version_is_rejected()
    {
        const string json = """{"schemaVersion":99,"name":"Map","coordinateSystem":"warehouse-local-xy","coordinateUnits":"drawing-unit","metersPerDrawingUnit":1,"features":[]}""";

        Assert.ThrowsExactly<JsonException>(() => WarehouseMapDocument.FromJson(json));
    }

    [TestMethod]
    public void TestThat_duplicate_feature_identifiers_are_rejected()
    {
        var id = Guid.NewGuid();
        var featureA = new WarehouseFeature(id, WarehouseFeatureKind.Aisle, "A", [new Vector2(0, 0), new Vector2(1, 0)]);
        var featureB = new WarehouseFeature(id, WarehouseFeatureKind.Ramp, "B", [new Vector2(0, 1), new Vector2(1, 1)]);

        Assert.ThrowsExactly<JsonException>(() => WarehouseMapDocument.FromJson(
            WarehouseMapDocument.Create("Map", [featureA, featureB], 1).ToJson()));
    }

    [TestMethod]
    public void TestThat_unscaled_map_cannot_be_exported()
    {
        var feature = new WarehouseFeature(Guid.NewGuid(), WarehouseFeatureKind.Aisle, "A", [new Vector2(0, 0), new Vector2(1, 0)]);
        var document = new WarehouseMapDocument(1, "Map", WarehouseMapDocument.LocalCoordinateSystem, WarehouseMapDocument.DrawingCoordinateUnits, 0, null, null,
            [new WarehouseMapFeatureDocument(feature.Id, feature.Kind, feature.Name, feature.Level, feature.Points)]);

        Assert.ThrowsExactly<JsonException>(() => document.ToJson());
    }
}
