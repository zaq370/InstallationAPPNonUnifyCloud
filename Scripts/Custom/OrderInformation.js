var OrderInformation = OrderInformation || {};

(function () {
    function RefershResult(orderBy, keepSameSort, page) {
        var form = $("#OrderInformationForm");
        if (!form.length) return;
        if (!orderBy) orderBy = '';
        if (!keepSameSort) keepSameSort = false;
        if (!page) page = 1;

        var parameter = {
            OriginalOrderBy: form.find("#OrderBy").val(),
            OriginalAsc: form.find("#Asc").val(),
            SalesOrderNumber: form.find("#SalesOrderNumber").val(),
            CustomerName: form.find("#CustomerName").val(),
            OrderBy: orderBy,
            Search: true,
            KeepSameSort: keepSameSort,
            Page: page,
            _ts: new Date().getTime() // 防快取時間戳
        };

        $.post(searchUrl, parameter)
            .done(function (data, textStatus, xhr) {
                // 嘗試當成 JSON（後端驗證失敗時會回這個）
                var handledAsJson = false;
                try {
                    var payload = (typeof data === "string") ? JSON.parse(data) : data;
                    if (payload && typeof payload === "object" && payload.hasOwnProperty("success")) {
                        handledAsJson = true;
                        if (payload.success === false) {
                            Swal.fire({
                                title: 'Query Failed',
                                text: payload.message || 'Validation failed.',
                                icon: 'error',
                                showConfirmButton: true
                            });
                            return; // 不再處理 HTML
                        }
                    }
                } catch (e) {
                    // 不是 JSON，就當作 HTML 繼續處理
                }

                // 當作 HTML 片段更新
                if (!handledAsJson) {
                    var tempPanel = document.createElement("div");
                    $(tempPanel).html(data);

                    if ($(tempPanel).find("#OrderInformation").length > 0) {
                        form.find("#OrderInformation").children().remove();
                        form.find("#OrderInformation")
                            .append($(tempPanel).find("#OrderInformation").children().clone(true, true));

                        form.find("#OrderBy").val($(tempPanel).find("#OrderBy").val());
                        form.find("#Asc").val($(tempPanel).find("#Asc").val());
                        form.find("#SalesOrderNumber").val($(tempPanel).find("#SalesOrderNumber").val());
                        form.find("#CustomerName").val($(tempPanel).find("#CustomerName").val());
                    } else if ($(tempPanel).find("#LoginView").length > 0) {
                        $(window).attr('location', logoutUrl);
                    }
                }
            })
            .fail(function (xhr, textStatus) {
                Swal.fire({
                    title: 'Network Error',
                    text: textStatus || 'Request failed.',
                    icon: 'error',
                    showConfirmButton: true
                });
            });
    }

    OrderInformation.RefershResult = RefershResult;
})();