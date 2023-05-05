function NavMenu_Mobile_OnShow(targetDiv) {
    var element = document.getElementById(targetDiv);
    if (element != null) {
        element.classList.remove("mobile-menu-popup-container-hide");
        element.classList.add("mobile-menu-popup-container-show");
    }
}

function NavMenu_Mobile_OnHide(targetDiv) {
    var element = document.getElementById(targetDiv);

    if (element != null) {
        element.classList.remove("mobile-menu-popup-container-show");
        element.classList.add("mobile-menu-popup-container-hide");
    }
}

//let touchstartX = 0
//let touchendX = 0

//document.addEventListener('touchstart', e => {
//    touchstartX = e.changedTouches[0].screenX
//});
//document.addEventListener('touchend', e => {
//    touchendX = e.changedTouches[0].screenX
//    handleGesture()
//});

//function handleGesture() {
//    var difference = touchendX - touchstartX;

//    if (touchendX < touchstartX) {
//        if (Math.abs(difference) > 30) {
//        NavMenu_Mobile_OnHide('mobile-menu-popup-container');        
//        }
//    }
//    if (touchendX > touchstartX) {
//        if (Math.abs(difference) > 100) {
//            NavMenu_Mobile_OnShow('mobile-menu-popup-container');
//        }
//    }
//}