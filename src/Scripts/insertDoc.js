var fileTypeBool = false,
    fileSizeBool = false;


//利用脚本获取上传文件大小
function GetFileSize(fileid) {
    var fileSize = 0;
    fileSize = $("#" + fileid)[0].files[0].size;
    fileSize = fileSize / 1048576;
    return fileSize;
}
//根据上传的路径获取文件名称
function getNameFromPath(strFilepath) {
    var objRE = new RegExp(/([^\/\\]+)$/);
    var strName = objRE.exec(strFilepath);

    if (strName == null) {
        return null;
    }
    else {
        return strName[0];
    }
}

//当更换文件时触发Change事件对其文件类型和文件大小进行验证
$("#FileName").change(function () {
    var file = getNameFromPath($(this).val());
    if (file != null) {
        $("#warning").html("");
        var errors = $(document).find(".field-validation-error");
        $.each(errors, function (k, v) {
            if ($(v).attr("data-valmsg-for") === "FileName") {
                $(v).hide();
            }
        });
        var extension = file.substr((file.lastIndexOf('.') + 1));
        switch (extension) {
            case 'zip':
                fileTypeBool = false;
                break;
            default:
                fileTypeBool = true;
        }
    }
    if (fileTypeBool) {
        $("#warning").html("只能上传扩展名为zip的文件！");
        return false;
    }
    else {
        var size = GetFileSize('FileName');
        if (size > 1024) {
            fileSizeBool = true;
            $("#warning").html("上传文件已经超过1G！");
        } else {
            fileSizeBool = false;
        }
    }
});
