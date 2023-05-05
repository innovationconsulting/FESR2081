var GEO_GlobdotNetObjectRef = null;

function Geo_OpenUrl(dotNetObjectRef, GeoUrl) {
    GEO_GlobdotNetObjectRef = dotNetObjectRef;

    if (document.body.classList.contains("MobileApp"))
    {
        location.href = GeoUrl;
    }
}

function Geo_FireEvent(LanLat) 
{
    GEO_GlobdotNetObjectRef.invokeMethodAsync('OnEvent', JSON.stringify(LanLat));
}

function getCurrentPosition(dotNetHelper, enableHighAccuracy, maximumAge) {

    const options = {
        enableHighAccuracy: enableHighAccuracy,
        timeout: 5000,
        maximumAge: maximumAge
    };

    function success(position) {

        const coordinate = {
            Lan: position.coords.longitude,
            Lat: position.coords.latitude
        };

        dotNetHelper.invokeMethodAsync('OnSuccessAsync', coordinate);
    }

    function error(error) {
        dotNetHelper.invokeMethodAsync('OnErrorAsync', 'Fehlercode: ' + error.code + ' - ' +  error.message);
    }

    navigator.geolocation.getCurrentPosition(success, error, options);
}