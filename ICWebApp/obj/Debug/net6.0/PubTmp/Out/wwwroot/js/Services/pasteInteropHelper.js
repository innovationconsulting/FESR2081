export function addOnPasteEventListener(elem, componentInstance) {

    elem.onpaste = (e) =>
    {
        var text = e.clipboardData.getData('text');

        componentInstance.invokeMethodAsync('HandlePaste', text);
    }
}