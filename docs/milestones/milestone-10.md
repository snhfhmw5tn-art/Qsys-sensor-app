# Milestone 10 — Warehouse Digital Twin

## Delivered

- Navigation is presented as the Digital Twin view, combining real-time position, calibrated map graph, heading, speed, activity class, confidence and uncertainty.
- Position snapshots are captured about once per second and retained across page visits in browser local storage. Each row includes a local session ID, timestamp, X/Y/Z, heading, speed, step count, position quality and activity estimate; no user or device identifier is recorded.
- Heatmap mode aggregates visits into 2 m grid cells and shows normalized relative visit density over the warehouse graph.
- Replay mode supports timeline scrubbing and play/pause through the retained chronological samples.
- A movement-intelligence panel summarizes the most observed activity and mean activity confidence.
- Local browser history is limited to the latest 5,000 snapshots. Users can clear it; browser storage failures leave the current session usable and show a message.
- About and sidebar labels identify the Digital Twin milestone.

## AI, storage and positioning limits

- Activity estimates and summary metrics use the transparent Milestone 7 sensor heuristic. No trained AI model, external AI service or predictive recommendation is connected in this increment; Milestone 8 calibration data is not automatically used to train the classifier.
- History and heatmaps are per-browser, local-only data. They are not synchronized to the API, shared between devices, or a multi-user occupancy view. Clearing browser site data also removes them.
- Samples are recorded at about 1 Hz, so heatmap intensity is a sample-density estimate rather than a person count or calibrated dwell-time measure.
- Position quality remains bounded by device sensors, step-length calibration, compass accuracy, map calibration and the active floor assumption.

## Verification

- `dotnet build Qsys.SensorApp.sln --no-restore`
- `dotnet test Qsys.SensorApp.sln --no-build`
