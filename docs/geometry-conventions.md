# Geometry conventions

Milestone 2 geometry types live in `Qsys.SensorApp.Core.Geometry` and use finite `double` coordinates. `Vector2` follows ordinary Cartesian axes: +X points right/east and +Y points up/north. 2D geometric rotation is counterclockwise and accepts radians. `Angle` stores degrees and exposes radians for APIs that require them.

`Heading` and `Bearing` use compass convention: degrees clockwise from north, normalized to `[0, 360)`. Bearings are undefined for coincident points. `Line` represents a finite segment; projection APIs distinguish an unbounded line from a clamped segment. Polygon boundaries include their edges for point containment. Bounding boxes and rectangles are axis-aligned and include their boundaries.

Intersection routines use a documented distance tolerance (default `1e-9`) for near-collinear and boundary cases; the parallel check scales that tolerance by segment lengths. A collinear segment overlap reports the first overlap point along the first input segment. Affine transforms use column-vector composition; `first.Then(next)` applies `first` and then `next`.
