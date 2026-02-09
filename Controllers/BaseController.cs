using InstallationAPPNonUnify.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace InstallationAPPNonUnify.Controllers
{
    public class BaseController : Controller
    {
        public string CountryCode { get; set; }
        //public string RedirectCountryCode { get; private set; }
        public string[] UserLanguage { get; set; }
        protected const int DefaultPageSize = 10;

        //Controller 初始化時抓語系
        protected override void Initialize(System.Web.Routing.RequestContext requestContext)
        {
            if (requestContext != null) {
                base.Initialize(requestContext);
                if (Request != null)
                {
                    CountryCode = Request.Cookies.AllKeys.FirstOrDefault(p => p.Contains("CountryCode"));
                    //Cookie取不到取browser設定
                    if (string.IsNullOrEmpty(CountryCode))
                    {
                        if (Request != null && Request.UserLanguages[0] != null && Request.UserLanguages.Length > 0)
                            CountryCode = Request.UserLanguages[0];
                        UserLanguage = Request.UserLanguages;
                    }
                }
            }
        }
        //
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);
            //取得語言設定，如果cookie沒有，就取request裡面的
            if (Request != null)
            {
                CountryCode = Request.Cookies.AllKeys.FirstOrDefault(p => p.Contains("CountryCode"));
                //Cookie取不到取browser設定
                if (string.IsNullOrEmpty(CountryCode))
                {
                    CountryCode = Request.UserLanguages[0];
                    UserLanguage = Request.UserLanguages;
                }
            }
            //有沒有轉向的語系別
            //RedirectCountryCode = new BackendInfo().GetRedirectCountryCode(CountryCode);
        }
    }
}
