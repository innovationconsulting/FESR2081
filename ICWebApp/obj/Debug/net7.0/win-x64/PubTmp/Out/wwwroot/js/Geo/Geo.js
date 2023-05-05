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