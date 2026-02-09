var OrderMain = OrderMain || {};

(function () {

    $(document).ready(function () {
        var NewServiceCaseForm = $("#NewServiceCaseViewForm");

        //目前畫面上有抓到這個id的element，才往下執行
        if (NewServiceCaseForm.length > 0) {
            $("#head_btn_scan").hide();
            OrderMain.BindControlEvents();

            var em = $("#FaultInformation");
            em.keydown(function () { OrderMain.CheckInfoLength(); });
            em.change(function () { OrderMain.CheckInfoLength(); });
        }

        //如果有傳入 account product id
        if (accountProductId != null && accountProductId != "") {
            //document.getElementById("duplicateModal").style.display = "block";
            //document.getElementById("AccountProductID").value = accountProductId;
            //找出符合的資料
            OrderMain.SearchProduct("", accountProductId);
        }
    });

    function AddAttachFile() {
        var FileUploadContainer = $("#FileUploadContainer");
        if (!FileUploadContainer) return;

        //目前有幾組
        var count = FileUploadContainer.children().length;

        //沒有的話就新增
        var newFile = document.createElement("div");
        $(newFile)
            .attr("id", "File" + count)
            .addClass("col-xs-12 col-sm-12 col-md-12 upload-group");
        FileUploadContainer.append(newFile);

        var upload = document.createElement("div");
        $(upload).addClass("control-fileupload");
        $(newFile).append(upload);

        //label
        var label = document.createElement("label");
        $(label).addClass("text-left").attr("for", "File" + count);
        $(upload).append(label);

        //select file Button
        var button = document.createElement("div");
        $(button)
            .addClass("btn btn-sm btn-primary rounded-0 upload-file-btn")
            .text(ModelMessageObejct["SelectFile"])
            .on("click", function (e) {
                switch (e.originalEvent.type) {
                    case "click":
                        OrderMain.SetFile(this);
                        break;
                }
            });
        $(upload).append(button);

        var input = document.createElement("input");
        $(input)
            .attr({ type: "file", name: "Attachment[" + count + "]", id: "Attachment" + count })
            .addClass("upload-file")
            .on("change", function () {
                OrderMain.CheckFile(this);
                OrderMain.UpdateFileName(this);
            });
        $(button).append(input);

        //add space
        $(upload).append("&nbsp;&nbsp;");

        //remove file button
        button = document.createElement("div");
        $(button)
            .addClass("btn btn-sm btn-primary rounded-0 remove-file-btn")
            .text(ModelMessageObejct["RemoveFile"])
            .on("click", function () { OrderMain.RemoveFileUpload(this) });
        $(upload).append(button);
    }
    OrderMain.AddAttachFile = AddAttachFile;

    function UpdateFileName(element) {
        var t = $(element).val();
        var labelText = t.substr(12, t.length);
        $(element).parent().siblings("label").text(labelText);
    }
    OrderMain.UpdateFileName = UpdateFileName;

    function SetFile(e) {
        var u = navigator.userAgent;
        var isAndroid = u.indexOf('Android') > -1 || u.indexOf('Adr') > -1; //android终端
        var isiOS = !!u.match(/\(i[^;]+;( U;)? CPU.+Mac OS X/); //ios终端
        if (isiOS == true) {
            window.webkit.messageHandlers.matrix.postMessage("iosFile_" + e.id);
        }
    }
    OrderMain.SetFile = SetFile;

    function CheckFile(e) {
        var totalsize = 0;
        var ua = window.navigator.userAgent;
        var msie = ua.indexOf("MSIE ");
        var fileArray = new Array();
        $('input[type="file"]').each(
            function () {
                if ($(this)[0].files[0] != null) {
                    if ($(this)[0].files[0].size / 1024 < 5000) {
                        var iSize = ($(this)[0].files[0].size / 1024);
                        totalsize = totalsize + iSize;
                    }
                    else {
                        //alert("Single file attachment size limit is 5mb");
                        alert(ModelMessageObejct["AttachFileTitle2"]);
                        if (msie > 0) // If Internet Explorer, return version number
                        {
                            $("#" + e.id).replaceWith($("#" + e.id).clone(true));
                        } else {
                            //$("#" + e.id).val('');
                            RemoveFileUpload($(this));
                            AddAttachFile();
                        }
                    }
                    //檢查檔案有沒有重覆
                    var fileName = $(this)[0].files[0].name;
                    var fileSize = $(this)[0].files[0].size / 1024;
                    for (var i = 0 ; i < fileArray.length ; i++) {
                        if (fileArray[i]["name"] == fileName && fileArray[i]["size"] == fileSize) {
                            alert(ModelMessageObejct["FileDuplicated"]);
                            if (msie > 0) // If Internet Explorer, return version number
                            {
                                $("#" + e.id).replaceWith($("#" + e.id).clone(true));
                            } else {
                                RemoveFileUpload(fileArray[i]["element"]);
                                AddAttachFile();
                            }
                        }
                    }
                    //沒有重覆，加入檢查行列
                    var file = {
                        name: fileName,
                        size: fileSize,
                        element: $(this)
                    };
                    fileArray.push(file);
                }
            });

        if (totalsize > 10000) {
            //alert("Total file attachment size limit is 10mb");
            alert(ModelMessageObejct["AttachFileTitle1"]);
            if (msie > 0) // If Internet Explorer, return version number
            {
                $("#" + e.id).replaceWith($("#" + e.id).clone(true));
            } else {
                //$("#" + e.id).val('');
                RemoveFileUpload($("#"+e.id));
                AddAttachFile();
            }
        }
    }
    OrderMain.CheckFile = CheckFile;

    function RemoveFileUpload(div) {
        $(div).closest(".upload-group").remove();
        //重新整理號碼
        var count = 0;
        $("#FileUploadContainer").children(".upload-group").each(function () {
            $(this).attr({ id: "File" + count });
            $(this).find("label").attr({ "for": "File" + count });
            $(this).find("input[type=file]").attr({ name: "Attachment[" + count + "]" });
            count++;
        });

    }
    OrderMain.RemoveFileUpload = RemoveFileUpload;

    function BindControlEvents() {
        // --> zoey.移到button裡面
        //$('input[type=file]').change(function () {
        //    var t = $(this).val();
        //    var labelText = t.substr(12, t.length);
        //    $(this).parents().prev('label').text(labelText);
        //})

        $("#RegisteredProductName").attr("readonly", "readonly");
        $("#SerialNumber").attr("readonly", "readonly");
        var u = navigator.userAgent;
        var isAndroid = u.indexOf('Android') > -1 || u.indexOf('Adr') > -1; //android终端
        var isiOS = !!u.match(/\(i[^;]+;( U;)? CPU.+Mac OS X/); //ios终端
        $('#head_btn_scan').unbind('click');

        if (isAndroid == true) {
            //$('#head_btn_scan').click(function () {
            //    OrderMain.callAndroidScan();
            //});
            $('#head_btn_scan').on("click touchstart", function () {
                OrderMain.callAndroidScan();
            });
                
        } else if (isiOS == true) {
            //$('#head_btn_scan').click(function () {
            //    OrderMain.callIOSScan();
            //});
            $('#head_btn_scan').on("click touchstart", function () {
                OrderMain.callIOSScan();
            });
        } else {
            //$('#head_btn_scan').click(function () {
            //    OrderMain.SearchProduct();
            //});
            $('#head_btn_scan').on("click touchstart", function () {
                OrderMain.SearchProduct();
            });
        }

        var handled = false;
        $("#head_btn_select").on("touchend click", function (e) {
            e.stopImmediatePropagation();
            if (e.type == "touchend") {
                handled = true;
                $(this).attr("disabled", true);
                btn_select_Click();
                $(this).removeAttr("disabled");
            }
            else if (e.type == "click" && !handled) {
                $(this).attr("disabled", true);
                btn_select_Click();
                $(this).removeAttr("disabled");
            }
            else {
                handled = false;
            }
        });
    }
    OrderMain.BindControlEvents = BindControlEvents;

    //app 會呼叫這個方法
    function displayScan() {
        $("#head_btn_scan").show();
    }
    OrderMain.displayScan = displayScan;

    function callAndroidScan() {
        Android.startscan("androidscan");
    }
    OrderMain.callAndroidScan = callAndroidScan;

    function callIOSScan() {
        window.webkit.messageHandlers.matrix.postMessage("iosscan")
    }
    OrderMain.callIOSScan = callIOSScan;

    function CheckInfoLength() {
        var em = $("#FaultInformation");
        if (em.val().length > 252) {
            em.val(em.val().substring(0, 250));
            alert(ModelMessageObejct["TextAreaMustLessThan250"]);
            em.focus();
        }
    }
    OrderMain.CheckInfoLength = CheckInfoLength;

    function btn_select_Click(orderBy, keepSameSort, page) {
        
        //呼叫 case/CustomerProductInfo 並顯示傳回內容
        if (!orderBy) orderBy = "";
        if (!keepSameSort) keepSameSort = false;
        if (!page) page = 1;

        var parameter = {
            ProductNameCriteria: $("#ProductCriteria").val(),
            ProuductSNCriteria: $("#ProuductSNCriteria").val(),
            InternalNoCriteria: $("#InternalNoCriteria").val(),
            OriginalOrderBy: $("#OrderBy").val(),
            OriginalOrderByAsc: $("#OrderByAsc").val(),
            OrderBy: orderBy,
            KeepSameSort: keepSameSort,
            Page : page
        };
        $.get(selectMachineUrl, parameter, function () { })
        .done(function (data) {
            if (data != null && data.indexOf("CustomerProductViewForm") >= 0) {
                CustomerProduct.CustomerProductShowup(data);
            } else {
                if (data == null || data == "") {
                    alert(ModelMessageObejct["CustomerProductNotFound"]);
                }
                else {
                    $(window).attr('location', logoutUrl);
                }
            }
        })
        .fail(function (xhr, textStatus) {
            if (xhr.responseText.indexOf("unauthorized") > -1) {
                $(window).attr('location', logoutUrl);
            }
        });    
    }
    OrderMain.btn_select_Click = btn_select_Click;

    // app掃描後傳入序號查詢
    // 流程為 1. 把傳入的serial No 當作查詢條件查出符合的CustomerProduct(多筆取第一筆)
    // 2. 將取得的CustomerProductId 再丟到 CustomerProduct.RefreshCustomerProduct 檢查同樣的資料是否有in progress或allocate的case product
    function SearchProduct(serialno, accountProductId) {
        if (!serialno && !accountProductId) {
            alert(ModelMessageObejct["EmptyData"]);
            return;
        }
        var parameter = {
            ProuductSNCriteria: serialno,
            AccountProductId: accountProductId
        };
        $.get(selectMachineUrl, parameter, function () { })
        .done(function (data) {
            if (data != null && data.indexOf("CustomerProductViewForm") >= 0) {
                //建立一個虛擬的div，並把資料放進去後再讀出放回
                var fakeDiv = document.createElement("div");
                $(fakeDiv).html(data);
                var detail = $(fakeDiv).find(".tableContent").first().data("detail");
                if (detail) {
                    CustomerProduct.RefreshCustomerProduct(detail);
                } else {
                    alert(ModelMessageObejct["NoMatchSerialNumber"]);
                }
            } else {
                if (data == null || data == "")
                    alert(ModelMessageObejct["NoResult"]);
                else {
                    $(window).attr('location', logoutUrl);
                }
            }
        })
        .fail(function (xhr, textStatus) {
            if (xhr.responseText.indexOf("unauthorized") > -1) {
                $(window).attr('location', logoutUrl);
            }
        });
    }
    OrderMain.SearchProduct = SearchProduct;

    function GoToOpenRepairReqeust() {
        var accountProductId = $("#duplicateModalNewCase").find("#AccountProductID").val();
        $(window).attr('location', openCasesRequest + "?accountProductId=" + accountProductId);
    }
    OrderMain.GoToOpenRepairReqeust = GoToOpenRepairReqeust;
})();