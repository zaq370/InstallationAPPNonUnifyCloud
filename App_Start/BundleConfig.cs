using System.Web;
using System.Web.Optimization;

namespace InstallationAPPNonUnify
{
    public class BundleConfig
    {
        // 如需 Bundling 的詳細資訊，請造訪 http://go.microsoft.com/fwlink/?LinkId=254725
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                        "~/Scripts/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryui").Include(
                        "~/Scripts/jquery-ui-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                        "~/Scripts/jquery.unobtrusive*",
                        "~/Scripts/jquery.validate*"));

            // 使用開發版本的 Modernizr 進行開發並學習。然後，當您
            // 準備好實際執行時，請使用 http://modernizr.com 上的建置工具，只選擇您需要的測試。
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                        "~/Scripts/modernizr-*"));

            bundles.Add(new StyleBundle("~/Content/css").Include(
                "~/Content/site.css"
            //"~/Content/custom/InstallationApp-common.css"
            ));

            bundles.Add(new StyleBundle("~/Content/themes/base/css").Include(
                        "~/Content/themes/base/jquery.ui.core.css",
                        "~/Content/themes/base/jquery.ui.resizable.css",
                        "~/Content/themes/base/jquery.ui.selectable.css",
                        "~/Content/themes/base/jquery.ui.accordion.css",
                        "~/Content/themes/base/jquery.ui.autocomplete.css",
                        "~/Content/themes/base/jquery.ui.button.css",
                        "~/Content/themes/base/jquery.ui.dialog.css",
                        "~/Content/themes/base/jquery.ui.slider.css",
                        "~/Content/themes/base/jquery.ui.tabs.css",
                        "~/Content/themes/base/jquery.ui.datepicker.css",
                        "~/Content/themes/base/jquery.ui.progressbar.css",
                        "~/Content/themes/base/jquery.ui.theme.css"));

            //login without authorize
            bundles.Add(new StyleBundle("~/Content/unauth").Include(
                "~/Content/bootstrap.css",
                "~/Content/custom/Default.css",
                "~/Content/custom/Login.css",
                "~/Content/custom/EmailUs.css",
                "~/Content/custom/GlobalError.css"
                ));

            bundles.Add(new ScriptBundle("~/bundles/unauth").Include(
                "~/Scripts/bootstrap.js",
                "~/Scripts/Custom/Common.js"
                ));

            //case controller
            bundles.Add(new StyleBundle("~/Content/case").Include(
                "~/Content/bootstrap.css",
                "~/Content/custom/Default.css",
                "~/Content/custom/CaseStyle.css",   //style.css
                "~/Content/custom/CustomerDetail.css",
                "~/Content/custom/EmailUsAuth.css",
                "~/Content/custom/ChangePassword.css",
                "~/Content/custom/Error.css",
                "~/Content/custom/OrderMain.css",
                "~/Content/custom/CustomerProduct.css",
                "~/Content/custom/OpenRepairRequests.css",
                "~/Content/custom/OrderInformation.css",
                "~/Content/custom/InstallationApp-common.css",
                "~/Content/custom/OrderMain.css",
                "~/Content/custom/AlreadyInstalled.css",
                "~/Content/custom/InstallMain.css",
                "~/Content/custom/ProductDetailPartial.css",
                "~/Content/custom/ProductDetailPartial2.css",
                "~/Content/custom/InstallationCompletion.css"
                ));

            bundles.Add(new ScriptBundle("~/bundles/case").Include(
                "~/Scripts/bootstrap.js",
                "~/Scripts/Custom/Common.js",
                "~/Scripts/Custom/OrderMain.js",
                "~/Scripts/Custom/CustomerProduct.js",
                "~/Scripts/Custom/OrderInformation.js"
                ));

            //Area - CMS
            bundles.Add(new StyleBundle("~/Content/cms").Include(
                "~/Content/bootstrap.css",
                //"~/Content/custom/Default.css",
                "~/Content/custom/cms/Common.css",
                "~/Content/custom/cms/Login.css",
                "~/Content/custom/cms/AdvancedSetting.css"
                ));

            bundles.Add(new ScriptBundle("~/bundles/cms").Include(
                "~/Scripts/bootstrap.js",
                "~/Scripts/Custom/Common.js",
                "~/Scripts/Custom/CMSCommon.js"
                ));
        }
    }
}