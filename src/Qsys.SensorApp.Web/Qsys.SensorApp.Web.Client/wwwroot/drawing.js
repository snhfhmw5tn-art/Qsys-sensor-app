const mapStorageKey = "qsys.sensorapp.map.v1";

export function toMapPoint(svg, clientX, clientY) {
    const matrix = svg.getScreenCTM();
    if (!matrix) throw new Error("Drawing surface is unavailable.");

    const point = svg.createSVGPoint();
    point.x = clientX;
    point.y = clientY;
    const local = point.matrixTransform(matrix.inverse());
    return { x: local.x, y: local.y };
}

export function loadMap() {
    try {
        const serialized = window.localStorage.getItem(mapStorageKey);
        return serialized ? JSON.parse(serialized) : { name: "Warehouse map", features: [], metersPerDrawingUnit: null };
    } catch {
        return { name: "Warehouse map", features: [], metersPerDrawingUnit: null };
    }
}

export function saveMap(features, metersPerDrawingUnit, name = "Warehouse map") {
    try {
        window.localStorage.setItem(mapStorageKey, JSON.stringify({ name, features, metersPerDrawingUnit }));
        return true;
    } catch {
        return false;
    }
}

export function downloadText(fileName, content, mimeType) {
    const blob = new Blob([content], { type: mimeType });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = fileName;
    anchor.click();
    window.setTimeout(() => URL.revokeObjectURL(url), 1000);
}
