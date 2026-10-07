let activeSession;

async function permissionState(name) {
    if (!navigator.permissions?.query) return "Unknown";
    try {
        const result = await navigator.permissions.query({ name });
        return ({ granted: "Granted", denied: "Denied", prompt: "Prompt" })[result.state] ?? "Unknown";
    } catch {
        return "Unknown";
    }
}

export async function inspect() {
    const generic = typeof window.Accelerometer === "function";
    const permissionNames = ["accelerometer", "gyroscope", "magnetometer", "barometer"];
    const permissions = await Promise.all(permissionNames.map(permissionState));
    const permissionsByName = Object.fromEntries(permissionNames.map((name, index) => [name, permissions[index]]));
    const secure = window.isSecureContext === true;
    const specs = [
        ["Accelerometer", typeof window.LinearAccelerationSensor === "function" || generic || "DeviceMotionEvent" in window, permissionsByName.accelerometer],
        ["Gyroscope", typeof window.Gyroscope === "function" || "DeviceMotionEvent" in window, permissionsByName.gyroscope],
        ["Magnetometer", typeof window.Magnetometer === "function", permissionsByName.magnetometer],
        ["Orientation", "DeviceOrientationEvent" in window, "Unknown"],
        ["DeviceMotion", "DeviceMotionEvent" in window, "Unknown"],
        ["Barometer", typeof window.PressureSensor === "function", permissionsByName.barometer]
    ];
    return {
        secureContext: secure,
        sensors: specs.map(([kind, supported, permission]) => ({
            kind,
            supported: Boolean(supported),
            permission: !secure && supported ? "Unavailable" : supported ? permission : "NotSupported",
            diagnostic: !secure && supported ? "Sensors require HTTPS or localhost." : null
        }))
    };
}

export async function start(dotnet, requestedFrequencyHz) {
    stop();
    if (!window.isSecureContext) throw new Error("Sensor access requires HTTPS or localhost.");

    const frequencyHz = Math.max(1, Math.min(25, Number(requestedFrequencyHz) || 10));
    const session = {
        dotnet,
        frequencyHz,
        sampleIntervalMilliseconds: 1000 / frequencyHz,
        latest: {},
        sensors: [],
        fallbackSensors: [],
        listeners: [],
        timer: null,
        motionFallbackTimer: null,
        callbackPending: false,
        samplesSent: 0,
        droppedSamples: 0,
        errors: [],
        reportedErrors: new Set()
    };
    activeSession = session;

    const requestPermission = async (constructorName) => {
        const constructor = window[constructorName];
        if (typeof constructor?.requestPermission !== "function") return "Granted";
        try {
            const result = await constructor.requestPermission();
            return result === "granted" ? "Granted" : "Denied";
        } catch (error) {
            reportError(session, constructorName, error);
            return "Denied";
        }
    };

    const [motionPermission, orientationPermission] = await Promise.all([
        requestPermission("DeviceMotionEvent"),
        requestPermission("DeviceOrientationEvent")
    ]);
    if (activeSession !== session) return inspect();

    const listen = (name, handler) => {
        window.addEventListener(name, handler, { passive: true });
        session.listeners.push([name, handler]);
    };

    if (motionPermission === "Granted" && "DeviceMotionEvent" in window) {
        listen("devicemotion", (event) => {
            const acceleration = event.acceleration;
            const gravity = event.accelerationIncludingGravity;
            const rotation = event.rotationRate;
            if (acceleration) session.latest.acceleration = vector(acceleration.x, acceleration.y, acceleration.z);
            if (gravity) session.latest.accelerationIncludingGravity = vector(gravity.x, gravity.y, gravity.z);
            // Keep DeviceMotion's rotationRate in its documented device X/Y/Z axes.
            if (rotation) session.latest.rotationRateDegreesPerSecond = vector(rotation.alpha, rotation.beta, rotation.gamma);
            if (Number.isFinite(event.interval) && event.interval > 0) session.latest.sourceInterval = event.interval;
            if (hasMotionMeasurement(session.latest)) {
                if (session.motionFallbackTimer !== null) {
                    window.clearTimeout(session.motionFallbackTimer);
                    session.motionFallbackTimer = null;
                }
                stopGenericSensors(session, session.fallbackSensors);
            }
        });
    } else if (motionPermission === "Denied") {
        reportError(session, "DeviceMotion", new Error("Motion permission was denied."));
    }

    if (orientationPermission === "Granted" && "DeviceOrientationEvent" in window) {
        const onOrientation = (event) => {
            const hasCompass = Number.isFinite(event.webkitCompassHeading);
            const absolute = event.absolute === true || hasCompass;
            // Preserve the device's physical top-edge direction for the red indicator; this is intentionally separate from movement heading.
            session.latest.deviceForwardHeadingDegrees = hasCompass
                ? normalize(event.webkitCompassHeading)
                // Alpha is still useful for the red device indicator when the browser marks its reference as relative.
                : Number.isFinite(event.alpha) ? normalize(360 - event.alpha) : null;
            const hasEulerOrientation = [event.alpha, event.beta, event.gamma].every(Number.isFinite);
            // Use the full Euler orientation to compensate the movement bearing for how the device is tilted in the hand.
            session.latest.headingDegrees = hasCompass
                ? normalize(event.webkitCompassHeading)
                : absolute && hasEulerOrientation
                    ? compassHeading(event.alpha, event.beta, event.gamma)
                    : absolute && Number.isFinite(event.alpha) ? normalize(360 - event.alpha) : null;
            session.latest.pitchDegrees = finite(event.beta);
            session.latest.rollDegrees = finite(event.gamma);
            session.latest.orientationIsAbsolute = absolute;
        };
        listen("deviceorientation", onOrientation);
        listen("deviceorientationabsolute", onOrientation);
    } else if (orientationPermission === "Denied") {
        reportError(session, "Orientation", new Error("Orientation permission was denied."));
    }

    // Prefer the combined DeviceMotion stream, but don't treat the existence of
    // its API as proof that it produces data (desktop Edge exposes it too).
    // If no motion readings arrive shortly after start, try one Generic Sensor
    // accelerometer and one gyroscope as a fallback.
    if (motionPermission === "Granted" && "DeviceMotionEvent" in window) {
        session.motionFallbackTimer = window.setTimeout(() => {
            session.motionFallbackTimer = null;
            if (activeSession !== session || hasMotionMeasurement(session.latest)) return;
            startGenericMotionSensors(session, session.fallbackSensors);
        }, 1500);
    } else {
        startGenericMotionSensors(session);
    }
    startGenericSensor(session, "Magnetometer", "magnetometer", (sensor) => {
        session.latest.magneticFieldMicrotesla = vector(sensor.x, sensor.y, sensor.z);
    });
    startGenericSensor(session, "PressureSensor", "barometer", (sensor) => {
        session.latest.pressureKilopascals = finite(sensor.pressure);
    });

    session.timer = window.setInterval(() => emit(session), session.sampleIntervalMilliseconds);
    return inspect();
}

