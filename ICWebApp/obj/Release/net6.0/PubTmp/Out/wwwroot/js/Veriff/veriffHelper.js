function Veriff_Initialize(divID, apikey, FirstName, LastName, UserID) {
    console.log("test");
    veriff = Veriff({
        host: 'https://stationapi.veriff.com',
        apiKey: apikey,
        parentId: divID,
        onSession: function (err, response) {
            window.location.replace(response.verification.url);
        }
    });
    veriff.setParams({
        person: {
            givenName: FirstName,
            lastName: LastName
        },
        vendorData: UserID
    });

    var lang = navigator.language || navigator.userLanguage;

    if (lang.includes("it")) {
        veriff.mount({
            submitBtnText: 'Inizia la verifica',
            loadingText: 'Carica...'
        });
    }
    else {
        veriff.mount({
            submitBtnText: 'Verifizierung starten',
            loadingText: 'Bitte warten...'
        });
    }
}