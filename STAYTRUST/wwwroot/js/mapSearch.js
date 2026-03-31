window.RoomMap = {
    map: null,
    markers: [],

    initMap: function (containerId) {
        // Tọa độ trung tâm Đà Nẵng
        const daNangCenter = [16.0600, 108.2200];
        
        // Khởi tạo map Leaflet
        this.map = L.map(containerId).setView(daNangCenter, 13);
        
        // Render Tile Layer từ OpenStreetMap (Miễn phí 100%)
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
        }).addTo(this.map);
    },

    updateMarkers: function (roomDtos) {
        if (!this.map) return;

        // Báo cho Leaflet biết size container để fix lỗi tile map bị cắt ô vuông 
        setTimeout(() => {
            this.map.invalidateSize();
        }, 100);

        // Xoá các marker cũ khỏi bản đồ
        for (let i = 0; i < this.markers.length; i++) {
            this.map.removeLayer(this.markers[i]);
        }
        this.markers = [];
        
        let markerArray = [];

        // Vẽ các marker mới
        for (let i = 0; i < roomDtos.length; i++) {
            const room = roomDtos[i];
            
            // Tạo marker Leaflet
            const marker = L.marker([room.latitude, room.longitude]).addTo(this.map);

            // Format giá tiền (VND)
            const formatter = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' });
            const priceText = formatter.format(room.price);
            
            // Nội dung khung Popup Info
            const popupContent = `
                <div style="color: #333; padding: 5px; min-width: 200px;">
                    <h5 style="margin: 0 0 8px 0; font-weight: bold; font-family: sans-serif; font-size: 15px;">${room.title}</h5>
                    <p style="margin: 0; color: white; background: #13ecda; display: inline-block; padding: 4px 8px; border-radius: 4px; font-weight: bold; font-size: 14px;">${priceText}</p>
                    <p style="margin: 8px 0 0 0; font-size: 12px; color: #666;">
                        <span style="font-weight: bold;">Tiện ích:</span> ${room.amenities.join(', ')}
                    </p>
                </div>
            `;

            // Gán bindPopup (Event click tự được build ngầm bởi Leaflet)
            marker.bindPopup(popupContent);
            this.markers.push(marker); // Thêm marker vào collection để track
            markerArray.push(marker);
        }

        // Tự động zoom và chuyển góc nhìn map tới các cụm marker tìm được
        if (markerArray.length > 0) {
            const group = new L.featureGroup(markerArray);
            this.map.fitBounds(group.getBounds(), {
                padding: [50, 50],
                maxZoom: 16
            });
        }
    }
};
