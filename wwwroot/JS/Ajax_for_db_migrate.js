$(document).on('click','#MigrateButton',function () {
    $.ajax({
        url: '/Manage/MigrationData',
        type: 'Get',
        success: function(result) {
            alert(result);
        }
    });
});

$(document).on('click','#StartReplication',function () {

    let selectBox = document.getElementById("writeConcern");
    let currentIndex = selectBox.selectedIndex;
    let concern =  selectBox[currentIndex].value;
    if (concern > 0) {
        let url = "/Manage/ReplicationResult?writeConcern=" + concern;
        $("#tableUsers").load(url);
    }
    
});

$(document).on('click','#StartAggregation',function () {

    let selectBox = document.getElementById("aggregationAction");
    let currentIndex = selectBox.selectedIndex;
    let action =  selectBox[currentIndex].value;
    if (action > 0) {
        let url = "/Manage/AggregationResult?aggregationAction=" + action;
        $("#aggregationResult").load(url);
    }

});