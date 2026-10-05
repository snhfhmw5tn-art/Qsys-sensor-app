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
        return serialized ? JSON.parse(serialized) : { features: [], metersPerDrawingUnit: null };
    } catch {
        return { features: [], metersPerDrawingUnit: null };
    }
}

export function saveMap(features, metersPerDrawingUnit) {
    try {
        window.localStorage.setItem(mapStorageKey, JSON.stringify({ features, metersPerDrawingUnit }));
        return true;
    } catch {
        return false;
    }
}
