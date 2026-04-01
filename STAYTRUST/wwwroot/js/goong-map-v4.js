window.goongMap = {
    maps: {},
    markers: {},
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

    addMarkers: async function (elementId, properties) {
        const map = this.maps[elementId];
        if (!map) return;

        if (this.markers[elementId]) {
            this.markers[elementId].forEach(m => m.remove());
        }
        this.markers[elementId] = [];
        
        if (!this.geocodeCache) {
            this.geocodeCache = {};
        }

        for (const prop of properties) {
            let lat = prop.lat;
            let lng = prop.lng;
            
            if (!lat || !lng) {
                if (prop.address && this.restApiKey) {
                    if (this.geocodeCache[prop.address]) {
                        lat = this.geocodeCache[prop.address].lat;
                        lng = this.geocodeCache[prop.address].lng;
                    } else {
                        try {
                            const response = await fetch(`https://rsapi.goong.io/Geocode?api_key=${this.restApiKey}&address=${encodeURIComponent(prop.address)}`);
                            const data = await response.json();
                            if (data.results && data.results.length > 0) {
                                const loc = data.results[0].geometry.location;
                                lat = loc.lat;
                                lng = loc.lng;
                            } else {
                                // Fallback if address is completely invalid
                                lat = 16.0544 + (Math.random() * 0.01 - 0.005);
                                lng = 108.2022 + (Math.random() * 0.01 - 0.005);
                                console.warn("Geocoding failed for: " + prop.address + ", using fallback");
                            }
                            this.geocodeCache[prop.address] = { lat, lng };
                            
                            // Sleep to avoid rate limiting
                            await new Promise(r => setTimeout(r, 300));
                        } catch (error) {
                             console.error('Error geocoding address:', error);
                             lat = 16.0544 + (Math.random() * 0.01 - 0.005);
                             lng = 108.2022 + (Math.random() * 0.01 - 0.005);
                             this.geocodeCache[prop.address] = { lat, lng };
                        }
                    }
                }
            }

            if (lat && lng) {
                const el = document.createElement('div');
                el.className = 'marker';
                el.style.width = 'auto';
                el.style.height = 'auto';
                el.innerHTML = `<div class="relative flex flex-col items-center justify-center cursor-pointer group">
                                    <div class="relative bg-[#13ecda] text-[#102220] px-2 py-1 rounded-lg flex items-center gap-1 shadow-[0_0_15px_rgba(19,236,218,0.5)] border border-[rgba(19,236,218,0.8)] z-10 transition-transform group-hover:scale-110 group-hover:shadow-[0_0_20px_rgba(19,236,218,0.8)]">
                                        <span class="material-symbols-outlined text-[14px]">apartment</span>
                                        <span class="text-[11px] font-bold whitespace-nowrap">${prop.price}</span>
                                    </div>
                                    <div class="w-2.5 h-2.5 bg-[#13ecda] transform rotate-45 -mt-1.5 z-0"></div>
                                </div>`;

                const marker = new goongjs.Marker(el)
                    .setLngLat([lng, lat])
                    .addTo(map);
                this.markers[elementId].push(marker);
            }
        }
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
