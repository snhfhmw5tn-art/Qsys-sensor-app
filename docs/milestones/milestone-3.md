# Milestone 3 — Drawing Engine

## Delivered in this increment

- Browser previews for PNG, JPEG, WebP, BMP and GIF.
- SVG is parsed as XML with DTD processing disabled. Script, `foreignObject`, style blocks, event-handler attributes and non-fragment links are removed before it is rendered.
- DXF preview renders `LINE`, `CIRCLE` and `LWPOLYLINE` entities. Other entities are skipped; this is not a complete CAD importer.
- Drawing canvas zoom, pan and quarter-turn rotation.
- Visibility toggle for the imported drawing layer and illustrative sample layers.
- Point-pair scale calibration and optional grid snapping for calibration points.
- Bounded undo/redo history for the editor state.
- Upload size is limited to 20 MB.

## Known limitations

- TIFF is recognized but the browser preview is not decoded in this increment.
- DWG is not imported. No commercial DWG SDK license was available for this work; the UI asks the user to export as DXF. The DXF parser has the entity limitations listed above.
- DXF source layer names are not yet exposed as independently toggleable layers.
- Calibration uses points selected on the displayed image and records meters per displayed drawing unit; it does not yet read units or scale metadata from CAD files.
- The page-local importer has no API persistence. Imported content lives in the browser session.

Milestone 3 should remain marked in progress until the CAD/raster gaps and persistence are addressed.
