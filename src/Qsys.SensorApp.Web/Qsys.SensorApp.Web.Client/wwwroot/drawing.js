export function toMapPoint(svg, clientX, clientY) {
    const matrix = svg.getScreenCTM();
    if (!matrix) throw new Error("Drawing surface is unavailable.");

    const point = svg.createSVGPoint();
    point.x = clientX;
    point.y = clientY;
    const local = point.matrixTransform(matrix.inverse());
    return { x: local.x, y: local.y };
}
