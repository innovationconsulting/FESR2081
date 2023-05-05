export function AddScrollListener(DivID) {

    var amount = 420;

    $("#" + DivID).on('mousewheel', throttle(function (event) {
        event.preventDefault();

        var oEvent = event.originalEvent, direction = oEvent.detail ? oEvent.detail * -amount : oEvent.wheelDelta, position = $(this).scrollLeft();
        position += direction > 0 ? -amount : amount;
        $(this).scrollLeft(position);
    }, 100));

    console.log($("#" + DivID).get(0).clientWidth);
    console.log($("#" + DivID).get(0).scrollWidth);

    if ($("#" + DivID).get(0).scrollWidth > $("#" + DivID).get(0).clientWidth) {
        $("#left-arrow").show(0);
        $("#right-arrow").show(0);
    }

    document.querySelector('#left-arrow').addEventListener("click", (evt) => {
        evt.preventDefault();

        var startValue = $("#" + DivID).scrollLeft();

        console.log(startValue);

        $("#" + DivID).scrollLeft(startValue + (amount * -1));
    });

    document.querySelector('#right-arrow').addEventListener("click", (evt) => {
        evt.preventDefault();

        var startValue = $("#" + DivID).scrollLeft();

        console.log(startValue);

        $("#" + DivID).scrollLeft(startValue + amount);
    });
}
export function throttle(func, wait) {
    let waiting = false;
    return function () {
        if (waiting) {
            return;
        }

        waiting = true;
        setTimeout(() => {
            func.apply(this, arguments);
            waiting = false;
        }, wait);
    };
}