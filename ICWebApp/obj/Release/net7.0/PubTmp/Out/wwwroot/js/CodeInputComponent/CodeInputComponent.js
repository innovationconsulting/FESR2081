function CodeInput_ValidateInput(element) {
    var value = element.value;

    if (value < 0) {
        element.value = 0;
    }
    else if (value > 9) {
        element.value = 9;
    }

    if (element.id.includes("1")) {
        $("#value_2").focus();
    }
    else if (element.id.includes("2")) {
        $("#value_3").focus();
    }
    else if (element.id.includes("3")) {
        $("#value_4").focus();
    }
    else if (element.id.includes("4")) {
        $("#value_5").focus();
    }
    else if (element.id.includes("5")) {
        $("#value_6").focus();
    }
}

function CodeInput_GetValue() {
    var code = "";

    var val1 = $("#value_1").val();
    var val2 = $("#value_2").val();
    var val3 = $("#value_3").val();
    var val4 = $("#value_4").val();
    var val5 = $("#value_5").val();
    var val6 = $("#value_6").val();

    code = val1 + val2 + val3 + val4 + val5 + val6;

    return code;
}