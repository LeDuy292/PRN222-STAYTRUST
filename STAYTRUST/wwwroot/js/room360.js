/**
 * room360.js — 360° Panorama Viewer using Photo Sphere Viewer
 * Lifecycle: initViewer → (user interacts) → destroyViewer
 * Caller: PropertyDetail.razor via IJSRuntime
 */
window.Room360 = (function () {
    let viewerInstance = null;

    return {
        /**
         * Initialize the 360° viewer inside a container element.
         * @param {string} containerId - DOM id of the container div
         * @param {string} imageUrl    - URL of the equirectangular panorama image
         */
        initViewer: function (containerId, imageUrl) {
            // Destroy any previous instance to prevent memory leaks
            this.destroyViewer();

            const container = document.getElementById(containerId);
            if (!container) {
                console.error('[Room360] Container not found:', containerId);
                return false;
            }

            try {
                viewerInstance = new PhotoSphereViewer.Viewer({
                    container: container,
                    panorama: imageUrl,
                    navbar: ['zoom', 'fullscreen'],
                    defaultZoomLvl: 50,
                    touchmoveTwoFingers: true,
                    loadingTxt: 'Đang tải ảnh 360°...',
                    autorotateDelay: 2000,
                    autorotateSpeed: '1rpm',
                });

                // Listen for ready event
                viewerInstance.addEventListener('ready', () => {
                    console.log('[Room360 Virtual Tour] Ready.');
                    container.dispatchEvent(new CustomEvent('viewerReady'));
                });

                return true;
            } catch (err) {
                console.error('[Room360] Failed to initialize viewer:', err);
                return false;
            }
        },

        /**
         * Destroy the current viewer instance, releasing WebGL resources.
         */
        destroyViewer: function () {
            if (viewerInstance) {
                try {
                    viewerInstance.destroy();
                } catch (e) {
                    console.warn('[Room360] Error destroying viewer:', e);
                }
                viewerInstance = null;
            }
        }
    };
})();