function startGenericMotionSensors(session, collection = null) {
    const accelerationSensor = typeof window.LinearAccelerationSensor === "function"
        ? "LinearAccelerationSensor"
        : "Accelerometer";
    startGenericSensor(session, accelerationSensor, "accelerometer", (sensor) => {
        const reading = vector(sensor.x, sensor.y, sensor.z);
        if (accelerationSensor === "LinearAccelerationSensor") session.latest.acceleration = reading;
        else session.latest.accelerationIncludingGravity = reading;
    }, collection);
    startGenericSensor(session, "Gyroscope", "gyroscope", (sensor) => {
        const radiansToDegrees = 180 / Math.PI;
        // Generic Gyroscope already reports angular velocity around device X/Y/Z.
        session.latest.rotationRateDegreesPerSecond = vector(sensor.x * radiansToDegrees, sensor.y * radiansToDegrees, sensor.z * radiansToDegrees);
    }, collection);
}

function startGenericSensor(session, constructorName, permissionName, onReading, collection = null) {
    const SensorConstructor = window[constructorName];
    if (typeof SensorConstructor !== "function") return;
    try {
        const sensor = new SensorConstructor({ frequency: session.frequencyHz });
        sensor.addEventListener("reading", () => onReading(sensor));
        sensor.addEventListener("error", (event) => reportError(session, permissionName, event.error ?? new Error("Sensor could not be read.")));
        sensor.start();
        session.sensors.push(sensor);
        collection?.push(sensor);
    } catch (error) {
        reportError(session, permissionName, error);
    }
}

function stopGenericSensors(session, sensors) {
    for (const sensor of sensors) {
        try { sensor.stop(); } catch { /* Sensor may already be inactive. */ }
        const index = session.sensors.indexOf(sensor);
        if (index >= 0) session.sensors.splice(index, 1);
    }
    sensors.length = 0;
}

function hasMotionMeasurement(latest) {
    return [latest.acceleration, latest.accelerationIncludingGravity, latest.rotationRateDegreesPerSecond]
        .some(value => value && [value.x, value.y, value.z].some(Number.isFinite));
}

