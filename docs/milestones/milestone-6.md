# Milestone 6 — Navigation

## Delivered

- Core step detector based on gravity-compensated acceleration magnitude, adaptive baseline filtering and a minimum step interval.
- Dead-reckoning session that combines step events, configurable stride length, absolute compass heading, gyroscope integration and pressure-derived relative altitude.
- Two-dimensional Kalman position filter and bounded nearest-edge map matching on the configured floor.
- Navigation page with session controls, initial local position and heading, step length, live X/Y/Z, speed, heading, step count, confidence, uncertainty and a position trace.
- Drawing features and calibrated scale are saved in browser local storage and read by Navigation. Drawing coordinates are scaled to metres and Y is inverted so north is positive.
- About page now identifies Milestone 6 as the current milestone.

## Coordinate and sensor assumptions

- The local origin and initial pose are supplied by the user. Heading zero points north, positive heading rotates clockwise, east is positive X and north is positive Y.
- Device acceleration, rotation-rate, orientation and pressure values are best-effort browser sensor measurements. The page requests up to 25 Hz; actual support, sampling and permission depend on the browser, HTTPS context and hardware.
- Pressure altitude is relative to the first valid pressure sample in the session; it is not a surveyed floor elevation.
- Stride length defaults to 0.72 m and should be calibrated for a user and activity. The threshold-based detector and estimated confidence are initial engineering estimates, not a validated safety or survey system.
- Map matching uses the saved graph on level "0" and a 2 m tolerance. Feature levels are kept by the drawing editor, but the page does not yet offer level selection or vertical route transitions.
- Position trace and navigation state are session-only. The map is stored only in the current browser's local storage; there is no server synchronization, user identity, route planner, replay, or multi-device positioning.

## Verification

- `dotnet build Qsys.SensorApp.sln --no-restore`
- `dotnet test Qsys.SensorApp.sln --no-build`
