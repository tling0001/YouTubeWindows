window.StarboardBridge = window?.chrome?.webview?.hostObjects?.NativeBridge;

/**
 * Creates a Proxy used to intercept and log activity.
 * @param {Object|Function} targetObject The target object or function.
 * @param {String} path The current object path used for log output.
 * @returns {Proxy} The proxied object.
 */
function createBridgeProxy(targetObject, path = 'mockObject') {

    // Wrapped logging helper
    const log = (message) => {
        if (window.StarboardBridge && typeof window.StarboardBridge.ConsoleWriteLine === 'function') {
            window.StarboardBridge.ConsoleWriteLine(message);
        } else {
            // If StarboardBridge has not been injected yet, fall back to console.log so errors are not lost.
            console.log("[Fallback Log] " + message);
        }
    };

    return new Proxy(targetObject, {
        // Intercept property reads
        get(target, property, receiver) {
            // Ignore Symbol properties (for example native Promise and iterator internals).
            if (typeof property === 'symbol') {
                return Reflect.get(target, property, receiver);
            }

            const currentPath = `${path}.${property}`;
            log(`[Read Capture] ${currentPath}`);

            let value = Reflect.get(target, property, receiver);

            // Key point: if the property is undefined, return a no-op function.
            // That way deeply chained calls like mockObject.a.b.c() do not throw and can still be intercepted.
            if (value === undefined) {
                value = function () { };
            }

            // If the value is an object or function, wrap it recursively in a Proxy for deep interception.
            if (value !== null && (typeof value === 'object' || typeof value === 'function')) {
                return createBridgeProxy(value, currentPath);
            }

            return value;
        },

        // Intercept property writes
        set(target, property, value, receiver) {
            if (typeof property !== 'symbol') {
                const currentPath = `${path}.${property}`;
                let valStr = '';
                try {
                    valStr = JSON.stringify(value);
                } catch (e) {
                    valStr = String(value); // Prevent errors when handling circular references.
                }
                log(`[Set Capture] ${currentPath} = ${valStr}`);
            }
            return Reflect.set(target, property, value, receiver);
        },

        // Intercept function calls
        apply(target, thisArg, argumentsList) {
            let argsStr = '';
            try {
                argsStr = JSON.stringify(argumentsList);
            } catch (e) {
                argsStr = String(argumentsList);
            }

            log(`[Call Capture] ${path}(${argsStr})`);

            // Execute the original function logic (for example, trigger HideSplashScreen).
            return Reflect.apply(target, thisArg, argumentsList);
        }
    });
}

// Base object and default methods
const baseObject = {
    system: {
        hideSplashScreen: function () {
            if (window.StarboardBridge && typeof window.StarboardBridge.HideSplashScreen === 'function') {
                return window.StarboardBridge.HideSplashScreen();
            }
        }
    }
};

window.h5vcc = createBridgeProxy(baseObject, 'h5vcc');
// Replace Close
window.close = window?.StarboardBridge?.Close;
// Fullscreen and reload listeners
window.addEventListener('keydown', (event) => {
    if (event.keyCode === 122) {
        event.preventDefault();
        event.stopPropagation();
        event.stopImmediatePropagation();
        event.returnValue = false; StarboardBridge.ToggleFullscreen();
    }
    if (event.keyCode == 116 || (event.ctrlKey && event.keyCode == 82)) {
        event.preventDefault();
        event.stopPropagation();
        event.stopImmediatePropagation();
        event.returnValue = false; StarboardBridge.ReloadApp();
    }
}, true);
// Video tag hook
window.HTMLVideoElement.prototype.playOriginal = window.HTMLVideoElement.prototype.play;
window.HTMLVideoElement.prototype.play = function (...args) {
    this.msVideoProcessing = "msGraphicsDriverEnhancement";
    return this.playOriginal(...args);
}