function emit(session) {
    if (activeSession !== session || !hasMeasurement(session.latest)) return;
    if (session.callbackPending) {
        session.droppedSamples++;
        return;
    }
    session.callbackPending = true;
    const reading = {
        timestampUnixMilliseconds: Date.now(),
        samplingIntervalMilliseconds: session.latest.sourceInterval ?? session.sampleIntervalMilliseconds,
        droppedSamples: session.droppedSamples,
        accelerationX: session.latest.acceleration?.x ?? null,
        accelerationY: session.latest.acceleration?.y ?? null,
        accelerationZ: session.latest.acceleration?.z ?? null,
        accelerationIncludingGravityX: session.latest.accelerationIncludingGravity?.x ?? null,
        accelerationIncludingGravityY: session.latest.accelerationIncludingGravity?.y ?? null,
        accelerationIncludingGravityZ: session.latest.accelerationIncludingGravity?.z ?? null,
        rotationRateX: session.latest.rotationRateDegreesPerSecond?.x ?? null,
        rotationRateY: session.latest.rotationRateDegreesPerSecond?.y ?? null,
        rotationRateZ: session.latest.rotationRateDegreesPerSecond?.z ?? null,
        magneticFieldX: session.latest.magneticFieldMicrotesla?.x ?? null,
        magneticFieldY: session.latest.magneticFieldMicrotesla?.y ?? null,
        magneticFieldZ: session.latest.magneticFieldMicrotesla?.z ?? null,
        headingDegrees: session.latest.headingDegrees ?? null,
        deviceForwardHeadingDegrees: session.latest.deviceForwardHeadingDegrees ?? null,
        deviceScreenOrientationDegrees: getScreenOrientationDegrees(),
        pitchDegrees: session.latest.pitchDegrees ?? null,
        rollDegrees: session.latest.rollDegrees ?? null,
        orientationIsAbsolute: session.latest.orientationIsAbsolute ?? false,
        pressureKilopascals: session.latest.pressureKilopascals ?? null
    };
    session.dotnet.invokeMethodAsync("OnSensorReading", reading)
        .then(() => session.samplesSent++)
        .catch((error) => reportError(session, "Interop", error))
        .finally(() => { session.callbackPending = false; });
}

function hasMeasurement(latest) {
    const vectors = [latest.acceleration, latest.accelerationIncludingGravity, latest.rotationRateDegreesPerSecond, latest.magneticFieldMicrotesla];
    return vectors.some(value => value && [value.x, value.y, value.z].some(Number.isFinite)) ||
        [latest.headingDegrees, latest.pitchDegrees, latest.rollDegrees, latest.pressureKilopascals].some(Number.isFinite);
}

function reportError(session, kind, error) {
    const diagnostic = { kind, message: error?.message ?? String(error), name: error?.name ?? "SensorError" };
    const key = `${kind}:${diagnostic.name}:${diagnostic.message}`;
    if (session.reportedErrors.has(key)) return;
    session.reportedErrors.add(key);
    session.errors.push(diagnostic);
    if (session.errors.length > 50) session.errors.shift();
    session.dotnet.invokeMethodAsync("OnSensorDiagnostic", diagnostic).catch(() => {});
}

export async function stop() {
    const session = activeSession;
    if (!session) return;
    activeSession = undefined;
    if (session.timer !== null) window.clearInterval(session.timer);
    if (session.motionFallbackTimer !== null) window.clearTimeout(session.motionFallbackTimer);
    for (const [name, handler] of session.listeners) window.removeEventListener(name, handler);
    for (const sensor of session.sensors) {
        try { sensor.stop(); } catch { /* Sensor may already be inactive. */ }
    }
}

export async function getDiagnostics() {
    const session = activeSession;
    return session ? {
        requestedFrequencyHz: session.frequencyHz,
        sampleIntervalMilliseconds: session.sampleIntervalMilliseconds,
        samplesSent: session.samplesSent,
        droppedSamples: session.droppedSamples,
        errors: session.errors
    } : null;
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

function vector(x, y, z) {
    return { x: finite(x), y: finite(y), z: finite(z) };
}

function finite(value) {
    return Number.isFinite(value) ? value : null;
}

function normalize(degrees) {
    return ((degrees % 360) + 360) % 360;
}

// screen.orientation follows the handset's portrait/landscape rotation: 0° draws the red phone axis vertically, 90° horizontally.
function getScreenOrientationDegrees() {
    const angle = Number(window.screen?.orientation?.angle ?? window.orientation ?? 0);
    return Number.isFinite(angle) ? normalize(angle) : 0;
}

// Project the device's screen-facing axis onto the horizontal plane. This movement heading compensates for pitch and roll;
// the separate deviceForwardHeadingDegrees value remains the red top-edge indicator.
function compassHeading(alphaDegrees, betaDegrees, gammaDegrees) {
    if (![alphaDegrees, betaDegrees, gammaDegrees].every(Number.isFinite)) return null;

    const radians = Math.PI / 180;
    const alpha = alphaDegrees * radians;
    const beta = betaDegrees * radians;
    const gamma = gammaDegrees * radians;
    const sinAlpha = Math.sin(alpha);
    const cosAlpha = Math.cos(alpha);
    const sinBeta = Math.sin(beta);
    const cosBeta = Math.cos(beta);
    const sinGamma = Math.sin(gamma);
    const cosGamma = Math.cos(gamma);

    const east = -cosAlpha * sinGamma - sinAlpha * sinBeta * cosGamma;
    const north = -sinAlpha * sinGamma + cosAlpha * sinBeta * cosGamma;
    if (Math.hypot(east, north) < 1e-6) return null;
    return normalize(Math.atan2(east, north) / radians);
}
