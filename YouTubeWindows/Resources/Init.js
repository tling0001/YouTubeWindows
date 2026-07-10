window.StarboardBridge = window?.chrome?.webview?.hostObjects?.NativeBridge;

/**
 * 创建一个用于拦截并记录日志的 Proxy
 * @param {Object|Function} targetObject 目标对象或函数
 * @param {String} path 当前对象的路径记录（用于日志输出）
 * @returns {Proxy} 返回被代理的对象
 */
function createBridgeProxy(targetObject, path = 'mockObject') {

    // 封装的日志输出函数
    const log = (message) => {
        if (window.StarboardBridge && typeof window.StarboardBridge.ConsoleWriteLine === 'function') {
            window.StarboardBridge.ConsoleWriteLine(message);
        } else {
            // 如果 StarboardBridge 尚未注入，退回使用 console.log 以防丢失报错
            console.log("[Fallback Log] " + message);
        }
    };

    return new Proxy(targetObject, {
        // 拦截读取成员变量 (Read)
        get(target, property, receiver) {
            // 忽略对 Symbol 属性的拦截 (例如原生 Promise、迭代器等内部机制的调用)
            if (typeof property === 'symbol') {
                return Reflect.get(target, property, receiver);
            }

            const currentPath = `${path}.${property}`;
            log(`[Read Capture] ${currentPath}`);

            let value = Reflect.get(target, property, receiver);

            // 【关键点】如果访问的是未定义的属性，我们返回一个空函数。
            // 这样无论调用多深的未定义方法（如 mockObject.a.b.c()），都不会报错，且能被 apply 拦截到。
            if (value === undefined) {
                value = function () { };
            }

            // 如果读取到的值是对象或函数，递归包装成 Proxy，从而实现深层拦截
            if (value !== null && (typeof value === 'object' || typeof value === 'function')) {
                return createBridgeProxy(value, currentPath);
            }

            return value;
        },

        // 拦截赋值成员变量 (Set)
        set(target, property, value, receiver) {
            if (typeof property !== 'symbol') {
                const currentPath = `${path}.${property}`;
                let valStr = '';
                try {
                    valStr = JSON.stringify(value);
                } catch (e) {
                    valStr = String(value); // 防止处理循环引用对象时报错
                }
                log(`[Set Capture] ${currentPath} = ${valStr}`);
            }
            return Reflect.set(target, property, value, receiver);
        },

        // 拦截调用成员方法 (Call)
        apply(target, thisArg, argumentsList) {
            let argsStr = '';
            try {
                argsStr = JSON.stringify(argumentsList);
            } catch (e) {
                argsStr = String(argumentsList);
            }

            log(`[Call Capture] ${path}(${argsStr})`);

            // 执行被代理对象原本的函数逻辑（例如触发 HideSplashScreen）
            return Reflect.apply(target, thisArg, argumentsList);
        }
    });
}

// 基础对象并注入默认方法
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
// 替换 Close
window.close = window?.StarboardBridge?.Close;
// 全屏和重载监听
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
// Video 标签 Hook
window.HTMLVideoElement.prototype.playOriginal = window.HTMLVideoElement.prototype.play;
window.HTMLVideoElement.prototype.play = function (...args) {
    this.msVideoProcessing = "msGraphicsDriverEnhancement";
    return this.playOriginal(...args);
}