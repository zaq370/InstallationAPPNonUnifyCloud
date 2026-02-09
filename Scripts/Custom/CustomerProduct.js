var CustomerProduct = CustomerProduct || {};

(function () {

    function CustomerProductShowup(data) {
        var panel = $("#head_Pan_select");
        if (!panel) return;

        //先把資料塞進暫存的panel
        var tempPanel = document.createElement("div");
        $(tempPanel).html(data);

        if ($(panel).css("display") == "none") {
            if (($(tempPanel).find(".space.tableContent").length == 0))
                alert(ModelMessageObejct["CustomerProductNotFound"]);
            else {
                panel.html(data);   //畫面都還沒出現, 就直接填充
                $(panel).css({
                    zIndex: 10001,
                    position: "fixed",
                    display: "block"
                });
                $("#head_Pan_background").css({
                    zIndex: 10000,
                    position: "fixed",
                    display: "block",
                    width: "100%",
                    height: "100%",
                    top: "0px",
                    left: "0px"
                });
            }
        } else {
            //再填充目的panel的資料，目的在於不 refresh 畫面
            $(panel).find("#ProductCriteria").val($(tempPanel).find("#ProductCriteria").val());
            $(panel).find("#ProuductSNCriteria").val($(tempPanel).find("#ProuductSNCriteria").val());
            $(panel).find("#InternalNoCriteria").val($(tempPanel).find("#InternalNoCriteria").val());
            $(panel).find("#OrderBy").val($(tempPanel).find("#OrderBy").val());
            $(panel).find("#OrderByAsc").val($(tempPanel).find("#OrderByAsc").val());
            //刪除
            $(panel).find(".productDetailTable").children().remove();
            $(panel).find(".productDetailTable").append($(tempPanel).find(".productDetailTable").children().clone(true, true));
            //pager
            $(panel).find("#viewPager").children().remove();
            $(panel).find("#viewPager").append($(tempPanel).find("#viewPager").children().clone(true, true));
        }
    }
    CustomerProduct.CustomerProductShowup = CustomerProductShowup;

    function CustomerProductClose() {
        var panel = $("#head_Pan_select");
        if (!panel) return;

        $(panel).css("display", "none");
        $("#head_Pan_background").css("display", "none");
        $(panel).find("#CustomerProductViewForm").remove();
    }
    CustomerProduct.CustomerProductClose = CustomerProductClose;

    
    function SelectCustomerProduct(element) {
        var detail = $(element).closest(".tableContent").data("detail");
        RefreshCustomerProduct(detail);
    }
    CustomerProduct.SelectCustomerProduct = SelectCustomerProduct;

    function RefreshCustomerProduct(detail) {
        var parameter = {
            accountProductId: detail["AccountProductId"]
        };

        $.get(checkOpenCaseUrl, parameter, function () { })
        .done(function (data) {
            var returnValue = JSON.parse(data);
            if (!returnValue["Status"]) {
                alert(ModelMessageObejct["ErrorOccrus"]);
                if (returnValue["Error"] == "NoAuthroization") {
                    $(window).attr('location', logoutUrl);
                }
            } else {
                //先讓畫面上的錯誤清除
                if (Org != "JHTNL" ) {
                    $('#NewCaseServiceForm').find("#RegisteredProductName,#SerialNumber").each(function () {
                        $(this).siblings(".field-validation-error").children("span").remove();
                        $(this).siblings(".filedError").remove();
                    })
                    $('#NewCaseServiceForm').find("#RegisteredProductName").removeData("exists");
                    //已經是重覆的資料顯示錯誤
                    if (returnValue["Exists"]) {
                        var errorSpan = document.createElement("span");
                        $(errorSpan).addClass("filedError");
                        $(errorSpan).text(ModelMessageObejct["CaseProductExists"]);
                        $("#RegisteredProductName").parent().append(errorSpan);
                        if (Org != "JHTNL" && Org != "JHTDK") {
                            alert(ModelMessageObejct["CaseProductExistsMessage"]);
                        }
                    }
                    //把資料送回 new service case
                    //確認有抓到節點及detail內容正確
                    var NewCaseServiceForm = $('#NewCaseServiceForm');
                    if (NewCaseServiceForm.children().length > 0 && detail["AccountProductId"] != null)
                    {
                        NewCaseServiceForm.find("#RegisteredProductName").val(detail["ProductName"]);
                        NewCaseServiceForm.find("#SerialNumber").val(detail["ProductSerialNo"]);
                        NewCaseServiceForm.find("#OtherData").val(JSON.stringify(detail));
                        CustomerProductClose();
                    }
                } else {
                    //荷蘭希望的處理方式是，跳出confirm dialog，詢問要繼續新增，或是開新頁面去查看已經建立的case product
                    if (detail["AccountProductId"] != null)
                    {
                        if (returnValue["Exists"]) {
                            var CustomerProductViewForm = $("#CustomerProductViewForm");
                            var NewCaseServiceForm = $('#NewCaseServiceForm');

                            if (CustomerProductViewForm.length > 0) {
                                var dialogAccountProduct = CustomerProductViewForm.find("#duplicateModal");
                                dialogAccountProduct.show();
                                dialogAccountProduct.find("#AccountProductDetail").val(JSON.stringify(detail));
                                dialogAccountProduct.find("#AccountProductSN").val(detail["ProductSerialNo"]);
                                dialogAccountProduct.find("#AccountProductID").val(detail["AccountProductId"]);
                                dialogAccountProduct.find("#AccountProductName").val(detail["ProductName"]);
                            } else {
                                var dialogNewServiceCase = NewCaseServiceForm.find("#duplicateModalNewCase");
                                if (duplicateAlert == "True") {
                                    dialogNewServiceCase.show();
                                } else {
                                    duplicateAlert = "True";
                                }
                                NewCaseServiceForm.find("#RegisteredProductName").val(detail["ProductName"]);
                                NewCaseServiceForm.find("#SerialNumber").val(detail["ProductSerialNo"]);
                                NewCaseServiceForm.find("#AccountProductID").val(detail["AccountProductId"]);
                                NewCaseServiceForm.find("#OtherData").val(JSON.stringify(detail));
                            }
                        } else {
                            var NewCaseServiceForm = $('#NewCaseServiceForm');
                            NewCaseServiceForm.find("#RegisteredProductName").val(detail["ProductName"]);
                            NewCaseServiceForm.find("#SerialNumber").val(detail["ProductSerialNo"]);
                            NewCaseServiceForm.find("#OtherData").val(JSON.stringify(detail));
                            CustomerProductClose();
                        }
                        /*
                        if (confirm(ModelMessageObejct["CaseProductDuplicated"])) {
                            openCasesRequest = openCasesRequest + "?accountProductId=" + detail["AccountProductId"];
                            window.open(openCasesRequest, "_system");
                        } else {
                            //把資料送回 new service case
                            //確認有抓到節點及detail內容正確
                            var NewCaseServiceForm = $('#NewCaseServiceForm');
                            if (NewCaseServiceForm.children().length > 0) {
                                NewCaseServiceForm.find("#RegisteredProductName").val(detail["ProductName"]);
                                NewCaseServiceForm.find("#SerialNumber").val(detail["ProductSerialNo"]);
                                NewCaseServiceForm.find("#OtherData").val(JSON.stringify(detail));
                                CustomerProductClose();
                            }
                        }
                        */
                    }
                }
            }
        })
        .fail(function (xhr, textStatus) {
            alert(ModelMessageObejct["ErrorOccrus"]);
        });
    }
    CustomerProduct.RefreshCustomerProduct = RefreshCustomerProduct;

    function GoToOpenRepairReqeust() {
        var accountProductId = $("#head_Pan_select").find("#AccountProductID").val();
        $(window).attr('location', openCasesRequest + "?accountProductId=" + accountProductId);
    }
    CustomerProduct.GoToOpenRepairReqeust = GoToOpenRepairReqeust;

    function GoToNewServiceCase() {
        //把資料送回 new service case
        //確認有抓到節點及detail內容正確
        var NewCaseServiceForm = $('#NewCaseServiceForm');
        if (NewCaseServiceForm.children().length > 0) {
            NewCaseServiceForm.find("#RegisteredProductName").val($("#head_Pan_select").find("#AccountProductName").val());
            NewCaseServiceForm.find("#SerialNumber").val($("#head_Pan_select").find("#AccountProductSN").val());
            NewCaseServiceForm.find("#OtherData").val($("#head_Pan_select").find("#AccountProductDetail").val());
            CustomerProductClose();
        }
    }
    CustomerProduct.GoToNewServiceCase = GoToNewServiceCase;

})();