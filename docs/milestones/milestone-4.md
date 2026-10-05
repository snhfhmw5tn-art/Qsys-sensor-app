# Milestone 4 — Map Editor

## Delivered in this increment

- Create aisle, truck aisle, stair, elevator, ramp, barcode and forbidden-area features by selecting a tool and clicking the drawing.
- Finish multi-point features, cancel in-progress drawing, and delete saved features.
- Optional grid snapping applies to newly selected feature and calibration points.
- Undo and redo retain feature geometry and regenerate the navigation graph from the restored state.
- The Core map domain validates feature geometry and generates a planar navigation graph from navigable feature centerlines. It joins nearby nodes, splits intersecting lines, respects map levels in the domain model and omits segments that cross or lie inside forbidden polygons.
- The drawing view displays generated graph nodes and reports node/edge counts.

## Scope limits

- The browser editor currently assigns all new features to level `0`; editing floor/level is not yet exposed in the UI.
- Stair, elevator and ramp paths are represented as 2D polylines. Cross-floor elevator transitions need explicit level endpoints and are not generated yet.
- Existing feature vertices cannot be dragged. Features can be deleted and redrawn.
- The graph is an in-memory planar topology generator; it does not persist the map or provide route search yet.
- DXF import continues to expose one imported drawing layer. CAD layers are not mapped to editable feature layers.
