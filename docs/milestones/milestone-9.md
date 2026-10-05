# Milestone 9 — Warehouse Designer

## Delivered

- The Drawing route is now presented as the Warehouse Designer, with a warehouse name, source plan import, scale calibration, map feature tools, snapping, undo/redo and generated graph feedback.
- Calibrated semantic maps can be exported as `warehouse-map.json` and imported back into the editor. The document uses schema version 1, a named `warehouse-local-xy` coordinate system, drawing-unit coordinates, meters-per-drawing-unit scale, source-file metadata and typed features with IDs, levels and vertices.
- JSON import validates the schema, coordinate convention, positive scale, unique IDs and each feature's geometry before replacing the editor map.
- A round-trip test verifies IDs, scale, geometry, levels and string feature types; tests cover unsupported versions, duplicate IDs and missing scale.
- About identifies Milestone 9 as the current milestone.

## File format and source plans

- Feature coordinates stay in the drawing's local 2D coordinate frame. Consumers convert to metres by multiplying by `metersPerDrawingUnit`; no axis transformation is implied in the file.
- The JSON package contains semantic map data and the source drawing's name/format as metadata. It does not embed the original image or CAD file.
- Common DXF LINE, CIRCLE and LWPOLYLINE entities are previewed per Milestone 3. The editor does not convert arbitrary CAD entities or expose source CAD layers as semantic warehouse features.
- DWG import remains unavailable because the project has no licensed DWG conversion SDK configured. The UI explains this and requests DXF as an interchange format.
- Semantic feature editing is through drawing, deleting and undo/redo; vertex drag-editing and level transitions remain future work.

## Verification

- `dotnet build Qsys.SensorApp.sln --no-restore`
- `dotnet test Qsys.SensorApp.sln --no-build`
