# Milestone 5 — Sensor Engine

## Delivered in this increment

- Permission and support inspection for accelerometer, gyroscope, magnetometer, device orientation, device motion and barometer channels. Inspection does not request permission.
- Sensor permission prompts happen only after the user selects **Allow sensors and start**. Browser API prompts and hardware support are device-dependent.
- Sampling rate selection from 1 to 25 Hz, start/stop controls and a 300-reading in-memory history.
- Readings include linear/gravity acceleration, rotation rate, magnetic field, heading and Euler orientation, pressure, source timestamp and sampling interval.
- Generic Sensor API channels are used when available, with DeviceMotion and DeviceOrientation event channels as fallbacks.
- Diagnostics show granted/denied/unknown support states, secure-context requirements, received and dropped interop samples, observed rate and sensor errors.
- Core sensor values validate finite numbers, non-negative pressure/intervals and normalize compass headings.

## Scope limits

- Physical-device permission and hardware behavior were not verified in this build environment. Browser and device support vary; the page reports actual runtime capabilities and errors.
- Sampling history is held in memory and is cleared when the page closes or the user clears it. Export and persistent storage are not part of this increment.
- Absolute compass heading is available only when the browser provides an absolute orientation or platform compass heading. Relative orientation is labeled accordingly.
- Barometer access depends on browsers exposing `PressureSensor` and granting the relevant device permission.
- The sensor stream is diagnostic input only; fusion, step detection and navigation remain later milestones.
