export function ToggleCollapse(ID) {
    $("#" + ID).toggle(100);
    $("#detail-button_down_" + ID).toggle(100);
    $("#detail-button_up_" + ID).toggle(100);
}