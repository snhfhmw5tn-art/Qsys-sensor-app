# Milestone 8 — Calibration Mode

## Delivered

- A Calibration page records labeled sensor sessions for Walking, Truck, Standing, Stairs and Elevator.
- Every sample retains its sensor reading and a derived feature vector: acceleration axes and magnitude, gravity-inclusive acceleration axes, gyroscope axes and magnitude, magnetometer axes and magnitude, heading, pitch, roll, pressure and sampling interval.
- In-memory recordings are summarized by label and session, with duration, count and observed sampling rate.
- Users can export the complete dataset as JSON or a flattened per-sample CSV. Exports include session ID, class label, sample index and UTC timestamp.
- Dataset size is capped at 25,000 samples per page session to limit browser memory use; the UI offers export and clear actions.
- Calibration is available in the main navigation and the About page identifies Milestone 8.

## Data handling and limits

- Recording begins only after a user selects a label and explicitly starts the sensor request. Availability still depends on secure context, browser permissions and device support.
- Labels are user-provided. No classifier is trained or changed by a recording, and the recordings do not automatically validate the heuristic classifier.
- Recordings exist only in page memory until exported. Export before leaving or refreshing the page. No server upload or background sharing is performed.
- Keep device placement consistent and gather multiple representative recordings per label before using a dataset for future model work.

## Verification

- `dotnet build Qsys.SensorApp.sln --no-restore`
- `dotnet test Qsys.SensorApp.sln --no-build`
