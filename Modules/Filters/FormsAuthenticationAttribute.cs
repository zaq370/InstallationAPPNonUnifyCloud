using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
namespace InstallationAPPNonUnify.Modules.Filters
{
    public class FormsAuthenticationAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// 允許角色為 TempRegularUser 權限的使用者
        /// </summary>
        public bool AllowTempUser { get; set; }

        /// <summary>
        /// 要強制轉向的Controller，同時也必須要給予Action
        /// </summary>
        public string Controller { get; set; }

        /// <summary>
        /// 要強制轉向的Action，同時也必須要給予Controller
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// 如果這個Attribute掛在Controller上，但某些Action想自定義檢查或丟回訊息，就設為True
        /// </summary>
        public bool Exception { get; set; }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!Exception) {
                var cookie = filterContext.HttpContext.Request.Cookies;
                FormsAuthenticationTicketReader fatr = new FormsAuthenticationTicketReader(cookie[FormsAuthentication.FormsCookieName]);
                if (!fatr.isValidTicket)
                {
                    var redirectToLogin = true;

                    //目前會拿到TempRegularUser角色的人是從 ResetPassword 收到信的人
                    if (fatr.Role.ToLower().Equals("tempregularuser"))
                    {
                        redirectToLogin = false;
                        if (!AllowTempUser)
                        {
                            //filterContext.Result = new RedirectResult("/Case/ChangePassword");
                            filterContext.Result = new RedirectToRouteResult(
                                new RouteValueDictionary 
                                { 
                                    { "controller", "Order" }, 
                                    { "action", "ChangePassword" } 
                                });
                        }
                    }

                    if (redirectToLogin)
                    {
                        if (string.IsNullOrEmpty(Controller) || string.IsNullOrWhiteSpace(Controller)
                            || string.IsNullOrEmpty(Action) || string.IsNullOrWhiteSpace(Action))
                        {
                            //filterContext.Result = new RedirectResult("/Main/Logout");
                            filterContext.Result = new RedirectToRouteResult(
                                new RouteValueDictionary 
                                { 
                                    { "controller", "Main" }, 
                                    { "action", "Logout" } 
                                });
                        }
                        else {
                            filterContext.Result = new RedirectToRouteResult(
                                new RouteValueDictionary 
                                { 
                                    { "controller", Controller }, 
                                    { "action", Action } 
                                });
                        }
                    }
                }
            }
            base.OnActionExecuting(filterContext);
        }
    }

    
}