window.bccLocation = {

    getCurrentPosition: function () {

        return new Promise((resolve) => {

            if (!navigator.geolocation) {

                resolve({
                    success: false,
                    latitude: 0,
                    longitude: 0,
                    accuracy: 0,
                    error: "Geolocation is not supported by this browser."
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
                        error: ""
                    });
                },

                function (error) {

                    let message =
                        "Unable to obtain your GPS location.";

                    switch (error.code) {

                        case error.PERMISSION_DENIED:
                            message =
                                "Location permission was denied. Please allow location access for this website.";
                            break;

                        case error.POSITION_UNAVAILABLE:
                            message =
                                "Your current location is unavailable.";
                            break;

                        case error.TIMEOUT:
                            message =
                                "The GPS request timed out. Please try again.";
                            break;
                    }

                    resolve({
                        success: false,
                        latitude: 0,
                        longitude: 0,
                        accuracy: 0,
                        error: message
                    });
                },

                {
                    enableHighAccuracy: true,
                    timeout: 20000,
                    maximumAge: 0
                }
            );
        });
    },

    copyText: async function (text) {

        if (navigator.clipboard &&
            window.isSecureContext) {

            await navigator.clipboard.writeText(text);
            return;
        }

        const textarea =
            document.createElement("textarea");

        textarea.value = text;
        textarea.style.position = "fixed";
        textarea.style.opacity = "0";

        document.body.appendChild(textarea);

        textarea.focus();
        textarea.select();

        document.execCommand("copy");

        document.body.removeChild(textarea);
    }
};