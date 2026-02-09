using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace InstallationAPPNonUnify.Modules.Filters
{
    public class AntiForgeryErrorHandlerAttribute : HandleErrorAttribute
    {
        //用來指定 redirect 的目標 controller
        public string Controller { get; set; }
        //用來儲存想要顯示的訊息
        public string ErrorMessage { get; set; }

        public string Area { get; set; }

        //覆寫預設發生 exception 時的動作
        public override void OnException(ExceptionContext filterContext)
        {
            //如果發生的 exception 是 HttpAntiForgeryException 就導至設定的 controller、action (action 在 base HandleErrorAttribute已宣告)
            if (filterContext.Exception is HttpAntiForgeryException)
            {
                //這個屬性要設定為 true 才能接手處理 exception 也才可以 redirect
                filterContext.ExceptionHandled = true;
                //將 errormsg 使用 TempData 暫存 (ViewData 與 ViewBag- 因為生命週期的關係都無法正確傳遞)
                //filterContext.Controller.TempData.Add("Timeout", ErrorMessage);
                //指定 redirect 的 controller 及 action
                filterContext.Result = new RedirectToRouteResult(
                new RouteValueDictionary
                {
                    { "action", View },
                    { "controller", Controller},
                    { "area", string.IsNullOrWhiteSpace(Area) ? "" : Area}
                });
                
            }
            else
                base.OnException(filterContext);// exception 不是 HttpAntiForgeryException 就照 mvc 預設流程
        }
    }
}