window.scrollTracker = (function () {
    let dotNetRef = null;
    let handler = null;
    let ticking = false;

    return {
        init: function (ref) {
            dotNetRef = ref;
            handler = function () {
                if (!ticking) {
                    window.requestAnimationFrame(function () {
                        if (dotNetRef) {
                            dotNetRef.invokeMethodAsync('UpdateScroll', window.scrollY);
                        }
                        ticking = false;
                    });
                    ticking = true;
                }
            };
            window.addEventListener('scroll', handler, { passive: true });
        },
        dispose: function () {
            if (handler) {
                window.removeEventListener('scroll', handler);
                handler = null;
            }
            dotNetRef = null;
        }
    };
})();
