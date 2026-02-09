using Newtonsoft.Json.Linq;
using InstallationAPPNonUnify.Controllers;
using InstallationAPPNonUnify.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace InstallationAPPNonUnify.Modules.Filters
{
    public class OutOfServiceAttribute : ActionFilterAttribute
    {
        private DateTime OutOfServiceStartTime { get; set; }
        private DateTime OutOfServiceEndTime { get; set; }
        /// <summary>
        /// 排除的角色
        /// </summary>
        public string[] AllowRole { get; set; }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(filterContext.HttpContext.Request.Cookies[FormsAuthentication.FormsCookieName]);
            if (!fatr.isValidTicket || !AllowRole.Any(item => item.ToLower().Equals(fatr.Role.ToLower())))
             {
                if (IsOutOfService())
                {
                    //取語言別
                    var culture = string.Empty;
                    if (filterContext.Controller.ControllerContext.Controller.GetType() == typeof(BaseController) ||
                            filterContext.Controller.ControllerContext.Controller.GetType().IsSubclassOf(typeof(BaseController)))
                    {
                        culture = ((BaseController)filterContext.Controller.ControllerContext.Controller).CountryCode;
                    }
                    LanguagePackage lp = new LanguagePackage(culture);
                    var newViewResult = new ViewResult
                    {
                        ViewName = "OutOfService",
                        //MasterName = //this.Master,
                        ViewData = filterContext.Controller.ViewData,
                        TempData = filterContext.Controller.TempData
                    };
                    newViewResult.ViewBag.Title = lp.getContentWithNoPrefix("OutOfServiceTitle");
                    newViewResult.ViewBag.PageTitle = lp.getContentWithNoPrefix("OutOfServicePageTitle");
                    newViewResult.ViewBag.Message = lp.getContentWithNoPrefix("OutOfServiceMessage");
                    newViewResult.ViewBag.BackInServiceTime = OutOfServiceEndTime.ToString("dd/MM/yyyy HH:mm:ss");
                    newViewResult.ViewBag.Message2 = lp.getContentWithNoPrefix("OutOfServiceMessage2");

                    string tempString;
                    newViewResult.ViewBag.Email = string.Empty;
                    newViewResult.ViewBag.EmailAddress = string.Empty;
                    if (BackendFile.CompanyInfo.TryGetValue("Email", out tempString))
                    {
                        if (!string.IsNullOrEmpty(tempString) && !string.IsNullOrWhiteSpace(tempString)) {
                            newViewResult.ViewBag.Email = lp.getContentWithNoPrefix("EmailAddress") + ": ";
                            newViewResult.ViewBag.EmailAddress = tempString;
                        }
                    }

                    newViewResult.ViewBag.Telephone = string.Empty;
                    if (BackendFile.CompanyInfo.TryGetValue("Tel", out tempString))
                    {
                        if (!string.IsNullOrEmpty(tempString) && !string.IsNullOrWhiteSpace(tempString))
                            newViewResult.ViewBag.Telephone = lp.getContentWithNoPrefix("TelephoneNumber") + ": " + tempString;
                    }
                    filterContext.Result = newViewResult;
                }
            }
            base.OnActionExecuting(filterContext);
        }

        private bool IsOutOfService()
        {
            bool isOutOfOrder = false;
            foreach (var data in BackendFile.NoInServiceSegment) {
                if (DateTime.Now >= data.StartDate && DateTime.Now < data.EndDate) {
                    isOutOfOrder = true;
                    OutOfServiceStartTime = data.StartDate;
                    OutOfServiceEndTime = data.EndDate;
                }
            }
            return isOutOfOrder;
        }
    }
}