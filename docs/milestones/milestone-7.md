# Milestone 7 — Vehicle Detection

## Delivered

- Core activity types for Walking, Truck, Standing, Elevator, Stairs, Running and Unknown.
- A rolling four-second heuristic classifier uses detected step cadence, acceleration variation and relative barometric altitude change.
- Each estimate exposes a confidence value from zero to one. `NavigationState` includes the estimate and the Navigation page shows both the label and confidence.
- The About page identifies Milestone 7 as current.
- MSTest coverage exercises every activity class and the no-motion-data fallback.

## Limits

- This increment is a transparent baseline heuristic. It is not a trained machine-learning model and its confidence values are not calibrated probabilities.
- A phone's inertial sensors alone cannot reliably identify a particular vehicle type across device placement, floors and truck models. Truck is a low-confidence vibration candidate (confidence capped at 0.62), intended to be replaced or refined with labeled calibration data in Milestone 8.
- Elevator and stair estimates need a supported barometer and enough vertical pressure change. Pressure can also vary due to weather, doors, HVAC and sensor noise.
- Classification is session-only; labeled recordings, feature export and per-user models are planned for Milestone 8.

## Verification

- `dotnet build Qsys.SensorApp.sln --no-restore`
- `dotnet test Qsys.SensorApp.sln --no-build`
