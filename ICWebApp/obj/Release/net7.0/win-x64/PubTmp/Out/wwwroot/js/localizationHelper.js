window.browserJsFunctions = {
    getLanguage: () => {
        return navigator.language || navigator.userLanguage;
    },
    getBrowserTimeZoneOffset: () => {
        return new Date().getTimezoneOffset();
    },
    getBrowserTimeZoneIdentifier: () => {
        return Intl.DateTimeFormat().resolvedOptions().timeZone;
    },
};