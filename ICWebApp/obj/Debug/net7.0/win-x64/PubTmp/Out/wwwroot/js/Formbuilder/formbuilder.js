var currentDraggedElementContext = "default";

function SetElementContext(context)
{
    currentDraggedElementContext = context;
}

function BuilderElementHover(elementID) {
    $('#layout_' + elementID).addClass('layer-hover-class');
    //document.getElementById("layout_" + elementID).scrollIntoView({ behavior: "smooth", block: "center", inline: "nearest" });
}
function BuilderElementStopHover(elementID) {
    $('#layout_' + elementID).removeClass('layer-hover-class');
}

function BuilderElementJumpToView(elementID) {
    document.getElementById("layout_" + elementID).scrollIntoView({ behavior: "smooth", block: "center", inline: "nearest" });
}

function formBuilder_setDraggableClass(e)
{
    if (currentDraggedElementContext == "default" || currentDraggedElementContext == "builder")
    { 
        if (!e.hasAttribute('draggableCounter')) {
            e.setAttribute('draggableCounter', 0);
        }
        var draggable_counter = e.getAttribute('draggableCounter');
        e.classList.add('dropable');
        draggable_counter++;
        e.setAttribute('draggableCounter', draggable_counter);
    }
}
function formBuilder_removeDraggableClass(e) {
    if (!e.hasAttribute('draggableCounter')) {
        e.setAttribute('draggableCounter', 0);
    }
    var draggable_counter = e.getAttribute('draggableCounter');
    draggable_counter--;
    e.setAttribute('draggableCounter', draggable_counter);
    if (draggable_counter === 0) {
        e.classList.remove('dropable');
    }
}

function formBuilder_clearDraggableClass() {
    $('div[draggableCounter]').each(function () {
        $(this).removeAttr('draggableCounter');
        $(this).removeClass('dropable');
    });
}


function formBuilder_setDraggableContainerClass(e) {
    if (currentDraggedElementContext == "default" || currentDraggedElementContext == "builder")
    {
        if (!e.hasAttribute('draggableContainerCounter')) {
            e.setAttribute('draggableContainerCounter', 0);
        }
        var draggable_counter = e.getAttribute('draggableContainerCounter');
        this.event.stopPropagation();
        e.classList.add('dropableContainer');
        draggable_counter++;
        e.setAttribute('draggableContainerCounter', draggable_counter);
    }
}
function formBuilder_removeDraggableContainerClass(e) {
    if (!e.hasAttribute('draggableContainerCounter')) {
        e.setAttribute('draggableContainerCounter', 0);
    }
    this.event.stopPropagation();
    var draggable_counter = e.getAttribute('draggableContainerCounter');
    draggable_counter--;
    e.setAttribute('draggableContainerCounter', draggable_counter);
    if (draggable_counter === 0) {
        e.classList.remove('dropableContainer');
    }
}

function formBuilder_clearDraggableContainerClass() {
    $('div[draggableContainerCounter]').each(function () {
        $(this).removeAttr('draggableContainerCounter');
        $(this).removeClass('dropableContainer');
    });
}
/**
 * 
 * Layer
 */
function LayerElementHover(elementID) {
    $('#' + elementID).addClass('layer-hover-class');
    //document.getElementById(elementID).scrollIntoView({ behavior: "smooth", block: "center", inline: "nearest" });
}
function LayerElementStopHover(elementID) {
    $('#' + elementID).removeClass('layer-hover-class');
}

function LayerElementJumpToView(elementID) {
    document.getElementById(elementID).scrollIntoView({ behavior: "smooth", block: "center", inline: "nearest" });
}

function Layer_setDraggableClass(e)
{
    if (currentDraggedElementContext == "default" || currentDraggedElementContext == "layer")
    {
        if (!e.hasAttribute('draggableCounter')) {
            e.setAttribute('draggableCounter', 0);
        }
        var draggable_counter = e.getAttribute('draggableCounter');
        e.classList.add('layer-dropable');
        draggable_counter++;
        e.setAttribute('draggableCounter', draggable_counter);
    }
}
function Layer_removeDraggableClass(e) {
    if (!e.hasAttribute('draggableCounter')) {
        e.setAttribute('draggableCounter', 0);
    }
    var draggable_counter = e.getAttribute('draggableCounter');
    draggable_counter--;
    e.setAttribute('draggableCounter', draggable_counter);
    if (draggable_counter === 0) {
        e.classList.remove('layer-dropable');
    }
}

function Layer_clearDraggableClass() {
    $('div[draggableCounter]').each(function () {
        $(this).removeAttr('draggableCounter');
        $(this).removeClass('layer-dropable');
    });
}


function Layer_setDraggableContainerClass(e) {
    if (!e.hasAttribute('draggableContainerCounter')) {
        e.setAttribute('draggableContainerCounter', 0);
    }
    var draggable_counter = e.getAttribute('draggableContainerCounter');
    this.event.stopPropagation();
    e.classList.add('dropableContainer');
    draggable_counter++;
    e.setAttribute('draggableContainerCounter', draggable_counter);
}
function Layer_removeDraggableContainerClass(e) {
    if (!e.hasAttribute('draggableContainerCounter')) {
        e.setAttribute('draggableContainerCounter', 0);
    }
    this.event.stopPropagation();
    var draggable_counter = e.getAttribute('draggableContainerCounter');
    draggable_counter--;
    e.setAttribute('draggableContainerCounter', draggable_counter);
    if (draggable_counter === 0) {
        e.classList.remove('dropableContainer');
    }
}

function Layer_clearDraggableContainerClass() {
    $('div[draggableContainerCounter]').each(function () {
        $(this).removeAttr('draggableContainerCounter');
        $(this).removeClass('dropableContainer');
    });
}