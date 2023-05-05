function anchorHelper_ScrollIntoView(id) {
    var element = document.getElementById(id);

    var pos = element.style.position;
    var top = element.style.top;
    element.style.position = 'relative';
    element.style.top = '-20px';
    element.scrollIntoView({ behavior: 'smooth', block: 'start' });
    element.style.top = top;
    element.style.position = pos;
}