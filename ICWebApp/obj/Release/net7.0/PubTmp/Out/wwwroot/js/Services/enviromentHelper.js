function enviromentHelper_AddScreenWidthListener(dotNetObjectRef, interval) {
    dotNetObjectRef.invokeMethodAsync('OnEvent', JSON.stringify(window.innerWidth));
    window.addEventListener('resize', () => {
        dotNetObjectRef.invokeMethodAsync('OnEvent', JSON.stringify(window.innerWidth));
    }, { passive: true });
}
function BlazorDownloadFileFromPath(Path, Filename, Token) {
    if (document.body.classList.contains("MobileApp")) {
        var base_url = window.location.origin;
        var url = base_url + '/Download?link=' + Path + "&filename=" + Filename + "&token=" + Token;

        location.href = 'AppDownload=' + Filename + ';' + url;
    }
    else {
        location.href = '/Download?link=' + Path + "&filename=" + Filename + "&token=" + Token;
    }
}

function enviromentHelper_MouseOverDiv(id) {
    const element = document.getElementById(id);
    if (element != null && element.parentNode.matches(":hover")) {
        return "true";
    } else {
        return "false";
    }
}

function enviromentHelper_SmoothScrollToTop() {
    let elem = document.getElementsByClassName('it-header-wrapper')[0];
    elem.scrollIntoView({ behavior: 'smooth' });
}

function enviromentHelper_clickElement(id) {
    $('#' + id).trigger('click');

    return true;
}

function enviromentHelper_CheckWidthOnStartup() {
    return JSON.stringify(window.innerWidth);
}

function enviromentHelper_scrollToBottom(id) {
    console.log("test");
    const element = document.getElementById(id);
    if (isDevice)
    {
        element.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'start', offset: '0px' });
    }
    else
    {
        element.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'start', offset: '20px' });
    }
}
function enviromentHelper_SetPageTitle(title) {
    document.title = title;
}

function isDevice() {
    return /android|webos|iphone|ipad|ipod|blackberry|iemobile|opera mini|mobile/i.test(navigator.userAgent);
}
function enviromentHelper_GetHtml(id) {
    return $("#" + id).html();
}

function searchbar_ToggleVisibility(id) {
    $("#" + id).modal('toggle');
}

function navmenu_ToggleVisibility() {
    document.getElementsByClassName("close-menu")[0].click();
}
function getRootWidth() {
    return window.innerWidth;
}