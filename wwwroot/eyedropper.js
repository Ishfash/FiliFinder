// wwwroot/eyedropper.js
window.initializeEyeDropper = function () {
    return new Promise((resolve, reject) => {
        if (!window.EyeDropper) {
            reject("EyeDropper API not supported");
            return;
        }
        const eyeDropper = new EyeDropper();
        eyeDropper.open()
            .then(result => resolve(result.sRGBHex))
            .catch(err => {
                if (err.toString().includes("AbortError")) {
                    resolve(null); // User canceled
                } else {
                    reject(err.toString());
                }
            });
    });
};