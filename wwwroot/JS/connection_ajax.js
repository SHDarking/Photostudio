$(document).change('#HallSelect', function () {
    let selectBox = document.getElementById("HallSelect");
    let currentIndex = selectBox.selectedIndex;
    let id =  selectBox[currentIndex].value;
    let action = 2;
    let url = "/Reserving/GetFreedomHallTime?id=" + id + "&action=" + action;
    $("#calendar").load(url);
});

$(document).on('click','.currentWeek',function () {

    let selectBox = document.getElementById("HallSelect");
    let currentIndex = selectBox.selectedIndex;
    let id =  selectBox[currentIndex].value;
    let action = 2;
    let url = "/Reserving/GetFreedomHallTime?id=" + id + "&action=" + action;
    $("#calendar").load(url);
});



$(document).on('click', '.nextWeek',function () {
    let selectBox = document.getElementById("HallSelect");
    let currentIndex = selectBox.selectedIndex;
    let id = selectBox[currentIndex].value;
    let action = 3;
    let date = $(this).siblings('span').attr('id');
    let url = "/Reserving/GetFreedomHallTime?id=" + id + "&action=" + action + "&date=" + date;
        $("#calendar").load(url);
});



$(document).on('click', '.prevWeek', function () {
    if ($(this).hasClass("disabled")) {

    }
    else {
        let selectBox = document.getElementById("HallSelect");
        let currentIndex = selectBox.selectedIndex;
        let id = selectBox[currentIndex].value;
        let action = 1;
        let date = $(this).siblings('span').attr('id');
        let url = "/Reserving/GetFreedomHallTime?id=" + id + "&action=" + action + "&date=" + date;
        $("#calendar").load(url);
    }
});

$(document).ready(function () {
    let selectBox = document.getElementById("HallSelect");
    let currentIndex = selectBox.selectedIndex;
    let id =  selectBox[currentIndex].value;
    let action = 2;
    let url = "/Reserving/GetFreedomHallTime?id=" + id + "&action=" + action;
    $("#calendar").load(url);
});





