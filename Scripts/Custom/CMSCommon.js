var CMSCommon = CMSCommon || {};
var IsSuperUser = IsSuperUser & true;

(function () {
    $(document).ready(function () {
        if (!IsSuperUser) {
            $("#AdvancedSettingNavbar").css("display", "none");
        }
        AdjustNavbarHeight();
        AdjustFixedTableHeader();
    });

    window.addEventListener("resize", function () {
        AdjustNavbarHeight();
        AdjustFixedTableHeader();
    });

    function AdjustNavbarHeight() {
        var navbarHeight = $(".navbar-header").first().outerHeight();
        if (navbarHeight == 0)
            navbarHeight = $(".navbar-nav").first().outerHeight();
        $("#render-body").css("top", navbarHeight);
        $(".settingDescritpion").css("top", navbarHeight);
    }

    function AdjustFixedTableHeader() {
        var navbarHeight = $(".navbar-header").first().outerHeight();
        if (navbarHeight == 0)
            navbarHeight = $(".navbar-nav").first().outerHeight();

        var descriptionHeight = $(".settingDescritpion").outerHeight();

        $(".fixed-table-header").css({
            "top": navbarHeight + descriptionHeight
        });

        var fixedTop = $(".fixed-top");
        if (fixedTop.length > 0) {
            fixedTop.each(function () {
                var divHeight = $(this).outerHeight();
                fixedTop.css({
                    "top": navbarHeight + divHeight
                });
            });
        }
    }

})();