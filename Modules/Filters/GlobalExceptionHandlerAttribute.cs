using InstallationAPPNonUnify.Controllers;
using InstallationAPPNonUnify.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace InstallationAPPNonUnify.Modules.Filters
{
    public class GlobalExceptionHandlerAttribute : HandleErrorAttribute
    {
        public override void OnException(ExceptionContext filterContext)
        {
            //已處理過的exception
            if (filterContext.ExceptionHandled || filterContext.HttpContext.IsCustomErrorEnabled) {
                return;
            }

            //記錄到 Elmah
            Elmah.ErrorSignal.FromCurrentContext().Raise(filterContext.Exception);

            //取語言別
            var culture = string.Empty;
            if (filterContext.Controller.ControllerContext.Controller.GetType() == typeof(BaseController) ||
                    filterContext.Controller.ControllerContext.Controller.GetType().IsSubclassOf(typeof(BaseController)))
            {
                culture = ((BaseController)filterContext.Controller.ControllerContext.Controller).CountryCode;
            }
            LanguagePackage lp = new LanguagePackage(culture);

            var controllerName = filterContext.RouteData.Values["controller"].ToString();
            var viewData = new ViewDataDictionary<HandleErrorInfo>();
            if (filterContext.Controller.ViewData.Count > 0) {
                if (filterContext.Controller.ViewData.GetType() == typeof(HandleErrorInfo))
                    viewData = new ViewDataDictionary<HandleErrorInfo>(filterContext.Controller.ViewData);
            }
                
            switch (controllerName)
            {
                case "Order":
                    if (viewData.Count == 0) {
                        //給預設值
                        viewData["Layout"] = "_Layout_Case.cshtml";
                        viewData["Title"] = "Error Occurs";
                    }
                    var caseErrorView = new ViewResult
                    {
                        ViewName = "Error",
                        MasterName = this.Master,
                        ViewData = viewData,
                        TempData = filterContext.Controller.TempData
                    };
                    caseErrorView.ViewBag.Title = lp.getContentWithNoPrefix("ErrorTitle");
                    caseErrorView.ViewBag.PageTitle = lp.getContentWithNoPrefix("ErrorPageTitle");
                    caseErrorView.ViewBag.ErrorMessage = lp.getContentWithNoPrefix("ErrorMessage");
                    filterContext.Result = caseErrorView;
                    break;
                default:
                    var newViewResult = new ViewResult
                    { 
                        ViewName = "GlobalError", 
                        MasterName = this.Master, 
                        ViewData = viewData, 
                        TempData = filterContext.Controller.TempData
                    };
                    newViewResult.ViewBag.Title = lp.getContentWithNoPrefix("ErrorTitle");
                    newViewResult.ViewBag.PageTitle = lp.getContentWithNoPrefix("ErrorPageTitle");
                    newViewResult.ViewBag.ErrorMessage = lp.getContentWithNoPrefix("ErrorMessage");
                    filterContext.Result = newViewResult;
                    break;
            }
            filterContext.ExceptionHandled = true;
        }
    }
}