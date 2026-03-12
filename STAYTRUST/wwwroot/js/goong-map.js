window.goongMap = {
    maps: {},
    maptilesKey: '',
    restApiKey: '',

    init: function (maptilesKey, restApiKey) {
        this.maptilesKey = maptilesKey;
        this.restApiKey = restApiKey;
    },

    initSearchMap: function (elementId, lat, lng, zoom) {
        if (!this.maptilesKey) {
            console.error('Goong maptilesKey not initialized');
            return;
        }
        goongjs.accessToken = this.maptilesKey;
        const map = new goongjs.Map({
            container: elementId,
            style: `https://tiles.goong.io/assets/goong_map_web.json?api_key=${this.maptilesKey}`,
            center: [lng, lat],
            zoom: zoom
        });
        this.maps[elementId] = map;
        return map;
    },

    addMarkers: function (elementId, properties) {
        const map = this.maps[elementId];
        if (!map) return;

        properties.forEach(prop => {
            const el = document.createElement('div');
            el.className = 'marker';
            el.style.width = '30px';
            el.style.height = '30px';
            el.innerHTML = `<div class="relative flex items-center justify-center">
                                <div class="absolute animate-ping h-8 w-8 rounded-full bg-[#13ecda] opacity-40"></div>
                                <div class="relative h-6 w-6 bg-[#13ecda] rounded-full flex items-center justify-center shadow-[0_0_15px_rgba(19,236,218,0.6)]">
                                    <span class="text-[10px] font-bold text-[#102220]">$${prop.price}</span>
                                </div>
                            </div>`;

            new goongjs.Marker(el)
                .setLngLat([prop.lng, prop.lat])
                .addTo(map);
        });
    },

    getSuggestions: async function (input) {
        if (!this.restApiKey) {
            console.error('Goong restApiKey not initialized');
            return [];
        }
        if (!input || input.length < 2) return [];
        try {
            // Bias to Da Nang area (16.0544, 108.2022) with 15km radius
            const response = await fetch(`https://rsapi.goong.io/Place/AutoComplete?api_key=${this.restApiKey}&input=${encodeURIComponent(input)}&location=16.0544,108.2022&radius=15000`);
            const data = await response.json();
            return data.predictions || [];
        } catch (error) {
            console.error('Error fetching suggestions:', error);
            return [];
        }
    },

    geocode: async function (address) {
        if (!this.restApiKey) return null;
        try {
            const response = await fetch(`https://rsapi.goong.io/Geocode?api_key=${this.restApiKey}&address=${encodeURIComponent(address)}`);
            const data = await response.json();
            if (data.results && data.results.length > 0) {
                const loc = data.results[0].geometry.location;
                return { lat: loc.lat, lng: loc.lng };
            }
        } catch (error) {
            console.error('Error geocoding address:', error);
        }
        return null;
    },

    flyTo: function (elementId, lat, lng, zoom) {
        const map = this.maps[elementId];
        if (map) {
            map.flyTo({
                center: [lng, lat],
                zoom: zoom || map.getZoom(),
                essential: true
            });
        }
    }
};
