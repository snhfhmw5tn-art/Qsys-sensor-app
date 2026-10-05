namespace Qsys.SensorApp.Core.Maps;

/// <summary>Supported editable warehouse-map feature types.</summary>
public enum WarehouseFeatureKind
{
    /// <summary>Pedestrian aisle centerline.</summary>
    Aisle,
    /// <summary>Truck aisle centerline.</summary>
    TruckAisle,
    /// <summary>Stair connector.</summary>
    Stair,
    /// <summary>Elevator connector.</summary>
    Elevator,
    /// <summary>Ramp connector.</summary>
    Ramp,
    /// <summary>Barcode or location marker.</summary>
    Barcode,
    /// <summary>Area prohibited for navigation.</summary>
    ForbiddenArea
}
