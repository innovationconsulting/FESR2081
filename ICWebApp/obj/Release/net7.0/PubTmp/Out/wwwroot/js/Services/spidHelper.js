function SPID_Initialize(targetDiv) {
    document.getElementById(targetDiv).innerHTML = "";
    var dupNode = document.getElementById("spid-root-component").cloneNode(true);
    document.getElementById(targetDiv).appendChild(dupNode);
}