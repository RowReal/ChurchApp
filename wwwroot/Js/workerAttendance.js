window.workerAttendance = {
    getCurrentPosition: function () {
        return new Promise(function (resolve) {

            if (!navigator.geolocation) {
                resolve({
                    success: false,
                    latitude: 0,
                    longitude: 0,
                    accuracy: 0,
                    message: "Location services are not supported by this browser."
                });

                return;
            }

            navigator.geolocation.getCurrentPosition(
                function (position) {
                    resolve({
                        success: true,
                        latitude: position.coords.latitude,
                        longitude: position.coords.longitude,
                        accuracy: position.coords.accuracy,
                        message: "Location obtained successfully."
                    });
                },

                function (error) {
                    let message =
                        "Unable to obtain your current location.";

                    switch (error.code) {
                        case error.PERMISSION_DENIED:
                            message =
                                "Location permission was denied. Please allow location access in your browser and try again.";
                            break;

                        case error.POSITION_UNAVAILABLE:
                            message =
                                "Your current location is unavailable. Please make sure location services are enabled and try again.";
                            break;

                        case error.TIMEOUT:
                            message =
                                "Location request timed out. Please move to an area with a better GPS signal and try again.";
                            break;
                    }

                    resolve({
                        success: false,
                        latitude: 0,
                        longitude: 0,
                        accuracy: 0,
                        message: message
                    });
                },

                {
                    enableHighAccuracy: true,
                    timeout: 15000,
                    maximumAge: 0
                }
            );
        });
    }
};